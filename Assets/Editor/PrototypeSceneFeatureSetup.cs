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
            SpriteRenderer preview = controllerObject.FindProperty("previewRenderer").objectReferenceValue as SpriteRenderer;
            if (preview == null)
            {
                Debug.LogError("PlacementController has no preview renderer assigned.");
                return;
            }

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

            SpriteRenderer up = GetOrCreateDirection(root, "Up Effect", preview);
            SpriteRenderer down = GetOrCreateDirection(root, "Down Effect", preview);
            SpriteRenderer left = GetOrCreateDirection(root, "Left Effect", preview);
            SpriteRenderer right = GetOrCreateDirection(root, "Right Effect", preview);
            SpriteRenderer upLeft = GetOrCreateDirection(root, "Up Left Effect", preview);
            SpriteRenderer upRight = GetOrCreateDirection(root, "Up Right Effect", preview);
            SpriteRenderer downLeft = GetOrCreateDirection(root, "Down Left Effect", preview);
            SpriteRenderer downRight = GetOrCreateDirection(root, "Down Right Effect", preview);
            SpriteRenderer everything = GetOrCreateDirection(root, "Everything Effect", preview);

            SerializedObject visualizerObjectData = new SerializedObject(visualizer);
            visualizerObjectData.FindProperty("upRenderer").objectReferenceValue = up;
            visualizerObjectData.FindProperty("downRenderer").objectReferenceValue = down;
            visualizerObjectData.FindProperty("leftRenderer").objectReferenceValue = left;
            visualizerObjectData.FindProperty("rightRenderer").objectReferenceValue = right;
            visualizerObjectData.FindProperty("upLeftRenderer").objectReferenceValue = upLeft;
            visualizerObjectData.FindProperty("upRightRenderer").objectReferenceValue = upRight;
            visualizerObjectData.FindProperty("downLeftRenderer").objectReferenceValue = downLeft;
            visualizerObjectData.FindProperty("downRightRenderer").objectReferenceValue = downRight;
            visualizerObjectData.FindProperty("everythingRenderer").objectReferenceValue = everything;
            visualizerObjectData.ApplyModifiedPropertiesWithoutUndo();

            controllerObject.FindProperty("effectVisualizer").objectReferenceValue = visualizer;
            controllerObject.FindProperty("coreBlockData").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CoreBlockData>("Assets/Resources/Data/Block/CoreData.asset");
            controllerObject.FindProperty("dismantleRefundText").objectReferenceValue =
                GetOrCreateRefundText();
            controllerObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text GetOrCreateRefundText()
        {
            GameDefaultUI gameUI = Object.FindFirstObjectByType<GameDefaultUI>(FindObjectsInactive.Include);
            if (gameUI == null) return null;

            Transform existing = gameUI.transform.Find("Dismantle Refund Text");
            TextMeshProUGUI text;
            if (existing != null)
            {
                text = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                TMP_Text fontSource = Object.FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
                GameObject textObject = new GameObject(
                    "Dismantle Refund Text",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                textObject.transform.SetParent(gameUI.transform, false);
                text = textObject.GetComponent<TextMeshProUGUI>();
                if (fontSource != null) text.font = fontSource.font;
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 36f);
            text.text = "Refund +0";
            text.fontSize = 20f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left;
            text.color = new Color(1f, 0.85f, 0.25f, 1f);
            text.raycastTarget = false;
            text.gameObject.SetActive(false);
            return text;
        }

        private static SpriteRenderer GetOrCreateDirection(
            Transform parent,
            string objectName,
            SpriteRenderer preview)
        {
            Transform child = parent.Find(objectName);
            if (child == null)
            {
                GameObject childObject = new GameObject(objectName);
                child = childObject.transform;
                child.SetParent(parent, false);
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = preview.sprite;
            renderer.sharedMaterial = preview.sharedMaterial;
            renderer.sortingOrder = preview.sortingOrder + 1;
            renderer.enabled = false;
            return renderer;
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
