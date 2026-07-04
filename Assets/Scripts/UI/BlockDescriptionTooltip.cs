using DG.Tweening;
using KeepCoreSafe.Data;
using TMPro;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class BlockDescriptionTooltip : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private Vector2 pointerOffset = new(18f, -18f);
        [SerializeField, Min(0f)] private float fadeDuration = 0.12f;

        private void Awake()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        public void Show(BlockData data, Vector2 screenPosition)
        {
            if (data == null)
                return;

            titleLabel.text = data.DisplayName;
            descriptionLabel.text = data.Description;
            gameObject.SetActive(true);
            SetPosition(screenPosition);
            canvasGroup.DOKill(false);
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }

        public void SetPosition(Vector2 screenPosition)
        {
            if (canvas == null || panel == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPosition);

            Vector2 desired = localPosition + pointerOffset;
            Vector2 halfSize = panel.rect.size * 0.5f;
            Rect bounds = canvasRect.rect;
            desired.x = Mathf.Clamp(desired.x, bounds.xMin + halfSize.x, bounds.xMax - halfSize.x);
            desired.y = Mathf.Clamp(desired.y, bounds.yMin + halfSize.y, bounds.yMax - halfSize.y);
            panel.anchoredPosition = desired;
        }

        public void Hide()
        {
            canvasGroup.DOKill(false);
            canvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void OnDestroy()
        {
            canvasGroup?.DOKill(false);
        }
    }
}
