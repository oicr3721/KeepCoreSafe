using KeepCoreSafe.Analytics;
using KeepCoreSafe.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class AnalyticsPrivacySettingsUI : MonoBehaviour
    {
        [SerializeField] private Button preferencesButton;
        [SerializeField] private TMP_Text statusLabel;

        private void OnEnable()
        {
            preferencesButton?.onClick.AddListener(OpenPreferences);
            AnalyticsConsentSettings.ConsentChanged += HandleConsentChanged;
            LocalizationManager.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            preferencesButton?.onClick.RemoveListener(OpenPreferences);
            AnalyticsConsentSettings.ConsentChanged -= HandleConsentChanged;
            LocalizationManager.LanguageChanged -= Refresh;
        }

        private static void OpenPreferences()
        {
            AnalyticsConsentBootstrap.ShowPreferences();
        }

        private void HandleConsentChanged(AnalyticsConsentDecision decision)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (statusLabel == null)
                return;

            string statusKey = AnalyticsConsentSettings.IsGranted
                ? "analytics.settings.enabled"
                : "analytics.settings.disabled";
            string status = LocalizationManager.Get(
                statusKey,
                AnalyticsConsentSettings.IsGranted ? "ON" : "OFF");
            statusLabel.text = LocalizationManager.Format(
                "analytics.settings.label",
                "Playtest data: {0}",
                status);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (preferencesButton == null || statusLabel == null)
                Debug.LogWarning($"{nameof(AnalyticsPrivacySettingsUI)} on {name} has missing references.", this);
        }
#endif
    }
}
