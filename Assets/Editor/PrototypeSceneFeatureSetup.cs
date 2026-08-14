using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class PrototypeSceneFeatureSetup
    {
        private const string ScenePath = "Assets/Scenes/FoundationTestScene.unity";

        [MenuItem("Keep Core Safe/Setup Timer, Speed, and Placement Effects")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetupPlacementEffects();
            SetupSpeedButton();
            SetupWaveManager();
            SetupCameraController();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Prototype scene features configured.");
        }

        private static void SetupPlacementEffects()
        {
            PlacementController controller = Object.FindFirstObjectByType<PlacementController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogError("PlacementController was not found in the prototype scene.");
                return;
            }

            SerializedObject controllerObject = new SerializedObject(controller);
            PlacementVisualizer visualizer =
                Object.FindFirstObjectByType<PlacementVisualizer>(FindObjectsInactive.Include);
            Transform root = visualizer != null ? visualizer.transform : null;
            if (root == null)
            {
                GameObject visualizerObject = new GameObject("Block Effect Visualizer");
                root = visualizerObject.transform;
            }

            if (visualizer == null) visualizer = root.gameObject.AddComponent<PlacementVisualizer>();
            root.SetParent(null, true);
            BlockEffectVisualizerSetup.ConfigureVisualizer(visualizer);

            controllerObject.FindProperty("effectVisualizer").objectReferenceValue = visualizer;
            controllerObject.FindProperty("coreBlockData").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CoreBlockData>("Assets/Resources/Data/Block/CoreData.asset");
            controllerObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupSpeedButton()
        {
            GameDefaultUI gameUI = Object.FindFirstObjectByType<GameDefaultUI>(FindObjectsInactive.Include);
            if (gameUI == null)
            {
                Debug.LogError("GameDefaultUI was not found in the prototype scene.");
                return;
            }

            Transform buttonTransform = gameUI.transform.Find("Speed Button");
            Button button;
            TMP_Text label;

            if (buttonTransform == null)
            {
                button = CreateSpeedButton(gameUI.transform, out label);
            }
            else
            {
                button = buttonTransform.GetComponent<Button>();
                label = buttonTransform.GetComponentInChildren<TMP_Text>(true);
            }

            SerializedObject gameUIObject = new SerializedObject(gameUI);
            gameUIObject.FindProperty("timeScaleButton").objectReferenceValue = button;
            gameUIObject.FindProperty("timeScaleText").objectReferenceValue = label;
            gameUIObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CreateSpeedButton(Transform parent, out TMP_Text label)
        {
            GameObject buttonObject = new GameObject(
                "Speed Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(120f, 44f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.22f, 0.9f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
            TMP_Text existingText = Object.FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            if (existingText != null) text.font = existingText.font;
            text.text = "1x";
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            label = text;
            return button;
        }

        private static void SetupWaveManager()
        {
            WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
            if (waveManager == null) return;

            SerializedObject waveObject = new SerializedObject(waveManager);
            waveObject.FindProperty("rangedEnemyData").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RangedEnemyData>("Assets/Resources/Data/Enemy/RangedEnemyData.asset");
            waveObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupCameraController()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && !mainCamera.TryGetComponent(out GameCameraController _))
                mainCamera.gameObject.AddComponent<GameCameraController>();
        }
    }
}
