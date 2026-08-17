using System;
using System.Collections.Generic;
using GameAnalyticsSDK;
using UnityEngine;

namespace KeepCoreSafe.Analytics
{
    public sealed class GameAnalyticsBackend : IAnalyticsBackend
    {
        private bool initialized;
        private bool unavailable;

        public void SetEnabled(bool enabled)
        {
            if (!enabled)
            {
                if (initialized)
                    GameAnalytics.SetEnabledEventSubmission(false);
                return;
            }

            if (initialized)
                GameAnalytics.SetEnabledEventSubmission(true);
            else
                Initialize();
        }

        public void Initialize()
        {
            if (initialized
                || unavailable
                || !Application.isPlaying
                || !AnalyticsConsentSettings.IsGranted)
                return;

            try
            {
                if (UnityEngine.Object.FindAnyObjectByType<GameAnalytics>() == null)
                {
                    GameObject host = new("GameAnalytics");
                    host.AddComponent<GameAnalytics>();
                }

                // Playtest analytics never needs a cross-app advertising identifier.
                // This also forces the SDK's generated player identifier to be random.
                GameAnalytics.EnableAdvertisingIdTracking(false);
                GameAnalytics.Initialize();
                initialized = GameAnalytics.Initialized;
            }
            catch (Exception exception)
            {
                unavailable = true;
                Debug.LogWarning($"GameAnalytics initialization was disabled: {exception.Message}");
            }
        }

        public void SendDesign(
            string eventId,
            float? value,
            IReadOnlyDictionary<string, object> fields)
        {
            if (!EnsureInitialized())
                return;

            try
            {
                IDictionary<string, object> payload = CopyFields(fields);
                if (value.HasValue)
                    GameAnalytics.NewDesignEvent(eventId, value.Value, payload);
                else if (payload.Count > 0)
                    GameAnalytics.NewDesignEvent(eventId, payload);
                else
                    GameAnalytics.NewDesignEvent(eventId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GameAnalytics design event '{eventId}' was skipped: {exception.Message}");
            }
        }

        public void SendProgression(
            AnalyticsProgressionStatus status,
            string progression01,
            string progression02,
            string progression03,
            IReadOnlyDictionary<string, object> fields)
        {
            if (!EnsureInitialized())
                return;

            try
            {
                GAProgressionStatus sdkStatus = status switch
                {
                    AnalyticsProgressionStatus.Complete => GAProgressionStatus.Complete,
                    AnalyticsProgressionStatus.Fail => GAProgressionStatus.Fail,
                    _ => GAProgressionStatus.Start
                };
                IDictionary<string, object> payload = CopyFields(fields);
                if (!string.IsNullOrEmpty(progression03))
                    GameAnalytics.NewProgressionEvent(
                        sdkStatus, progression01, progression02, progression03, payload);
                else if (!string.IsNullOrEmpty(progression02))
                    GameAnalytics.NewProgressionEvent(
                        sdkStatus, progression01, progression02, payload);
                else
                    GameAnalytics.NewProgressionEvent(sdkStatus, progression01, payload);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GameAnalytics progression event was skipped: {exception.Message}");
            }
        }

        private bool EnsureInitialized()
        {
            if (!AnalyticsConsentSettings.IsGranted)
                return false;
            Initialize();
            return initialized && !unavailable;
        }

        private static IDictionary<string, object> CopyFields(
            IReadOnlyDictionary<string, object> fields)
        {
            Dictionary<string, object> copy = new();
            if (fields == null)
                return copy;

            foreach (KeyValuePair<string, object> field in fields)
                copy[field.Key] = field.Value;
            return copy;
        }
    }
}
