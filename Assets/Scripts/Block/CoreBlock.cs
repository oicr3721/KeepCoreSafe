using KeepCoreSafe.Managers;

namespace KeepCoreSafe.Blocks
{
    public sealed class CoreBlock : Block
    {
        private bool destructionStarted;

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
