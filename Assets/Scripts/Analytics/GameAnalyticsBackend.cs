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
#if UNITY_WEBGL && !UNITY_EDITOR
                // GameAnalytics Unity SDK 8.0.1's WebGL bridge forwards the no-value overload
                // one argument short, so its JavaScript runtime reads custom fields as `value`
                // and drops them. Using the value overload keeps the argument positions intact.
                if (payload.Count > 0)
                    GameAnalytics.NewDesignEvent(eventId, value ?? 0f, payload);
                else if (value.HasValue)
                    GameAnalytics.NewDesignEvent(eventId, value.Value);
                else
                    GameAnalytics.NewDesignEvent(eventId);
#else
                if (value.HasValue)
                    GameAnalytics.NewDesignEvent(eventId, value.Value, payload);
                else if (payload.Count > 0)
                    GameAnalytics.NewDesignEvent(eventId, payload);
                else
                    GameAnalytics.NewDesignEvent(eventId);
#endif
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
#if UNITY_WEBGL && !UNITY_EDITOR
                // The score-less WebGL bridge has the same missing-argument bug: custom fields
                // land in the JavaScript `score` slot. A neutral score preserves the payload.
                if (!string.IsNullOrEmpty(progression03))
                {
                    if (payload.Count > 0)
                        GameAnalytics.NewProgressionEvent(
                            sdkStatus, progression01, progression02, progression03, 0, payload);
                    else
                        GameAnalytics.NewProgressionEvent(
                            sdkStatus, progression01, progression02, progression03);
                }
                else if (!string.IsNullOrEmpty(progression02))
                {
                    if (payload.Count > 0)
                        GameAnalytics.NewProgressionEvent(
                            sdkStatus, progression01, progression02, 0, payload);
                    else
                        GameAnalytics.NewProgressionEvent(sdkStatus, progression01, progression02);
                }
                else if (payload.Count > 0)
                {
                    GameAnalytics.NewProgressionEvent(sdkStatus, progression01, 0, payload);
                }
                else
                {
                    GameAnalytics.NewProgressionEvent(sdkStatus, progression01);
                }
#else
                if (!string.IsNullOrEmpty(progression03))
                    GameAnalytics.NewProgressionEvent(
                        sdkStatus, progression01, progression02, progression03, payload);
                else if (!string.IsNullOrEmpty(progression02))
                    GameAnalytics.NewProgressionEvent(
                        sdkStatus, progression01, progression02, payload);
                else
                    GameAnalytics.NewProgressionEvent(sdkStatus, progression01, payload);
#endif
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
            {
                if (string.IsNullOrWhiteSpace(field.Key) || field.Value == null)
                    continue;
#if UNITY_WEBGL && !UNITY_EDITOR
                copy[field.Key] = NormalizeWebGlFieldValue(field.Value);
#else
                copy[field.Key] = field.Value;
#endif
            }
            return copy;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static object NormalizeWebGlFieldValue(object value)
        {
            // SDK 8.0.1 checks `!value` in JavaScript and therefore mistakes valid numeric zero
            // and false for null. Strings preserve the values without generating error events.
            return value switch
            {
                bool boolean when !boolean => "false",
                sbyte number when number == 0 => "0",
                byte number when number == 0 => "0",
                short number when number == 0 => "0",
                ushort number when number == 0 => "0",
                int number when number == 0 => "0",
                uint number when number == 0 => "0",
                long number when number == 0 => "0",
                ulong number when number == 0 => "0",
                float number when number == 0f => "0",
                double number when number == 0d => "0",
                decimal number when number == 0m => "0",
                _ => value
            };
        }
#endif
    }
}
