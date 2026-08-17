using System.Collections.Generic;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Core;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Presentation;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class HealerBlock : CombatBlock
    {
        [Header("Heal Projectile Pool")]
        [Tooltip("Prefab containing HealProjectile and its projectile/impact renderers.")]
        [SerializeField] private HealProjectile healProjectilePrefab;
        [SerializeField, Min(0)] private int initialPoolSize = 4;
        [SerializeField] private Transform projectileRoot;
        [SerializeField, Min(0.01f)] private float projectileDuration = 0.22f;
        [SerializeField, Min(0f)] private float projectileCurveHeight = 0.22f;

        private float cooldownRemaining;
        private ComponentPool<HealProjectile> projectilePool;
        private readonly HashSet<Block> observedTargets = new();
        private Block healTarget;
        private bool targetsDirty = true;
        private bool isGridSubscribed;
        private static readonly Vector2Int[] HealTargetDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private HealerBlockData HealerData => Data as HealerBlockData;

        protected override void Awake()
        {
            base.Awake();
            if (healProjectilePrefab != null)
            {
                projectilePool = new ComponentPool<HealProjectile>(
                    healProjectilePrefab,
                    initialPoolSize,
                    projectileRoot != null ? projectileRoot : transform);
            }
        }

        private void OnEnable()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
            TrySubscribeGridChanged();
            targetsDirty = true;
        }

        private void OnDisable()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
            UnsubscribeGridChanged();
            ClearObservedTargets();
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            if (HealerData == null)
                return;

            TrySubscribeGridChanged();
            if (targetsDirty)
                RefreshObservedTargets();

            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f || GridManager.Instance == null || !HasGridPosition)
            {
                return;
            }

            Block target = healTarget;
            if (target == null)
                return;

            if (projectilePool == null)
                ApplyHeal(target);
            else
            {
                HealProjectile projectile = projectilePool.Rent();
                projectile?.Launch(
                    transform.position,
                    target,
                    projectileDuration,
                    projectileCurveHeight,
                    ApplyHeal,
                    projectilePool.Return);
            }

            cooldownRemaining = GetAdjustedCooldown(HealerData.ActionCooldown);
        }

        private void RefreshObservedTargets()
        {
            ClearObservedTargets();
            targetsDirty = false;
            if (GridManager.Instance == null || !HasGridPosition)
                return;

            foreach (Vector2Int direction in HealTargetDirections)
            {
                if (!GridManager.Instance.TryGetBlock(GridPosition + direction, out Block candidate)
                    || candidate == null
                    || candidate == this
                    || !observedTargets.Add(candidate))
                {
                    continue;
                }

                candidate.HP.OnValueChanged += HandleObservedHealthChanged;
            }

            RefreshHealTarget();
        }

        private void RefreshHealTarget()
        {
            Block bestTarget = null;
            float lowestHP = float.MaxValue;
            foreach (Block candidate in observedTargets)
            {
                if (candidate == null
                    || candidate.HP.CurrentValue <= 0f
                    || candidate.HP.CurrentValue >= candidate.HP.MaxValue)
                {
                    continue;
                }

                if (candidate.HP.CurrentValue < lowestHP)
                {
                    lowestHP = candidate.HP.CurrentValue;
                    bestTarget = candidate;
                }
            }

            healTarget = bestTarget;
        }

        private void ApplyHeal(Block target)
        {
            if (target == null
                || GameManager.Phase != GamePhase.Combat
                || target.HP.CurrentValue <= 0f
                || target.HP.CurrentValue >= target.HP.MaxValue)
            {
                return;
            }

            float previousHP = target.HP.CurrentValue;
            target.Heal(HealerData.HealValue);
            if (target.HP.CurrentValue > previousHP)
            {
                HealParticleEffectManager.Instance?.PlayAt(target.transform.position);
                AudioManager.PlayAt(HealerData.HealSound, target.transform.position);
            }
        }

        private void HandleGridChanged()
        {
            targetsDirty = true;
            RefreshObservedTargets();
        }

        private void HandleObservedHealthChanged(float _, float __)
        {
            RefreshHealTarget();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Combat)
                RefreshObservedTargets();
        }

        private void TrySubscribeGridChanged()
        {
            if (isGridSubscribed || GridManager.Instance == null)
                return;

            GridManager.Instance.GridChanged += HandleGridChanged;
            isGridSubscribed = true;
        }

        private void UnsubscribeGridChanged()
        {
            if (!isGridSubscribed)
                return;

            if (GridManager.Instance != null)
                GridManager.Instance.GridChanged -= HandleGridChanged;
            isGridSubscribed = false;
        }

        private void ClearObservedTargets()
        {
            foreach (Block target in observedTargets)
            {
                if (target != null)
                    target.HP.OnValueChanged -= HandleObservedHealthChanged;
            }

            observedTargets.Clear();
            healTarget = null;
        }

        protected override void OnDestroy()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
            UnsubscribeGridChanged();
            ClearObservedTargets();
            base.OnDestroy();
        }
    }
}
