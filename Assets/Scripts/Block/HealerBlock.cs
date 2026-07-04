using KeepCoreSafe.Audio;
using KeepCoreSafe.Core;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Presentation;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class HealerBlock : Block
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

        protected override void OnCombatUpdate(float deltaTime)
        {
            if (HealerData == null)
                return;

            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f || GridManager.Instance == null || !HasGridPosition)
            {
                return;
            }

            bool launched = false;
            foreach (Block adjacentBlock in GridManager.Instance.GetBlocksInEffectArea(
                         GridPosition,
                         HealerData.AffectedDirections,
                         HealerData.EffectRange))
            {
                if (adjacentBlock == null
                    || adjacentBlock == this
                    || adjacentBlock.HP.CurrentValue <= 0f
                    || adjacentBlock.HP.CurrentValue >= adjacentBlock.HP.MaxValue)
                {
                    continue;
                }

                launched = true;
                if (projectilePool == null)
                {
                    ApplyHeal(adjacentBlock);
                    continue;
                }

                HealProjectile projectile = projectilePool.Rent();
                projectile?.Launch(
                    transform.position,
                    adjacentBlock,
                    projectileDuration,
                    projectileCurveHeight,
                    ApplyHeal,
                    projectilePool.Return);
            }

            if (launched)
                cooldownRemaining = GetAdjustedCooldown(HealerData.ActionCooldown);
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
                AudioManager.PlayAt(HealerData.HealSound, target.transform.position);
        }
    }
}
