using System;
using DG.Tweening;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class EnemySpawnIndicatorView : MonoBehaviour
    {
        [SerializeField] private LineRenderer ringRenderer;
        [SerializeField] private Color markerColor = new(1f, 0.08f, 0.04f, 0.72f);
        [SerializeField, Min(0.01f)] private float fadeInDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float initialPulseDuration = 0.32f;
        [SerializeField, Min(0.01f)] private float loopPulseDuration = 1.15f;
        [SerializeField, Range(1f, 2f)] private float initialPulseScale = 1.35f;
        [SerializeField, Range(1f, 1.5f)] private float loopPulseScale = 1.08f;

        private Sequence sequence;
        private Tween loopTween;

        public void Show(Vector3 worldPosition, float diameter)
        {
            StopAnimation();
            transform.position = worldPosition;
            transform.localScale = Vector3.zero;
            SetRingColor(WithAlpha(markerColor, 0f));

            float baseScale = Mathf.Max(0.1f, diameter);
            sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(transform.DOScale(baseScale * initialPulseScale, fadeInDuration)
                    .SetEase(Ease.OutCubic))
                .Join(DOTween.To(
                    () => ringRenderer != null ? ringRenderer.startColor.a : 0f,
                    alpha => SetRingColor(WithAlpha(markerColor, alpha)),
                    markerColor.a,
                    fadeInDuration))
                .Append(transform.DOScale(baseScale, initialPulseDuration)
                    .SetEase(Ease.OutBack))
                .OnComplete(() =>
                {
                    loopTween = transform.DOScale(
                            baseScale * loopPulseScale,
                            loopPulseDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetUpdate(true)
                        .SetTarget(this);
                });
        }

        public void Hide(float duration, Action onComplete)
        {
            StopAnimation();
            if (ringRenderer == null || duration <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(DOTween.To(
                    () => ringRenderer.startColor.a,
                    alpha => SetRingColor(WithAlpha(markerColor, alpha)),
                    0f,
                    duration))
                .OnComplete(() => onComplete?.Invoke());
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void StopAnimation()
        {
            sequence?.Kill();
            sequence = null;
            loopTween?.Kill();
            loopTween = null;
            transform.DOKill();
        }

        private void SetRingColor(Color color)
        {
            if (ringRenderer == null)
                return;

            ringRenderer.startColor = color;
            ringRenderer.endColor = color;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
