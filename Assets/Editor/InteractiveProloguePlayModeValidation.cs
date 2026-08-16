#if UNITY_EDITOR
using System;
using System.Reflection;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    [InitializeOnLoad]
    public static class InteractiveProloguePlayModeValidation
    {
        private const string RunningKey = "KeepCoreSafe.InteractivePrologueValidation.Running";
        private const string PlacementKey = "KeepCoreSafe.InteractivePrologueValidation.Placement";
        private const string PrologueRequestedKey = "KeepCoreSafe.InteractivePrologueValidation.PrologueRequested";
        private static double startedAt;
        private static bool placementTriggered;
        private static bool prologueRequested;
        private static string failure;

        static InteractiveProloguePlayModeValidation()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.delayCall += ResumeAfterReload;
        }

        [MenuItem("Keep Core Safe/Validate/Interactive Prologue Play Mode")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TutorialScene.unity", OpenSceneMode.Single);
            startedAt = EditorApplication.timeSinceStartup;
            placementTriggered = false;
            prologueRequested = false;
            failure = null;
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(PlacementKey, false);
            SessionState.SetBool(PrologueRequestedKey, false);
            HookCallbacks();
            EditorApplication.EnterPlaymode();
        }

        private static void ResumeAfterReload()
        {
            startedAt = EditorApplication.timeSinceStartup;
            placementTriggered = SessionState.GetBool(PlacementKey, false);
            prologueRequested = SessionState.GetBool(PrologueRequestedKey, false);
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
            if (elapsed > 20d)
            {
                Finish(false, failure ?? "Interactive Prologue play-mode validation timed out.");
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            string activeScene = SceneManager.GetActiveScene().name;
            if (!prologueRequested && activeScene == "TutorialScene" && elapsed > 0.5d)
            {
                SceneLoader.Load(SceneType.Prologue);
                prologueRequested = true;
                SessionState.SetBool(PrologueRequestedKey, true);
                return;
            }

            if (!placementTriggered && activeScene == "PrologueScene" && elapsed > 2d)
            {
                PrologueDirector director = UnityEngine.Object.FindFirstObjectByType<PrologueDirector>();
                GridManager grid = UnityEngine.Object.FindFirstObjectByType<GridManager>();
                if (director == null || grid == null)
                {
                    Finish(false, "PrologueDirector or GridManager did not start.");
                    return;
                }

                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo select = typeof(PrologueDirector).GetMethod("SelectPlacedLily", flags);
                MethodInfo place = typeof(PrologueDirector).GetMethod("PlaceLily", flags);
                FieldInfo inputEnabled = typeof(PrologueDirector).GetField("inputEnabled", flags);
                FieldInfo lilySelected = typeof(PrologueDirector).GetField("lilySelected", flags);
                FieldInfo lilyPlaced = typeof(PrologueDirector).GetField("lilyPlaced", flags);
                FieldInfo coreTransform = typeof(PrologueDirector).GetField("coreTransform", flags);
                FieldInfo cameraController = typeof(PrologueDirector).GetField("cameraController", flags);
                FieldInfo cameraOffset = typeof(PrologueDirector).GetField("cameraOffset", flags);
                if (select == null || place == null || inputEnabled == null
                    || lilySelected == null || lilyPlaced == null || coreTransform == null
                    || cameraController == null || cameraOffset == null)
                {
                    Finish(false, "Prologue interaction methods are unavailable.");
                    return;
                }

                if (!(bool)inputEnabled.GetValue(director))
                    return;

                Transform core = coreTransform.GetValue(director) as Transform;
                GameCameraController camera = cameraController.GetValue(director) as GameCameraController;
                Vector2 offset = (Vector2)cameraOffset.GetValue(director);
                Vector3 expectedCameraCenter = core.position + (Vector3)offset;
                if (camera == null
                    || Vector2.Distance(camera.transform.position, expectedCameraCenter) > 0.01f)
                {
                    Finish(false, "Prologue camera is not centered on Core plus its configured offset.");
                    return;
                }

                select.Invoke(director, null);
                if (!(bool)lilySelected.GetValue(director) || (bool)lilyPlaced.GetValue(director))
                {
                    Finish(false, "Clicking placed Lily did not enter selection directly.");
                    return;
                }
                Vector2Int coreCell = new(grid.Width / 2, grid.Height / 2);
                Vector2Int temporaryCell = coreCell + Vector2Int.left;
                place.Invoke(director, new object[] { temporaryCell });
                if (SceneManager.GetActiveScene().name != "PrologueScene")
                {
                    Finish(false, "Placing Lily outside the Core completed the Prologue.");
                    return;
                }
                select.Invoke(director, null);
                place.Invoke(director, new object[] { coreCell });
                placementTriggered = true;
                SessionState.SetBool(PlacementKey, true);
            }

            if (SceneManager.GetActiveScene().name == "GameScene")
            {
                SceneTransition transition = SceneTransition.Instance;
                if (transition == null)
                {
                    Finish(false, "Persistent SceneTransition was lost before GameScene.");
                    return;
                }

                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo isLoadingField = typeof(SceneTransition).GetField("isLoading", flags);
                if (isLoadingField == null)
                {
                    Finish(false, "SceneTransition validation members are unavailable.");
                    return;
                }

                if ((bool)isLoadingField.GetValue(transition))
                    return;

                if (string.IsNullOrEmpty(failure))
                    Finish(true, "INTERACTIVE_PROLOGUE_PLAYMODE_VALIDATION_COMPLETE");
                else
                    Finish(false, failure);
            }
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
            SessionState.EraseBool(PlacementKey);
            SessionState.EraseBool(PrologueRequestedKey);
            if (success)
                Debug.Log(message);
            else
                Debug.LogError(message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
#endif
