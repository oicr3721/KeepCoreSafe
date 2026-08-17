using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class CoreBlock : Block
    {
        private bool destructionStarted;
        private bool destructionProtected;

        public void SetDestructionProtected(bool protectedState)
        {
            destructionProtected = protectedState;
        }

        public override void TakeDamage(int amount)
        {
            if (!destructionProtected)
            {
                base.TakeDamage(amount);
                return;
            }

            int safeDamage = HP.CurrentValue > 1f
                ? Mathf.Min(amount, Mathf.FloorToInt(HP.CurrentValue - 1f))
                : 0;
            base.TakeDamage(safeDamage);
        }

        protected override void UpdateVisualSprite(float healthRatio)
        {
            // Core identity and hierarchy are authored by its selected prefab.
            // HP refreshes must not replace that prefab's visual with BlockData sprites.
        }

        public override void Die()
        {
            if (destructionStarted)
                return;

            destructionStarted = true;
            if (GameManager.Instance != null
                && GameManager.Phase == GamePhase.Combat
                && GameManager.Instance.TryPlayCoreDestruction(this, CompleteDestruction))
            {
                return;
            }

            CompleteDestruction();
        }

        private void CompleteDestruction()
        {
            base.Die();
        }
    }
}
