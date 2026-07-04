using DG.Tweening;
using KeepCoreSafe.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonInteraction : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private RectTransform animationTarget;

        [Header("Click Feedback")]
        [SerializeField] private AudioCue clickSound = new();
        [SerializeField, Range(0.85f, 0.99f)] private float pressedScale = 0.93f;
        [SerializeField, Min(0.01f)] private float pressDuration = 0.05f;
        [SerializeField, Min(0.01f)] private float releaseDuration = 0.07f;

        private Sequence currentTween;
        private Vector3 baseScale = Vector3.one;
        private bool acceptedPointerDown;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (animationTarget == null)
                animationTarget = transform as RectTransform;
            if (animationTarget != null)
                baseScale = animationTarget.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            acceptedPointerDown = button != null && button.IsInteractable();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!acceptedPointerDown || animationTarget == null)
                return;

            acceptedPointerDown = false;
            currentTween?.Kill(false);
            animationTarget.localScale = baseScale;
            AudioManager.Play(clickSound);
            currentTween = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animationTarget.DOScale(baseScale * pressedScale, pressDuration).SetEase(Ease.OutQuad))
                .Append(animationTarget.DOScale(baseScale, releaseDuration).SetEase(Ease.OutBack));
        }

        private void OnDisable()
        {
            currentTween?.Kill(false);
            currentTween = null;
            acceptedPointerDown = false;
            if (animationTarget != null)
                animationTarget.localScale = baseScale;
        }

        private void OnDestroy()
        {
            currentTween?.Kill(false);
        }
    }
}
