using DG.Tweening;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Core;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class WaveStartPresentationController : MonoBehaviour
    {
        [Header("Core Charge")]
        [SerializeField] private CoreEnergyPulseView corePulsePrefab;
        [SerializeField, Min(1)] private int pulseCount = 2;
        [SerializeField, Min(0.01f)] private float pulseDuration = 0.24f;
        [SerializeField, Min(0f)] private float pulseMinimumScale = 0.15f;
        [SerializeField, Min(0f)] private float pulseMaximumScale = 0.75f;

        [Header("Camera")]
        [SerializeField] private bool playCameraShake = true;

        private ComponentPool<CoreEnergyPulseView> pulsePool;
        private Tween returnTween;

        private void Awake()
        {
            if (corePulsePrefab != null)
                pulsePool = new ComponentPool<CoreEnergyPulseView>(corePulsePrefab, 1, transform);
        }

        private void OnEnable()
        {
            GameManager.WaveStarted += Play;
        }

        private void OnDisable()
        {
            GameManager.WaveStarted -= Play;
            returnTween?.Kill(false);
        }

        private void Play(int _)
        {
            CoreBlock core = GridManager.Instance?.Grid?.Core as CoreBlock;
            if (core == null)
                return;

            if (playCameraShake)
                GameCameraController.Instance?.PlayImpactShake();

            CoreEnergyPulseView pulse = pulsePool?.Rent();
            if (pulse == null)
                return;

            pulse.transform.position = core.transform.position;
            pulse.Play(
                pulseDuration,
                pulseCount,
                pulseMinimumScale,
                pulseMaximumScale);

            returnTween?.Kill(false);
            returnTween = DOVirtual.DelayedCall(
                    pulseDuration,
                    () => pulsePool.Return(pulse))
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void OnDestroy()
        {
            returnTween?.Kill(false);
            this.DOKill(false);
        }
    }
}
