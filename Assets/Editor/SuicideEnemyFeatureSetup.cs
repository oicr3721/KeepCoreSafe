#if UNITY_EDITOR
using System;
using System.Reflection;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class SuicideEnemyFeatureSetup
    {
        private const string MeleePrefabPath = "Assets/Prefabs/Enemies/MeleeEnemy.prefab";
        private const string SuicidePrefabPath = "Assets/Prefabs/Enemies/SuicideEnemy.prefab";
        private const string MeleeDataPath = "Assets/Resources/Data/Enemy/MeleeEnemyData.asset";
        private const string SuicideDataPath = "Assets/Resources/Data/Enemy/SuicideEnemyData.asset";
        private const string ExplosionParticlePath =
            "Assets/Prefabs/Particle/Explosion Particle System.prefab";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        [MenuItem("Keep Core Safe/Setup/Suicide Enemy")]
        public static void Apply()
        {
            ConfigureExplosionParticlePrefab();
            SuicideEnemyData data = GetOrCreateData();
            SuicideEnemy prefab = CreateSuicidePrefab();
            ConfigureData(data, prefab);
            foreach (string scenePath in ScenePaths)
                ConfigureScene(scenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("SUICIDE_ENEMY_FEATURE_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Suicide Enemy")]
        public static void Validate()
        {
            SuicideEnemyData data = AssetDatabase.LoadAssetAtPath<SuicideEnemyData>(SuicideDataPath);
            SuicideEnemy prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SuicidePrefabPath)
                ?.GetComponent<SuicideEnemy>();
            ParticleSystem explosion = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionParticlePath)
                ?.GetComponent<ParticleSystem>();
            if (data == null || data.Prefab != prefab || prefab == null || explosion == null
                || explosion.main.playOnAwake)
            {
                throw new InvalidOperationException("Suicide Enemy data, prefab, or particle setup is incomplete.");
            }

            SerializedObject prefabSerialized = new(prefab);
            if (prefabSerialized.FindProperty("warningRenderer").objectReferenceValue == null)
                throw new InvalidOperationException("Suicide Enemy warning renderer is missing.");

            ValidateBlockEncounterTrigger(data, prefab);
            foreach (string scenePath in ScenePaths)
                ValidateScene(scenePath);

            Debug.Log("SUICIDE_ENEMY_FEATURE_VALIDATION_COMPLETE");
        }

        private static void ValidateBlockEncounterTrigger(
            SuicideEnemyData data,
            SuicideEnemy prefab)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
            GridManager grid = UnityEngine.Object.FindFirstObjectByType<GridManager>(
                FindObjectsInactive.Include);
            if (grid == null)
                throw new InvalidOperationException("GameScene has no GridManager.");

            if (grid.Grid == null)
            {
                typeof(GridManager).GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(grid, null);
            }

            Vector2Int startCell = new(1, 1);
            Vector2Int blockerCell = new(2, 1);
            GameObject blockerObject = new("Suicide Trigger Validation Block");
            WallBlock blocker = blockerObject.AddComponent<WallBlock>();
            SuicideEnemy enemy = null;
            try
            {
                if (!grid.TryPlaceBlock(blocker, blockerCell))
                    throw new InvalidOperationException("Could not place Suicide trigger validation Block.");

                enemy = UnityEngine.Object.Instantiate(
                    prefab,
                    grid.GridToWorld(startCell),
                    Quaternion.identity);
                enemy.Initialize(data, new[] { startCell, blockerCell }, blocker);
                typeof(SuicideEnemy).GetMethod(
                        "Start",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(enemy, null);
                typeof(SuicideEnemy).GetMethod(
                        "OnCombatUpdate",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(enemy, new object[] { 0.02f });

                bool isPreparing = (bool)typeof(SuicideEnemy).GetField(
                        "isPreparingSelfDestruct",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(enemy);
                if (!isPreparing)
                {
                    throw new InvalidOperationException(
                        "Suicide Enemy did not begin self-destruct at a route-blocking Block.");
                }
            }
            finally
            {
                grid.TryRemoveBlock(blockerCell, out _);
                if (enemy != null)
                    UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                UnityEngine.Object.DestroyImmediate(blockerObject);
            }

            Debug.Log("SUICIDE_ENEMY_BLOCK_TRIGGER_VALIDATION_COMPLETE");
        }

        private static void ConfigureExplosionParticlePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ExplosionParticlePath);
            try
            {
                ParticleSystem particles = root.GetComponent<ParticleSystem>();
                if (particles == null)
                    throw new InvalidOperationException("Explosion Particle System prefab is invalid.");

                ParticleSystem.MainModule main = particles.main;
                main.playOnAwake = false;
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                PrefabUtility.SaveAsPrefabAsset(root, ExplosionParticlePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static SuicideEnemyData GetOrCreateData()
        {
            SuicideEnemyData data = AssetDatabase.LoadAssetAtPath<SuicideEnemyData>(SuicideDataPath);
            if (data != null)
                return data;

            data = ScriptableObject.CreateInstance<SuicideEnemyData>();
            AssetDatabase.CreateAsset(data, SuicideDataPath);
            return data;
        }

        private static SuicideEnemy CreateSuicidePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MeleePrefabPath);
            try
            {
                MeleeEnemy source = root.GetComponent<MeleeEnemy>();
                if (source == null)
                    throw new InvalidOperationException("MeleeEnemy prefab has no MeleeEnemy component.");

                SuicideEnemy destination = root.AddComponent<SuicideEnemy>();
                CopyMatchingSerializedProperties(source, destination);
                SerializedObject destinationSerialized = new(destination);
                destinationSerialized.FindProperty("warningRenderer").objectReferenceValue =
                    destinationSerialized.FindProperty("visualRenderer").objectReferenceValue;
                destinationSerialized.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(source, true);
                root.name = "SuicideEnemy";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, SuicidePrefabPath);
                return saved.GetComponent<SuicideEnemy>();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureData(SuicideEnemyData data, SuicideEnemy prefab)
        {
            MeleeEnemyData meleeData = AssetDatabase.LoadAssetAtPath<MeleeEnemyData>(MeleeDataPath);
            if (meleeData == null)
                throw new InvalidOperationException("MeleeEnemyData is missing.");

            CopyMatchingSerializedProperties(meleeData, data);
            SerializedObject serialized = new(data);
            serialized.FindProperty("displayName").stringValue = "enemy.suicide.name";
            serialized.FindProperty("maxHP").intValue = 45;
            serialized.FindProperty("moveSpeed").floatValue = 3.2f;
            serialized.FindProperty("attackDamage").intValue = 4;
            serialized.FindProperty("attackCooldown").floatValue = 0.65f;
            serialized.FindProperty("energyOnDeath").intValue = 4;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("forcedTriggerHealthRatio").floatValue = 0.3f;
            serialized.FindProperty("selfDestructPreparationDuration").floatValue = 1.6f;
            serialized.FindProperty("explosionDamage").intValue = 60;
            SetCueClips(serialized.FindProperty("warningSound"),
                "Assets/Audio/Clips/bleep001.wav",
                "Assets/Audio/Clips/scifi-bleep.wav");
            SetCueClips(serialized.FindProperty("explosionSound"),
                "Assets/Audio/Clips/Destroy2.wav",
                "Assets/Audio/Clips/Destroy3.wav");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static void ConfigureScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WaveManager wave = UnityEngine.Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (wave == null || game == null)
                throw new InvalidOperationException($"{scenePath} is missing WaveManager or GameManager.");

            ExplosionParticleEffectManager manager =
                UnityEngine.Object.FindFirstObjectByType<ExplosionParticleEffectManager>(
                    FindObjectsInactive.Include);
            if (manager == null)
            {
                Transform presentation = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    game.transform,
                    "Presentation");
                Transform poolRoot = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    presentation,
                    "Explosion Particle Pool");
                manager = poolRoot.gameObject.AddComponent<ExplosionParticleEffectManager>();
            }

            ParticleSystem prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionParticlePath)
                .GetComponent<ParticleSystem>();
            SerializedObject managerSerialized = new(manager);
            managerSerialized.FindProperty("effectPrefab").objectReferenceValue = prefab;
            managerSerialized.FindProperty("initialPoolSize").intValue = 12;
            managerSerialized.FindProperty("effectRoot").objectReferenceValue = manager.transform;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ValidateScene(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WaveManager wave = UnityEngine.Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
            ExplosionParticleEffectManager manager =
                UnityEngine.Object.FindFirstObjectByType<ExplosionParticleEffectManager>(
                    FindObjectsInactive.Include);
            SerializedObject managerSerialized = manager != null ? new SerializedObject(manager) : null;
            if (wave == null || manager == null
                || managerSerialized.FindProperty("effectPrefab").objectReferenceValue == null
                || managerSerialized.FindProperty("effectRoot").objectReferenceValue == null)
            {
                throw new InvalidOperationException($"{scenePath} has incomplete Suicide Enemy references.");
            }
        }

        private static void CopyMatchingSerializedProperties(UnityEngine.Object source, UnityEngine.Object destination)
        {
            SerializedObject sourceSerialized = new(source);
            SerializedObject destinationSerialized = new(destination);
            SerializedProperty property = destinationSerialized.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyPath == "m_Script")
                    continue;

                SerializedProperty sourceProperty = sourceSerialized.FindProperty(property.propertyPath);
                if (sourceProperty != null)
                    destinationSerialized.CopyFromSerializedProperty(sourceProperty);
            }

            destinationSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetCueClips(SerializedProperty cue, params string[] paths)
        {
            SerializedProperty clips = cue.FindPropertyRelative("clips");
            clips.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
        }
    }
}
#endif
