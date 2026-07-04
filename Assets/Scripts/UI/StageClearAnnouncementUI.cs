using DG.Tweening;
using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class StageClearAnnouncementUI : MonoBehaviour
    {
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string title = "STAGE CLEAR";
        [SerializeField] private string subtitle = "SHOCKWAVE DEPLOYED";

        [Header("Animation")]
        [SerializeField] private Vector2 slideOffset = new(0f, 38f);
        [SerializeField, Min(0f)] private float fadeInDuration = 0.18f;
        [SerializeField, Min(0f)] private float visibleDuration = 0.65f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.22f;

        private Vector2 shownPosition;

        private void Awake()
        {
            shownPosition = visualRoot != null ? visualRoot.anchoredPosition : Vector2.zero;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            GameManager.StageCleared += Show;
        }

        private void OnDisable()
        {
            GameManager.StageCleared -= Show;
            visualRoot?.DOKill(false);
            canvasGroup?.DOKill(false);
        }

        private void Show(int waveIndex)
        {
            if (visualRoot == null || canvasGroup == null || label == null)
                return;

            visualRoot.DOKill(false);
            canvasGroup.DOKill(false);
            label.text = $"{title}\n<size=45%>{subtitle}</size>";
            visualRoot.anchoredPosition = shownPosition + slideOffset;
            canvasGroup.alpha = 0f;

            DOTween.Sequence()
                .SetTarget(visualRoot)
                .SetUpdate(true)
                .Append(visualRoot.DOAnchorPos(shownPosition, fadeInDuration).SetEase(Ease.OutBack))
                .Join(canvasGroup.DOFade(1f, fadeInDuration))
                .AppendInterval(visibleDuration)
                .Append(visualRoot.DOAnchorPos(shownPosition - slideOffset, fadeOutDuration).SetEase(Ease.InCubic))
                .Join(canvasGroup.DOFade(0f, fadeOutDuration));
        }
    }
}
