using System;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class AdditionalCombatFeedbackSetup
    {
        private const string HitParticlePrefabPath =
            "Assets/Prefabs/Presentation/Enemy Hit Particles.prefab";
        private const string ParticleMaterialPath = "Assets/Materials/CoreShockwave.mat";
        private const string MergeSoundPath = "Assets/Audio/Clips/Clear.wav";

        private static readonly string[] EnemyPrefabPaths =
        {
            "Assets/Prefabs/Enemies/MeleeEnemy.prefab",
            "Assets/Prefabs/Enemies/RangedEnemy.prefab"
        };

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        [MenuItem("Keep Core Safe/Setup Additional Combat Feedback")]
        public static void Setup()
        {
            GameObject particlePrefab = CreateHitParticlePrefab();
            foreach (string enemyPath in EnemyPrefabPaths)
                ConfigureEnemyPrefab(enemyPath, particlePrefab);

            AudioClip mergeSound = AssetDatabase.LoadAssetAtPath<AudioClip>(MergeSoundPath);
            if (mergeSound == null)
                throw new InvalidOperationException("Special block merge sound could not be loaded.");
            foreach (string scenePath in ScenePaths)
                ConfigureMergeSound(scenePath, mergeSound);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ADDITIONAL_COMBAT_FEEDBACK_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Additional Combat Feedback")]
        public static void Validate()
        {
            foreach (string enemyPath in EnemyPrefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath);
                DamageFeedback feedback = prefab != null
                    ? prefab.GetComponent<DamageFeedback>()
                    : null;
                ParticleSystem particles = feedback != null
                    ? new SerializedObject(feedback).FindProperty("hitParticles").objectReferenceValue
                        as ParticleSystem
                    : null;
                if (particles == null || particles.main.playOnAwake || particles.main.loop)
                    throw new InvalidOperationException($"{enemyPath} has incomplete hit particles.");
            }

            foreach (string scenePath in ScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                MergePresentationController presentation =
                    UnityEngine.Object.FindFirstObjectByType<MergePresentationController>(
                        FindObjectsInactive.Include);
                AudioClip configuredSound = presentation != null
                    ? new SerializedObject(presentation).FindProperty("specialBlockMergeSound")
                        .objectReferenceValue as AudioClip
                    : null;
                if (configuredSound == null)
                    throw new InvalidOperationException($"{scenePath} has no special merge sound.");
            }

            Debug.Log("ADDITIONAL_COMBAT_FEEDBACK_VALIDATION_COMPLETE");
        }

        private static GameObject CreateHitParticlePrefab()
        {
            GameObject root = new("Enemy Hit Particles", typeof(ParticleSystem));
            try
            {
                ParticleSystem particles = root.GetComponent<ParticleSystem>();
                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
                main.playOnAwake = false;
                main.duration = 0.12f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.42f, 0.05f, 1f),
                    new Color(1f, 1f, 0.62f, 1f));
                main.gravityModifier = 0.12f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 12;

                ParticleSystem.EmissionModule emission = particles.emission;
                emission.enabled = true;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });

                ParticleSystem.ShapeModule shape = particles.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.055f;
                shape.radiusThickness = 1f;

                ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                    particles.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient fade = new();
                fade.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(1f, 1f, 0.72f), 0f),
                        new GradientColorKey(new Color(1f, 0.18f, 0.01f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.8f, 0.55f),
                        new GradientAlphaKey(0f, 1f)
                    });
                colorOverLifetime.color = fade;

                ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                AnimationCurve sizeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.15f);
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

                ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sortingOrder = 25;
                Material material = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
                if (material != null)
                    renderer.sharedMaterial = material;

                PrefabUtility.SaveAsPrefabAsset(root, HitParticlePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(
                HitParticlePrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(HitParticlePrefabPath);
        }

        private static void ConfigureEnemyPrefab(string path, GameObject particlePrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                DamageFeedback feedback = root.GetComponent<DamageFeedback>();
                if (feedback == null)
                    throw new InvalidOperationException($"{path} has no DamageFeedback.");

                Transform existing = root.transform.Find("Enemy Hit Particles");
                ParticleSystem particles;
                if (existing != null)
                {
                    particles = existing.GetComponent<ParticleSystem>();
                }
                else
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(particlePrefab) as GameObject;
                    if (instance == null)
                        throw new InvalidOperationException($"Could not add hit particles to {path}.");
                    instance.transform.SetParent(root.transform, false);
                    particles = instance.GetComponent<ParticleSystem>();
                }

                SerializedObject serialized = new(feedback);
                serialized.FindProperty("hitParticles").objectReferenceValue = particles;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureMergeSound(string scenePath, AudioClip mergeSound)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            MergePresentationController presentation =
                UnityEngine.Object.FindFirstObjectByType<MergePresentationController>(
                    FindObjectsInactive.Include);
            if (presentation == null)
                throw new InvalidOperationException($"{scenePath} has no merge presentation.");

            SerializedObject serialized = new(presentation);
            serialized.FindProperty("specialBlockMergeSound").objectReferenceValue = mergeSound;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
