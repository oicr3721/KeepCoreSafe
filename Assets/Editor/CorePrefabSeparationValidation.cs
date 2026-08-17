#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class CorePrefabSeparationValidation
    {
        private const string InGameDataPath = "Assets/Resources/Data/Block/CoreData.asset";
        private const string TutorialDataPath = "Assets/Resources/Data/Block/TutorialCoreData.asset";
        private const string PrologueScenePath = "Assets/Scenes/PrologueScene.unity";
        private const string ObsoleteSpriteGuid = "a6b9be541f599064daf92ce1ca91f21f";

        [MenuItem("Keep Core Safe/Validate/Core Prefab Separation")]
        public static void Validate()
        {
            CoreBlockData tutorialData = AssetDatabase.LoadAssetAtPath<CoreBlockData>(TutorialDataPath);
            CoreBlockData inGameData = AssetDatabase.LoadAssetAtPath<CoreBlockData>(InGameDataPath);
            if (tutorialData?.Prefab is not CoreBlock tutorialPrefab
                || inGameData?.Prefab is not CoreBlock inGamePrefab
                || tutorialPrefab == inGamePrefab)
            {
                throw new InvalidOperationException(
                    "Tutorial and In-Game CoreData must reference distinct CoreBlock prefabs.");
            }

            ValidateNoHealthSpriteOverride(tutorialData);
            ValidateNoHealthSpriteOverride(inGameData);
            ValidatePrefabVisualPersistence(tutorialData, false);
            ValidatePrefabVisualPersistence(inGameData, true);
            ValidatePrologueReferences(tutorialData, inGameData);
            ValidateObsoleteSpriteIsUnreferenced();
            Debug.Log("CORE_PREFAB_SEPARATION_VALIDATION_COMPLETE");
        }

        private static void ValidateNoHealthSpriteOverride(CoreBlockData data)
        {
            SerializedProperty stages = new SerializedObject(data).FindProperty("healthStageSprites");
            if (stages == null || stages.arraySize != 0)
            {
                throw new InvalidOperationException(
                    $"{data.name} must not define Core health-stage Sprite overrides.");
            }
        }

        private static void ValidatePrefabVisualPersistence(CoreBlockData data, bool requireAnimatorChild)
        {
            CoreBlock instance = UnityEngine.Object.Instantiate(data.Prefab) as CoreBlock;
            if (instance == null)
                throw new InvalidOperationException($"{data.name} prefab has the wrong component type.");

            try
            {
                SpriteRenderer renderer = instance.VisualRenderer;
                Sprite authoredSprite = renderer != null ? renderer.sprite : null;
                int childCount = instance.GetComponentsInChildren<Transform>(true).Length;
                instance.Initialize(data, false);
                instance.HP.SubtractValue(10);
                instance.UpdateHealthVisual(0.2f);
                if (renderer == null
                    || authoredSprite == null
                    || renderer.sprite != authoredSprite
                    || instance.GetComponentsInChildren<Transform>(true).Length != childCount)
                {
                    throw new InvalidOperationException(
                        $"{data.name} lost its prefab-authored visual or hierarchy after an HP refresh.");
                }

                if (requireAnimatorChild
                    && !instance.GetComponentsInChildren<Animator>(true).Any())
                {
                    throw new InvalidOperationException(
                        "The In-Game Core prefab must preserve its authored Lily Animator child.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance.gameObject);
            }
        }

        private static void ValidatePrologueReferences(
            CoreBlockData tutorialData,
            CoreBlockData inGameData)
        {
            Scene scene = EditorSceneManager.OpenScene(PrologueScenePath, OpenSceneMode.Single);
            PrologueDirector director = UnityEngine.Object.FindFirstObjectByType<PrologueDirector>(
                FindObjectsInactive.Include);
            if (director == null)
                throw new InvalidOperationException("PrologueDirector is missing.");

            SerializedObject serialized = new(director);
            Transform anchor = serialized.FindProperty("coreSpawnAnchor")
                .objectReferenceValue as Transform;
            if (serialized.FindProperty("tutorialCoreData").objectReferenceValue != tutorialData
                || serialized.FindProperty("inGameCoreData").objectReferenceValue != inGameData
                || anchor == null
                || anchor.gameObject.activeSelf
                || anchor.GetComponent<SpriteRenderer>() != null)
            {
                throw new InvalidOperationException(
                    "Prologue must use a visual-free Core anchor and the two CoreData prefab references.");
            }

            _ = scene;
        }

        private static void ValidateObsoleteSpriteIsUnreferenced()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string[] extensions = { "*.asset", "*.prefab", "*.unity", "*.controller", "*.anim" };
            foreach (string extension in extensions)
            {
                foreach (string path in Directory.EnumerateFiles(
                             Application.dataPath,
                             extension,
                             SearchOption.AllDirectories))
                {
                    if (File.ReadAllText(path).Contains(ObsoleteSpriteGuid, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Obsolete Core Sprite is still referenced by {Path.GetRelativePath(projectRoot, path)}.");
                    }
                }
            }
        }
    }
}
#endif
