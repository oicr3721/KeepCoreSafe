using System;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public sealed class MeleeEnemy : Enemy
    {
        private GridManager gridManager;
        private GridPathfinder pathfinder;
        private IReadOnlyList<Vector3> waypoints = Array.Empty<Vector3>();
        private Block currentTarget;
        private int waypointIndex;
        private float attackCooldownRemaining;

        protected override void Start()
        {
            base.Start();
            gridManager = FindFirstObjectByType<GridManager>();
            pathfinder = new GridPathfinder(gridManager, Data);
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            if (currentTarget == null)
            {
                RebuildPlan();
            }

            if (currentTarget == null)
            {
                StopMoving();
                return;
            }

            if (TryMoveToNextWaypoint())
            {
                return;
            }

            MoveOrAttackTarget(deltaTime);
        }

        private void RebuildPlan()
        {
            Block core = gridManager?.Grid?.Core;

            if (core == null || !pathfinder.TryBuildPath(transform.position, core, out GridPathfinder.PathResult path))
            {
                currentTarget = null;
                waypoints = Array.Empty<Vector3>();
                return;
            }

            currentTarget = path.BlockingBlock != null ? path.BlockingBlock : core;
            waypoints = path.Waypoints;
            waypointIndex = 0;
        }

        private bool TryMoveToNextWaypoint()
        {
            while (waypointIndex < waypoints.Count
                && Vector2.Distance(Body.position, waypoints[waypointIndex]) < 0.12f)
            {
                waypointIndex++;
            }

            if (waypointIndex >= waypoints.Count)
            {
                return false;
            }

            MoveTowards(waypoints[waypointIndex]);
            return true;
        }

        private void MoveOrAttackTarget(float deltaTime)
        {
            if (!IsInAttackRange())
            {
                MoveTowards(currentTarget.transform.position);
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

        private bool IsInAttackRange()
        {
            Collider2D targetCollider = currentTarget.GetComponent<Collider2D>();
            if (targetCollider == null)
            {
                return Vector2.Distance(Body.position, currentTarget.transform.position) <= 0.8f;
            }

            return CollisionCollider.Distance(targetCollider).distance <= Data.AttackRange;
        }

        private void MoveTowards(Vector3 destination)
        {
            Vector2 direction = ((Vector2)destination - Body.position).normalized;
            Body.linearVelocity = direction * Data.MoveSpeed;
        }

        private void StopMoving()
        {
            Body.linearVelocity = Vector2.zero;
        }
    }
}
