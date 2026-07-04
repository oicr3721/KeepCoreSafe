using System;
using DG.Tweening;
using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class HealProjectile : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private SpriteRenderer projectileRenderer;
        [SerializeField] private SpriteRenderer impactRenderer;

        [Header("Projectile")]
        [SerializeField, ColorUsage(true, true)]
        private Color projectileColor = new(0.35f, 1.7f, 0.65f, 1f);
        [SerializeField, Min(0f)] private float projectileScale = 0.22f;

        [Header("Impact")]
        [SerializeField, ColorUsage(true, true)]
        private Color impactColor = new(0.5f, 2f, 0.8f, 0.9f);
        [SerializeField, Min(0f)] private float impactScale = 0.7f;
        [SerializeField, Min(0f)] private float impactDuration = 0.14f;

        private Block target;
        private Vector3 startPosition;
        private Vector3 lastTargetPosition;
        private float arcHeight;
        private float progress;
        private Action<Block> arrived;
        private Action<HealProjectile> returnToPool;

        public void Launch(
            Vector3 start,
            Block healTarget,
            float travelDuration,
            float curveHeight,
            Action<Block> onArrived,
            Action<HealProjectile> onReleased)
        {
            transform.DOKill(false);
            projectileRenderer?.DOKill(false);
            impactRenderer?.DOKill(false);

            target = healTarget;
            startPosition = start;
            lastTargetPosition = healTarget != null ? healTarget.transform.position : start;
            arcHeight = curveHeight;
            arrived = onArrived;
            returnToPool = onReleased;
            progress = 0f;
            transform.position = start;
            transform.localScale = Vector3.one * projectileScale;

            if (projectileRenderer != null)
            {
                projectileRenderer.enabled = true;
                projectileRenderer.color = projectileColor;
            }
            if (impactRenderer != null)
                impactRenderer.enabled = false;

            DOTween.To(() => progress, value =>
                {
                    progress = value;
                    UpdatePosition();
                }, 1f, Mathf.Max(0.01f, travelDuration))
                .SetTarget(transform)
                .SetEase(Ease.InOutSine)
                .OnComplete(Arrive);

            transform.DOPunchScale(Vector3.one * (projectileScale * 0.35f), travelDuration, 5, 0.4f)
                .SetTarget(transform);
        }

        private void UpdatePosition()
        {
            if (target != null)
                lastTargetPosition = target.transform.position;

            Vector3 midpoint = (startPosition + lastTargetPosition) * 0.5f;
            Vector2 direction = lastTargetPosition - startPosition;
            Vector2 perpendicular = direction.sqrMagnitude > 0.0001f
                ? new Vector2(-direction.y, direction.x).normalized
                : Vector2.up;
            Vector3 control = midpoint + (Vector3)(perpendicular * arcHeight);
            float inverse = 1f - progress;
            transform.position = inverse * inverse * startPosition
                                 + 2f * inverse * progress * control
                                 + progress * progress * lastTargetPosition;
        }

        private void Arrive()
        {
            arrived?.Invoke(target);
            arrived = null;
            if (projectileRenderer != null)
                projectileRenderer.enabled = false;

            if (impactRenderer == null)
            {
                ReturnToPool();
                return;
            }

            transform.position = lastTargetPosition;
            transform.localScale = Vector3.one * 0.12f;
            impactRenderer.enabled = true;
            impactRenderer.color = impactColor;
            DOTween.Sequence()
                .SetTarget(transform)
                .Append(transform.DOScale(impactScale, impactDuration).SetEase(Ease.OutCubic))
                .Join(impactRenderer.DOFade(0f, impactDuration))
                .OnComplete(ReturnToPool);
        }

        private void ReturnToPool()
        {
            target = null;
            Action<HealProjectile> callback = returnToPool;
            returnToPool = null;
            callback?.Invoke(this);
        }

        private void OnDisable()
        {
            transform.DOKill(false);
            projectileRenderer?.DOKill(false);
            impactRenderer?.DOKill(false);
        }
    }
}
