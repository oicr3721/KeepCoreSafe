#if UNITY_EDITOR
using System;
using System.Reflection;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    [InitializeOnLoad]
    public static class TutorialFinalePlayModeValidation
    {
        private const string RunningKey = "KeepCoreSafe.TutorialFinaleValidation.Running";
        private static double startedAt;
        private static bool finaleStarted;
        private static string failure;

        static TutorialFinalePlayModeValidation()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.delayCall += ResumeAfterReload;
        }

        [MenuItem("Keep Core Safe/Validate/Tutorial Finale Play Mode")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TutorialScene.unity");
            SessionState.SetBool(RunningKey, true);
            startedAt = EditorApplication.timeSinceStartup;
            finaleStarted = false;
            failure = null;
            HookCallbacks();
            EditorApplication.EnterPlaymode();
        }

        private static void ResumeAfterReload()
        {
            startedAt = EditorApplication.timeSinceStartup;
            finaleStarted = false;
            failure = null;
            HookCallbacks();
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
            if (elapsed > 10d)
            {
                Finish(false, failure ?? "Tutorial finale play-mode validation timed out.");
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            TutorialDirector director =
                UnityEngine.Object.FindFirstObjectByType<TutorialDirector>();
            if (director == null)
                return;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo enemyField = typeof(TutorialDirector).GetField("finaleSuicideEnemy", flags);
            FieldInfo coreField = typeof(TutorialDirector).GetField("protectedCore", flags);
            if (!finaleStarted && elapsed > 0.75d)
            {
                MethodInfo begin = typeof(TutorialDirector).GetMethod("BeginFinalePresentation", flags);
                if (begin == null || enemyField == null || coreField == null)
                {
                    Finish(false, "Tutorial finale validation members are unavailable.");
                    return;
                }

                begin.Invoke(director, null);
                SuicideEnemy enemy = enemyField.GetValue(director) as SuicideEnemy;
                CoreBlock core = coreField.GetValue(director) as CoreBlock;
                if (enemy == null || core == null)
                {
                    Finish(false, "Tutorial finale did not spawn the configured Suicide Enemy.");
                    return;
                }

                core.TakeDamage(Mathf.CeilToInt(core.HP.MaxValue) * 2);
                if (core.HP.CurrentValue != 1f || GameManager.Phase == GamePhase.GameOver)
                {
                    Finish(false, "Tutorial finale Core protection did not prevent Game Over.");
                    return;
                }

                finaleStarted = true;
                return;
            }

            if (!finaleStarted || elapsed < 5d)
                return;

            if (enemyField.GetValue(director) as SuicideEnemy != null)
            {
                Finish(false, "Tutorial Suicide Enemy did not reach Lily and explode.");
                return;
            }

            if (GameManager.Phase == GamePhase.GameOver || GridManager.Instance?.Grid?.Core == null)
            {
                Finish(false, "Tutorial finale entered Game Over or destroyed the Core.");
                return;
            }

            Finish(true, "TUTORIAL_FINALE_PLAYMODE_VALIDATION_COMPLETE");
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
