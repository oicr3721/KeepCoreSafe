using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class HealerBlock : Block
    {
        private GridManager gridManager;
        private float cooldownRemaining;

        protected override void Start()
        {
            base.Start();
            gridManager = FindFirstObjectByType<GridManager>();
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f || gridManager == null || !HasGridPosition)
            {
                return;
            }

            foreach (Block adjacentBlock in gridManager.GetBlocksInEffectArea(
                         GridPosition,
                         Data.AffectedDirections,
                         Data.EffectRange))
            {
                adjacentBlock.Heal(Data.HealValue);
            }

            cooldownRemaining = GetAdjustedCooldown(Data.ActionCooldown);
        }
    }
}
