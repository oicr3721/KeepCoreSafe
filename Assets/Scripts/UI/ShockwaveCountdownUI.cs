using KeepCoreSafe.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace KeepCoreSafe.UI
{
    public sealed class ShockwaveCountdownUI : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [FormerlySerializedAs("gauge")]
        [SerializeField] private DelayedFillGauge normalFillGauge;
        [SerializeField] private DelayedFillGauge minusFillGauge;
        [FormerlySerializedAs("energyLabel")]
        [FormerlySerializedAs("countdownLabel")]
        [SerializeField] private TMP_Text currentEnergy;
        [SerializeField] private TMP_Text requiredEnergy;

        private ObservableInt source;

        private void OnEnable()
        {
            GameManager.PhaseChanged += HandlePhaseChanged;
            BindEnergy();
            HandlePhaseChanged(GameManager.Phase);
        }

        private void Start()
        {
            // OnEnable can run before GameManager.Awake on the initial scene load.
            BindEnergy();
        }

        private void OnDisable()
        {
            GameManager.PhaseChanged -= HandlePhaseChanged;

            if (source != null)
                source.OnValueChanged -= Refresh;
            source = null;
        }

        private void BindEnergy()
        {
            ObservableInt next = GameManager.Instance?.CoreEnergy;
            if (source == next)
                return;
            if (source != null)
                source.OnValueChanged -= Refresh;
            source = next;
            if (source != null)
            {
                source.OnValueChanged += Refresh;
                Refresh(source.CurrentValue, source.MaxValue, true);
            }
        }

        private void Refresh(int current, int maximum)
        {
            Refresh(current, maximum, false);
        }

        private void Refresh(int current, int maximum, bool immediate)
        {
            int safeMaximum = Mathf.Max(1, maximum);
            normalFillGauge?.SetRange(0f, safeMaximum);
            minusFillGauge?.SetRange(0f, safeMaximum);
            normalFillGauge?.SetValue(Mathf.Max(0, current), immediate);
            minusFillGauge?.SetValue(Mathf.Clamp(-current, 0, safeMaximum), immediate);
            if (currentEnergy != null)
                currentEnergy.text = $"{current}";
            if (requiredEnergy != null)
                requiredEnergy.text = $"/{maximum}";
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            BindEnergy();
            if (visualRoot != null)
                visualRoot.SetActive(phase != GamePhase.GameOver);
        }
    }
}
