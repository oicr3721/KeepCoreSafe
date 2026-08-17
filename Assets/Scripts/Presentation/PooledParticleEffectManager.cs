using System.Collections.Generic;
using KeepCoreSafe.Core;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    /// <summary>
    /// Shared lifetime and pooling implementation for one-shot ParticleSystem services.
    /// Concrete managers retain their domain-specific singleton and public API.
    /// </summary>
    public abstract class PooledParticleEffectManager : MonoBehaviour
    {
        [SerializeField] private ParticleSystem effectPrefab;
        [SerializeField, Min(0)] private int initialPoolSize = 12;
        [SerializeField] private Transform effectRoot;

        private readonly List<ParticleSystem> activeEffects = new();
        private ComponentPool<ParticleSystem> effectPool;

        protected bool InitializePool()
        {
            if (effectPrefab == null)
            {
                Debug.LogError($"{GetType().Name} has no particle prefab assigned.", this);
                enabled = false;
                return false;
            }

            effectPool = new ComponentPool<ParticleSystem>(
                effectPrefab,
                initialPoolSize,
                effectRoot != null ? effectRoot : transform);
            return true;
        }

        protected bool PlayEffectAt(Vector3 worldPosition)
        {
            ParticleSystem effect = effectPool?.Rent();
            if (effect == null)
                return false;

            effect.transform.position = worldPosition;
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(true);
            activeEffects.Add(effect);
            return true;
        }

        protected virtual void Update()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ParticleSystem effect = activeEffects[i];
                if (effect != null && effect.IsAlive(true))
                    continue;

                activeEffects.RemoveAt(i);
                if (effect != null)
                    effectPool.Return(effect);
            }
        }
    }
}
