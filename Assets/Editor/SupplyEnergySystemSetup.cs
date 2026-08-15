using System;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class SupplyEnergySystemSetup
    {
        private const string SupplyPrefabPath = "Assets/Prefabs/Blocks/SupplyBlock.prefab";
        private const string SupplyDataPath = "Assets/Resources/Data/Block/SupplyBlockData.asset";
        private const string PickupPrefabPath = "Assets/Prefabs/Presentation/Core Energy Pickup.prefab";
        private const string GaugePrefabPath = "Assets/Prefabs/UI/DelayedFillGauge.prefab";
        private const string WallPrefabPath = "Assets/Prefabs/Blocks/WallBlock.prefab";
        private const string PulsePrefabPath = "Assets/Prefabs/Presentation/CoreEnergyPulse.prefab";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        [MenuItem("Keep Core Safe/Setup Supply And Core Energy Systems")]
        public static void Setup()
        {
            CreateSupplyBlockAssets();
            CreatePickupPrefab();
            CreateGaugePrefab();
            ConfigureOfferCardPrefab();
            ConfigureBalanceAssets();
            foreach (string scenePath in ScenePaths)
                ConfigureScene(scenePath);

            ReserializeChangedData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SUPPLY_ENERGY_SYSTEM_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Supply And Core Energy Systems")]
        public static void Validate()
        {
            SupplyBlockData supplyData = AssetDatabase.LoadAssetAtPath<SupplyBlockData>(SupplyDataPath);
            if (supplyData == null || supplyData.MaxHP != 1 || supplyData.Prefab is not SupplyBlock)
                throw new InvalidOperationException("Supply Block data/prefab is incomplete.");

            DelayedFillGauge gauge = AssetDatabase.LoadAssetAtPath<GameObject>(GaugePrefabPath)
                ?.GetComponent<DelayedFillGauge>();
            if (gauge == null)
                throw new InvalidOperationException("DelayedFillGauge prefab is missing.");

            foreach (string scenePath in ScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
                ShopEventController supply = UnityEngine.Object.FindFirstObjectByType<ShopEventController>(FindObjectsInactive.Include);
                ShockwaveCountdownUI energyUI = UnityEngine.Object.FindFirstObjectByType<ShockwaveCountdownUI>(FindObjectsInactive.Include);
                CoreEnergyController energy = game != null
                    ? game.GetComponentInChildren<CoreEnergyController>(true)
                    : null;
                SerializedObject supplySerialized = supply != null ? new SerializedObject(supply) : null;
                SerializedObject energySerialized = energy != null ? new SerializedObject(energy) : null;
                SerializedObject uiSerialized = energyUI != null ? new SerializedObject(energyUI) : null;
                if (game == null || game.GetComponentInChildren<CoreEnergyController>(true) == null
                    || supply == null || energyUI == null
                    || supplySerialized.FindProperty("supplyBlockData").objectReferenceValue == null
                    || energySerialized.FindProperty("pickupPrefab").objectReferenceValue == null
                    || energySerialized.FindProperty("absorptionPulsePrefab").objectReferenceValue == null
                    || uiSerialized.FindProperty("normalFillGauge").objectReferenceValue == null
                    || uiSerialized.FindProperty("minusFillGauge").objectReferenceValue == null
                    || uiSerialized.FindProperty("currentEnergy").objectReferenceValue == null
                    || uiSerialized.FindProperty("requiredEnergy").objectReferenceValue == null)
                {
                    throw new InvalidOperationException($"{scenePath} has incomplete Supply/Energy migration.");
                }
            }

            Debug.Log("SUPPLY_ENERGY_SYSTEM_VALIDATION_COMPLETE");
        }

        private static SupplyBlockData CreateSupplyBlockAssets()
        {
            GameObject wallRoot = PrefabUtility.LoadPrefabContents(WallPrefabPath);
            try
            {
                WallBlock wall = wallRoot.GetComponent<WallBlock>();
                SpriteRenderer renderer = wallRoot.GetComponentInChildren<SpriteRenderer>(true);
                DamageFeedback feedback = wallRoot.GetComponent<DamageFeedback>();
                SerializedObject wallSerialized = new(wall);
                UnityEngine.Object healthBar = wallSerialized.FindProperty("healthBarPrefab").objectReferenceValue;
                UnityEngine.Object.DestroyImmediate(wall, true);
                SupplyBlock supply = wallRoot.AddComponent<SupplyBlock>();
                SerializedObject supplySerialized = new(supply);
                supplySerialized.FindProperty("visualRenderer").objectReferenceValue = renderer;
                supplySerialized.FindProperty("damageFeedback").objectReferenceValue = feedback;
                supplySerialized.FindProperty("healthBarPrefab").objectReferenceValue = healthBar;
                supplySerialized.ApplyModifiedPropertiesWithoutUndo();
                wallRoot.name = "SupplyBlock";
                PrefabUtility.SaveAsPrefabAsset(wallRoot, SupplyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(wallRoot);
            }

            SupplyBlockData data = AssetDatabase.LoadAssetAtPath<SupplyBlockData>(SupplyDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SupplyBlockData>();
                AssetDatabase.CreateAsset(data, SupplyDataPath);
            }

            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(SupplyPrefabPath);
            SupplyBlock prefab = prefabRoot.GetComponent<SupplyBlock>();
            Sprite sprite = prefabRoot.GetComponentInChildren<SpriteRenderer>(true).sprite;
            SerializedObject serialized = new(data);
            serialized.FindProperty("displayName").stringValue = "block.supply.name";
            serialized.FindProperty("description").stringValue = "block.supply.desc";
            serialized.FindProperty("maxHP").intValue = 1;
            serialized.FindProperty("sprite").objectReferenceValue = sprite;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("visualColor").colorValue = new Color(0.25f, 0.95f, 1f, 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static CoreEnergyPickupView CreatePickupPrefab()
        {
            GameObject root = new("Core Energy Pickup", typeof(SpriteRenderer), typeof(CoreEnergyPickupView));
            try
            {
                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                renderer.color = new Color(0.65f, 1f, 0.85f, 1f);
                renderer.sortingOrder = 50;
                root.transform.localScale = Vector3.one * 0.35f;
                SerializedObject serialized = new(root.GetComponent<CoreEnergyPickupView>());
                serialized.FindProperty("visual").objectReferenceValue = renderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PickupPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath)
                .GetComponent<CoreEnergyPickupView>();
        }

        private static DelayedFillGauge CreateGaugePrefab()
        {
            GameObject root = CreateUiObject("DelayedFillGauge", null);
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(320f, 24f);
                Image background = root.AddComponent<Image>();
                background.color = new Color(0.035f, 0.08f, 0.1f, 0.92f);

                Slider delayed = CreateFillSlider("Delayed Fill", root.transform,
                    new Color(1f, 0.88f, 0.2f, 1f), out Image delayedImage);
                Slider current = CreateFillSlider("Current Fill", root.transform,
                    new Color(0.25f, 1f, 0.72f, 1f), out _);
                DelayedFillGauge gauge = root.AddComponent<DelayedFillGauge>();
                SerializedObject serialized = new(gauge);
                serialized.FindProperty("delayedSlider").objectReferenceValue = delayed;
                serialized.FindProperty("currentSlider").objectReferenceValue = current;
                serialized.FindProperty("delayedFill").objectReferenceValue = delayedImage;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GaugePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(GaugePrefabPath)
                .GetComponent<DelayedFillGauge>();
        }

        private static Slider CreateFillSlider(string name, Transform parent, Color color, out Image fillImage)
        {
            GameObject sliderObject = CreateUiObject(name, parent);
            Stretch(sliderObject.GetComponent<RectTransform>());
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            GameObject fill = CreateUiObject("Fill", sliderObject.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            Stretch(fillRect);
            fillImage = fill.AddComponent<Image>();
            fillImage.color = color;
            fillImage.raycastTarget = false;
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            return slider;
        }

        private static void ConfigureOfferCardPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/ShopOfferButton.prefab");
            try
            {
                CanvasGroup group = root.GetComponent<CanvasGroup>();
                if (group == null)
                    group = root.AddComponent<CanvasGroup>();
                ShopOfferCardMotion motion = root.GetComponent<ShopOfferCardMotion>();
                SerializedObject serialized = new(motion);
                serialized.FindProperty("canvasGroup").objectReferenceValue = group;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = "Supply Reward";
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/UI/ShopOfferButton.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureBalanceAssets()
        {
            ConfigureEnemyEnergy("Assets/Resources/Data/Enemy/MeleeEnemyData.asset", 2);
            ConfigureEnemyEnergy("Assets/Resources/Data/Enemy/RangedEnemyData.asset", 3);
            ConfigureDifficulty("Assets/Resources/Data/Systems/WaveDifficultyData.asset", 12, 80, 2);
            ConfigureDifficulty("Assets/Resources/Data/Systems/TutorialDifficultyData.asset", 8, 14, 1);

            ShopEventData eventData = AssetDatabase.LoadAssetAtPath<ShopEventData>(
                "Assets/Resources/Data/Shop/ShopEventData.asset");
            SerializedObject serialized = new(eventData);
            serialized.FindProperty("appearanceChance").floatValue = 0.35f;
            serialized.FindProperty("minimumWaveInterval").intValue = 2;
            serialized.FindProperty("maximumWaveInterval").intValue = 5;
            serialized.FindProperty("supplyHunterRatio").floatValue = 0.2f;
            serialized.FindProperty("minimumSupplyHunters").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureEnemyEnergy(string path, int amount)
        {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            SerializedObject serialized = new(data);
            serialized.FindProperty("energyOnDeath").intValue = amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDifficulty(string path, int first, int late, int growth)
        {
            WaveDifficultyData data = AssetDatabase.LoadAssetAtPath<WaveDifficultyData>(path);
            SerializedObject serialized = new(data);
            serialized.FindProperty("firstWaveRequiredEnergy").intValue = first;
            serialized.FindProperty("lateGameRequiredEnergy").intValue = late;
            serialized.FindProperty("requiredEnergyGrowthPerExtraWave").intValue = growth;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SupplyBlockData supplyData = AssetDatabase.LoadAssetAtPath<SupplyBlockData>(SupplyDataPath);
            CoreEnergyPickupView pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath)
                .GetComponent<CoreEnergyPickupView>();
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            CoreEnergyController energy = game.GetComponentInChildren<CoreEnergyController>(true);
            if (energy == null)
            {
                Transform systems = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    game.transform,
                    "Game Systems");
                Transform energySystem = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    systems,
                    "Core Energy System");
                energy = energySystem.gameObject.AddComponent<CoreEnergyController>();
            }

            GameObject pulseRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PulsePrefabPath);
            SerializedObject energySerialized = new(energy);
            energySerialized.FindProperty("pickupPrefab").objectReferenceValue = pickupPrefab;
            energySerialized.FindProperty("pickupRoot").objectReferenceValue = energy.transform;
            energySerialized.FindProperty("absorptionPulsePrefab").objectReferenceValue =
                pulseRoot.GetComponent<CoreEnergyPulseView>();
            energySerialized.FindProperty("pulseRoot").objectReferenceValue = energy.transform;
            energySerialized.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject gameSerialized = new(game);
            gameSerialized.FindProperty("coreEnergyController").objectReferenceValue = energy;
            gameSerialized.ApplyModifiedPropertiesWithoutUndo();

            ShopEventController supply = game.GetComponentInChildren<ShopEventController>(true);
            SerializedObject supplySerialized = new(supply);
            supplySerialized.FindProperty("supplyBlockData").objectReferenceValue = supplyData;
            supplySerialized.ApplyModifiedPropertiesWithoutUndo();

            ShopEventUI offerUI = UnityEngine.Object.FindFirstObjectByType<ShopEventUI>(FindObjectsInactive.Include);
            SerializedObject offerSerialized = new(offerUI);
            GameObject visualRoot = offerSerialized.FindProperty("visualRoot").objectReferenceValue as GameObject;
            CanvasGroup background = visualRoot.GetComponent<CanvasGroup>();
            if (background == null)
                background = visualRoot.AddComponent<CanvasGroup>();
            offerSerialized.FindProperty("backgroundGroup").objectReferenceValue = background;
            offerSerialized.ApplyModifiedPropertiesWithoutUndo();

            ShockwaveCountdownUI shockwaveUI = UnityEngine.Object.FindFirstObjectByType<ShockwaveCountdownUI>(
                FindObjectsInactive.Include);
            RerollFeatureSetup.ConfigureShockwaveUI(scene, shockwaveUI);

            DestroyNamedObject("Close Button");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ReserializeChangedData()
        {
            List<string> paths = new();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Resources/Data" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssets);
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
                result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void DestroyNamedObject(string name)
        {
            GameObject target = FindNamedObject(name);
            if (target != null)
                UnityEngine.Object.DestroyImmediate(target);
        }

        private static GameObject FindNamedObject(string name)
        {
            foreach (GameObject candidate in UnityEngine.Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == name)
                    return candidate;
            }
            return null;
        }
    }
}
