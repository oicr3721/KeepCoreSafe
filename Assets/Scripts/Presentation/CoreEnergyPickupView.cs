using System;
using DG.Tweening;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class CoreEnergyPickupView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(0.01f)] private float duration = 0.42f;
        [SerializeField, Min(0f)] private float arcHeight = 0.8f;

        private Sequence sequence;

        public void Play(Vector3 origin, Transform target, Action onComplete)
        {
            sequence?.Kill(false);
            transform.position = origin;
            transform.localScale = Vector3.one;
            if (visual != null)
                visual.color = Color.white;

            Vector3 midpoint = Vector3.Lerp(origin, target.position, 0.5f) + Vector3.up * arcHeight;
            Vector3[] path = { origin, midpoint, target.position };
            sequence = DOTween.Sequence().SetTarget(this)
                .Append(transform.DOPath(path, duration, PathType.CatmullRom)
                    .SetEase(Ease.InQuad))
                .Join(transform.DOScale(0.42f, duration).SetEase(Ease.InQuad))
                .OnComplete(() => onComplete?.Invoke());
        }

        private void OnDisable()
        {
            sequence?.Kill(false);
            sequence = null;
        }
    }
}
