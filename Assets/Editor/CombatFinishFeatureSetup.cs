using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    public static class CombatFinishFeatureSetup
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string PrefabFolder = "Assets/Prefabs/Presentation";
        private const string MaterialFolder = "Assets/Materials";
        private const string PulsePrefabPath = PrefabFolder + "/CoreEnergyPulse.prefab";
        private const string ShockwavePrefabPath = PrefabFolder + "/CoreShockwave.prefab";
        private const string ShockwaveMaterialPath = MaterialFolder + "/CoreShockwave.mat";
        private const string KoreanFontPath = "Assets/Fonts/LimgulMono16 SDF 1.asset";

        [MenuItem("Keep Core Safe/Setup Combat Finish Features")]
        public static void Setup()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            CreatePulsePrefab();
            CreateShockwavePrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            GameDefaultUI gameUI = Object.FindFirstObjectByType<GameDefaultUI>(FindObjectsInactive.Include);
            if (gameManager == null || gameUI == null)
            {
                Debug.LogError("GameManager or GameDefaultUI was not found in GameScene.");
                return;
            }

            SetupPresentationController(gameManager);
            SetupCountdown(gameUI.transform);
            SetupStageClearAnnouncement(gameUI.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("COMBAT_FINISH_FEATURE_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Combat Finish Features")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            StageClearPresentationController presentation =
                Object.FindFirstObjectByType<StageClearPresentationController>(FindObjectsInactive.Include);
            ShockwaveCountdownUI countdown =
                Object.FindFirstObjectByType<ShockwaveCountdownUI>(FindObjectsInactive.Include);
            StageClearAnnouncementUI announcement =
                Object.FindFirstObjectByType<StageClearAnnouncementUI>(FindObjectsInactive.Include);

            if (gameManager == null || presentation == null || countdown == null || announcement == null)
                throw new System.InvalidOperationException("Combat finish scene components are incomplete.");

            SerializedObject managerData = new(gameManager);
            SerializedObject presentationData = new(presentation);
            if (managerData.FindProperty("stageClearPresentation").objectReferenceValue == null
                || presentationData.FindProperty("energyPulse").objectReferenceValue == null
                || presentationData.FindProperty("shockwave").objectReferenceValue == null)
            {
                throw new System.InvalidOperationException("Combat finish prefab references are incomplete.");
            }

            Debug.Log("COMBAT_FINISH_FEATURE_VALIDATION_COMPLETE");
        }

        private static GameObject CreatePulsePrefab()
        {
            GameObject root = new(
                "CoreEnergyPulse",
                typeof(SpriteRenderer),
                typeof(CoreEnergyPulseView));
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            renderer.color = new Color(0.45f, 1f, 0.82f, 0f);
            renderer.sortingOrder = 60;

            SerializedObject serialized = new(root.GetComponent<CoreEnergyPulseView>());
            serialized.FindProperty("pulseRenderer").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PulsePrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(PulsePrefabPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PulsePrefabPath);
            return prefab;
        }

        private static GameObject CreateShockwavePrefab()
        {
            Material material = GetOrCreateShockwaveMaterial();
            GameObject root = new(
                "CoreShockwave",
                typeof(LineRenderer),
                typeof(ShockwaveRingView));
            LineRenderer line = root.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.sharedMaterial = material;
            line.sortingOrder = 55;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.5f);
            }

            SerializedObject serialized = new(root.GetComponent<ShockwaveRingView>());
            serialized.FindProperty("ringRenderer").objectReferenceValue = line;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ShockwavePrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(ShockwavePrefabPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShockwavePrefabPath);
            return prefab;
        }

        private static Material GetOrCreateShockwaveMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ShockwaveMaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            material = new Material(shader)
            {
                name = "CoreShockwave",
                color = Color.white
            };
            AssetDatabase.CreateAsset(material, ShockwaveMaterialPath);
            return material;
        }

        private static void SetupPresentationController(GameManager gameManager)
        {
            StageClearPresentationController controller =
                gameManager.GetComponentInChildren<StageClearPresentationController>(true);
            if (controller == null)
            {
                Transform presentationRoot = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    gameManager.transform,
                    "Presentation");
                Transform stageClearRoot = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    presentationRoot,
                    "Stage Clear Presentation");
                controller = stageClearRoot.gameObject.AddComponent<StageClearPresentationController>();
            }

            GameManagerStructureRefactorSetup.ConfigureStageClearViews(
                gameManager.gameObject.scene,
                controller);

            SerializedObject manager = new(gameManager);
            manager.FindProperty("stageClearPresentation").objectReferenceValue = controller;
            manager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupCountdown(Transform parent)
        {
            Transform existing = parent.Find("Shockwave Countdown");
            GameObject controller = existing != null
                ? existing.gameObject
                : new GameObject("Shockwave Countdown", typeof(RectTransform), typeof(ShockwaveCountdownUI));
            controller.transform.SetParent(parent, false);

            Transform visualTransform = controller.transform.Find("Visual");
            GameObject visual = visualTransform != null
                ? visualTransform.gameObject
                : new GameObject("Visual", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            visual.transform.SetParent(controller.transform, false);

            RectTransform rect = visual.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(420f, 82f);

            TMP_Text label = visual.GetComponent<TMP_Text>();
            ApplyFont(label);
            label.text = "CORE ENERGY\n0 / 0";
            label.fontSize = 27f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.55f, 1f, 0.82f, 1f);
            label.raycastTarget = false;

            ShockwaveCountdownUI ui = controller.GetComponent<ShockwaveCountdownUI>();
            SerializedObject serialized = new(ui);
            serialized.FindProperty("visualRoot").objectReferenceValue = visual;
            serialized.FindProperty("energyLabel").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupStageClearAnnouncement(Transform parent)
        {
            Transform existing = parent.Find("Stage Clear Announcement");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Stage Clear Announcement",
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(TextMeshProUGUI),
                    typeof(StageClearAnnouncementUI));
            root.transform.SetParent(parent, false);
            root.transform.SetAsLastSibling();

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 55f);
            rect.sizeDelta = new Vector2(760f, 150f);

            TMP_Text label = root.GetComponent<TMP_Text>();
            ApplyFont(label);
            label.text = "STAGE CLEAR\n<size=45%>SHOCKWAVE DEPLOYED</size>";
            label.fontSize = 58f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.55f, 1f, 0.82f, 1f);
            label.raycastTarget = false;

            StageClearAnnouncementUI ui = root.GetComponent<StageClearAnnouncementUI>();
            SerializedObject serialized = new(ui);
            serialized.FindProperty("visualRoot").objectReferenceValue = rect;
            serialized.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyFont(TMP_Text label)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font != null)
                label.font = font;
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
