using System;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Combat;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public sealed class RangedEnemy : Enemy
    {
        private GridPathfinder pathfinder;
        private IReadOnlyList<Vector2Int> pathCells = Array.Empty<Vector2Int>();
        private Block currentTarget;
        private int pathIndex;
        private float attackCooldownRemaining;
        private float repathCooldownRemaining;
        private bool hasPlan;
        private bool isFallbackPlan;
        private bool planHadBlocker;

        protected override void Start()
        {
            base.Start();
            pathfinder = new GridPathfinder(GridManager, Data, GetInstanceID());
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            base.OnCombatUpdate(deltaTime);

            repathCooldownRemaining -= deltaTime;
            if (ContinueCellMovement(deltaTime))
                return;

            if (!hasPlan || planHadBlocker && currentTarget == null)
                RebuildPlan();

            if (!hasPlan)
            {
                StopMoving();
                return;
            }

            if (!TryGetCurrentCell(out Vector2Int currentCell))
            {
                if (pathCells.Count > 0)
                    TryBeginCellMovement(pathCells[0]);
                return;
            }

            if (currentTarget == null)
            {
                SkipCurrentPathCell(currentCell);
                if (pathIndex < pathCells.Count)
                    TryBeginCellMovement(pathCells[pathIndex]);
                else
                {
                    StopMoving();
                    if (isFallbackPlan && repathCooldownRemaining <= 0f)
                        RebuildPlan();
                }

                return;
            }

            int cellDistance = GetCellDistance(currentCell, currentTarget.GridPosition);
            float minimumRange = Mathf.Max(1f, Data.AttackRange - Data.AttackRangeTolerance);
            float maximumRange = Data.AttackRange + Data.AttackRangeTolerance;

            if (cellDistance < minimumRange)
            {
                TryRetreat(currentCell);
                return;
            }

            if (cellDistance <= maximumRange)
            {
                StopMoving();
                Attack(deltaTime);
                return;
            }

            SkipCurrentPathCell(currentCell);
            if (pathIndex < pathCells.Count)
            {
                TryBeginCellMovement(pathCells[pathIndex]);
                return;
            }

            RebuildPlan();
        }

        protected override void RebuildPlan()
        {
            Block core = GridManager?.Grid?.Core;
            bool foundPath = TryGetCurrentCell(out Vector2Int start)
                ? pathfinder.TryBuildPath(start, core, out GridPathfinder.PathResult path)
                : pathfinder.TryBuildPath(transform.position, core, out path);

            if (!foundPath)
            {
                hasPlan = false;
                currentTarget = null;
                pathCells = Array.Empty<Vector2Int>();
                return;
            }

            isFallbackPlan = !path.ReachesCore;
            planHadBlocker = path.BlockingBlock != null;
            currentTarget = path.BlockingBlock != null
                ? path.BlockingBlock
                : path.ReachesCore ? core : null;
            pathCells = path.Cells;
            pathIndex = 0;
            hasPlan = true;
            repathCooldownRemaining = 0.4f;
        }

        private void Attack(float deltaTime)
        {
            attackCooldownRemaining -= deltaTime;
            if (attackCooldownRemaining > 0f)
                return;

            GameObject missileObject = new GameObject("Ranged Missile");
            missileObject.transform.position = transform.position;
            MissileProjectile missile = missileObject.AddComponent<MissileProjectile>();
            missile.Launch(
                currentTarget,
                Data.AttackDamage,
                Data.ProjectileSpeed,
                Data.ProjectileArcHeight);
            attackCooldownRemaining = Data.AttackCooldown;
        }

        private void TryRetreat(Vector2Int currentCell)
        {
            Vector2Int bestCell = currentCell;
            int bestDistance = GetCellDistance(currentCell, currentTarget.GridPosition);

            foreach (Vector2Int direction in GridPathfinder.Directions)
            {
                Vector2Int candidate = currentCell + direction;
                if (!GridManager.Grid.IsWithinBounds(candidate)
                    || !GridManager.IsCellEmpty(candidate))
                {
                    continue;
                }

                int candidateDistance = GetCellDistance(candidate, currentTarget.GridPosition);
                if (candidateDistance > bestDistance)
                {
                    bestCell = candidate;
                    bestDistance = candidateDistance;
                }
            }

            if (bestCell != currentCell)
                TryBeginCellMovement(bestCell);
            else
                StopMoving();
        }

        private void SkipCurrentPathCell(Vector2Int currentCell)
        {
            while (pathIndex < pathCells.Count && pathCells[pathIndex] == currentCell)
                pathIndex++;
        }

        private static int GetCellDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
