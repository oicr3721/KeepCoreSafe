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
    public static class CoreLoopRefactorSetup
    {
        private const string ScenePath = "Assets/Scenes/FoundationTestScene.unity";
        private const string BlockButtonPath = "Assets/Prefabs/UI/Block Button.prefab";
        private const string ShopButtonPath = "Assets/Prefabs/UI/ShopOfferButton.prefab";
        private const string KoreanFontPath = "Assets/Fonts/LimgulMono16 SDF 1.asset";
        private const string ColorFolder = "Assets/Resources/Data/Block/Colors";
        private const string BasicFolder = "Assets/Resources/Data/Block/Basic";
        private const string SystemFolder = "Assets/Resources/Data/Systems";
        private const string ShopFolder = "Assets/Resources/Data/Shop";

        [MenuItem("Keep Core Safe/Setup New Core Loop")]
        public static void Setup()
        {
            EnsureFolder(ColorFolder);
            EnsureFolder(BasicFolder);
            EnsureFolder(SystemFolder);
            EnsureFolder(ShopFolder);

            WallBlockData wall = Load<WallBlockData>("Assets/Resources/Data/Block/WallData.asset");
            AttackBlockData attack = Load<AttackBlockData>("Assets/Resources/Data/Block/AttackData.asset");
            HealerBlockData healer = Load<HealerBlockData>("Assets/Resources/Data/Block/HealerData.asset");
            SupportBlockData support = Load<SupportBlockData>("Assets/Resources/Data/Block/SupportData.asset");

            BlockColorData red = ConfigureColor("Red", new Color(0.95f, 0.22f, 0.2f, 1f));
            BlockColorData blue = ConfigureColor("Blue", new Color(0.2f, 0.55f, 1f, 1f));
            BlockColorData green = ConfigureColor("Green", new Color(0.25f, 0.9f, 0.4f, 1f));
            ConfigureColor("Yellow", new Color(1f, 0.82f, 0.18f, 1f));

            BasicBlockData redBlock = ConfigureBasic("Red", red, wall);
            BasicBlockData blueBlock = ConfigureBasic("Blue", blue, wall);
            BasicBlockData greenBlock = ConfigureBasic("Green", green, wall);
            ConfigureBasic("Yellow", Load<BlockColorData>($"{ColorFolder}/Yellow.asset"), wall);

            BlockMatchData matchData = ConfigureMatchData(red, attack, blue, support, green, healer);
            BlockSupplyData supplyData = ConfigureSupplyData(
                new[] { redBlock, blueBlock, greenBlock },
                new BlockData[] { attack, healer, support });
            ShopEventData shopData = ConfigureShopData(attack, healer, support);

            SetupBlockButtonPrefab();
            GameObject shopButtonPrefab = SetupShopButtonPrefab();
            SetupScene(supplyData, matchData, shopData, shopButtonPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CORE_LOOP_REFACTOR_SETUP_COMPLETE");
        }

        private static BlockColorData ConfigureColor(string name, Color color)
        {
            BlockColorData data = GetOrCreate<BlockColorData>($"{ColorFolder}/{name}.asset");
            SerializedObject serialized = new(data);
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("color").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static BasicBlockData ConfigureBasic(
            string name,
            BlockColorData color,
            WallBlockData wall)
        {
            BasicBlockData data = GetOrCreate<BasicBlockData>($"{BasicFolder}/{name}Basic.asset");
            SerializedObject serialized = new(data);
            serialized.FindProperty("displayName").stringValue = $"{name} Block";
            serialized.FindProperty("description").stringValue =
                $"능력은 없지만 같은 {name} 색상 블록 3개를 연결하면 스킬 블록으로 변환됩니다.";
            serialized.FindProperty("maxHP").intValue = 100;
            serialized.FindProperty("dismantleValue").intValue = 2;
            serialized.FindProperty("sprite").objectReferenceValue = wall.Sprite;
            serialized.FindProperty("prefab").objectReferenceValue = wall.Prefab;
            serialized.FindProperty("additionalProperties").intValue = 0;
            serialized.FindProperty("color").objectReferenceValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static BlockMatchData ConfigureMatchData(
            BlockColorData red,
            AttackBlockData attack,
            BlockColorData blue,
            SupportBlockData support,
            BlockColorData green,
            HealerBlockData healer)
        {
            BlockMatchData data = GetOrCreate<BlockMatchData>($"{SystemFolder}/BlockMatchData.asset");
            SerializedObject serialized = new(data);
            SerializedProperty rules = serialized.FindProperty("rules");
            rules.arraySize = 3;
            ConfigureRule(rules.GetArrayElementAtIndex(0), red, attack);
            ConfigureRule(rules.GetArrayElementAtIndex(1), blue, support);
            ConfigureRule(rules.GetArrayElementAtIndex(2), green, healer);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void ConfigureRule(
            SerializedProperty rule,
            BlockColorData color,
            BlockData result)
        {
            rule.FindPropertyRelative("sourceColor").objectReferenceValue = color;
            rule.FindPropertyRelative("resultBlock").objectReferenceValue = result;
            rule.FindPropertyRelative("requiredCount").intValue = 3;
        }

        private static BlockSupplyData ConfigureSupplyData(
            BasicBlockData[] basics,
            BlockData[] rareBlocks)
        {
            BlockSupplyData data = GetOrCreate<BlockSupplyData>($"{SystemFolder}/BlockSupplyData.asset");
            SerializedObject serialized = new(data);
            serialized.FindProperty("minimumBlocks").intValue = 3;
            serialized.FindProperty("maximumBlocks").intValue = 5;
            serialized.FindProperty("rareBlockChance").floatValue = 0.05f;
            serialized.FindProperty("initialRerollCost").floatValue = 3f;
            serialized.FindProperty("rerollCostIncrease").floatValue = 2f;
            ConfigureWeightedBlocks(serialized.FindProperty("basicBlocks"), basics);
            ConfigureWeightedBlocks(serialized.FindProperty("rareBlocks"), rareBlocks);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void ConfigureWeightedBlocks<T>(SerializedProperty list, T[] blocks)
            where T : BlockData
        {
            list.arraySize = blocks.Length;
            for (int i = 0; i < blocks.Length; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("block").objectReferenceValue = blocks[i];
                entry.FindPropertyRelative("weight").floatValue = 1f;
            }
        }

        private static ShopEventData ConfigureShopData(
            AttackBlockData attack,
            HealerBlockData healer,
            SupportBlockData support)
        {
            GrantedBlockShopOfferData attackOffer = ConfigureOffer(
                "Attack", "완성된 공격 블록을 이번 배치 목록에 추가합니다.", 12f, attack);
            GrantedBlockShopOfferData healerOffer = ConfigureOffer(
                "Healer", "완성된 회복 블록을 이번 배치 목록에 추가합니다.", 12f, healer);
            GrantedBlockShopOfferData supportOffer = ConfigureOffer(
                "Support", "완성된 지원 블록을 이번 배치 목록에 추가합니다.", 12f, support);

            ShopEventData data = GetOrCreate<ShopEventData>($"{ShopFolder}/ShopEventData.asset");
            SerializedObject serialized = new(data);
            serialized.FindProperty("firstWave").intValue = 3;
            serialized.FindProperty("waveInterval").intValue = 3;
            serialized.FindProperty("offersPerEvent").intValue = 3;
            SerializedProperty offers = serialized.FindProperty("offers");
            offers.arraySize = 3;
            offers.GetArrayElementAtIndex(0).objectReferenceValue = attackOffer;
            offers.GetArrayElementAtIndex(1).objectReferenceValue = healerOffer;
            offers.GetArrayElementAtIndex(2).objectReferenceValue = supportOffer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static GrantedBlockShopOfferData ConfigureOffer(
            string name,
            string description,
            float cost,
            BlockData grantedBlock)
        {
            GrantedBlockShopOfferData data =
                GetOrCreate<GrantedBlockShopOfferData>($"{ShopFolder}/{name}Offer.asset");
            SerializedObject serialized = new(data);
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("cost").floatValue = cost;
            serialized.FindProperty("grantedBlock").objectReferenceValue = grantedBlock;
            serialized.FindProperty("playRareAppearance").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void SetupBlockButtonPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BlockButtonPath);
            RareBlockAppearance appearance = root.GetComponent<RareBlockAppearance>();
            if (appearance == null)
                appearance = root.AddComponent<RareBlockAppearance>();

            Transform existing = root.transform.Find("Rare Shine");
            GameObject shineObject = existing != null
                ? existing.gameObject
                : new GameObject("Rare Shine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shineObject.transform.SetParent(root.transform, false);
            shineObject.transform.SetAsFirstSibling();
            RectTransform shineRect = shineObject.GetComponent<RectTransform>();
            Stretch(shineRect);
            Image shine = shineObject.GetComponent<Image>();
            shine.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            shine.color = new Color(1f, 0.88f, 0.18f, 0f);
            shine.raycastTarget = false;

            TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = Color.white;
                ApplyFont(label);
            }

            SerializedObject serialized = new(appearance);
            serialized.FindProperty("target").objectReferenceValue = root.GetComponent<RectTransform>();
            serialized.FindProperty("shine").objectReferenceValue = shine;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, BlockButtonPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static GameObject SetupShopButtonPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShopButtonPath);
            if (existing != null)
                return existing;

            GameObject root = new(
                "ShopOfferButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 170f);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.08f, 0.16f, 0.2f, 0.98f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateText(root.transform, "Text", "Offer", 20f);
            Stretch(label.rectTransform);
            label.margin = new Vector4(14f, 14f, 14f, 14f);
            label.alignment = TextAlignmentOptions.Center;
            //label.enableWordWrapping = true;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ShopButtonPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(ShopButtonPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopButtonPath);
            return savedPrefab != null ? savedPrefab : prefab;
        }

        private static void SetupScene(
            BlockSupplyData supplyData,
            BlockMatchData matchData,
            ShopEventData shopData,
            GameObject shopButtonPrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlacementController placement = Object.FindFirstObjectByType<PlacementController>(FindObjectsInactive.Include);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            PreparationUI preparationUI = Object.FindFirstObjectByType<PreparationUI>(FindObjectsInactive.Include);
            GameDefaultUI gameUI = Object.FindFirstObjectByType<GameDefaultUI>(FindObjectsInactive.Include);

            BlockSupplyController supply = gameManager.GetComponent<BlockSupplyController>();
            if (supply == null)
                supply = gameManager.gameObject.AddComponent<BlockSupplyController>();
            SerializedObject supplyObject = new(supply);
            supplyObject.FindProperty("supplyData").objectReferenceValue = supplyData;
            supplyObject.ApplyModifiedPropertiesWithoutUndo();

            ShopEventController shop = gameManager.GetComponent<ShopEventController>();
            if (shop == null)
                shop = gameManager.gameObject.AddComponent<ShopEventController>();
            SerializedObject shopObject = new(shop);
            shopObject.FindProperty("shopData").objectReferenceValue = shopData;
            shopObject.FindProperty("supplyController").objectReferenceValue = supply;
            shopObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject placementObject = new(placement);
            placementObject.FindProperty("supplyController").objectReferenceValue = supply;
            placementObject.FindProperty("matchData").objectReferenceValue = matchData;
            placementObject.ApplyModifiedPropertiesWithoutUndo();

            Button rerollButton = GetOrCreateRerollButton(preparationUI.transform, out TMP_Text rerollLabel);
            SerializedObject preparationObject = new(preparationUI);
            preparationObject.FindProperty("placementController").objectReferenceValue = placement;
            preparationObject.FindProperty("supplyController").objectReferenceValue = supply;
            preparationObject.FindProperty("inventoryRoot").objectReferenceValue = preparationUI.transform;
            preparationObject.FindProperty("rerollButton").objectReferenceValue = rerollButton;
            preparationObject.FindProperty("rerollLabel").objectReferenceValue = rerollLabel;
            preparationObject.ApplyModifiedPropertiesWithoutUndo();

            SetupShopUI(gameUI.transform, shop, shopButtonPrefab);
            SetupSharedBlockHover(gameManager, placement);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupSharedBlockHover(
            GameManager gameManager,
            PlacementController placement)
        {
            SerializedObject placementData = new(placement);
            PlacementVisualizer visualizer =
                placementData.FindProperty("effectVisualizer").objectReferenceValue as PlacementVisualizer;
            BlockDescriptionTooltip tooltip =
                Object.FindFirstObjectByType<BlockDescriptionTooltip>(FindObjectsInactive.Include);
            if (visualizer == null || tooltip == null)
            {
                Debug.LogError("Shared tooltip or effect visualizer was not found.");
                return;
            }

            visualizer.name = "Block Effect Visualizer";
            visualizer.transform.SetParent(null, true);
            visualizer.transform.localScale = Vector3.one;
            visualizer.gameObject.SetActive(true);

            WorldBlockHoverController hover = gameManager.GetComponent<WorldBlockHoverController>();
            if (hover == null)
                hover = gameManager.gameObject.AddComponent<WorldBlockHoverController>();
            SerializedObject hoverData = new(hover);
            hoverData.FindProperty("tooltip").objectReferenceValue = tooltip;
            hoverData.FindProperty("effectVisualizer").objectReferenceValue = visualizer;
            hoverData.ApplyModifiedPropertiesWithoutUndo();

            ConfigureTooltipDetails(tooltip);
        }

        private static void ConfigureTooltipDetails(BlockDescriptionTooltip tooltip)
        {
            RectTransform panel = tooltip.transform as RectTransform;
            panel.sizeDelta = new Vector2(420f, 205f);

            TMP_Text title = tooltip.transform.Find("Title")?.GetComponent<TMP_Text>();
            TMP_Text description = tooltip.transform.Find("Description")?.GetComponent<TMP_Text>();
            TMP_Text details = GetOrCreateText(
                tooltip.transform,
                "Details",
                "HP 100  |  철거 가치 2",
                16f);

            ConfigureTooltipLine(title, -10f, 34f, 24f, FontStyles.Bold);
            ConfigureTooltipLine(description, -48f, 78f, 17f, FontStyles.Normal);
            ConfigureTooltipLine(details, -132f, 62f, 16f, FontStyles.Normal);
            details.color = new Color(0.68f, 0.88f, 1f, 1f);

            SerializedObject tooltipData = new(tooltip);
            tooltipData.FindProperty("detailsLabel").objectReferenceValue = details;
            tooltipData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTooltipLine(
            TMP_Text text,
            float y,
            float height,
            float fontSize,
            FontStyles style)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-28f, height);
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            ApplyFont(text);
        }

        private static Button GetOrCreateRerollButton(Transform parent, out TMP_Text label)
        {
            Transform existing = parent.Find("Reroll Button");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Reroll Button",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 100f);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.2f, 0.32f, 0.46f, 1f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            label = GetOrCreateText(root.transform, "Text", "Reroll 3", 20f);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static void SetupShopUI(
            Transform parent,
            ShopEventController controller,
            GameObject offerButtonPrefab)
        {
            Transform existing = parent.Find("Shop Event UI");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject("Shop Event UI", typeof(RectTransform), typeof(ShopEventUI));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            root.transform.SetAsLastSibling();

            GameObject visual = GetOrCreatePanel(root.transform, "Visual");
            Stretch(visual.GetComponent<RectTransform>());
            visual.GetComponent<Image>().color = new Color(0.01f, 0.025f, 0.04f, 0.94f);

            TMP_Text title = GetOrCreateText(visual.transform, "Title", "SHOP EVENT", 46f);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.78f);
            titleRect.sizeDelta = new Vector2(600f, 70f);
            title.alignment = TextAlignmentOptions.Center;

            Transform offerRoot = visual.transform.Find("Offers");
            if (offerRoot == null)
            {
                GameObject offers = new("Offers", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                offerRoot = offers.transform;
                offerRoot.SetParent(visual.transform, false);
            }
            RectTransform offersRect = offerRoot as RectTransform;
            offersRect.anchorMin = offersRect.anchorMax = new Vector2(0.5f, 0.48f);
            offersRect.sizeDelta = new Vector2(800f, 180f);
            HorizontalLayoutGroup layout = offerRoot.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button closeButton = GetOrCreateCloseButton(visual.transform);
            ShopEventUI ui = root.GetComponent<ShopEventUI>();
            SerializedObject serialized = new(ui);
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.FindProperty("visualRoot").objectReferenceValue = visual;
            serialized.FindProperty("offerRoot").objectReferenceValue = offerRoot;
            serialized.FindProperty("offerButtonPrefab").objectReferenceValue = offerButtonPrefab;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button GetOrCreateCloseButton(Transform parent)
        {
            Transform existing = parent.Find("Close Button");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Close Button",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.2f);
            rect.sizeDelta = new Vector2(180f, 54f);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.18f, 0.7f, 0.48f, 1f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            TMP_Text label = GetOrCreateText(root.transform, "Text", "Close", 22f);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static GameObject GetOrCreatePanel(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;
            GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            return panel;
        }

        private static TMP_Text GetOrCreateText(
            Transform parent,
            string name,
            string text,
            float fontSize)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                TMP_Text existingText = existing.GetComponent<TMP_Text>();
                existingText.text = text;
                existingText.fontSize = fontSize;
                ApplyFont(existingText);
                return existingText;
            }
            return CreateText(parent, name, text, fontSize);
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            TMP_Text label = root.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.raycastTarget = false;
            ApplyFont(label);
            return label;
        }

        private static void ApplyFont(TMP_Text label)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font != null)
                label.font = font;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
