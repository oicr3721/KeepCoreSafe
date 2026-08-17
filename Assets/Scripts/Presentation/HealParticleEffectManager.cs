using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class HealParticleEffectManager : PooledParticleEffectManager
    {
        public static HealParticleEffectManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Multiple HealParticleEffectManager instances are active.", this);
                enabled = false;
                return;
            }

            Instance = this;
            InitializePool();
        }

        public bool PlayAt(Vector3 worldPosition)
        {
            return PlayEffectAt(worldPosition);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
