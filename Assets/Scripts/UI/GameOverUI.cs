using DG.Tweening;
using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;
using KeepCoreSafe.Audio;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class GameOverUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private CanvasGroup blackout;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup waveGroup;
        [SerializeField] private CanvasGroup restartGroup;
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private Button restartButton;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float blackoutDuration = 0.12f;
        [SerializeField, Min(0f)] private float itemFadeDuration = 0.24f;
        [SerializeField, Min(0f)] private float itemInterval = 0.08f;
        [SerializeField, Min(0f)] private float itemSlideDistance = 24f;

        [Header("Audio")]
        [SerializeField] private AudioCue gameOverSound;

        private Sequence sequence;

        private void Awake()
        {
            restartButton?.onClick.AddListener(Restart);
            HideImmediate();
        }

        private void OnEnable()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.GameOver)
                Show();
            else if (phase == GamePhase.Preparation)
                HideImmediate();
        }

        private void Show()
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Play(gameOverSound);

            sequence?.Kill(false);
            visualRoot.SetActive(true);
            blackout.alpha = 0f;
            PrepareItem(titleGroup);
            PrepareItem(waveGroup);
            PrepareItem(restartGroup);
            waveLabel.text = $"Wave {GameManager.WaveIndex}";

            sequence = DOTween.Sequence()
                .SetUpdate(true)
                .Append(blackout.DOFade(1f, blackoutDuration))
                .Append(FadeItem(titleGroup))
                .AppendInterval(itemInterval)
                .Append(FadeItem(waveGroup))
                .AppendInterval(itemInterval)
                .Append(FadeItem(restartGroup));
        }

        private Tween FadeItem(CanvasGroup group)
        {
            RectTransform rect = group.transform as RectTransform;
            Vector2 destination = rect.anchoredPosition + Vector2.up * itemSlideDistance;
            return DOTween.Sequence()
                .Join(group.DOFade(1f, itemFadeDuration))
                .Join(rect.DOAnchorPos(destination, itemFadeDuration).SetEase(Ease.OutCubic));
        }

        private void PrepareItem(CanvasGroup group)
        {
            group.alpha = 0f;
            RectTransform rect = group.transform as RectTransform;
            rect.anchoredPosition -= Vector2.up * itemSlideDistance;
        }

        private void HideImmediate()
        {
            sequence?.Kill(false);
            if (visualRoot != null)
                visualRoot.SetActive(false);
        }

        private static void Restart()
        {
            Time.timeScale = 1f;
            SceneLoader.Load(SceneType.Title);
        }

        private void OnDestroy()
        {
            restartButton?.onClick.RemoveListener(Restart);
            sequence?.Kill(false);
        }
    }
}
