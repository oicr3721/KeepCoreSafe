using System;
using System.IO;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class EnemySpawnIndicatorFeatureSetup
    {
        private const string PrefabPath =
            "Assets/Prefabs/Presentation/Enemy Spawn Indicator.prefab";
        private const string MaterialPath = "Assets/Materials/CoreShockwave.mat";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        [MenuItem("Keep Core Safe/Setup Enemy Spawn Indicators")]
        public static void Setup()
        {
            GameObject prefab = CreatePrefab();
            foreach (string scenePath in ScenePaths)
                ConfigureScene(scenePath, prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ENEMY_SPAWN_INDICATOR_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Enemy Spawn Indicators")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EnemySpawnIndicatorView prefabView = prefab != null
                ? prefab.GetComponent<EnemySpawnIndicatorView>()
                : null;
            if (prefabView == null)
                throw new InvalidOperationException("Enemy spawn indicator prefab is missing.");

            SerializedObject view = new(prefabView);
            LineRenderer ring = view.FindProperty("ringRenderer").objectReferenceValue
                as LineRenderer;
            Color markerColor = view.FindProperty("markerColor").colorValue;
            if (ring == null || ring.useWorldSpace || ring.positionCount < 32 || markerColor.r <= markerColor.g)
                throw new InvalidOperationException("Enemy spawn indicator is not a red world-space ring.");

            foreach (string scenePath in ScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                WaveManager manager = UnityEngine.Object.FindFirstObjectByType<WaveManager>(
                    FindObjectsInactive.Include);
                GameObject configured = manager != null
                    ? new SerializedObject(manager).FindProperty("spawnIndicatorPrefab")
                        .objectReferenceValue as GameObject
                    : null;
                if (configured != prefab)
                    throw new InvalidOperationException($"{scenePath} has no spawn indicator prefab.");
            }

            string waveSource = File.ReadAllText("Assets/Scripts/Managers/WaveManager.cs");
            string gameSource = File.ReadAllText("Assets/Scripts/Managers/GameManager.cs");
            if (!waveSource.Contains("preparedSpawns")
                || !waveSource.Contains("PreparedSpawn prepared = preparedSpawns[i]")
                || !waveSource.Contains("HidePreparedSpawnIndicators()")
                || !gameSource.Contains("PrepareNextWave()"))
            {
                throw new InvalidOperationException(
                    "Preparation planning and combat spawning do not share the prepared positions.");
            }

            Debug.Log("ENEMY_SPAWN_INDICATOR_VALIDATION_COMPLETE");
        }

        private static GameObject CreatePrefab()
        {
            GameObject root = new("Enemy Spawn Indicator");
            try
            {
                LineRenderer ring = root.AddComponent<LineRenderer>();
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = 64;
                ring.widthMultiplier = 0.07f;
                ring.numCornerVertices = 3;
                ring.numCapVertices = 3;
                ring.sortingOrder = 45;
                ring.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = Mathf.PI * 2f * i / ring.positionCount;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.5f);
                }

                EnemySpawnIndicatorView view = root.AddComponent<EnemySpawnIndicatorView>();
                SerializedObject serialized = new(view);
                serialized.FindProperty("ringRenderer").objectReferenceValue = ring;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void ConfigureScene(string scenePath, GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WaveManager manager = UnityEngine.Object.FindFirstObjectByType<WaveManager>(
                FindObjectsInactive.Include);
            if (manager == null)
                throw new InvalidOperationException($"{scenePath} has no WaveManager.");

            SerializedObject serialized = new(manager);
            SerializedProperty property = serialized.FindProperty("spawnIndicatorPrefab");
            property.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            if (property.objectReferenceValue != prefab)
                throw new InvalidOperationException($"Could not assign spawn indicator in {scenePath}.");

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
