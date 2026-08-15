#if UNITY_EDITOR
using System;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class ParticleAndSupplySequenceSetup
    {
        private const string DustPrefabPath = "Assets/Prefabs/Particle/Dust Particle System.prefab";
        private const string PulsePrefabPath = "Assets/Prefabs/Presentation/CoreEnergyPulse.prefab";
        private const string ShockwavePrefabPath = "Assets/Prefabs/Presentation/CoreShockwave.prefab";
        private const string BurstPrefabPath = "Assets/Prefabs/Particle/MergeBurstParticles.prefab";

        private static readonly string[] BlockPrefabPaths =
        {
            "Assets/Prefabs/Blocks/AttackBlock.prefab",
            "Assets/Prefabs/Blocks/CoreBlock.prefab",
            "Assets/Prefabs/Blocks/HealerBlock.prefab",
            "Assets/Prefabs/Blocks/SupplyBlock.prefab",
            "Assets/Prefabs/Blocks/SupportBlock.prefab",
            "Assets/Prefabs/Blocks/WallBlock.prefab"
        };

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        [MenuItem("Keep Core Safe/Setup/Particles And Supply Spawn Sequence")]
        public static void Apply()
        {
            ConfigureBlockPrefabs();
            foreach (string scenePath in ScenePaths)
                ConfigureScene(scenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("PARTICLE_AND_SUPPLY_SEQUENCE_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Particles And Supply Spawn Sequence")]
        public static void Validate()
        {
            foreach (string prefabPath in BlockPrefabPaths)
                ValidateBlockPrefab(prefabPath);
            foreach (string scenePath in ScenePaths)
                ValidateScene(scenePath);

            Debug.Log("PARTICLE_AND_SUPPLY_SEQUENCE_VALIDATION_COMPLETE");
        }

        private static void ConfigureBlockPrefabs()
        {
            GameObject dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DustPrefabPath);
            if (dustPrefab == null)
                throw new InvalidOperationException("Required Dust particle prefab is missing.");

            foreach (string prefabPath in BlockPrefabPaths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    DamageFeedback feedback = root.GetComponent<DamageFeedback>();
                    if (feedback == null)
                        throw new InvalidOperationException($"{prefabPath} has no DamageFeedback.");

                    ParticleSystem dust = GetOrCreateParticleChild(root.transform, dustPrefab, "Dust Hit Particles");
                    SerializedObject feedbackSerialized = new(feedback);
                    feedbackSerialized.FindProperty("hitParticles").objectReferenceValue = dust;
                    feedbackSerialized.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static ParticleSystem GetOrCreateParticleChild(
            Transform parent,
            GameObject prefab,
            string childName)
        {
            Transform child = parent.Find(childName);
            GameObject instance = child != null
                ? child.gameObject
                : PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate {prefab.name}.");

            instance.name = childName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            ParticleSystem particles = instance.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }

        private static void ConfigureScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            ShopEventController shop = UnityEngine.Object.FindFirstObjectByType<ShopEventController>(
                FindObjectsInactive.Include);
            if (game == null || shop == null)
                throw new InvalidOperationException($"{scenePath} is missing GameManager or ShopEventController.");

            SupplySpawnPresentationController presentation =
                shop.GetComponent<SupplySpawnPresentationController>();
            if (presentation == null)
                presentation = shop.gameObject.AddComponent<SupplySpawnPresentationController>();

            SerializedObject presentationSerialized = new(presentation);
            presentationSerialized.FindProperty("energyPulsePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(PulsePrefabPath).GetComponent<CoreEnergyPulseView>();
            presentationSerialized.FindProperty("shockwavePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(ShockwavePrefabPath).GetComponent<ShockwaveRingView>();
            presentationSerialized.FindProperty("burstParticlesPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(BurstPrefabPath).GetComponent<ParticleSystem>();
            presentationSerialized.FindProperty("effectRoot").objectReferenceValue = presentation.transform;
            presentationSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject shopSerialized = new(shop);
            shopSerialized.FindProperty("spawnPresentation").objectReferenceValue = presentation;
            shopSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject gameSerialized = new(game);
            gameSerialized.FindProperty("shopEventController").objectReferenceValue = shop;
            gameSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ValidateBlockPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            DamageFeedback feedback = prefab != null ? prefab.GetComponent<DamageFeedback>() : null;
            SerializedObject feedbackSerialized = feedback != null ? new SerializedObject(feedback) : null;
            ParticleSystem dust = feedbackSerialized?.FindProperty("hitParticles").objectReferenceValue
                as ParticleSystem;
            if (dust == null || dust.main.playOnAwake)
                throw new InvalidOperationException($"{prefabPath} has incomplete Dust hit feedback.");

        }

        private static void ValidateScene(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            ShopEventController shop = UnityEngine.Object.FindFirstObjectByType<ShopEventController>(
                FindObjectsInactive.Include);
            SupplySpawnPresentationController presentation = shop != null
                ? shop.GetComponent<SupplySpawnPresentationController>()
                : null;
            if (game == null || shop == null || presentation == null)
                throw new InvalidOperationException($"{scenePath} has incomplete supply presentation objects.");

            SerializedObject gameSerialized = new(game);
            SerializedObject shopSerialized = new(shop);
            SerializedObject presentationSerialized = new(presentation);
            if (gameSerialized.FindProperty("shopEventController").objectReferenceValue != shop
                || shopSerialized.FindProperty("spawnPresentation").objectReferenceValue != presentation
                || presentationSerialized.FindProperty("energyPulsePrefab").objectReferenceValue == null
                || presentationSerialized.FindProperty("shockwavePrefab").objectReferenceValue == null
                || presentationSerialized.FindProperty("burstParticlesPrefab").objectReferenceValue == null)
            {
                throw new InvalidOperationException($"{scenePath} has incomplete supply presentation references.");
            }
        }
    }
}
#endif
