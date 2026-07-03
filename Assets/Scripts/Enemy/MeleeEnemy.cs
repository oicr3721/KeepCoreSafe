using System;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public sealed class MeleeEnemy : Enemy
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

            SkipCurrentPathCell();
            if (pathIndex < pathCells.Count)
            {
                TryBeginCellMovement(pathCells[pathIndex]);
                return;
            }

            if (currentTarget == null)
            {
                StopMoving();
                if (isFallbackPlan && repathCooldownRemaining <= 0f)
                    RebuildPlan();
                return;
            }

            if (!IsAdjacentToTarget())
            {
                RebuildPlan();
                return;
            }

            StopMoving();
            attackCooldownRemaining -= deltaTime;
            if (attackCooldownRemaining <= 0f)
            {
                currentTarget.TakeDamage(Data.AttackDamage);
                attackCooldownRemaining = Data.AttackCooldown;
            }
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

        private void SkipCurrentPathCell()
        {
            if (!TryGetCurrentCell(out Vector2Int current))
                return;

            while (pathIndex < pathCells.Count && pathCells[pathIndex] == current)
                pathIndex++;
        }

        private bool IsAdjacentToTarget()
        {
            Vector2Int current = TryGetCurrentCell(out Vector2Int currentCell)
                ? currentCell
                : GridManager.WorldToGrid(transform.position);
            Vector2Int offset = current - currentTarget.GridPosition;
            return Mathf.Abs(offset.x) + Mathf.Abs(offset.y) <= 1;
        }
    }
}
