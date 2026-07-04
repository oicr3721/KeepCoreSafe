using DG.Tweening;
using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class WaveAnnouncementUI : MonoBehaviour
    {
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;

        [Header("Animation")]
        [SerializeField] private Vector2 slideOffset = new(0f, 45f);
        [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
        [SerializeField, Min(0f)] private float visibleDuration = 0.75f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;

        private Vector2 shownPosition;

        private void Awake()
        {
            shownPosition = visualRoot != null ? visualRoot.anchoredPosition : Vector2.zero;
            SetHidden();
        }

        private void OnEnable()
        {
            GameManager.WaveStarted += Show;
        }

        private void OnDisable()
        {
            GameManager.WaveStarted -= Show;
            visualRoot?.DOKill(false);
            canvasGroup?.DOKill(false);
        }

        private void Show(int waveIndex)
        {
            if (visualRoot == null || canvasGroup == null || label == null)
                return;

            visualRoot.DOKill(false);
            canvasGroup.DOKill(false);
            label.text = $"Wave {waveIndex}";
            visualRoot.anchoredPosition = shownPosition + slideOffset;
            canvasGroup.alpha = 0f;
            gameObject.SetActive(true);

            DOTween.Sequence()
                .SetTarget(visualRoot)
                .SetUpdate(true)
                .Append(visualRoot.DOAnchorPos(shownPosition, fadeInDuration).SetEase(Ease.OutCubic))
                .Join(canvasGroup.DOFade(1f, fadeInDuration))
                .AppendInterval(visibleDuration)
                .Append(visualRoot.DOAnchorPos(shownPosition - slideOffset, fadeOutDuration).SetEase(Ease.InCubic))
                .Join(canvasGroup.DOFade(0f, fadeOutDuration));
        }

        private void SetHidden()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }
    }
}
