using System.Collections.Generic;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class BlockDestroyEffectManager : MonoBehaviour
    {
        public static BlockDestroyEffectManager Instance { get; private set; }

        [Tooltip("Prefab used only when every pre-created effect is already playing.")]
        [SerializeField] private BlockDestroyEffect effectPrefab;
        [SerializeField] private List<BlockDestroyEffect> effectPool = new();

        private int nextEffectIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Multiple BlockDestroyEffectManager instances are active.", this);
                enabled = false;
                return;
            }

            Instance = this;
            effectPool ??= new List<BlockDestroyEffect>();
            for (int i = effectPool.Count - 1; i >= 0; i--)
            {
                BlockDestroyEffect effect = effectPool[i];
                if (effect == null)
                {
                    effectPool.RemoveAt(i);
                    continue;
                }

                effect.gameObject.SetActive(false);
            }
        }

        public bool PlayAt(Vector3 worldPosition, Color blockColor)
        {
            effectPool ??= new List<BlockDestroyEffect>();
            for (int offset = 0; offset < effectPool.Count; offset++)
            {
                int index = (nextEffectIndex + offset) % effectPool.Count;
                BlockDestroyEffect effect = effectPool[index];
                if (effect == null || effect.gameObject.activeSelf)
                    continue;

                nextEffectIndex = (index + 1) % effectPool.Count;
                return effect.Play(worldPosition, blockColor);
            }

            if (effectPrefab == null)
            {
                Debug.LogError("Block Destroy Effect Pool has no expansion prefab.", this);
                return false;
            }

            BlockDestroyEffect expandedEffect = Instantiate(
                effectPrefab,
                worldPosition,
                Quaternion.identity,
                transform);
            expandedEffect.name = $"Block Destroy Effect {effectPool.Count + 1:00}";
            effectPool.Add(expandedEffect);
            nextEffectIndex = 0;
            return expandedEffect.Play(worldPosition, blockColor);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
