#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using KeepCoreSafe.Data;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class OfferExpansionAndSelectionSetup
    {
        private const string ShopFolder = "Assets/Resources/Data/Shop";
        private const string SupplyEventPath = ShopFolder + "/SupplyEventData.asset";
        private const string BlockButtonPath = "Assets/Prefabs/UI/Block Button.prefab";
        private const string HealerPrefabPath = "Assets/Prefabs/Blocks/HealerBlock.prefab";
        private const string HealParticlePrefabPath = "Assets/Prefabs/Particle/Heal Particle System.prefab";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        private readonly struct RecoveryDefinition
        {
            public readonly string Id;
            public readonly string ColorPath;
            public readonly string BlockPath;

            public RecoveryDefinition(string id)
            {
                Id = id;
                ColorPath = $"Assets/Resources/Data/Block/Colors/{id}.asset";
                BlockPath = $"Assets/Resources/Data/Block/Basic/{id}Basic.asset";
            }
        }

        private static readonly RecoveryDefinition[] RecoveryDefinitions =
        {
            new("Red"),
            new("Blue"),
            new("Green"),
            new("Yellow")
        };

        [MenuItem("Keep Core Safe/Setup/Offer Expansion And Block Selection")]
        public static void Apply()
        {
            ConfigureHealParticlePrefab();
            RemoveLegacyHealerParticle();
            List<ShopOfferData> offers = ConfigureOfferAssets();
            AppendOffersToSupplyEvent(offers);
            ConfigureBlockButton();
            foreach (string scenePath in ScenePaths)
                ConfigureScene(scenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("OFFER_EXPANSION_AND_SELECTION_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Offer Expansion And Block Selection")]
        public static void Validate()
        {
            CoreEnergyShopOfferData energy = AssetDatabase.LoadAssetAtPath<CoreEnergyShopOfferData>(
                ShopFolder + "/CoreEnergyOffer.asset");
            if (energy == null || energy.EnergyAmount != 10)
                throw new InvalidOperationException("Core Energy offer is missing or has an invalid default amount.");

            foreach (RecoveryDefinition definition in RecoveryDefinitions)
            {
                ColorRecoveryShopOfferData offer =
                    AssetDatabase.LoadAssetAtPath<ColorRecoveryShopOfferData>(
                        $"{ShopFolder}/{definition.Id}RecoveryOffer.asset");
                BlockColorData color = AssetDatabase.LoadAssetAtPath<BlockColorData>(definition.ColorPath);
                if (offer == null || offer.TargetColor != color)
                    throw new InvalidOperationException($"{definition.Id} recovery offer is incomplete.");
            }

            ValidateSupplyEvent();
            ValidateBlockButton();
            ValidateHealerPrefab();
            foreach (string scenePath in ScenePaths)
                ValidateScene(scenePath);

            Debug.Log("OFFER_EXPANSION_AND_SELECTION_VALIDATION_COMPLETE");
        }

        private static void ConfigureHealParticlePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HealParticlePrefabPath);
            try
            {
                ParticleSystem particles = root.GetComponent<ParticleSystem>();
                if (particles == null)
                    throw new InvalidOperationException("Heal particle prefab has no ParticleSystem.");

                ParticleSystem.MainModule main = particles.main;
                main.playOnAwake = false;
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                PrefabUtility.SaveAsPrefabAsset(root, HealParticlePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemoveLegacyHealerParticle()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HealerPrefabPath);
            try
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child != root.transform && child.name == "Heal Particles")
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                }

                PrefabUtility.SaveAsPrefabAsset(root, HealerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static List<ShopOfferData> ConfigureOfferAssets()
        {
            List<ShopOfferData> offers = new();
            CoreEnergyShopOfferData energy = GetOrCreateAsset<CoreEnergyShopOfferData>(
                ShopFolder + "/CoreEnergyOffer.asset");
            SerializedObject energySerialized = new(energy);
            SetObjectIfMissing(
                energySerialized.FindProperty("displayImage"),
                AssetDatabase.LoadAssetAtPath<BlockData>("Assets/Resources/Data/Block/CoreData.asset")?.Sprite);
            SetStringIfEmpty(energySerialized.FindProperty("displayName"), "shop.offer.energy.name");
            SetStringIfEmpty(energySerialized.FindProperty("description"), "shop.offer.energy.desc");
            energySerialized.FindProperty("energyAmount").intValue = 10;
            energySerialized.ApplyModifiedPropertiesWithoutUndo();
            offers.Add(energy);

            foreach (RecoveryDefinition definition in RecoveryDefinitions)
            {
                ColorRecoveryShopOfferData offer = GetOrCreateAsset<ColorRecoveryShopOfferData>(
                    $"{ShopFolder}/{definition.Id}RecoveryOffer.asset");
                SerializedObject serialized = new(offer);
                serialized.FindProperty("targetColor").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<BlockColorData>(definition.ColorPath);
                SetObjectIfMissing(
                    serialized.FindProperty("displayImage"),
                    AssetDatabase.LoadAssetAtPath<BlockData>(definition.BlockPath)?.Sprite);
                string keyId = definition.Id.ToLowerInvariant();
                SetStringIfEmpty(serialized.FindProperty("displayName"), $"shop.offer.recovery.{keyId}.name");
                SetStringIfEmpty(serialized.FindProperty("description"), $"shop.offer.recovery.{keyId}.desc");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                offers.Add(offer);
            }

            return offers;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void AppendOffersToSupplyEvent(IReadOnlyList<ShopOfferData> offers)
        {
            ShopEventData eventData = AssetDatabase.LoadAssetAtPath<ShopEventData>(SupplyEventPath);
            if (eventData == null)
                throw new InvalidOperationException("SupplyEventData is missing.");

            SerializedObject serialized = new(eventData);
            SerializedProperty array = serialized.FindProperty("offers");
            foreach (ShopOfferData offer in offers)
            {
                bool exists = false;
                for (int i = 0; i < array.arraySize; i++)
                {
                    if (array.GetArrayElementAtIndex(i).objectReferenceValue == offer)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                    continue;

                int index = array.arraySize;
                array.InsertArrayElementAtIndex(index);
                array.GetArrayElementAtIndex(index).objectReferenceValue = offer;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBlockButton()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BlockButtonPath);
            try
            {
                SupplyBlockButtonView view = root.GetComponent<SupplyBlockButtonView>();
                if (view == null)
                    throw new InvalidOperationException("Block Button has no SupplyBlockButtonView.");

                Transform existing = root.transform.Find("Selected Highlight");
                GameObject highlightObject;
                if (existing == null)
                {
                    highlightObject = new GameObject(
                        "Selected Highlight",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    highlightObject.transform.SetParent(root.transform, false);
                }
                else
                {
                    highlightObject = existing.gameObject;
                }

                RectTransform rect = (RectTransform)highlightObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-5f, -5f);
                rect.offsetMax = new Vector2(5f, 5f);
                rect.localScale = Vector3.one;
                rect.SetAsFirstSibling();

                Image image = highlightObject.GetComponent<Image>();
                Transform rareShine = root.transform.Find("Rare Shine");
                Image rareImage = rareShine != null ? rareShine.GetComponent<Image>() : null;
                if (image.sprite == null)
                    image.sprite = rareImage != null ? rareImage.sprite : root.GetComponent<Image>()?.sprite;
                image.raycastTarget = false;
                image.color = new Color(0.15f, 0.95f, 1f, 0f);
                image.preserveAspect = true;

                SerializedObject serialized = new(view);
                serialized.FindProperty("selectionHighlight").objectReferenceValue = image;
                serialized.FindProperty("selectedAlpha").floatValue = 0.9f;
                serialized.FindProperty("selectionDuration").floatValue = 0.14f;
                serialized.FindProperty("selectedHighlightScale").floatValue = 1.12f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, BlockButtonPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Transform presentation = FindTransform(scene, "Presentation");
            if (presentation == null)
            {
                GameObject presentationObject = new("Presentation");
                SceneManager.MoveGameObjectToScene(presentationObject, scene);
                presentation = presentationObject.transform;
            }

            Transform poolTransform = presentation.Find("Heal Particle Pool");
            if (poolTransform == null)
            {
                GameObject poolObject = new("Heal Particle Pool");
                poolObject.transform.SetParent(presentation, false);
                poolTransform = poolObject.transform;
            }

            HealParticleEffectManager manager = poolTransform.GetComponent<HealParticleEffectManager>();
            if (manager == null)
                manager = poolTransform.gameObject.AddComponent<HealParticleEffectManager>();

            GameObject particlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealParticlePrefabPath);
            SerializedObject serialized = new(manager);
            serialized.FindProperty("effectPrefab").objectReferenceValue =
                particlePrefab != null ? particlePrefab.GetComponent<ParticleSystem>() : null;
            serialized.FindProperty("initialPoolSize").intValue = 12;
            serialized.FindProperty("effectRoot").objectReferenceValue = poolTransform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == objectName)
                        return candidate;
                }
            }

            return null;
        }

        private static void ValidateSupplyEvent()
        {
            ShopEventData eventData = AssetDatabase.LoadAssetAtPath<ShopEventData>(SupplyEventPath);
            SerializedObject serialized = eventData != null ? new SerializedObject(eventData) : null;
            SerializedProperty array = serialized?.FindProperty("offers");
            if (array == null)
                throw new InvalidOperationException("SupplyEventData offers are unavailable.");

            string[] paths =
            {
                ShopFolder + "/CoreEnergyOffer.asset",
                ShopFolder + "/RedRecoveryOffer.asset",
                ShopFolder + "/BlueRecoveryOffer.asset",
                ShopFolder + "/GreenRecoveryOffer.asset",
                ShopFolder + "/YellowRecoveryOffer.asset"
            };
            foreach (string path in paths)
            {
                ShopOfferData expected = AssetDatabase.LoadAssetAtPath<ShopOfferData>(path);
                bool found = false;
                for (int i = 0; i < array.arraySize; i++)
                    found |= array.GetArrayElementAtIndex(i).objectReferenceValue == expected;
                if (!found)
                    throw new InvalidOperationException($"SupplyEventData does not include {path}.");
            }
        }

        private static void ValidateBlockButton()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlockButtonPath);
            SupplyBlockButtonView view = prefab != null ? prefab.GetComponent<SupplyBlockButtonView>() : null;
            SerializedObject serialized = view != null ? new SerializedObject(view) : null;
            Image highlight = serialized?.FindProperty("selectionHighlight").objectReferenceValue as Image;
            if (highlight == null || highlight.raycastTarget)
                throw new InvalidOperationException("Block Button selected highlight is incomplete.");
        }

        private static void ValidateHealerPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealerPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Healer prefab is missing.");
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Heal Particles")
                    throw new InvalidOperationException("Healer prefab still owns a legacy Heal Particles object.");
            }
        }

        private static void ValidateScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HealParticleEffectManager manager = UnityEngine.Object.FindFirstObjectByType<HealParticleEffectManager>(
                FindObjectsInactive.Include);
            SerializedObject serialized = manager != null ? new SerializedObject(manager) : null;
            if (manager == null
                || serialized.FindProperty("effectPrefab").objectReferenceValue == null
                || serialized.FindProperty("effectRoot").objectReferenceValue == null)
            {
                throw new InvalidOperationException($"{scenePath} has no configured Heal particle pool.");
            }
        }

        private static void SetObjectIfMissing(SerializedProperty property, UnityEngine.Object value)
        {
            if (property.objectReferenceValue == null)
                property.objectReferenceValue = value;
        }

        private static void SetStringIfEmpty(SerializedProperty property, string value)
        {
            if (string.IsNullOrWhiteSpace(property.stringValue))
                property.stringValue = value;
        }
    }
}
#endif
