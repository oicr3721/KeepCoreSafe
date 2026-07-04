using KeepCoreSafe.Managers;
using KeepCoreSafe.Data;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class HealerBlock : Block
    {
        private float cooldownRemaining;

        private HealerBlockData HealerData => Data as HealerBlockData;

        protected override void OnCombatUpdate(float deltaTime)
        {
            if (HealerData == null)
                return;

            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f || GridManager.Instance == null || !HasGridPosition)
            {
                return;
            }

            foreach (Block adjacentBlock in GridManager.Instance.GetBlocksInEffectArea(
                         GridPosition,
                         HealerData.AffectedDirections,
                         HealerData.EffectRange))
            {
                adjacentBlock.Heal(HealerData.HealValue);
            }

            cooldownRemaining = GetAdjustedCooldown(HealerData.ActionCooldown);
        }
    }
}
