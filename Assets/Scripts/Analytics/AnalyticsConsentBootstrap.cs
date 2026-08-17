using System;
using UnityEngine;

namespace KeepCoreSafe.Analytics
{
    public static class AnalyticsConsentBootstrap
    {
        private const string PromptResourcePath = "UI/Analytics Consent Prompt";
        private static AnalyticsConsentPrompt activePrompt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            activePrompt = null;
        }

        public static void ShowPreferences()
        {
            ShowPrompt(null);
        }

        public static void ContinueAfterDecision(Action continuation)
        {
            if (continuation == null)
                return;

            if (AnalyticsConsentSettings.HasDecision)
            {
                continuation.Invoke();
                return;
            }

            ShowPrompt(continuation);
        }

        private static void ShowPrompt(Action continuation)
        {
            if (activePrompt != null)
                return;

            AnalyticsConsentPrompt prefab =
                Resources.Load<AnalyticsConsentPrompt>(PromptResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"Analytics consent prompt is missing at Resources/{PromptResourcePath}. "
                    + "Analytics remains disabled.");
                continuation?.Invoke();
                return;
            }

            activePrompt = UnityEngine.Object.Instantiate(prefab);
            activePrompt.Show(() =>
            {
                activePrompt = null;
                continuation?.Invoke();
            });
        }
    }
}
