#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Localization;
using KeepCoreSafe.Tutorial;
using KeepCoreSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class TutorialTaskSetup
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
        private const string PromptPrefabPath = "Assets/Prefabs/UI/Tutorial Colorblind Prompt.prefab";
        private const float TutorialGreenHue = 0.405f;

        [MenuItem("Keep Core Safe/Setup/Tutorial UI And Selection Guard")]
        public static void Apply()
        {
            CreateOrUpdatePromptPrefab();
            SyncTutorialUi();
            ConfigureTutorialPrompt();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("TUTORIAL_UI_AND_SELECTION_GUARD_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Tutorial UI And Selection Guard")]
        public static void Validate()
        {
            GameObject promptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptPrefabPath);
            TutorialColorblindPrompt promptComponent =
                promptPrefab != null ? promptPrefab.GetComponent<TutorialColorblindPrompt>() : null;
            if (promptComponent == null)
                throw new InvalidOperationException("Tutorial colorblind prompt prefab is missing.");
            SerializedObject promptSerialized = new(promptComponent);
            if (promptSerialized.FindProperty("canvasGroup").objectReferenceValue == null
                || promptSerialized.FindProperty("panel").objectReferenceValue == null
                || promptSerialized.FindProperty("applyButton").objectReferenceValue == null
                || promptSerialized.FindProperty("declineButton").objectReferenceValue == null)
            {
                throw new InvalidOperationException("Tutorial colorblind prompt prefab references are incomplete.");
            }
            foreach (string key in new[]
                     {
                         "tutorial.colorblind.prompt",
                         "tutorial.colorblind.apply",
                         "tutorial.colorblind.later"
                     })
            {
                bool found = false;
                foreach (LocalizedText localized in promptPrefab.GetComponentsInChildren<LocalizedText>(true))
                    found |= localized.LocalizationKey == key;
                if (!found)
                    throw new InvalidOperationException($"Tutorial prompt localization key '{key}' is missing.");
            }

            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            RectTransform gameStart = FindNamedTransform(gameScene, "Start Wave Button") as RectTransform;
            RectTransform gameSupply = FindNamedTransform(gameScene, "Content Root") as RectTransform;
            RectTransform gameSpeed = FindNamedTransform(gameScene, "Speed Button") as RectTransform;

            Scene tutorialScene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Additive);
            TutorialDirector director = FindInScene<TutorialDirector>(tutorialScene);
            SerializedObject directorSerialized = director != null ? new SerializedObject(director) : null;
            TutorialColorblindPrompt prompt = directorSerialized?
                .FindProperty("colorblindPrompt").objectReferenceValue as TutorialColorblindPrompt;
            if (director == null || prompt == null || !prompt.transform.IsChildOf(FindNamedTransform(tutorialScene, "Upper Canvas")))
                throw new InvalidOperationException("Tutorial Director colorblind prompt reference is incomplete.");

            if (FindNamedTransform(tutorialScene, "Tutorial Dialogue") == null
                || FindNamedTransform(tutorialScene, "Tutorial Glitch Transition") == null
                || FindInScene<TutorialGridHighlight>(tutorialScene) == null)
            {
                throw new InvalidOperationException("Tutorial-only UI or Grid highlight was removed.");
            }

            RectTransform tutorialStart = FindNamedTransform(tutorialScene, "Start Wave Button") as RectTransform;
            RectTransform tutorialSupply = FindNamedTransform(tutorialScene, "Content Root") as RectTransform;
            RectTransform tutorialSpeed = FindNamedTransform(tutorialScene, "Speed Button") as RectTransform;
            ValidateRect(gameStart, tutorialStart, "Start Wave Button");
            ValidateRect(gameSupply, tutorialSupply, "Supply Content Root");
            ValidateRect(gameSpeed, tutorialSpeed, "Speed Button");

            ShockwaveCountdownUI energy = FindInScene<ShockwaveCountdownUI>(tutorialScene);
            ShopEventUI offers = FindInScene<ShopEventUI>(tutorialScene);
            ShopEventController tutorialShop = FindInScene<ShopEventController>(tutorialScene);
            Transform upperCanvas = FindNamedTransform(tutorialScene, "Upper Canvas");
            if (energy == null
                || energy.transform.parent != upperCanvas
                || energy.name != "Shockwave Charge UI"
                || offers == null
                || offers.name != "Offer Event UI"
                || offers.gameObject.activeSelf
                || new SerializedObject(offers).FindProperty("controller").objectReferenceValue != tutorialShop)
            {
                throw new InvalidOperationException("Tutorial main UI structure is not synchronized.");
            }

            Image startImage = tutorialStart != null ? tutorialStart.GetComponent<Image>() : null;
            if (startImage == null || !IsGreenPalette(startImage.color))
                throw new InvalidOperationException("Tutorial UI green palette was not preserved.");

            EditorSceneManager.CloseScene(gameScene, true);
            Debug.Log("TUTORIAL_UI_AND_SELECTION_GUARD_VALIDATION_COMPLETE");
        }

        private static void SyncTutorialUi()
        {
            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Scene tutorialScene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Additive);

            ReplaceSelfContainedUi<ShopEventUI>(gameScene, tutorialScene, true);
            ReplaceSelfContainedUi<ShockwaveCountdownUI>(gameScene, tutorialScene, true);
            ReplaceSelfContainedUi<StageClearAnnouncementUI>(gameScene, tutorialScene, true);
            ReplaceSelfContainedUi<WaveAnnouncementUI>(gameScene, tutorialScene, true);

            Dictionary<ulong, RectTransform> sourceRects = BuildUiRectMap(gameScene);
            Dictionary<ulong, RectTransform> targetRects = BuildUiRectMap(tutorialScene);
            foreach ((ulong id, RectTransform source) in sourceRects)
            {
                if (!targetRects.TryGetValue(id, out RectTransform target) || IsTutorialOnly(target))
                    continue;

                ulong sourceParentId = source.parent != null ? GetLocalId(source.parent) : 0;
                if (sourceParentId != 0
                    && targetRects.TryGetValue(sourceParentId, out RectTransform targetParent)
                    && target.parent != targetParent)
                {
                    target.SetParent(targetParent, false);
                }

                target.name = source.name;
                CopyRect(source, target);
                CopyUiComponents(source.gameObject, target.gameObject, GetPath(source));
            }

            foreach ((ulong id, RectTransform source) in sourceRects)
            {
                if (targetRects.TryGetValue(id, out RectTransform target)
                    && !IsTutorialOnly(target)
                    && target.parent != null)
                {
                    target.SetSiblingIndex(Mathf.Min(source.GetSiblingIndex(), target.parent.childCount - 1));
                }
            }

            RewireClonedShopController(tutorialScene);
            EditorSceneManager.MarkSceneDirty(tutorialScene);
            EditorSceneManager.SaveScene(tutorialScene);
            EditorSceneManager.CloseScene(gameScene, true);
        }

        private static void ReplaceSelfContainedUi<T>(Scene sourceScene, Scene targetScene, bool preserveTargetActive)
            where T : Component
        {
            T source = FindInScene<T>(sourceScene);
            T target = FindInScene<T>(targetScene);
            if (source == null || target == null)
                throw new InvalidOperationException($"Could not synchronize {typeof(T).Name}.");

            bool wasActive = target.gameObject.activeSelf;
            int siblingIndex = target.transform.GetSiblingIndex();
            string sourceParentName = source.transform.parent != null ? source.transform.parent.name : string.Empty;
            Transform targetParent = FindNamedTransform(targetScene, sourceParentName);
            if (targetParent == null)
                throw new InvalidOperationException($"Target parent for {typeof(T).Name} is missing.");

            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = source.gameObject.name;
            SceneManager.MoveGameObjectToScene(clone, targetScene);
            clone.transform.SetParent(targetParent, false);
            clone.transform.SetSiblingIndex(Mathf.Min(siblingIndex, targetParent.childCount - 1));
            clone.SetActive(!preserveTargetActive || wasActive);
            TintHierarchy(clone.transform, clone.name);
            UnityEngine.Object.DestroyImmediate(target.gameObject);
        }

        private static void RewireClonedShopController(Scene tutorialScene)
        {
            ShopEventUI ui = FindInScene<ShopEventUI>(tutorialScene);
            ShopEventController controller = FindInScene<ShopEventController>(tutorialScene);
            if (ui == null || controller == null)
                throw new InvalidOperationException("Tutorial Shop Event references are missing.");

            SerializedObject serialized = new(ui);
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Dictionary<ulong, RectTransform> BuildUiRectMap(Scene scene)
        {
            Dictionary<ulong, RectTransform> result = new();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    foreach (RectTransform rect in canvas.GetComponentsInChildren<RectTransform>(true))
                        result[GetLocalId(rect)] = rect;
                }
            }
            return result;
        }

        private static ulong GetLocalId(UnityEngine.Object value)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(value).targetObjectId;
        }

        private static void CopyRect(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.pivot = source.pivot;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static void CopyUiComponents(GameObject source, GameObject target, string path)
        {
            Image sourceImage = source.GetComponent<Image>();
            Image targetImage = target.GetComponent<Image>();
            if (sourceImage != null && targetImage != null)
            {
                targetImage.sprite = sourceImage.sprite;
                targetImage.overrideSprite = sourceImage.overrideSprite;
                targetImage.type = sourceImage.type;
                targetImage.preserveAspect = sourceImage.preserveAspect;
                targetImage.fillCenter = sourceImage.fillCenter;
                targetImage.fillMethod = sourceImage.fillMethod;
                targetImage.fillAmount = sourceImage.fillAmount;
                targetImage.fillClockwise = sourceImage.fillClockwise;
                targetImage.fillOrigin = sourceImage.fillOrigin;
                targetImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
            }

            Graphic sourceGraphic = source.GetComponent<Graphic>();
            Graphic targetGraphic = target.GetComponent<Graphic>();
            if (sourceGraphic != null && targetGraphic != null)
            {
                targetGraphic.color = MapTutorialColor(sourceGraphic.color, path);
                targetGraphic.material = sourceGraphic.material;
                targetGraphic.raycastTarget = sourceGraphic.raycastTarget;
            }

            TMP_Text sourceText = source.GetComponent<TMP_Text>();
            TMP_Text targetText = target.GetComponent<TMP_Text>();
            if (sourceText != null && targetText != null)
            {
                targetText.font = sourceText.font;
                targetText.fontSharedMaterial = sourceText.fontSharedMaterial;
                targetText.fontSize = sourceText.fontSize;
                targetText.fontStyle = sourceText.fontStyle;
                targetText.alignment = sourceText.alignment;
                targetText.margin = sourceText.margin;
                targetText.characterSpacing = sourceText.characterSpacing;
                targetText.wordSpacing = sourceText.wordSpacing;
                targetText.lineSpacing = sourceText.lineSpacing;
                targetText.paragraphSpacing = sourceText.paragraphSpacing;
                targetText.enableAutoSizing = sourceText.enableAutoSizing;
                targetText.fontSizeMin = sourceText.fontSizeMin;
                targetText.fontSizeMax = sourceText.fontSizeMax;
                targetText.textWrappingMode = sourceText.textWrappingMode;
                targetText.overflowMode = sourceText.overflowMode;
            }

            CanvasGroup sourceGroup = source.GetComponent<CanvasGroup>();
            CanvasGroup targetGroup = target.GetComponent<CanvasGroup>();
            if (sourceGroup != null && targetGroup != null)
            {
                targetGroup.alpha = sourceGroup.alpha;
                targetGroup.interactable = sourceGroup.interactable;
                targetGroup.blocksRaycasts = sourceGroup.blocksRaycasts;
                targetGroup.ignoreParentGroups = sourceGroup.ignoreParentGroups;
            }

            CopyLayout(source, target);
            CopySelectable(source, target, path);
        }

        private static void CopyLayout(GameObject source, GameObject target)
        {
            HorizontalOrVerticalLayoutGroup sourceFlow = source.GetComponent<HorizontalOrVerticalLayoutGroup>();
            HorizontalOrVerticalLayoutGroup targetFlow = target.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (sourceFlow != null && targetFlow != null)
            {
                targetFlow.padding = new RectOffset(
                    sourceFlow.padding.left, sourceFlow.padding.right,
                    sourceFlow.padding.top, sourceFlow.padding.bottom);
                targetFlow.spacing = sourceFlow.spacing;
                targetFlow.childAlignment = sourceFlow.childAlignment;
                targetFlow.childControlWidth = sourceFlow.childControlWidth;
                targetFlow.childControlHeight = sourceFlow.childControlHeight;
                targetFlow.childForceExpandWidth = sourceFlow.childForceExpandWidth;
                targetFlow.childForceExpandHeight = sourceFlow.childForceExpandHeight;
                targetFlow.childScaleWidth = sourceFlow.childScaleWidth;
                targetFlow.childScaleHeight = sourceFlow.childScaleHeight;
                targetFlow.reverseArrangement = sourceFlow.reverseArrangement;
            }

            GridLayoutGroup sourceGrid = source.GetComponent<GridLayoutGroup>();
            GridLayoutGroup targetGrid = target.GetComponent<GridLayoutGroup>();
            if (sourceGrid != null && targetGrid != null)
            {
                targetGrid.padding = new RectOffset(
                    sourceGrid.padding.left, sourceGrid.padding.right,
                    sourceGrid.padding.top, sourceGrid.padding.bottom);
                targetGrid.cellSize = sourceGrid.cellSize;
                targetGrid.spacing = sourceGrid.spacing;
                targetGrid.startCorner = sourceGrid.startCorner;
                targetGrid.startAxis = sourceGrid.startAxis;
                targetGrid.childAlignment = sourceGrid.childAlignment;
                targetGrid.constraint = sourceGrid.constraint;
                targetGrid.constraintCount = sourceGrid.constraintCount;
            }

            ContentSizeFitter sourceFitter = source.GetComponent<ContentSizeFitter>();
            ContentSizeFitter targetFitter = target.GetComponent<ContentSizeFitter>();
            if (sourceFitter != null && targetFitter != null)
            {
                targetFitter.horizontalFit = sourceFitter.horizontalFit;
                targetFitter.verticalFit = sourceFitter.verticalFit;
            }
        }

        private static void CopySelectable(GameObject source, GameObject target, string path)
        {
            Selectable sourceSelectable = source.GetComponent<Selectable>();
            Selectable targetSelectable = target.GetComponent<Selectable>();
            if (sourceSelectable == null || targetSelectable == null)
                return;

            targetSelectable.transition = sourceSelectable.transition;
            ColorBlock colors = sourceSelectable.colors;
            colors.normalColor = MapTutorialColor(colors.normalColor, path);
            colors.highlightedColor = MapTutorialColor(colors.highlightedColor, path);
            colors.pressedColor = MapTutorialColor(colors.pressedColor, path);
            colors.selectedColor = MapTutorialColor(colors.selectedColor, path);
            colors.disabledColor = MapTutorialColor(colors.disabledColor, path);
            targetSelectable.colors = colors;
            targetSelectable.spriteState = sourceSelectable.spriteState;
            targetSelectable.animationTriggers = sourceSelectable.animationTriggers;
        }

        private static void TintHierarchy(Transform root, string rootPath)
        {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.color = MapTutorialColor(graphic.color, rootPath + "/" + GetPath(graphic.transform));

            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                ColorBlock colors = selectable.colors;
                string path = rootPath + "/" + GetPath(selectable.transform);
                colors.normalColor = MapTutorialColor(colors.normalColor, path);
                colors.highlightedColor = MapTutorialColor(colors.highlightedColor, path);
                colors.pressedColor = MapTutorialColor(colors.pressedColor, path);
                colors.selectedColor = MapTutorialColor(colors.selectedColor, path);
                colors.disabledColor = MapTutorialColor(colors.disabledColor, path);
                selectable.colors = colors;
            }
        }

        private static Color MapTutorialColor(Color source, string path)
        {
            if (path.Contains("Minus Fill", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Game Over", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Red Flash", StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }

            Color.RGBToHSV(source, out _, out float saturation, out float value);
            if (saturation < 0.08f)
                return source;

            Color result = Color.HSVToRGB(TutorialGreenHue, saturation, value);
            result.a = source.a;
            return result;
        }

        private static bool IsGreenPalette(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out _);
            return saturation < 0.08f
                   || Mathf.Abs(Mathf.DeltaAngle(hue * 360f, TutorialGreenHue * 360f)) < 3f;
        }

        private static bool IsTutorialOnly(Transform target)
        {
            for (Transform current = target; current != null; current = current.parent)
            {
                if (current.name.StartsWith("Tutorial ", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static string GetPath(Transform value)
        {
            string path = value.name;
            for (Transform current = value.parent; current != null; current = current.parent)
                path = current.name + "/" + path;
            return path;
        }

        private static void CreateOrUpdatePromptPrefab()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Mona12 SDF.asset");
            if (font == null)
                throw new InvalidOperationException("Mona12 font asset is missing.");

            GameObject root = new(
                "Tutorial Colorblind Prompt",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup), typeof(TutorialColorblindPrompt));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            Image backdrop = root.GetComponent<Image>();
            backdrop.color = new Color(0.004f, 0.012f, 0.009f, 0.82f);
            backdrop.raycastTarget = true;

            GameObject panel = CreateUiObject(root.transform, "Panel", typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(660f, 280f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.12f, 0.075f, 0.98f);

            GameObject message = CreateText(
                panel.transform, "Message", font, 24f,
                "tutorial.colorblind.prompt", "Apply Colorblind Mode now?");
            RectTransform messageRect = message.GetComponent<RectTransform>();
            messageRect.anchorMin = messageRect.anchorMax = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = new Vector2(0f, 42f);
            messageRect.sizeDelta = new Vector2(560f, 130f);

            Button apply = CreatePromptButton(
                panel.transform, "Apply Button", font, new Vector2(-110f, -82f),
                "tutorial.colorblind.apply", "Apply");
            Button decline = CreatePromptButton(
                panel.transform, "Later Button", font, new Vector2(110f, -82f),
                "tutorial.colorblind.later", "Later");

            SerializedObject serialized = new(root.GetComponent<TutorialColorblindPrompt>());
            serialized.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            serialized.FindProperty("panel").objectReferenceValue = panelRect;
            serialized.FindProperty("applyButton").objectReferenceValue = apply;
            serialized.FindProperty("declineButton").objectReferenceValue = decline;
            serialized.FindProperty("animationDuration").floatValue = 0.16f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PromptPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ConfigureTutorialPrompt()
        {
            Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            TutorialDirector director = FindInScene<TutorialDirector>(scene);
            Transform upperCanvas = FindNamedTransform(scene, "Upper Canvas");
            if (director == null || upperCanvas == null)
                throw new InvalidOperationException("Tutorial Director or Upper Canvas is missing.");

            TutorialColorblindPrompt prompt = FindInScene<TutorialColorblindPrompt>(scene);
            if (prompt == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptPrefabPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Could not instantiate Tutorial Colorblind Prompt.");
                instance.transform.SetParent(upperCanvas, false);
                instance.transform.SetAsLastSibling();
                instance.SetActive(false);
                prompt = instance.GetComponent<TutorialColorblindPrompt>();
            }

            SerializedObject serialized = new(director);
            serialized.FindProperty("colorblindPrompt").objectReferenceValue = prompt;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject CreateUiObject(Transform parent, string name, params Type[] components)
        {
            List<Type> types = new() { typeof(RectTransform), typeof(CanvasRenderer) };
            types.AddRange(components);
            GameObject result = new(name, types.ToArray());
            result.transform.SetParent(parent, false);
            return result;
        }

        private static GameObject CreateText(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float fontSize,
            string key,
            string fallback)
        {
            GameObject result = CreateUiObject(parent, name, typeof(TextMeshProUGUI), typeof(LocalizedText));
            TextMeshProUGUI text = result.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.88f, 1f, 0.94f, 1f);
            text.textWrappingMode = TextWrappingModes.Normal;
            result.GetComponent<LocalizedText>().SetKey(key, fallback);
            return result;
        }

        private static Button CreatePromptButton(
            Transform parent,
            string name,
            TMP_FontAsset font,
            Vector2 position,
            string key,
            string fallback)
        {
            GameObject buttonObject = CreateUiObject(
                parent, name, typeof(Image), typeof(Button), typeof(UIButtonInteraction));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(190f, 54f);
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.82f, 0.48f, 1f);

            GameObject label = CreateText(buttonObject.transform, "Text", font, 22f, key, fallback);
            Stretch(label.GetComponent<RectTransform>());
            label.GetComponent<TextMeshProUGUI>().color = new Color(0.02f, 0.08f, 0.05f, 1f);
            return buttonObject.GetComponent<Button>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static Transform FindNamedTransform(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == name)
                        return candidate;
                }
            }
            return null;
        }

        private static void ValidateRect(RectTransform source, RectTransform target, string label)
        {
            if (source == null || target == null
                || source.anchorMin != target.anchorMin
                || source.anchorMax != target.anchorMax
                || source.anchoredPosition != target.anchoredPosition
                || source.sizeDelta != target.sizeDelta)
            {
                throw new InvalidOperationException($"{label} layout does not match GameScene.");
            }
        }
    }
}
#endif
