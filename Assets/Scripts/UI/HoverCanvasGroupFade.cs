using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class HoverCanvasGroupFade : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Hover Fade")]
        [SerializeField, Range(0.0001f, 1f)] private float hoverAlpha = 0.001f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.15f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;

        [SerializeField] private Image raycastImage;
        [SerializeField] private CanvasGroup canvasGroup;
        private Tween fadeTween;

        public float HoverAlpha => hoverAlpha;
        public float FadeDuration => fadeDuration;

        private void Awake()
        {
            CacheComponents();
            raycastImage.raycastTarget = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            FadeTo(Mathf.Max(0.0001f, hoverAlpha));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            FadeTo(1f);
        }

        private void FadeTo(float targetAlpha)
        {
            CacheComponents();
            fadeTween?.Kill(false);
            fadeTween = canvasGroup
                .DOFade(targetAlpha, fadeDuration)
                .SetEase(fadeEase)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void CacheComponents()
        {
            if (raycastImage == null)
                raycastImage = GetComponent<Image>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnDisable()
        {
            fadeTween?.Kill(false);
            fadeTween = null;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private void OnDestroy()
        {
            fadeTween?.Kill(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            hoverAlpha = Mathf.Clamp(hoverAlpha, 0.0001f, 1f);
            fadeDuration = Mathf.Max(0f, fadeDuration);
            Image image = GetComponent<Image>();
            if (image != null)
                image.raycastTarget = true;
        }
#endif
    }
}
