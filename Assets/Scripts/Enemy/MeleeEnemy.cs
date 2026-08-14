using System;
using System.Collections.Generic;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public sealed class MeleeEnemy : Enemy
    {
        private IReadOnlyList<Vector2Int> pathCells = Array.Empty<Vector2Int>();
        private Block currentTarget;
        private int pathIndex;
        private float attackCooldownRemaining;
        private bool hasPlan;
        private Block routeGoal;

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
            if (ContinueCellMovement(deltaTime))
                return;

            if (!hasPlan)
            {
                StopMoving();
                return;
            }

            SkipCurrentPathCell();
            if (currentTarget == null)
                AcquireNextTargetOrMove();

            if (currentTarget == null)
                return;

            if (!IsAdjacentToTarget())
            {
                StopMoving();
                return;
            }

            StopMoving(false);
            FaceAttackTarget(currentTarget);
            attackCooldownRemaining -= deltaTime;
            if (attackCooldownRemaining > 0f)
                return;

            currentTarget.TakeDamage(Data.AttackDamage);
            AudioManager.PlayAt(Data.AttackSound, transform.position);
            attackCooldownRemaining = Data.AttackCooldown;
        }

        private void AcquireNextTargetOrMove()
        {
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
