using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class StartWaveButtonUI : MonoBehaviour
    {
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button button;
        [SerializeField] private Vector2 hiddenOffset = new(35f, 0f);
        [SerializeField, Min(0f)] private float showDuration = 0.2f;
        [SerializeField, Min(0f)] private float hideDuration = 0.12f;

        private Vector2 shownPosition;

        private void Awake()
        {
            shownPosition = visualRoot != null ? visualRoot.anchoredPosition : Vector2.zero;
            Hide(true);
        }

        public void Show()
        {
            if (visualRoot == null || canvasGroup == null)
                return;

            visualRoot.DOKill(false);
            canvasGroup.DOKill(false);
            gameObject.SetActive(true);
            visualRoot.anchoredPosition = shownPosition + hiddenOffset;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            if (button != null)
                button.interactable = false;

            DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(visualRoot.DOAnchorPos(shownPosition, showDuration).SetEase(Ease.OutBack))
                .Join(canvasGroup.DOFade(1f, showDuration))
                .OnComplete(() =>
                {
                    if (button != null)
                        button.interactable = true;
                });
        }

        public void Hide(bool immediate = false)
        {
            if (visualRoot == null || canvasGroup == null)
                return;

            visualRoot.DOKill(false);
            canvasGroup.DOKill(false);
            if (button != null)
                button.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (immediate || !gameObject.activeInHierarchy)
            {
                canvasGroup.alpha = 0f;
                visualRoot.anchoredPosition = shownPosition + hiddenOffset;
                return;
            }

            DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(visualRoot.DOAnchorPos(shownPosition + hiddenOffset, hideDuration).SetEase(Ease.InCubic))
                .Join(canvasGroup.DOFade(0f, hideDuration));
        }

        private void OnDestroy()
        {
            visualRoot?.DOKill(false);
            canvasGroup?.DOKill(false);
            this.DOKill(false);
        }
    }
}
