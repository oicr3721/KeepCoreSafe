using System;
using UnityEngine;

namespace KeepCoreSafe.Analytics
{
    public enum AnalyticsConsentDecision
    {
        Unknown = 0,
        Granted = 1,
        Declined = 2
    }

    public static class AnalyticsConsentSettings
    {
        private const string PrefsKey = "KeepCoreSafe.AnalyticsConsent";

        public static event Action<AnalyticsConsentDecision> ConsentChanged;

        public static AnalyticsConsentDecision Decision
        {
            get
            {
                int saved = PlayerPrefs.GetInt(PrefsKey, (int)AnalyticsConsentDecision.Unknown);
                return Enum.IsDefined(typeof(AnalyticsConsentDecision), saved)
                    ? (AnalyticsConsentDecision)saved
                    : AnalyticsConsentDecision.Unknown;
            }
        }

        public static bool IsGranted => Decision == AnalyticsConsentDecision.Granted;
        public static bool HasDecision => Decision != AnalyticsConsentDecision.Unknown;

        public static void SetGranted(bool granted)
        {
            AnalyticsConsentDecision decision = granted
                ? AnalyticsConsentDecision.Granted
                : AnalyticsConsentDecision.Declined;
            if (Decision == decision)
                return;

            PlayerPrefs.SetInt(PrefsKey, (int)decision);
            PlayerPrefs.Save();
            ConsentChanged?.Invoke(decision);
        }
    }
}
