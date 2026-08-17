using System;
using System.IO;
using GameAnalyticsSDK;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    public sealed class AnalyticsPlaytestBuildProcessor :
        IPreprocessBuildWithReport,
        IPostprocessBuildWithReport
    {
        private const string ConsentPrefabPath =
            "Assets/Resources/UI/Analytics Consent Prompt.prefab";
        private const string PrivacyNoticePath =
            "Markdown/PLAYTEST_PRIVACY_NOTICE.txt";

        public int callbackOrder => 0;

        [MenuItem("Tools/Analytics/Validate Playtest Build")]
        public static void ValidateMenu()
        {
            ValidateWindowsConfiguration();
            Debug.Log("[Playtest Build] GameAnalytics credentials and privacy assets are ready.");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows
                && report.summary.platform != BuildTarget.StandaloneWindows64)
            {
                return;
            }

            ValidateWindowsConfiguration();
        }

        private static void ValidateWindowsConfiguration()
        {
            if (string.IsNullOrWhiteSpace(PlayerSettings.companyName)
                || string.Equals(
                    PlayerSettings.companyName,
                    "DefaultCompany",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException(
                    "Player Settings still uses 'DefaultCompany'. Set the final developer/studio "
                    + "name in Edit > Project Settings > Player before the first external test. "
                    + "Changing it later can move PlayerPrefs and persistent analytics data to a "
                    + "different Windows storage location.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ConsentPrefabPath) == null)
                throw new BuildFailedException(
                    $"Playtest build requires the analytics consent prefab: {ConsentPrefabPath}");

            if (!File.Exists(PrivacyNoticeAbsolutePath))
                throw new BuildFailedException(
                    $"Playtest build requires its privacy notice: {PrivacyNoticePath}");

            GameAnalyticsSDK.Setup.Settings settings = GameAnalytics.SettingsGA;
            int platformIndex = settings != null
                ? settings.Platforms.IndexOf(RuntimePlatform.WindowsPlayer)
                : -1;
            if (platformIndex < 0)
                throw MissingCredentials(
                    "WindowsPlayer is not configured. Add the Windows platform.");

            bool hasKey = !string.IsNullOrWhiteSpace(settings.GetGameKey(platformIndex));
            bool hasSecret = !string.IsNullOrWhiteSpace(settings.GetSecretKey(platformIndex));
            bool hasBuild = platformIndex < settings.Build.Count
                            && !string.IsNullOrWhiteSpace(settings.Build[platformIndex]);
            if (!hasKey || !hasSecret || !hasBuild)
                throw MissingCredentials(
                    "Game Key, Secret Key, and Build must all be set for WindowsPlayer.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows
                && report.summary.platform != BuildTarget.StandaloneWindows64)
            {
                return;
            }

            string outputDirectory = Path.GetDirectoryName(report.summary.outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
                return;

            string source = PrivacyNoticeAbsolutePath;
            string destination = Path.Combine(outputDirectory, "PLAYTEST_PRIVACY_NOTICE.txt");
            File.Copy(source, destination, true);
            Debug.Log($"[Playtest Build] Privacy notice copied to {destination}");
        }

        private static BuildFailedException MissingCredentials(string detail)
        {
            return new BuildFailedException(
                "GameAnalytics playtest configuration is incomplete. " + detail + "\n"
                + "Open Window > GameAnalytics > Select Settings, then configure the Setup tab. "
                + "The build was stopped so a playtest executable cannot be distributed without "
                + "the intended analytics configuration.");
        }

        private static string PrivacyNoticeAbsolutePath => Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath,
            "Markdown",
            "PLAYTEST_PRIVACY_NOTICE.txt");
    }
}
