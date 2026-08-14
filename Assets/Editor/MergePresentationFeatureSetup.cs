using KeepCoreSafe.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class MergePresentationFeatureSetup
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        private const string MaterialFolder = "Assets/Materials";
        private const string PrefabFolder = "Assets/Prefabs/Presentation";
        private const string MaskShaderPath = "Assets/Shaders/MergeWhiteMask.shader";
        private const string MaskMaterialPath = MaterialFolder + "/MergeWhiteMask.mat";
        private const string BurstPrefabPath = PrefabFolder + "/MergeBurstParticles.prefab";
        private const string PulsePrefabPath = PrefabFolder + "/CoreEnergyPulse.prefab";
        private const string ShockwavePrefabPath = PrefabFolder + "/CoreShockwave.prefab";
        private const string ParticleMaterialPath = MaterialFolder + "/CoreShockwave.mat";

        [MenuItem("Keep Core Safe/Setup Merge Presentation")]
        public static void Setup()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            Material maskMaterial = GetOrCreateMaskMaterial();
            ParticleSystem burstPrefab = CreateBurstPrefab();
            CoreEnergyPulseView pulsePrefab = LoadPrefabComponent<CoreEnergyPulseView>(PulsePrefabPath);
            ShockwaveRingView shockwavePrefab = LoadPrefabComponent<ShockwaveRingView>(ShockwavePrefabPath);

            if (maskMaterial == null || burstPrefab == null || pulsePrefab == null || shockwavePrefab == null)
                throw new System.InvalidOperationException("Merge presentation assets could not be prepared.");

            foreach (string scenePath in ScenePaths)
                SetupScene(scenePath, maskMaterial, pulsePrefab, shockwavePrefab, burstPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MERGE_PRESENTATION_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Merge Presentation")]
        public static void Validate()
        {
            foreach (string scenePath in ScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                PlacementController placement =
                    Object.FindFirstObjectByType<PlacementController>(FindObjectsInactive.Include);
                MergePresentationController presentation =
                    Object.FindFirstObjectByType<MergePresentationController>(FindObjectsInactive.Include);
                if (placement == null || presentation == null)
                    throw new System.InvalidOperationException($"{scenePath} has no merge presentation setup.");

                SerializedObject placementData = new(placement);
                SerializedObject presentationData = new(presentation);
                if (placementData.FindProperty("mergePresentation").objectReferenceValue != presentation
                    || presentationData.FindProperty("whiteMaskMaterial").objectReferenceValue == null
                    || presentationData.FindProperty("energyPulsePrefab").objectReferenceValue == null
                    || presentationData.FindProperty("shockwavePrefab").objectReferenceValue == null
                    || presentationData.FindProperty("burstParticlesPrefab").objectReferenceValue == null
                    || presentationData.FindProperty("effectRoot").objectReferenceValue == null)
                {
                    throw new System.InvalidOperationException($"{scenePath} has incomplete merge presentation references.");
                }
            }

            Debug.Log("MERGE_PRESENTATION_VALIDATION_COMPLETE");
        }

        private static void SetupScene(
            string scenePath,
            Material maskMaterial,
            CoreEnergyPulseView pulsePrefab,
            ShockwaveRingView shockwavePrefab,
            ParticleSystem burstPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            PlacementController placement =
                Object.FindFirstObjectByType<PlacementController>(FindObjectsInactive.Include);
            if (placement == null)
                throw new System.InvalidOperationException($"PlacementController was not found in {scenePath}.");

            Transform presentationTransform = placement.transform.Find("Merge Presentation");
            GameObject presentationObject = presentationTransform != null
                ? presentationTransform.gameObject
                : new GameObject("Merge Presentation", typeof(MergePresentationController));
            presentationObject.transform.SetParent(placement.transform, false);

            MergePresentationController presentation =
                presentationObject.GetComponent<MergePresentationController>();
            if (presentation == null)
                presentation = presentationObject.AddComponent<MergePresentationController>();

            Transform effectRoot = presentationObject.transform.Find("Effects");
            if (effectRoot == null)
            {
                GameObject effects = new("Effects");
                effects.transform.SetParent(presentationObject.transform, false);
                effectRoot = effects.transform;
            }

            SerializedObject presentationData = new(presentation);
            presentationData.FindProperty("whiteMaskMaterial").objectReferenceValue = maskMaterial;
            presentationData.FindProperty("energyPulsePrefab").objectReferenceValue = pulsePrefab;
            presentationData.FindProperty("shockwavePrefab").objectReferenceValue = shockwavePrefab;
            presentationData.FindProperty("burstParticlesPrefab").objectReferenceValue = burstPrefab;
            presentationData.FindProperty("effectRoot").objectReferenceValue = effectRoot;
            presentationData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject placementData = new(placement);
            placementData.FindProperty("mergePresentation").objectReferenceValue = presentation;
            placementData.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Material GetOrCreateMaskMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(MaskShaderPath);
            if (shader == null)
                return null;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaskMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "MergeWhiteMask" };
                AssetDatabase.CreateAsset(material, MaskMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static ParticleSystem CreateBurstPrefab()
        {
            GameObject root = new("MergeBurstParticles", typeof(ParticleSystem));
            ParticleSystem particles = root.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.startLifetime = 0.34f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.3f, 0.9f, 1f, 1f),
                Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.16f;
            shape.radiusThickness = 1f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.15f, 0.75f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 62;
            Material particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            if (particleMaterial != null)
                renderer.sharedMaterial = particleMaterial;

            PrefabUtility.SaveAsPrefabAsset(root, BurstPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(BurstPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return LoadPrefabComponent<ParticleSystem>(BurstPrefabPath);
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
