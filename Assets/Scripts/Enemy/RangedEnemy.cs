using System;
using System.Collections.Generic;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Data;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public sealed class RangedEnemy : Enemy
    {
        private IReadOnlyList<Vector2Int> pathCells = Array.Empty<Vector2Int>();
        private Block currentTarget;
        private int pathIndex;
        private float attackCooldownRemaining;
        private bool hasPlan;
        private Block routeGoal;

        private RangedEnemyData RangedData => Data as RangedEnemyData;

        protected override void Start()
        {
            base.Start();
            pathCells = InitialPathCells;
            hasPlan = pathCells.Count > 0;
            routeGoal = InitialRouteTarget != null ? InitialRouteTarget : GridManager.Grid.Core;
            if (routeGoal is SupplyBlock)
                routeGoal.Died += HandleRouteGoalDied;
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            if (RangedData == null || ContinueCellMovement(deltaTime))
                return;

            if (!hasPlan)
            {
                StopMoving();
                return;
            }

            if (!TryGetCurrentCell(out Vector2Int currentCell))
            {
                EnterGridFromSpawn();
                return;
            }

            SkipCurrentPathCell(currentCell);
            if (currentTarget == null)
                AcquireUpcomingTarget(currentCell);

            if (currentTarget != null)
            {
                int targetDistance = GetCellDistance(currentCell, currentTarget.GridPosition);
                float minimumRange = Mathf.Max(
                    1f,
                    RangedData.AttackRange - RangedData.AttackRangeTolerance);
                float maximumRange = RangedData.AttackRange + RangedData.AttackRangeTolerance;

                if (targetDistance < minimumRange && TryRetreatAlongPath(currentCell))
                    return;

                if (targetDistance <= maximumRange)
                {
                    StopMoving(false);
                    FaceAttackTarget(currentTarget);
                    Attack(deltaTime);
                    return;
                }
            }

            if (pathIndex < pathCells.Count)
            {
                Vector2Int nextCell = pathCells[pathIndex];
                if (GridManager.TryGetBlock(nextCell, out Block blocker))
                    SetTarget(blocker);
                else
                    TryBeginCellMovement(nextCell);
                return;
            }

            SetTarget(routeGoal != null ? routeGoal : GridManager.Grid.Core);
        }

        private void HandleRouteGoalDied(Block target)
        {
            target.Died -= HandleRouteGoalDied;
            if (currentTarget == target)
                SetTarget(null);

            routeGoal = GridManager.Grid.Core;
            RebuildRouteToCore();
        }

        private void RebuildRouteToCore()
        {
            GridPathfinder pathfinder = new(GridManager, Data, GetInstanceID());
            bool found = TryGetCurrentCell(out Vector2Int current)
                ? pathfinder.TryBuildPath(current, routeGoal, out GridPathfinder.PathResult path)
                : pathfinder.TryBuildPath(transform.position, routeGoal, out path);
            pathCells = found ? path.Cells : Array.Empty<Vector2Int>();
            pathIndex = 0;
            hasPlan = pathCells.Count > 0;
        }

        private void EnterGridFromSpawn()
        {
            if (pathCells.Count == 0)
            {
                StopMoving();
                return;
            }

            Vector2Int entryCell = pathCells[0];
            if (!GridManager.Grid.IsWithinBounds(entryCell))
            {
                hasPlan = false;
                StopMoving();
                Debug.LogError($"{name} has an out-of-bounds entry cell {entryCell}.", this);
                return;
            }

            if (GridManager.TryGetBlock(entryCell, out Block blocker))
            {
                SetTarget(blocker);
                StopMoving();
                return;
            }

            TryBeginCellMovement(entryCell);
        }

        private void AcquireUpcomingTarget(Vector2Int currentCell)
        {
            float maximumRange = RangedData.AttackRange + RangedData.AttackRangeTolerance;
            for (int i = pathIndex; i < pathCells.Count; i++)
            {
                Vector2Int routeCell = pathCells[i];
                if (GetCellDistance(currentCell, routeCell) > maximumRange)
                    break;

                if (GridManager.TryGetBlock(routeCell, out Block blocker))
                {
                    SetTarget(blocker);
                    return;
                }
            }

            Block goal = routeGoal != null ? routeGoal : GridManager.Grid.Core;
            if (goal != null && GetCellDistance(currentCell, goal.GridPosition) <= maximumRange)
                SetTarget(goal);
        }

        private void SetTarget(Block target)
        {
            if (currentTarget == target)
                return;

            if (currentTarget != null)
                currentTarget.Died -= HandleTargetDied;
            currentTarget = target;
            if (currentTarget != null)
                currentTarget.Died += HandleTargetDied;
        }

        private void HandleTargetDied(Block target)
        {
            if (currentTarget != target)
                return;

            currentTarget.Died -= HandleTargetDied;
            currentTarget = null;
        }

        private void Attack(float deltaTime)
        {
            attackCooldownRemaining -= deltaTime;
            if (attackCooldownRemaining > 0f)
                return;

            if (RangedData.ProjectilePrefab == null)
            {
                Debug.LogError($"{name} has no projectile prefab assigned.", this);
                attackCooldownRemaining = Data.AttackCooldown;
                return;
            }

            MissileProjectile missile = Instantiate(
                RangedData.ProjectilePrefab,
                transform.position,
                Quaternion.identity);
            missile.Launch(
                currentTarget,
                Data.AttackDamage,
                RangedData.ProjectileSpeed,
                RangedData.ProjectileArcHeight);
            AudioManager.PlayAt(Data.AttackSound, transform.position);
            attackCooldownRemaining = Data.AttackCooldown;
        }

        private bool TryRetreatAlongPath(Vector2Int currentCell)
        {
            int currentIndex = -1;
            for (int i = 0; i < pathCells.Count; i++)
            {
                if (pathCells[i] == currentCell)
                    currentIndex = i;
            }

            if (currentIndex <= 0)
            {
                StopMoving();
                return false;
            }

            Vector2Int retreatCell = pathCells[currentIndex - 1];
            if (!GridManager.IsCellEmpty(retreatCell))
            {
                StopMoving();
                return false;
            }

            return TryBeginCellMovement(retreatCell);
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

        protected override void OnDestroy()
        {
            if (currentTarget != null)
                currentTarget.Died -= HandleTargetDied;
            if (routeGoal is SupplyBlock)
                routeGoal.Died -= HandleRouteGoalDied;
            base.OnDestroy();
        }
    }
}
