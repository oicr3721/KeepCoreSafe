using System;
using System.Collections.Generic;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Presentation;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public sealed class SuicideEnemy : Enemy
    {
        private readonly struct ExplosionTarget
        {
            public ExplosionTarget(Block block, Vector3 position)
            {
                Block = block;
                Position = position;
            }

            public Block Block { get; }
            public Vector3 Position { get; }
        }

        [Header("Self Destruct Visual")]
        [SerializeField] private SpriteRenderer warningRenderer;
        [SerializeField] private Color warningColor = new(1f, 0.08f, 0.04f, 1f);

        private IReadOnlyList<Vector2Int> pathCells = Array.Empty<Vector2Int>();
        private Block currentTarget;
        private Block routeGoal;
        private int pathIndex;
        private bool hasPlan;
        private bool isPreparingSelfDestruct;
        private float preparationElapsed;
        private float pulsePhase;
        private Vector3 warningBaseScale = Vector3.one;
        private Color baseColor = Color.white;
        private bool usesScriptedDestination;
        private Vector2Int scriptedDestination;
        private Transform scriptedPresentationTarget;

        private SuicideEnemyData SuicideData => Data as SuicideEnemyData;

        protected override void Start()
        {
            base.Start();
            pathCells = InitialPathCells;
            hasPlan = pathCells.Count > 0;
            routeGoal = usesScriptedDestination
                ? null
                : InitialRouteTarget != null ? InitialRouteTarget : GridManager.Grid.Core;
            if (routeGoal is SupplyBlock)
                routeGoal.Died += HandleRouteGoalDied;
            if (warningRenderer != null)
            {
                warningBaseScale = warningRenderer.transform.localScale;
                baseColor = warningRenderer.color;
            }

            if (SuicideData == null)
            {
                Debug.LogError($"{name} requires SuicideEnemyData.", this);
                enabled = false;
            }
        }

        public void ConfigureScriptedDestination(
            Vector2Int destination,
            Transform presentationTarget)
        {
            usesScriptedDestination = true;
            scriptedDestination = destination;
            scriptedPresentationTarget = presentationTarget;
            SetSimulateOutsideCombat(true);
        }

        protected override void OnDamaged(int amount)
        {
            if (isPreparingSelfDestruct || SuicideData == null || MaxHP <= 0)
                return;

            if (CurrentHP / (float)MaxHP <= SuicideData.ForcedTriggerHealthRatio)
                BeginSelfDestruct();
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            if (isPreparingSelfDestruct)
            {
                UpdateSelfDestructPreparation(deltaTime);
                return;
            }

            if (ContinueCellMovement(deltaTime))
                return;

            if (!hasPlan)
            {
                StopMoving();
                return;
            }

            SkipCurrentPathCell();
            if (usesScriptedDestination && pathIndex >= pathCells.Count)
            {
                if (TryGetCurrentCell(out Vector2Int current)
                    && current == scriptedDestination)
                {
                    StopMoving(false);
                    FacePosition(scriptedPresentationTarget != null
                        ? scriptedPresentationTarget.position
                        : GridManager.GridToWorld(scriptedDestination));
                    BeginSelfDestruct();
                }
                else
                {
                    StopMoving();
                }

                return;
            }

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
            BeginSelfDestruct();
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

        private void BeginSelfDestruct()
        {
            if (isPreparingSelfDestruct || IsDead)
                return;

            isPreparingSelfDestruct = true;
            preparationElapsed = 0f;
            pulsePhase = 0f;
            StopMoving();
            SetTarget(null);
            ApplyWarningFrame(0f, 0f);
            AudioManager.PlayAt(SuicideData.WarningSound, transform.position);
        }

        private void UpdateSelfDestructPreparation(float deltaTime)
        {
            float duration = Mathf.Max(0.1f, SuicideData.SelfDestructPreparationDuration);
            preparationElapsed = Mathf.Min(duration, preparationElapsed + deltaTime);
            float progress = preparationElapsed / duration;
            float acceleratedProgress = progress * progress;
            float interval = Mathf.Lerp(
                SuicideData.InitialPulseInterval,
                SuicideData.FinalPulseInterval,
                acceleratedProgress);

            float previousPhase = pulsePhase;
            pulsePhase += deltaTime / Mathf.Max(0.03f, interval);
            if (Mathf.FloorToInt(pulsePhase) > Mathf.FloorToInt(previousPhase))
                AudioManager.PlayAt(SuicideData.WarningSound, transform.position);

            float beat = 0.5f - 0.5f * Mathf.Cos((pulsePhase % 1f) * Mathf.PI * 2f);
            ApplyWarningFrame(progress, beat);
            if (preparationElapsed >= duration)
                Explode();
        }

        private void ApplyWarningFrame(float progress, float beat)
        {
            if (warningRenderer != null)
                warningRenderer.color = Color.Lerp(baseColor, warningColor, beat);

            float pulseAmount = Mathf.Lerp(
                SuicideData.InitialScalePulse,
                SuicideData.FinalScalePulse,
                progress);
            float finalStart = 1f - SuicideData.FinalScaleUpPortion;
            float finalBoost = Mathf.InverseLerp(finalStart, 1f, progress);
            finalBoost *= finalBoost * SuicideData.FinalScaleBoost;
            if (warningRenderer != null)
            {
                warningRenderer.transform.localScale = warningBaseScale
                    * (1f + beat * pulseAmount + finalBoost);
            }
        }

        private void Explode()
        {
            if (IsDead)
                return;

            StopMoving();
            AudioManager.PlayAt(SuicideData.ExplosionSound, transform.position);
            ExplosionParticleEffectManager.Instance?.PlayAt(transform.position);
            Vector2Int center = TryGetCurrentCell(out Vector2Int currentCell)
                ? currentCell
                : GridManager.WorldToGrid(transform.position);
            List<ExplosionTarget> targets = CollectExplosionTargets(center);
            foreach (ExplosionTarget target in targets)
            {
                if (target.Block == null)
                    continue;

                target.Block.TakeDamage(SuicideData.ExplosionDamage);
                ExplosionParticleEffectManager.Instance?.PlayAt(target.Position);
            }

            Die(false);
        }

        private List<ExplosionTarget> CollectExplosionTargets(Vector2Int center)
        {
            List<ExplosionTarget> targets = new(8);
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector2Int cell = center + new Vector2Int(x, y);
                    if (GridManager.TryGetBlock(cell, out Block block) && block != null)
                        targets.Add(new ExplosionTarget(block, block.transform.position));
                }
            }

            return targets;
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
            if (currentTarget == target)
                SetTarget(null);
        }

        protected override void OnDestroy()
        {
            SetTarget(null);
            if (routeGoal is SupplyBlock)
                routeGoal.Died -= HandleRouteGoalDied;
            base.OnDestroy();
        }
    }
}
