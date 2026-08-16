#if UNITY_EDITOR
using System;
using System.Linq;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.GridSystem;
using KeepCoreSafe.Localization;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.Tutorial;
using KeepCoreSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class InteractivePrologueSetup
    {
        private const string PrologueScenePath = "Assets/Scenes/PrologueScene.unity";
        private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
        private const string TutorialCorePath = "Assets/Resources/Data/Block/TutorialCoreData.asset";
        private const string CorePath = "Assets/Resources/Data/Block/CoreData.asset";
        private const string LilyControllerPath = "Assets/Animation/LilyAnimator.controller";
        private const string LilySpritePath = "Assets/Resources/Sprites/Lily-Sheet.png";
        private const string HighlightSpritePath = "Assets/Resources/Sprites/EffectCell.png";
        private const string FontPath = "Assets/Fonts/Mona12 SDF.asset";
        private const string BoldFontPath = "Assets/Fonts/Mona12-Bold SDF.asset";
        private const string GridLinePrefabPath = "Assets/Prefabs/Grid/GridLine.prefab";
        private const string PulsePrefabPath = "Assets/Prefabs/Presentation/CoreEnergyPulse.prefab";
        private const string ShockwavePrefabPath = "Assets/Prefabs/Presentation/CoreShockwave.prefab";
        private const string BurstPrefabPath = "Assets/Prefabs/Particle/MergeBurstParticles.prefab";
        private const string BlockButtonPrefabPath = "Assets/Prefabs/UI/Block Button.prefab";

        [MenuItem("Keep Core Safe/Setup/Interactive Prologue")]
        public static void Apply()
        {
            ConfigureTutorialAnimator();
            ConfigurePrologueScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("INTERACTIVE_PROLOGUE_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Interactive Prologue")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(PrologueScenePath, OpenSceneMode.Single);
            PrologueDirector director = UnityEngine.Object.FindFirstObjectByType<PrologueDirector>(FindObjectsInactive.Include);
            PrologueThreatOverlay threats = UnityEngine.Object.FindFirstObjectByType<PrologueThreatOverlay>(FindObjectsInactive.Include);
            GridManager grid = UnityEngine.Object.FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);
            if (director == null || threats == null || grid == null)
                throw new InvalidOperationException("Interactive Prologue components are missing.");

            SerializedObject directorObject = new(director);
            string[] requiredReferences =
            {
                "gridManager", "coreTransform", "coreRenderer", "tutorialCoreData", "completedCoreData",
                "lilyTransform", "lilyRenderer", "lilyAnimator", "placementPreview", "gridHighlight",
                "cameraController", "objectiveGroup", "threatOverlay", "atmosphereOverlay",
                "energyPulsePrefab", "shockwavePrefab", "burstParticlesPrefab",
                "pickupSound", "placementSound"
            };
            foreach (string propertyName in requiredReferences)
            {
                SerializedProperty property = directorObject.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null)
                    throw new InvalidOperationException($"PrologueDirector.{propertyName} is not assigned.");
            }

            SerializedProperty labels = new SerializedObject(threats).FindProperty("commandLabels");
            if (labels == null || labels.arraySize < 12)
                throw new InvalidOperationException("Prologue threat overlay needs a pre-authored full-screen label pool.");

            if (FindNamed(scene, "Prologue Text")?.activeSelf == true
                || FindNamed(scene, "Earth Illustration Slot")?.activeSelf == true)
            {
                throw new InvalidOperationException("Legacy text prologue visuals are still active.");
            }

            EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            TutorialDirector tutorial = UnityEngine.Object.FindFirstObjectByType<TutorialDirector>(FindObjectsInactive.Include);
            if (tutorial == null)
                throw new InvalidOperationException("Tutorial Director is missing.");

            SerializedObject tutorialObject = new(tutorial);
            if (tutorialObject.FindProperty("lilyTransform").objectReferenceValue == null
                || tutorialObject.FindProperty("lilyAnimator").objectReferenceValue == null)
            {
                throw new InvalidOperationException("Tutorial Lily references are not assigned.");
            }

            Debug.Log("INTERACTIVE_PROLOGUE_VALIDATION_COMPLETE");
        }

        private static void ConfigureTutorialAnimator()
        {
            Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            TutorialDirector director = UnityEngine.Object.FindFirstObjectByType<TutorialDirector>(FindObjectsInactive.Include);
            GameObject lily = FindNamed(scene, "Lily");
            Animator animator = lily != null ? lily.GetComponent<Animator>() : null;
            if (director == null || animator == null)
                throw new InvalidOperationException("Tutorial Director or Lily Animator is missing.");

            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(LilyControllerPath);
            if (animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = controller;
            SerializedObject directorObject = new(director);
            SerializedProperty lilyTransformProperty = directorObject.FindProperty("lilyTransform");
            bool isFirstLilyPlacementMigration = lilyTransformProperty.objectReferenceValue == null;
            lilyTransformProperty.objectReferenceValue = lily.transform;
            directorObject.FindProperty("lilyAnimator").objectReferenceValue = animator;
            if (isFirstLilyPlacementMigration)
            {
                GridManager grid = UnityEngine.Object.FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);
                if (grid == null)
                    throw new InvalidOperationException("Tutorial GridManager is missing.");

                Vector2Int coreCell = new(grid.Width / 2, grid.Height / 2);
                Vector2Int currentLilyCell = grid.WorldToGrid(lily.transform.position);
                directorObject.FindProperty("lilyOffsetFromCore").vector2IntValue = currentLilyCell - coreCell;
            }
            directorObject.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePrologueScene()
        {
            Scene scene = EditorSceneManager.OpenScene(PrologueScenePath, OpenSceneMode.Single);
            DisableLegacyPrologue(scene);

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }
            camera.gameObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.012f, 0.025f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            GameCameraController cameraController = GetOrAdd<GameCameraController>(camera.gameObject);

            GameObject gridObject = GetOrCreateRoot(scene, "Prologue Grid");
            GridManager gridManager = GetOrAdd<GridManager>(gridObject);
            GridVisualizer gridVisualizer = GetOrAdd<GridVisualizer>(gridObject);
            SerializedObject gridData = new(gridManager);
            gridData.FindProperty("width").intValue = 10;
            gridData.FindProperty("height").intValue = 8;
            gridData.FindProperty("cellSize").floatValue = 1f;
            gridData.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject visualizerData = new(gridVisualizer);
            visualizerData.FindProperty("linePrefab").objectReferenceValue =
                LoadPrefabComponent<LineRenderer>(GridLinePrefabPath);
            visualizerData.FindProperty("lineColor").colorValue = new Color(0.34f, 0.07f, 0.1f, 0.58f);
            visualizerData.ApplyModifiedPropertiesWithoutUndo();

            CoreBlockData tutorialCore = AssetDatabase.LoadAssetAtPath<CoreBlockData>(TutorialCorePath);
            CoreBlockData completedCore = AssetDatabase.LoadAssetAtPath<CoreBlockData>(CorePath);
            GameObject coreObject = GetOrCreateRoot(scene, "Prologue Core");
            SpriteRenderer coreRenderer = GetOrAdd<SpriteRenderer>(coreObject);
            coreRenderer.sprite = tutorialCore.Sprite;
            coreRenderer.sortingOrder = 10;

            GameObject lilyObject = GetOrCreateRoot(scene, "Lily");
            SpriteRenderer lilyRenderer = GetOrAdd<SpriteRenderer>(lilyObject);
            lilyRenderer.sprite = LoadFirstSprite(LilySpritePath);
            lilyRenderer.sortingOrder = 20;
            Animator lilyAnimator = GetOrAdd<Animator>(lilyObject);
            lilyAnimator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(LilyControllerPath);

            GameObject previewObject = GetOrCreateRoot(scene, "Lily Placement Preview");
            SpriteRenderer previewRenderer = GetOrAdd<SpriteRenderer>(previewObject);
            previewRenderer.sprite = lilyRenderer.sprite;
            previewRenderer.sortingOrder = 18;
            previewObject.SetActive(false);

            GameObject highlightObject = GetOrCreateRoot(scene, "Prologue Grid Highlight");
            SpriteRenderer highlightRenderer = GetOrAdd<SpriteRenderer>(highlightObject);
            highlightRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HighlightSpritePath);
            highlightRenderer.sortingOrder = 15;
            TutorialGridHighlight highlight = GetOrAdd<TutorialGridHighlight>(highlightObject);
            SerializedObject highlightData = new(highlight);
            highlightData.FindProperty("highlightRenderer").objectReferenceValue = highlightRenderer;
            highlightData.FindProperty("color").colorValue = new Color(0.95f, 0.18f, 0.22f, 0.68f);
            highlightData.ApplyModifiedPropertiesWithoutUndo();

            GameObject effectRoot = GetOrCreateRoot(scene, "Prologue Effects");
            Canvas canvas = GetOrCreateCanvas(scene);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);

            Image atmosphere = CreateFullscreenImage(canvas.transform, "Prologue Atmosphere", new Color(0.45f, 0f, 0.025f, 0.1f));
            atmosphere.raycastTarget = false;
            PrologueThreatOverlay threats = CreateThreatOverlay(canvas.transform, font);
            CreateObjective(canvas.transform, boldFont, out CanvasGroup objectiveGroup);
            SetNamedObjectActive(scene, "Lily Button", false);
            SetNamedObjectActive(scene, "Prologue White Flash", false);

            GameObject directorObject = FindNamed(scene, "Prologue Director")
                                        ?? GetOrCreateRoot(scene, "Prologue Director");
            PrologueDirector director = GetOrAdd<PrologueDirector>(directorObject);
            SerializedObject directorData = new(director);
            Assign(directorData, "gridManager", gridManager);
            Assign(directorData, "coreTransform", coreObject.transform);
            Assign(directorData, "coreRenderer", coreRenderer);
            Assign(directorData, "tutorialCoreData", tutorialCore);
            Assign(directorData, "completedCoreData", completedCore);
            Assign(directorData, "lilyTransform", lilyObject.transform);
            Assign(directorData, "lilyRenderer", lilyRenderer);
            Assign(directorData, "lilyAnimator", lilyAnimator);
            Assign(directorData, "placementPreview", previewRenderer);
            Assign(directorData, "gridHighlight", highlight);
            Assign(directorData, "cameraController", cameraController);
            Assign(directorData, "objectiveGroup", objectiveGroup);
            Assign(directorData, "threatOverlay", threats);
            Assign(directorData, "atmosphereOverlay", atmosphere);
            Assign(directorData, "energyPulsePrefab", LoadPrefabComponent<CoreEnergyPulseView>(PulsePrefabPath));
            Assign(directorData, "shockwavePrefab", LoadPrefabComponent<ShockwaveRingView>(ShockwavePrefabPath));
            Assign(directorData, "burstParticlesPrefab", LoadPrefabComponent<ParticleSystem>(BurstPrefabPath));
            Assign(directorData, "effectRoot", effectRoot.transform);
            string fusionAudioPath = AssetDatabase.GUIDToAssetPath("e19b67d3a49a1d14bb682dc20cf579c8");
            Assign(directorData, "fusionSound", AssetDatabase.LoadAssetAtPath<AudioClip>(fusionAudioPath));
            Assign(directorData, "pickupSound", AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Clips/Dismantle.wav"));
            Assign(directorData, "placementSound", AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/Clips/Place.wav"));
            directorData.ApplyModifiedPropertiesWithoutUndo();

            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void DisableLegacyPrologue(Scene scene)
        {
            string[] legacyNames = { "Input Background", "Earth Illustration Slot", "Prologue Text", "Blackout" };
            foreach (string name in legacyNames)
            {
                GameObject legacy = FindNamed(scene, name);
                if (legacy != null)
                    legacy.SetActive(false);
            }
        }

        private static Canvas GetOrCreateCanvas(Scene scene)
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObject = GetOrCreateRoot(scene, "Prologue Canvas");
                canvas = GetOrAdd<Canvas>(canvasObject);
                GetOrAdd<CanvasScaler>(canvasObject);
                GetOrAdd<GraphicRaycaster>(canvasObject);
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }

        private static PrologueThreatOverlay CreateThreatOverlay(Transform canvas, TMP_FontAsset font)
        {
            GameObject root = GetOrCreateUI(canvas, "External AI Commands", typeof(CanvasGroup), typeof(PrologueThreatOverlay));
            Stretch(root.GetComponent<RectTransform>());
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            const int labelCount = 14;
            TMP_Text[] labels = new TMP_Text[labelCount];
            for (int i = 0; i < labelCount; i++)
            {
                GameObject labelObject = GetOrCreateUI(root.transform, $"Command {i + 1:00}", typeof(TextMeshProUGUI));
                RectTransform rect = labelObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(620f, 120f);
                TMP_Text label = labelObject.GetComponent<TMP_Text>();
                label.font = font;
                label.fontSize = 36f;
                label.color = new Color(1f, 0.08f, 0.06f, 0.58f);
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                labelObject.SetActive(false);
                labels[i] = label;
            }
            PrologueThreatOverlay overlay = root.GetComponent<PrologueThreatOverlay>();
            SerializedObject overlayData = new(overlay);
            Assign(overlayData, "canvasGroup", group);
            overlayData.FindProperty("spawnDelayRange").vector2Value = new Vector2(0.04f, 0.32f);
            overlayData.FindProperty("visibleDurationRange").vector2Value = new Vector2(0.24f, 0.8f);
            overlayData.FindProperty("screenCoverage").floatValue = 0.94f;
            SerializedProperty labelsProperty = overlayData.FindProperty("commandLabels");
            labelsProperty.arraySize = labels.Length;
            for (int i = 0; i < labels.Length; i++)
                labelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            overlayData.ApplyModifiedPropertiesWithoutUndo();
            return overlay;
        }

        private static void CreateObjective(
            Transform canvas,
            TMP_FontAsset font,
            out CanvasGroup group)
        {
            GameObject root = GetOrCreateUI(canvas, "Prologue Objective", typeof(Image), typeof(CanvasGroup));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.22f, 0.79f);
            rect.anchorMax = new Vector2(0.78f, 0.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.035f, 0.88f);
            root.GetComponent<Image>().raycastTarget = false;
            group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            GameObject labelObject = GetOrCreateUI(root.transform, "Objective Text", typeof(TextMeshProUGUI));
            Stretch(labelObject.GetComponent<RectTransform>());
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.font = font;
            label.fontSize = 36f;
            label.color = new Color(0.86f, 0.97f, 1f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            LocalizedText localizedText = GetOrAdd<LocalizedText>(labelObject);
            SerializedObject localizedData = new(localizedText);
            Assign(localizedData, "label", label);
            localizedData.FindProperty("localizationKey").stringValue = "prologue.objective";
            localizedData.FindProperty("fallbackText").stringValue =
                "Lily를 Core에 동면시키면 살릴 수 있다.";
            localizedData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePlacementSlot(
            Transform canvas,
            TMP_FontAsset font,
            Sprite lilySprite,
            out CanvasGroup group,
            out Button button)
        {
            Transform legacy = canvas.Find("Lily Placement Slot");
            if (legacy != null)
                UnityEngine.Object.DestroyImmediate(legacy.gameObject);

            GameObject root;
            Transform existing = canvas.Find("Lily Button");
            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlockButtonPrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Block Button prefab is missing.");
                root = PrefabUtility.InstantiatePrefab(prefab, canvas.gameObject.scene) as GameObject;
                root.name = "Lily Button";
                root.transform.SetParent(canvas, false);
            }

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            rect.sizeDelta = new Vector2(100f, 100f);
            group = root.GetComponent<CanvasGroup>();
            button = root.GetComponent<Button>();
            SupplyBlockButtonView view = root.GetComponent<SupplyBlockButtonView>();
            if (view == null)
                throw new InvalidOperationException("Block Button prefab has no SupplyBlockButtonView.");
            SerializedObject viewData = new(view);
            Image icon = viewData.FindProperty("icon").objectReferenceValue as Image;
            TMP_Text label = viewData.FindProperty("label").objectReferenceValue as TMP_Text;
            Image selection = viewData.FindProperty("selectionHighlight").objectReferenceValue as Image;
            if (icon == null || label == null || selection == null)
                throw new InvalidOperationException("Block Button visual references are incomplete.");
            icon.sprite = lilySprite;
            icon.color = Color.white;
            label.font = font;
            label.fontSize = 24f;
            label.text = "Lily";
            selection.gameObject.SetActive(false);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            root.SetActive(true);
        }

        private static Image CreateFullscreenImage(Transform parent, string name, Color color)
        {
            GameObject root = GetOrCreateUI(parent, name, typeof(Image));
            Stretch(root.GetComponent<RectTransform>());
            Image image = root.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
                return;
            GameObject eventSystem = GetOrCreateRoot(scene, "EventSystem");
            GetOrAdd<EventSystem>(eventSystem);
            GetOrAdd<InputSystemUIInputModule>(eventSystem);
        }

        private static Sprite LoadFirstSprite(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            GameObject existing = FindNamed(scene, name);
            if (existing != null)
                return existing;
            GameObject created = new(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static GameObject FindNamed(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name)
                        return transform.gameObject;
                }
            }
            return null;
        }

        private static void SetNamedObjectActive(Scene scene, string name, bool active)
        {
            GameObject target = FindNamed(scene, name);
            if (target != null)
                target.SetActive(active);
        }

        private static GameObject GetOrCreateUI(Transform parent, string name, params Type[] components)
        {
            Transform existing = parent.Find(name);
            GameObject root = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            foreach (Type component in components)
            {
                if (root.GetComponent(component) == null)
                    root.AddComponent(component);
            }
            root.SetActive(true);
            return root;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component)
                ? component
                : gameObject.AddComponent<T>();
        }

        private static void Assign(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized property {target.targetObject.GetType().Name}.{propertyName}.");
            property.objectReferenceValue = value;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
