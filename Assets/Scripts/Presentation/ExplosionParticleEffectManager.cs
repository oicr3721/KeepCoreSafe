using System.Collections.Generic;
using KeepCoreSafe.Core;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class ExplosionParticleEffectManager : MonoBehaviour
    {
        public static ExplosionParticleEffectManager Instance { get; private set; }

        [SerializeField] private ParticleSystem effectPrefab;
        [SerializeField, Min(0)] private int initialPoolSize = 12;
        [SerializeField] private Transform effectRoot;

        private readonly List<ParticleSystem> activeEffects = new();
        private ComponentPool<ParticleSystem> effectPool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Multiple ExplosionParticleEffectManager instances are active.", this);
                enabled = false;
                return;
            }

            Instance = this;
            if (effectPrefab == null)
            {
                Debug.LogError("Explosion particle pool has no prefab assigned.", this);
                enabled = false;
                return;
            }

            effectPool = new ComponentPool<ParticleSystem>(
                effectPrefab,
                initialPoolSize,
                effectRoot != null ? effectRoot : transform);
        }

        public bool PlayAt(Vector3 worldPosition)
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

        private void Update()
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
