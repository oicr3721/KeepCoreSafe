using DG.Tweening;
using KeepCoreSafe.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonInteraction : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private RectTransform animationTarget;

        [Header("Click Feedback")]
        [SerializeField] private AudioCue clickSound = new();
        [SerializeField, Range(0.85f, 0.99f)] private float pressedScale = 0.93f;
        [SerializeField, Min(0.01f)] private float pressDuration = 0.05f;
        [SerializeField, Min(0.01f)] private float releaseDuration = 0.07f;

        [Header("Hover Feedback")]
        [SerializeField, Range(1f, 1.12f)] private float hoverScale = 1.06f;
        [SerializeField, Min(0.01f)] private float hoverInDuration = 0.14f;
        [SerializeField, Min(0.01f)] private float hoverOutDuration = 0.1f;
        [SerializeField] private Ease hoverInEase = Ease.OutBack;
        [SerializeField] private Ease hoverOutEase = Ease.OutQuad;

        private Sequence currentTween;
        private Vector3 baseScale = Vector3.one;
        private bool acceptedPointerDown;
        private bool pointerInside;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (animationTarget == null)
                animationTarget = transform as RectTransform;
            if (animationTarget != null)
                baseScale = animationTarget.localScale;
        }

        private void Update()
        {
            if (pointerInside && button != null && !button.IsInteractable())
            {
                pointerInside = false;
                AnimateScale(baseScale, hoverOutDuration, hoverOutEase);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanAnimate())
                return;

            pointerInside = true;
            AnimateScale(baseScale * hoverScale, hoverInDuration, hoverInEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            if (animationTarget == null)
                return;

            AnimateScale(baseScale, hoverOutDuration, hoverOutEase);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            acceptedPointerDown = button != null && button.IsInteractable();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!acceptedPointerDown || animationTarget == null)
            {
                acceptedPointerDown = false;
                return;
            }

            acceptedPointerDown = false;
            currentTween?.Kill(false);
            AudioManager.Play(clickSound);
            Vector3 releaseScale = pointerInside && CanAnimate()
                ? baseScale * hoverScale
                : baseScale;
            currentTween = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animationTarget.DOScale(baseScale * pressedScale, pressDuration).SetEase(Ease.OutQuad))
                .Append(animationTarget.DOScale(releaseScale, releaseDuration).SetEase(Ease.OutBack));
        }

        private bool CanAnimate()
        {
            return button != null
                   && button.IsInteractable()
                   && animationTarget != null;
        }

        private void AnimateScale(Vector3 targetScale, float duration, Ease ease)
        {
            if (animationTarget == null)
                return;

            currentTween?.Kill(false);
            currentTween = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animationTarget.DOScale(targetScale, duration).SetEase(ease));
        }

        private void OnDisable()
        {
            currentTween?.Kill(false);
            currentTween = null;
            acceptedPointerDown = false;
            pointerInside = false;
            if (animationTarget != null)
                animationTarget.localScale = baseScale;
        }

        private void OnDestroy()
        {
            currentTween?.Kill(false);
        }
    }
}
