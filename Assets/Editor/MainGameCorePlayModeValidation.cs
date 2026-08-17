#if UNITY_EDITOR
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    [InitializeOnLoad]
    public static class MainGameCorePlayModeValidation
    {
        private const string RunningKey = "KeepCoreSafe.MainGameCoreValidation.Running";
        private static double startedAt;
        private static bool shockwaveStarted;
        private static CoreBlock validatedCore;
        private static Sprite authoredSprite;
        private static int authoredChildCount;
        private static string failure;

        static MainGameCorePlayModeValidation()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.delayCall += ResumeAfterReload;
        }

        [MenuItem("Keep Core Safe/Validate/Main Game Core Play Mode")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
            SessionState.SetBool(RunningKey, true);
            ResetState();
            HookCallbacks();
            EditorApplication.EnterPlaymode();
        }

        private static void ResumeAfterReload()
        {
            ResetState();
            HookCallbacks();
        }

        private static void ResetState()
        {
            startedAt = EditorApplication.timeSinceStartup;
            shockwaveStarted = false;
            validatedCore = null;
            authoredSprite = null;
            authoredChildCount = 0;
            failure = null;
        }

        private static void HookCallbacks()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= HandleLog;
            EditorApplication.update += Tick;
            Application.logMessageReceived += HandleLog;
        }

        private static void Tick()
        {
            double elapsed = EditorApplication.timeSinceStartup - startedAt;
            if (elapsed > 12d)
            {
                Finish(false, failure ?? "Main Game Core validation timed out.");
                return;
            }

            if (!EditorApplication.isPlaying || elapsed < 0.75d)
                return;

            if (!shockwaveStarted)
            {
                validatedCore = GridManager.Instance?.Grid?.Core as CoreBlock;
                if (validatedCore == null || validatedCore.VisualRenderer == null)
                {
                    Finish(false, "GameScene did not instantiate the In-Game Core prefab.");
                    return;
                }

                authoredSprite = validatedCore.VisualRenderer.sprite;
                authoredChildCount = validatedCore.GetComponentsInChildren<Transform>(true).Length;
                validatedCore.TakeDamage(1);
                if (!HasPreservedPrefabVisual())
                {
                    Finish(false, "Core damage replaced the In-Game Core prefab visual or hierarchy.");
                    return;
                }

                if (GameManager.Instance == null || !GameManager.Instance.TryStartCombat())
                {
                    Finish(false, "Could not start Combat for Core Shockwave validation.");
                    return;
                }

                GameManager.Instance.TriggerEnergyShockwave();
                shockwaveStarted = true;
                return;
            }

            if (elapsed < 3.5d)
                return;

            if (!HasPreservedPrefabVisual())
            {
                Finish(false, "Shockwave replaced the In-Game Core prefab visual or hierarchy.");
                return;
            }

            Finish(true, "MAIN_GAME_CORE_PLAYMODE_VALIDATION_COMPLETE");
        }

        private static bool HasPreservedPrefabVisual()
        {
            return validatedCore != null
                && validatedCore.VisualRenderer != null
                && validatedCore.VisualRenderer.sprite == authoredSprite
                && validatedCore.GetComponentsInChildren<Transform>(true).Length == authoredChildCount;
        }

        private static void HandleLog(string condition, string _, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                && string.IsNullOrEmpty(failure))
            {
                failure = condition;
            }
        }

        private static void Finish(bool success, string message)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= HandleLog;
            SessionState.EraseBool(RunningKey);
            if (success)
                Debug.Log(message);
            else
                Debug.LogError(message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
#endif
