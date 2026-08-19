using DG.Tweening;
using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Localization;
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
        [SerializeField] private CanvasGroup bestWaveGroup;
        [SerializeField] private TMP_Text bestWaveLabel;
        [SerializeField] private RectTransform bestWavePulseTarget;
        [SerializeField] private Button restartButton;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float blackoutDuration = 0.12f;
        [SerializeField, Min(0f)] private float itemFadeDuration = 0.24f;
        [SerializeField, Min(0f)] private float itemInterval = 0.08f;
        [SerializeField, Min(0f)] private float itemSlideDistance = 24f;
        [SerializeField, Range(1f, 1.5f)] private float newRecordPulseScale = 1.14f;
        [SerializeField, Min(0.05f)] private float newRecordPulseDuration = 0.38f;
        [SerializeField, Min(1)] private int newRecordPulseCount = 3;

        [Header("Audio")]
        [SerializeField] private AudioCue gameOverSound;

        private Sequence sequence;
        private Sequence recordPulse;
        private Vector3 recordBaseScale = Vector3.one;

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
            if (bestWaveGroup != null)
                PrepareItem(bestWaveGroup);
            PrepareItem(restartGroup);
            
            if(waveLabel != null)
            {
                waveLabel.text = $"wave {GameManager.WaveIndex}";
                LocalizationManager.Format(
                    "gameover.wave", "wave {0}", GameManager.WaveIndex);
            }
            if (bestWaveLabel != null)
            {
                bestWaveLabel.text = LocalizationManager.Format(
                    "gameover.bestWave", "Best wave: {0}", BestWaveRecord.BestWave);
            }
            if (bestWavePulseTarget != null)
            {
                recordBaseScale = bestWavePulseTarget.localScale;
                bestWavePulseTarget.localScale = recordBaseScale;
            }

            sequence = DOTween.Sequence()
                .SetUpdate(true)
                .Append(blackout.DOFade(1f, blackoutDuration))
                .Append(FadeItem(titleGroup))
                .AppendInterval(itemInterval)
                .Append(FadeItem(waveGroup));
            if (bestWaveGroup != null)
            {
                sequence.AppendInterval(itemInterval)
                    .Append(FadeItem(bestWaveGroup));
            }
            sequence.AppendInterval(itemInterval)
                .Append(FadeItem(restartGroup));
            if (BestWaveRecord.LastGameOverWasNewBest && bestWavePulseTarget != null)
                sequence.OnComplete(PlayNewRecordPulse);
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
            recordPulse?.Kill(false);
            if (bestWavePulseTarget != null)
                bestWavePulseTarget.localScale = recordBaseScale;
            if (visualRoot != null)
                visualRoot.SetActive(false);
        }

        private void PlayNewRecordPulse()
        {
            recordPulse?.Kill(false);
            recordPulse = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            float halfDuration = newRecordPulseDuration * 0.5f;
            Vector3 enlarged = recordBaseScale * newRecordPulseScale;
            for (int i = 0; i < newRecordPulseCount; i++)
            {
                recordPulse.Append(
                    bestWavePulseTarget.DOScale(enlarged, halfDuration).SetEase(Ease.OutQuad));
                recordPulse.Append(
                    bestWavePulseTarget.DOScale(recordBaseScale, halfDuration).SetEase(Ease.InQuad));
            }
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
            recordPulse?.Kill(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (restartButton == null)
            {
                Debug.LogWarning(
                    $"{nameof(GameOverUI)} on {name} needs a Restart Button reference assigned in the Inspector.",
                    this);
            }
        }
#endif
    }
}
