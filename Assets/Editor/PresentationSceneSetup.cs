using KeepCoreSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class PresentationSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/FoundationTestScene.unity";
        private const string BlockButtonPrefabPath = "Assets/Prefabs/UI/Block Button.prefab";
        private const string KoreanFontPath = "Assets/Fonts/LimgulMono16 SDF.asset";

        [MenuItem("Keep Core Safe/Setup Presentation UI")]
        public static void Setup()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameDefaultUI gameUI = Object.FindFirstObjectByType<GameDefaultUI>(FindObjectsInactive.Include);
            PreparationUI preparationUI = Object.FindFirstObjectByType<PreparationUI>(FindObjectsInactive.Include);
            if (gameUI == null || preparationUI == null)
            {
                Debug.LogError("GameDefaultUI or PreparationUI was not found.");
                return;
            }

            Canvas canvas = gameUI.GetComponentInParent<Canvas>();
            TMP_Text fontSource = Object.FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            AddTooltipTriggerToBlockButtonPrefab();
            SetupWaveAnnouncement(gameUI.transform, fontSource);
            BlockDescriptionTooltip tooltip = SetupTooltip(gameUI.transform, canvas, fontSource);
            SetupPreparationTooltip(preparationUI, tooltip);
            SetupGameOver(gameUI.transform, fontSource);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("PRESENTATION_SCENE_SETUP_COMPLETE");
        }

        private static void AddTooltipTriggerToBlockButtonPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BlockButtonPrefabPath);
            if (root.GetComponent<BlockButtonTooltipTrigger>() == null)
                root.AddComponent<BlockButtonTooltipTrigger>();
            PrefabUtility.SaveAsPrefabAsset(root, BlockButtonPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void SetupWaveAnnouncement(Transform parent, TMP_Text fontSource)
        {
            Transform existing = parent.Find("Wave Announcement");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Wave Announcement",
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(TextMeshProUGUI),
                    typeof(WaveAnnouncementUI));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -90f);
            rect.sizeDelta = new Vector2(520f, 90f);

            TMP_Text label = root.GetComponent<TMP_Text>();
            ApplyFont(label, fontSource);
            label.text = "Wave 1";
            label.fontSize = 50f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.8f, 1f, 0.9f, 1f);
            label.raycastTarget = false;

            WaveAnnouncementUI ui = root.GetComponent<WaveAnnouncementUI>();
            SerializedObject data = new(ui);
            data.FindProperty("visualRoot").objectReferenceValue = rect;
            data.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            data.FindProperty("label").objectReferenceValue = label;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BlockDescriptionTooltip SetupTooltip(
            Transform parent,
            Canvas canvas,
            TMP_Text fontSource)
        {
            Transform existing = parent.Find("Block Description Tooltip");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Block Description Tooltip",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CanvasGroup),
                    typeof(BlockDescriptionTooltip));
            root.transform.SetParent(parent, false);
            root.transform.SetAsLastSibling();

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(390f, 132f);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.035f, 0.07f, 0.1f, 0.96f);
            background.raycastTarget = false;

            TMP_Text title = CreateOrGetText(root.transform, "Title", fontSource);
            ConfigureTooltipText(title, new Vector2(14f, -10f), new Vector2(-14f, -48f));
            title.fontSize = 24f;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.45f, 1f, 0.72f, 1f);

            TMP_Text description = CreateOrGetText(root.transform, "Description", fontSource);
            ConfigureTooltipText(description, new Vector2(14f, 48f), new Vector2(-14f, 8f));
            description.fontSize = 18f;
            description.color = Color.white;
            //description.enableWordWrapping = true;

            BlockDescriptionTooltip tooltip = root.GetComponent<BlockDescriptionTooltip>();
            SerializedObject data = new(tooltip);
            data.FindProperty("canvas").objectReferenceValue = canvas;
            data.FindProperty("panel").objectReferenceValue = rect;
            data.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            data.FindProperty("titleLabel").objectReferenceValue = title;
            data.FindProperty("descriptionLabel").objectReferenceValue = description;
            data.ApplyModifiedPropertiesWithoutUndo();
            return tooltip;
        }

        private static void SetupPreparationTooltip(
            PreparationUI preparationUI,
            BlockDescriptionTooltip tooltip)
        {
            SerializedObject data = new(preparationUI);
            data.FindProperty("descriptionTooltip").objectReferenceValue = tooltip;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupGameOver(Transform parent, TMP_Text fontSource)
        {
            Transform existing = parent.Find("Game Over UI");
            GameObject controller = existing != null
                ? existing.gameObject
                : new GameObject("Game Over UI", typeof(RectTransform), typeof(GameOverUI));
            controller.transform.SetParent(parent, false);
            Stretch(controller.GetComponent<RectTransform>());
            controller.transform.SetAsLastSibling();

            GameObject visual = GetOrCreatePanel(controller.transform, "Visual");
            Stretch(visual.GetComponent<RectTransform>());
            Image blackoutImage = visual.GetComponent<Image>();
            blackoutImage.color = new Color(0.005f, 0.008f, 0.015f, 0.96f);
            blackoutImage.raycastTarget = true;
            CanvasGroup blackout = visual.GetComponent<CanvasGroup>();

            CanvasGroup titleGroup = CreateTextGroup(
                visual.transform, "Game Over Title", "GAME OVER", 58f, 96f, fontSource,
                new Color(1f, 0.25f, 0.2f, 1f), out _);
            CanvasGroup waveGroup = CreateTextGroup(
                visual.transform, "Wave Result", "Wave 1", 30f, 14f, fontSource,
                Color.white, out TMP_Text waveLabel);
            CanvasGroup restartGroup = CreateRestartButton(
                visual.transform, fontSource, out Button restartButton);

            GameOverUI ui = controller.GetComponent<GameOverUI>();
            SerializedObject data = new(ui);
            data.FindProperty("visualRoot").objectReferenceValue = visual;
            data.FindProperty("blackout").objectReferenceValue = blackout;
            data.FindProperty("titleGroup").objectReferenceValue = titleGroup;
            data.FindProperty("waveGroup").objectReferenceValue = waveGroup;
            data.FindProperty("restartGroup").objectReferenceValue = restartGroup;
            data.FindProperty("waveLabel").objectReferenceValue = waveLabel;
            data.FindProperty("restartButton").objectReferenceValue = restartButton;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CanvasGroup CreateTextGroup(
            Transform parent,
            string name,
            string content,
            float fontSize,
            float y,
            TMP_Text fontSource,
            Color color,
            out TMP_Text label)
        {
            Transform existing = parent.Find(name);
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(600f, 80f);

            label = root.GetComponent<TMP_Text>();
            ApplyFont(label, fontSource);
            label.text = content;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            return root.GetComponent<CanvasGroup>();
        }

        private static CanvasGroup CreateRestartButton(
            Transform parent,
            TMP_Text fontSource,
            out Button button)
        {
            Transform existing = parent.Find("Restart Button");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Restart Button",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -88f);
            rect.sizeDelta = new Vector2(220f, 58f);

            Image image = root.GetComponent<Image>();
            image.color = new Color(0.15f, 0.72f, 0.46f, 1f);
            button = root.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateOrGetText(root.transform, "Text", fontSource);
            Stretch(label.rectTransform);
            label.text = "처음으로";
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return root.GetComponent<CanvasGroup>();
        }

        private static GameObject GetOrCreatePanel(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;

            GameObject panel = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            return panel;
        }

        private static TMP_Text CreateOrGetText(Transform parent, string name, TMP_Text fontSource)
        {
            Transform existing = parent.Find(name);
            TMP_Text text;
            if (existing != null)
            {
                text = existing.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject textObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                textObject.transform.SetParent(parent, false);
                text = textObject.GetComponent<TMP_Text>();
            }

            ApplyFont(text, fontSource);
            return text;
        }

        private static void ConfigureTooltipText(TMP_Text text, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
        }

        private static void ApplyFont(TMP_Text target, TMP_Text source)
        {
            TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (koreanFont != null)
                target.font = koreanFont;
            else if (source != null)
                target.font = source.font;
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
