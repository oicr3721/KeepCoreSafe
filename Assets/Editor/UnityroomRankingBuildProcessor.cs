using System;
using System.Linq;
using KeepCoreSafe.Ranking;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using unityroom.Api;

namespace KeepCoreSafe.Editor
{
    internal static class UnityroomRankingLocalSettings
    {
        internal const string HmacEnvironmentVariable = "UNITYROOM_HMAC_KEY";
        internal const string BoardEnvironmentVariable = "UNITYROOM_BOARD_NO";
        private const string HmacEditorPrefsKey = "KeepCoreSafe.Unityroom.HmacKey";
        private const string BoardEditorPrefsKey = "KeepCoreSafe.Unityroom.BoardNo";

        internal static string HmacKey
        {
            get
            {
                string environmentValue = Environment.GetEnvironmentVariable(HmacEnvironmentVariable);
                return !string.IsNullOrWhiteSpace(environmentValue)
                    ? environmentValue.Trim()
                    : EditorPrefs.GetString(HmacEditorPrefsKey, string.Empty);
            }
        }

        internal static int BoardNo
        {
            get
            {
                string environmentValue = Environment.GetEnvironmentVariable(BoardEnvironmentVariable);
                return int.TryParse(environmentValue, out int environmentBoard) && environmentBoard > 0
                    ? environmentBoard
                    : Mathf.Max(1, EditorPrefs.GetInt(BoardEditorPrefsKey, 1));
            }
        }

        internal static void Save(string hmacKey, int boardNo)
        {
            EditorPrefs.SetString(HmacEditorPrefsKey, hmacKey?.Trim() ?? string.Empty);
            EditorPrefs.SetInt(BoardEditorPrefsKey, Mathf.Max(1, boardNo));
        }

        internal static void Clear()
        {
            EditorPrefs.DeleteKey(HmacEditorPrefsKey);
            EditorPrefs.DeleteKey(BoardEditorPrefsKey);
        }
    }

    public sealed class UnityroomRankingSettingsWindow : EditorWindow
    {
        private string hmacKey;
        private int boardNo;

        [MenuItem("Tools/KeepCoreSafe/Unityroom Ranking/Local Build Settings")]
        private static void Open()
        {
            GetWindow<UnityroomRankingSettingsWindow>(true, "Unityroom Ranking", true);
        }

        private void OnEnable()
        {
            hmacKey = UnityroomRankingLocalSettings.HmacKey;
            boardNo = UnityroomRankingLocalSettings.BoardNo;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unityroom WebGL Build Credentials", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Saved in this machine's EditorPrefs, not in project assets or Git. "
                + "Environment variables override these values for CI builds.",
                MessageType.Info);
            hmacKey = EditorGUILayout.PasswordField("HMAC Key", hmacKey);
            boardNo = EditorGUILayout.IntField("Board No", boardNo);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(hmacKey) || boardNo < 1))
            {
                if (GUILayout.Button("Save Local Settings"))
                {
                    UnityroomRankingLocalSettings.Save(hmacKey, boardNo);
                    ShowNotification(new GUIContent("Saved outside the project."));
                }
            }

            if (GUILayout.Button("Clear Local Settings"))
            {
                UnityroomRankingLocalSettings.Clear();
                hmacKey = string.Empty;
                boardNo = 1;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("CI environment variables", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(
                UnityroomRankingLocalSettings.HmacEnvironmentVariable + "\n"
                + UnityroomRankingLocalSettings.BoardEnvironmentVariable,
                GUILayout.Height(38f));
        }
    }

    public sealed class UnityroomRankingBuildProcessor :
        IPreprocessBuildWithReport,
        IProcessSceneWithReport,
        IPostprocessBuildWithReport
    {
        private static bool isUnityroomBuild;
        private static bool runtimeInjected;
        private static string hmacKey;
        private static int boardNo;

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            isUnityroomBuild = IsUnityroomProfile(report.summary.platform);
            runtimeInjected = false;
            hmacKey = null;
            boardNo = 0;
            if (!isUnityroomBuild)
                return;

            hmacKey = UnityroomRankingLocalSettings.HmacKey;
            boardNo = UnityroomRankingLocalSettings.BoardNo;
            if (string.IsNullOrWhiteSpace(hmacKey))
            {
                throw new BuildFailedException(
                    "Unityroom HMAC key is missing. Open Tools > KeepCoreSafe > Unityroom Ranking > "
                    + "Local Build Settings, or set the UNITYROOM_HMAC_KEY environment variable.");
            }

            if (boardNo < 1)
                throw new BuildFailedException("Unityroom Board No must be 1 or greater.");
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!isUnityroomBuild || runtimeInjected)
                return;

            GameObject host = new GameObject("Unityroom Ranking Runtime");
            UnityroomApiClient apiClient = host.AddComponent<UnityroomApiClient>();
            SerializedObject apiObject = new SerializedObject(apiClient);
            SerializedProperty hmacProperty = apiObject.FindProperty("HmacKey");
            if (hmacProperty == null)
                throw new BuildFailedException("The official Unityroom client no longer exposes the expected HmacKey field.");
            hmacProperty.stringValue = hmacKey;
            apiObject.ApplyModifiedPropertiesWithoutUndo();

            UnityroomRankingRuntime runtime = host.AddComponent<UnityroomRankingRuntime>();
            SerializedObject runtimeObject = new SerializedObject(runtime);
            runtimeObject.FindProperty("boardNo").intValue = boardNo;
            runtimeObject.ApplyModifiedPropertiesWithoutUndo();
            runtimeInjected = true;
            Debug.Log($"[Unityroom Ranking] Official runtime client injected for Board No {boardNo}.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (isUnityroomBuild && !runtimeInjected)
                Debug.LogWarning("[Unityroom Ranking] Build completed without injecting the runtime client.");
            isUnityroomBuild = false;
            runtimeInjected = false;
            hmacKey = null;
            boardNo = 0;
        }

        private static bool IsUnityroomProfile(BuildTarget target)
        {
            if (target != BuildTarget.WebGL)
                return false;

            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            return profile != null
                   && profile.scriptingDefines != null
                   && profile.scriptingDefines.Contains("UNITYROOM", StringComparer.Ordinal);
        }
    }
}
