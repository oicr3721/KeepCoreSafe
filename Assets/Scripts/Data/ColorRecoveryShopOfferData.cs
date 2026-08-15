using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "ColorRecoveryOffer", menuName = "Keep Core Safe/Shop/Color Recovery Offer")]
    public sealed class ColorRecoveryShopOfferData : ShopOfferData
    {
        [SerializeField] private BlockColorData targetColor;

        public BlockColorData TargetColor => targetColor;

        protected override bool CanApply(BlockSupplyController supplyController)
        {
            if (targetColor == null || GridManager.Instance == null)
                return false;

            foreach (Block block in GridManager.Instance.GetBlocks())
            {
                if (IsDamagedTarget(block))
                    return true;
            }

            return false;
        }

        protected override void Apply(BlockSupplyController supplyController)
        {
            if (targetColor == null || GridManager.Instance == null)
                return;

            foreach (Block block in GridManager.Instance.GetBlocks())
            {
                if (!IsDamagedTarget(block))
                    continue;

                float previousHP = block.HP.CurrentValue;
                block.Heal(Mathf.CeilToInt(block.HP.MaxValue - previousHP));
                if (block.HP.CurrentValue > previousHP)
                    HealParticleEffectManager.Instance?.PlayAt(block.transform.position);
            }
        }

        private bool IsDamagedTarget(Block block)
        {
            return block != null
                   && block.Data != null
                   && block.Data.Color == targetColor
                   && block.HP.CurrentValue > 0f
                   && block.HP.CurrentValue < block.HP.MaxValue;
        }
    }
}
