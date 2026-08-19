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
        private const string PrefsNoticeVersionKey = "KeepCoreSafe.AnalyticsConsentNoticeVersion";
        private const string PrefsRecordedAtUtcKey = "KeepCoreSafe.AnalyticsConsentRecordedAtUtc";
        private const int CurrentNoticeVersion = 2;

        public static event Action<AnalyticsConsentDecision> ConsentChanged;

        public static AnalyticsConsentDecision Decision
        {
            get
            {
                if (PlayerPrefs.GetInt(PrefsNoticeVersionKey, 0) != CurrentNoticeVersion)
                    return AnalyticsConsentDecision.Unknown;

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
            PlayerPrefs.SetInt(PrefsNoticeVersionKey, CurrentNoticeVersion);
            PlayerPrefs.SetString(PrefsRecordedAtUtcKey, DateTime.UtcNow.ToString("O"));
            PlayerPrefs.Save();
            ConsentChanged?.Invoke(decision);
        }
    }
}
