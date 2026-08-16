#if UNITY_EDITOR
using System;
using System.Reflection;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    [InitializeOnLoad]
    public static class TutorialLilyPlayModeValidation
    {
        private const string RunningKey = "KeepCoreSafe.TutorialLilyValidation.Running";
        private static double startedAt;
        private static string failure;

        static TutorialLilyPlayModeValidation()
        {
            if (SessionState.GetBool(RunningKey, false))
                EditorApplication.delayCall += ResumeAfterReload;
        }

        [MenuItem("Keep Core Safe/Validate/Tutorial Lily Play Mode")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TutorialScene.unity", OpenSceneMode.Single);
            SessionState.SetBool(RunningKey, true);
            startedAt = EditorApplication.timeSinceStartup;
            failure = null;
            HookCallbacks();
            EditorApplication.EnterPlaymode();
        }

        private static void ResumeAfterReload()
        {
            startedAt = EditorApplication.timeSinceStartup;
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
            if (elapsed > 15d)
            {
                Finish(false, failure ?? "Tutorial Lily play-mode validation timed out.");
                return;
            }

            if (!EditorApplication.isPlaying || elapsed < 1.5d)
                return;

            try
            {
                ValidateRuntimeState();
                Finish(true, "TUTORIAL_LILY_PLAYMODE_VALIDATION_COMPLETE");
            }
            catch (Exception exception)
            {
                Finish(false, exception.GetBaseException().Message);
            }
        }

        private static void ValidateRuntimeState()
        {
            TutorialDirector director = UnityEngine.Object.FindFirstObjectByType<TutorialDirector>();
            GridManager grid = GridManager.Instance;
            if (director == null || grid?.Grid?.Core == null)
                throw new InvalidOperationException("Tutorial Director, Grid, or Core did not start.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo lilyTransformField = typeof(TutorialDirector).GetField("lilyTransform", flags);
            FieldInfo lilyOffsetField = typeof(TutorialDirector).GetField("lilyOffsetFromCore", flags);
            FieldInfo dialogueRootField = typeof(TutorialDirector).GetField("dialogueRoot", flags);
            MethodInfo validator = typeof(TutorialDirector).GetMethod("CanPlaceBlockAtLilyCell", flags);
            MethodInfo rejected = typeof(TutorialDirector).GetMethod("HandleBlockPlacementRejected", flags);
            if (lilyTransformField == null || lilyOffsetField == null || dialogueRootField == null
                || validator == null || rejected == null)
            {
                throw new InvalidOperationException("Tutorial Lily validation members are unavailable.");
            }

            Transform lily = lilyTransformField.GetValue(director) as Transform;
            Vector2Int offset = (Vector2Int)lilyOffsetField.GetValue(director);
            Vector2Int lilyCell = grid.Grid.Core.GridPosition + offset;
            if (lily == null || Vector3.Distance(lily.position, grid.GridToWorld(lilyCell)) > 0.001f)
                throw new InvalidOperationException("Tutorial Lily is not centered on its configured Grid cell.");

            bool allowsLilyCell = (bool)validator.Invoke(
                director,
                new object[] { null as BlockData, lilyCell });
            bool allowsAdjacentCell = (bool)validator.Invoke(
                director,
                new object[] { null as BlockData, lilyCell + Vector2Int.up });
            if (allowsLilyCell || !allowsAdjacentCell)
                throw new InvalidOperationException("Tutorial Lily placement validation returned an invalid result.");

            GameObject dialogueRoot = dialogueRootField.GetValue(director) as GameObject;
            if (dialogueRoot == null)
                throw new InvalidOperationException("Tutorial Lily dialogue reference is missing.");

            dialogueRoot.SetActive(false);
            rejected.Invoke(director, new object[] { null as BlockData, lilyCell });
            if (!dialogueRoot.activeSelf)
                throw new InvalidOperationException("Rejected Lily-cell placement did not start the dialogue reaction.");

            if (!grid.IsCellEmpty(lilyCell))
                throw new InvalidOperationException("A Block occupies the Tutorial Lily cell.");
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
            if (success && string.IsNullOrEmpty(failure))
                Debug.Log(message);
            else
                Debug.LogError(failure ?? message);
            EditorApplication.Exit(success && string.IsNullOrEmpty(failure) ? 0 : 1);
        }
    }
}
#endif
