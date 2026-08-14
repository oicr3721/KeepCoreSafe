using System;
using System.Collections.Generic;
using DG.Tweening;
using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    /// <summary>
    /// Owns the short post-wave supply reveal. Supply events are infrequent and never overlap,
    /// so reusing the merge effect prefabs directly is simpler than adding another effect pool.
    /// </summary>
    public sealed class SupplySpawnPresentationController : MonoBehaviour
    {
        [Header("Merge Effect Prefabs")]
        [SerializeField] private CoreEnergyPulseView energyPulsePrefab;
        [SerializeField] private ShockwaveRingView shockwavePrefab;
        [SerializeField] private ParticleSystem burstParticlesPrefab;
        [SerializeField] private Transform effectRoot;

        [Header("Supply Reveal")]
        [SerializeField, Min(0.01f)] private float revealDuration = 0.28f;
        [SerializeField, Min(1f)] private float revealOvershoot = 1.12f;
        [SerializeField, Min(0f)] private float recognitionHoldDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float effectDuration = 0.2f;
        [SerializeField, Min(0f)] private float shockwaveScale = 1.4f;
        [SerializeField, Min(0)] private int particleCount = 18;

        private readonly List<GameObject> spawnedEffects = new();
        private Sequence activeSequence;
        private Block activeBlock;
        private Vector3 activeBlockScale;
        private Action completion;
        private bool released;

        public bool Play(Block block, Action onComplete)
        {
            if (block == null || activeSequence != null)
                return false;

            activeBlock = block;
            activeBlockScale = block.transform.localScale;
            completion = onComplete;
            released = false;

            block.SetPresentationHealthBarVisible(false);
            block.transform.DOKill();
            block.transform.localScale = Vector3.zero;
            PlayEffects(block.transform.position);

            float growDuration = revealDuration * 0.68f;
            float settleDuration = Mathf.Max(0.01f, revealDuration - growDuration);
            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(block.transform.DOScale(activeBlockScale * revealOvershoot, growDuration)
                    .SetEase(Ease.OutBack))
                .Append(block.transform.DOScale(activeBlockScale, settleDuration)
                    .SetEase(Ease.OutQuad))
                .AppendInterval(recognitionHoldDuration)
                .OnComplete(() => Release(true))
                .OnKill(() => Release(false));
            return true;
        }

        public void Cancel()
        {
            activeSequence?.Kill(false);
            Release(false);
        }

        private void PlayEffects(Vector3 position)
        {
            CoreEnergyPulseView pulse = Spawn(energyPulsePrefab, position);
            pulse?.Play(effectDuration, 1, 0.08f, shockwaveScale * 0.72f, 1.15f);

            ShockwaveRingView shockwave = Spawn(shockwavePrefab, position);
            shockwave?.Play(effectDuration, shockwaveScale);

            ParticleSystem particles = Spawn(burstParticlesPrefab, position);
            if (particles == null)
                return;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Emit(particleCount);
        }

        private T Spawn<T>(T prefab, Vector3 position) where T : Component
        {
            if (prefab == null)
                return null;

            T instance = Instantiate(
                prefab,
                position,
                Quaternion.identity,
                effectRoot != null ? effectRoot : transform);
            spawnedEffects.Add(instance.gameObject);
            return instance;
        }

        private void Release(bool invokeCompletion)
        {
            if (released)
                return;

            released = true;
            activeSequence = null;
            if (activeBlock != null)
            {
                activeBlock.transform.localScale = activeBlockScale;
                activeBlock.SetPresentationHealthBarVisible(true);
            }

            foreach (GameObject effect in spawnedEffects)
            {
                if (effect != null)
                    Destroy(effect);
            }

            spawnedEffects.Clear();
            activeBlock = null;
            Action callback = completion;
            completion = null;
            if (invokeCompletion)
                callback?.Invoke();
        }

        private void OnDestroy()
        {
            Cancel();
        }
    }
}
