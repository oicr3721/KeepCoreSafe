using System;
using UnityEngine;

namespace KeepCoreSafe.Settings
{
    public static class AccessibilitySettings
    {
        private const string ColorblindUnlockedKey = "accessibility.colorblind.unlocked";
        private const string ColorblindEnabledKey = "accessibility.colorblind.enabled";

        public static bool ColorblindModeUnlocked =>
            PlayerPrefs.GetInt(ColorblindUnlockedKey, 0) != 0;

        public static bool ColorblindModeEnabled =>
            ColorblindModeUnlocked && PlayerPrefs.GetInt(ColorblindEnabledKey, 0) != 0;

        public static event Action<bool> ColorblindModeUnlockedChanged;
        public static event Action<bool> ColorblindModeEnabledChanged;

        public static bool UnlockColorblindMode()
        {
            if (ColorblindModeUnlocked)
                return false;

            PlayerPrefs.SetInt(ColorblindUnlockedKey, 1);
            PlayerPrefs.Save();
            ColorblindModeUnlockedChanged?.Invoke(true);
            return true;
        }

        public static void SetColorblindModeEnabled(bool enabled)
        {
            if (enabled)
                UnlockColorblindMode();

            bool nextValue = enabled && ColorblindModeUnlocked;
            if (ColorblindModeEnabled == nextValue)
                return;

            PlayerPrefs.SetInt(ColorblindEnabledKey, nextValue ? 1 : 0);
            PlayerPrefs.Save();
            ColorblindModeEnabledChanged?.Invoke(nextValue);
        }
    }
}
