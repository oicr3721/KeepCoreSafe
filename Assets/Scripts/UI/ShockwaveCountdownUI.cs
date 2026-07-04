using DG.Tweening;
using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class ShockwaveCountdownUI : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private TMP_Text countdownLabel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private string objectiveLabel = "SHOCKWAVE CHARGE";
        [SerializeField] private Color normalColor = new(0.55f, 1f, 0.82f, 1f);
        [SerializeField] private Color urgentColor = new(1f, 0.55f, 0.2f, 1f);
        [SerializeField, Min(0f)] private float urgentThreshold = 5f;

        [Header("Wave Start Reveal")]
        [SerializeField, Range(1, 6)] private int flickerCount = 3;
        [SerializeField, Min(0.05f)] private float revealDuration = 0.3f;
        [SerializeField, Min(0f)] private float revealPunchScale = 0.08f;

        private int displayedSeconds = -1;
        private Sequence revealSequence;

        private void OnEnable()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
            GameManager.WaveStarted += PlayWaveStartReveal;
            HandlePhaseChanged(GameManager.Phase);
        }

        private void OnDisable()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;
            GameManager.WaveStarted -= PlayWaveStartReveal;
            revealSequence?.Kill(false);
        }

        private void Update()
        {
            if (GameManager.Phase != GamePhase.Combat || GameManager.Instance == null)
                return;

            float remaining = GameManager.Instance.RemainingCombatTime;
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds == displayedSeconds)
                return;

            displayedSeconds = seconds;
            int minutes = seconds / 60;
            int remainder = seconds % 60;
            countdownLabel.text = $"{objectiveLabel}\n{minutes:00}:{remainder:00}";
            countdownLabel.color = remaining <= urgentThreshold ? urgentColor : normalColor;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            revealSequence?.Kill(false);
            bool show = phase == GamePhase.Combat;
            if (visualRoot != null)
                visualRoot.SetActive(show);
            else if (countdownLabel != null)
                countdownLabel.gameObject.SetActive(show);

            displayedSeconds = -1;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            if (countdownLabel != null)
                countdownLabel.rectTransform.localScale = Vector3.one;
        }

        private void PlayWaveStartReveal(int _)
        {
            if (canvasGroup == null || countdownLabel == null)
                return;

            revealSequence?.Kill(false);
            canvasGroup.alpha = 0f;
            countdownLabel.rectTransform.localScale = Vector3.one * 0.94f;
            float step = revealDuration / Mathf.Max(1, flickerCount * 2);
            revealSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (int i = 0; i < flickerCount; i++)
            {
                revealSequence.Append(canvasGroup.DOFade(0.28f, step));
                revealSequence.Append(canvasGroup.DOFade(1f, step));
            }

            revealSequence.Insert(0f, countdownLabel.rectTransform.DOPunchScale(
                Vector3.one * revealPunchScale,
                revealDuration,
                6,
                0.45f));
        }
    }
}
