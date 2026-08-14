#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.Tutorial;
using KeepCoreSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class GameManagerStructureRefactorSetup
    {
        private const string PulsePrefabPath = "Assets/Prefabs/Presentation/CoreEnergyPulse.prefab";
        private const string ShockwavePrefabPath = "Assets/Prefabs/Presentation/CoreShockwave.prefab";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        [MenuItem("Keep Core Safe/Setup/Game Manager Structure Refactor")]
        public static void Apply()
        {
            foreach (string scenePath in ScenePaths)
                ConfigureScene(scenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GAME_MANAGER_STRUCTURE_REFACTOR_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Game Manager Structure Refactor")]
        public static void Validate()
        {
            foreach (string scenePath in ScenePaths)
                ValidateScene(scenePath);

            Debug.Log("GAME_MANAGER_STRUCTURE_REFACTOR_VALIDATION_COMPLETE");
        }

        private static void ConfigureScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>(
                FindObjectsInactive.Include);
            if (gameManager == null)
                throw new InvalidOperationException($"{scene.name}: GameManager is missing.");

            Transform gameSystems = GetOrCreateChild(gameManager.transform, "Game Systems");
            Transform waveSystem = GetOrCreateChild(gameSystems, "Wave System");
            Transform supplySystem = GetOrCreateChild(gameSystems, "Supply System");
            Transform energySystem = GetOrCreateChild(gameSystems, "Core Energy System");
            Transform worldInteraction = GetOrCreateChild(gameManager.transform, "World Interaction");
            Transform presentation = GetOrCreateChild(gameManager.transform, "Presentation");
            Transform stageClearObject = GetOrCreateChild(presentation, "Stage Clear Presentation");
            Transform waveStartObject = GetOrCreateChild(presentation, "Wave Start Presentation");

            Dictionary<UnityEngine.Object, UnityEngine.Object> replacements = new();
            WaveManager wave = MoveComponent<WaveManager>(gameManager, waveSystem.gameObject, replacements);
            WaveDifficultyController difficulty = MoveComponent<WaveDifficultyController>(
                gameManager,
                waveSystem.gameObject,
                replacements);
            BlockSupplyController supply = MoveComponent<BlockSupplyController>(
                gameManager,
                supplySystem.gameObject,
                replacements);
            ShopEventController shop = MoveComponent<ShopEventController>(
                gameManager,
                supplySystem.gameObject,
                replacements);
            CoreEnergyController energy = MoveComponent<CoreEnergyController>(
                gameManager,
                energySystem.gameObject,
                replacements);
            MoveComponent<WorldBlockHoverController>(gameManager, worldInteraction.gameObject, replacements);
            StageClearPresentationController stageClear = MoveComponent<StageClearPresentationController>(
                gameManager,
                stageClearObject.gameObject,
                replacements);
            MoveComponent<WaveStartPresentationController>(gameManager, waveStartObject.gameObject, replacements);

            TutorialDirector tutorial = gameManager.GetComponent<TutorialDirector>();
            if (tutorial != null)
            {
                Transform tutorialSystem = GetOrCreateChild(gameManager.transform, "Tutorial System");
                MoveComponent<TutorialDirector>(gameManager, tutorialSystem.gameObject, replacements);
            }

            RemapSceneReferences(scene, replacements);
            ConfigureRuntimeReferences(gameManager, wave, difficulty, supply, shop, energy, stageClear);
            ConfigureStageClearViews(scene, stageClear);
            ConfigureSupplyControls();

            foreach (UnityEngine.Object source in replacements.Keys)
            {
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T MoveComponent<T>(
            GameManager gameManager,
            GameObject target,
            IDictionary<UnityEngine.Object, UnityEngine.Object> replacements) where T : Component
        {
            T source = gameManager.GetComponent<T>();
            if (source == null)
                source = gameManager.GetComponentInChildren<T>(true);
            if (source == null)
                throw new InvalidOperationException($"{gameManager.gameObject.scene.name}: {typeof(T).Name} is missing.");
            if (source.gameObject == target)
                return source;

            T destination = target.AddComponent<T>();
            EditorUtility.CopySerialized(source, destination);
            if (source is Behaviour sourceBehaviour && destination is Behaviour destinationBehaviour)
                destinationBehaviour.enabled = sourceBehaviour.enabled;
            replacements.Add(source, destination);
            return destination;
        }

        private static void RemapSceneReferences(
            Scene scene,
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                        continue;

                    SerializedObject serialized = new(component);
                    SerializedProperty property = serialized.GetIterator();
                    bool changed = false;
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference)
                            continue;
                        UnityEngine.Object current = property.objectReferenceValue;
                        if (current != null && replacements.TryGetValue(current, out UnityEngine.Object replacement))
                        {
                            property.objectReferenceValue = replacement;
                            changed = true;
                        }
                    }

                    if (changed)
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ConfigureRuntimeReferences(
            GameManager gameManager,
            WaveManager wave,
            WaveDifficultyController difficulty,
            BlockSupplyController supply,
            ShopEventController shop,
            CoreEnergyController energy,
            StageClearPresentationController stageClear)
        {
            SerializedObject game = new(gameManager);
            game.FindProperty("waveManager").objectReferenceValue = wave;
            game.FindProperty("difficultyController").objectReferenceValue = difficulty;
            game.FindProperty("coreEnergyController").objectReferenceValue = energy;
            game.FindProperty("stageClearPresentation").objectReferenceValue = stageClear;
            game.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject waveSerialized = new(wave);
            waveSerialized.FindProperty("supplyEventController").objectReferenceValue = shop;
            waveSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject supplySerialized = new(supply);
            supplySerialized.FindProperty("shopEventController").objectReferenceValue = shop;
            supplySerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject energySerialized = new(energy);
            energySerialized.FindProperty("pickupRoot").objectReferenceValue = energy.transform;
            energySerialized.FindProperty("pulseRoot").objectReferenceValue = energy.transform;
            energySerialized.ApplyModifiedPropertiesWithoutUndo();

            PlacementController placement = UnityEngine.Object.FindFirstObjectByType<PlacementController>(
                FindObjectsInactive.Include);
            if (placement != null)
            {
                SerializedObject placementSerialized = new(placement);
                placementSerialized.FindProperty("waveManager").objectReferenceValue = wave;
                placementSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        internal static void ConfigureStageClearViews(
            Scene scene,
            StageClearPresentationController stageClear)
        {
            CoreEnergyPulseView pulse = FindDirectComponent<CoreEnergyPulseView>(stageClear.transform);
            if (pulse == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PulsePrefabPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                instance.name = "Energy Pulse";
                instance.transform.SetParent(stageClear.transform, false);
                pulse = instance.GetComponent<CoreEnergyPulseView>();
            }

            ShockwaveRingView shockwave = FindDirectComponent<ShockwaveRingView>(stageClear.transform);
            if (shockwave == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShockwavePrefabPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                instance.name = "Shockwave";
                instance.transform.SetParent(stageClear.transform, false);
                shockwave = instance.GetComponent<ShockwaveRingView>();
            }

            pulse.gameObject.SetActive(false);
            shockwave.gameObject.SetActive(false);
            SerializedObject serialized = new(stageClear);
            serialized.FindProperty("energyPulse").objectReferenceValue = pulse;
            serialized.FindProperty("shockwave").objectReferenceValue = shockwave;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSupplyControls()
        {
            SupplyPresentationUI presentation = UnityEngine.Object.FindFirstObjectByType<SupplyPresentationUI>(
                FindObjectsInactive.Include);
            if (presentation == null)
                throw new InvalidOperationException("SupplyPresentationUI is missing.");

            SerializedObject serialized = new(presentation);
            RectTransform content = serialized.FindProperty("contentRoot").objectReferenceValue as RectTransform;
            Button confirm = serialized.FindProperty("confirmButton").objectReferenceValue as Button;
            Button reroll = serialized.FindProperty("rerollButton").objectReferenceValue as Button;
            if (content == null || confirm == null || reroll == null)
                throw new InvalidOperationException("Supply controls have incomplete references.");

            Transform existing = FindDirectChild(content, "Buttons");
            RectTransform controls;
            CanvasGroup group;
            if (existing == null)
            {
                GameObject controlsObject = new("Buttons", typeof(RectTransform), typeof(CanvasGroup));
                controlsObject.layer = content.gameObject.layer;
                controls = controlsObject.GetComponent<RectTransform>();
                controls.SetParent(content, false);
                controls.anchorMin = Vector2.zero;
                controls.anchorMax = Vector2.one;
                controls.offsetMin = Vector2.zero;
                controls.offsetMax = Vector2.zero;
                controls.SetSiblingIndex(Mathf.Min(confirm.transform.GetSiblingIndex(), reroll.transform.GetSiblingIndex()));
                group = controlsObject.GetComponent<CanvasGroup>();
            }
            else
            {
                controls = existing as RectTransform;
                group = existing.GetComponent<CanvasGroup>();
                if (group == null)
                    group = existing.gameObject.AddComponent<CanvasGroup>();
            }

            ReparentPreservingRect(reroll.transform as RectTransform, controls);
            ReparentPreservingRect(confirm.transform as RectTransform, controls);
            confirm.gameObject.SetActive(true);
            reroll.gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            serialized.FindProperty("controlsRoot").objectReferenceValue = controls;
            serialized.FindProperty("controlsGroup").objectReferenceValue = group;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReparentPreservingRect(RectTransform child, RectTransform parent)
        {
            if (child.parent == parent)
                return;

            Vector2 anchorMin = child.anchorMin;
            Vector2 anchorMax = child.anchorMax;
            Vector2 pivot = child.pivot;
            Vector2 position = child.anchoredPosition;
            Vector2 size = child.sizeDelta;
            Quaternion rotation = child.localRotation;
            Vector3 scale = child.localScale;
            child.SetParent(parent, false);
            child.anchorMin = anchorMin;
            child.anchorMax = anchorMax;
            child.pivot = pivot;
            child.anchoredPosition = position;
            child.sizeDelta = size;
            child.localRotation = rotation;
            child.localScale = scale;
        }

        internal static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null)
                return existing;

            GameObject gameObject = new(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }
            return null;
        }

        private static T FindDirectComponent<T>(Transform parent) where T : Component
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                T component = parent.GetChild(i).GetComponent<T>();
                if (component != null)
                    return component;
            }
            return null;
        }

        private static void ValidateScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>(
                FindObjectsInactive.Include);
            if (gameManager == null || gameManager.GetComponents<Component>().Length != 2)
                throw new InvalidOperationException($"{scene.name}: GameManager root is not cleanly separated.");

            SerializedObject game = new(gameManager);
            if (game.FindProperty("waveManager").objectReferenceValue == null
                || game.FindProperty("difficultyController").objectReferenceValue == null
                || game.FindProperty("coreEnergyController").objectReferenceValue == null
                || game.FindProperty("stageClearPresentation").objectReferenceValue == null)
            {
                throw new InvalidOperationException($"{scene.name}: GameManager references are incomplete.");
            }

            StageClearPresentationController stageClear = gameManager.GetComponentInChildren<StageClearPresentationController>(true);
            SerializedObject stage = new(stageClear);
            GameObject pulse = stage.FindProperty("energyPulse").objectReferenceValue is Component pulseComponent
                ? pulseComponent.gameObject
                : null;
            GameObject shockwave = stage.FindProperty("shockwave").objectReferenceValue is Component shockwaveComponent
                ? shockwaveComponent.gameObject
                : null;
            if (pulse == null || shockwave == null || pulse.activeSelf || shockwave.activeSelf)
                throw new InvalidOperationException($"{scene.name}: reusable stage-clear views are not configured.");

            SupplyPresentationUI supply = UnityEngine.Object.FindFirstObjectByType<SupplyPresentationUI>(
                FindObjectsInactive.Include);
            SerializedObject supplySerialized = new(supply);
            RectTransform controls = supplySerialized.FindProperty("controlsRoot").objectReferenceValue as RectTransform;
            CanvasGroup group = supplySerialized.FindProperty("controlsGroup").objectReferenceValue as CanvasGroup;
            Button confirm = supplySerialized.FindProperty("confirmButton").objectReferenceValue as Button;
            Button reroll = supplySerialized.FindProperty("rerollButton").objectReferenceValue as Button;
            if (controls == null || group == null || confirm.transform.parent != controls || reroll.transform.parent != controls)
                throw new InvalidOperationException($"{scene.name}: Supply button fade group is incomplete.");
        }
    }
}
#endif
