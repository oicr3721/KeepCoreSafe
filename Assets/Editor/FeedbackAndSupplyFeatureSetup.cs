using KeepCoreSafe.Audio;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class FeedbackAndSupplyFeatureSetup
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string PresentationPrefabFolder = "Assets/Prefabs/Presentation";
        private const string ElectricLinePrefabPath = PresentationPrefabFolder + "/ElectricLine.prefab";
        private const string HealProjectilePrefabPath = PresentationPrefabFolder + "/HealProjectile.prefab";
        private const string SupportPrefabPath = "Assets/Prefabs/Blocks/SupportBlock.prefab";
        private const string HealerPrefabPath = "Assets/Prefabs/Blocks/HealerBlock.prefab";
        private const string BlockButtonPrefabPath = "Assets/Prefabs/UI/Block Button.prefab";
        private const string DifficultyDataPath = "Assets/Resources/Data/Systems/WaveDifficultyData.asset";
        private const string CorePulsePrefabPath = PresentationPrefabFolder + "/CoreEnergyPulse.prefab";
        private const string ClickClipPath = "Assets/Audio/Clips/Click.wav";
        private const string LandingClipPath = "Assets/Audio/Clips/Place.wav";
        private const string RareClipPath = "Assets/Audio/Clips/Clear.wav";
        private const string ConfirmClipPath = "Assets/Audio/Clips/place2.wav";
        private const string KoreanFontPath = "Assets/Fonts/LimgulMono16 SDF 1.asset";

        [MenuItem("Keep Core Safe/Setup Feedback, Audio and Supply Presentation")]
        public static void Setup()
        {
            EnsureFolder(PresentationPrefabFolder);
            GameObject electricLinePrefab = CreateElectricLinePrefab();
            GameObject healProjectilePrefab = CreateHealProjectilePrefab();
            AssignBlockEffectPrefabs(electricLinePrefab, healProjectilePrefab);
            AddCanvasGroupToBlockButton();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetupAudioManager();
            SetupWaveDifficulty();
            SetupSupplyPresentation();
            SetupWaveStartPresentation();
            SetupButtonInteractionsInScene();
            SetupButtonInteractionsInPrefabs();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SetupButtonInteractionsInAdditionalScenes();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("FEEDBACK_AUDIO_SUPPLY_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Feedback, Audio and Supply Presentation")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AudioManager audioManager = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            SupplyPresentationUI supply =
                Object.FindFirstObjectByType<SupplyPresentationUI>(FindObjectsInactive.Include);
            PreparationUI preparation =
                Object.FindFirstObjectByType<PreparationUI>(FindObjectsInactive.Include);
            WaveDifficultyController difficulty =
                Object.FindFirstObjectByType<WaveDifficultyController>(FindObjectsInactive.Include);
            UIShowHide startWave =
                Object.FindFirstObjectByType<UIShowHide>(FindObjectsInactive.Include);

            if (audioManager == null || supply == null || preparation == null
                || difficulty == null || startWave == null)
                throw new System.InvalidOperationException("Audio or Supply Presentation scene setup is incomplete.");

            SerializedObject audioData = new(audioManager);
            SerializedObject supplyData = new(supply);
            if (audioData.FindProperty("sfxSources").arraySize == 0
                || audioData.FindProperty("musicPlayer").objectReferenceValue == null
                || supplyData.FindProperty("backgroundPanel").objectReferenceValue == null
                || supplyData.FindProperty("blockContainer").objectReferenceValue == null
                || supplyData.FindProperty("confirmButton").objectReferenceValue == null
                || new SerializedObject(difficulty).FindProperty("difficultyData").objectReferenceValue == null)
            {
                throw new System.InvalidOperationException("Audio or Supply Presentation references are incomplete.");
            }

            ValidateBlockPrefab<SupportBlock>(SupportPrefabPath, "electricLinePrefab");
            ValidateBlockPrefab<HealerBlock>(HealerPrefabPath, "healProjectilePrefab");
            Debug.Log("FEEDBACK_AUDIO_SUPPLY_VALIDATION_COMPLETE");
        }

        private static void SetupWaveDifficulty()
        {
            WaveDifficultyData data = AssetDatabase.LoadAssetAtPath<WaveDifficultyData>(DifficultyDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WaveDifficultyData>();
                AssetDatabase.CreateAsset(data, DifficultyDataPath);
            }

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            WaveDifficultyController controller = gameManager.GetComponentInChildren<WaveDifficultyController>(true);
            if (controller == null)
            {
                Transform systems = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    gameManager.transform,
                    "Game Systems");
                Transform waveSystem = GameManagerStructureRefactorSetup.GetOrCreateChild(systems, "Wave System");
                controller = waveSystem.gameObject.AddComponent<WaveDifficultyController>();
            }
            SerializedObject serialized = new(controller);
            serialized.FindProperty("difficultyData").objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject manager = new(gameManager);
            manager.FindProperty("difficultyController").objectReferenceValue = controller;
            manager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupAudioManager()
        {
            AudioManager manager = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            GameObject root = manager != null
                ? manager.gameObject
                : new GameObject("Audio Manager", typeof(AudioManager));
            manager = root.GetComponent<AudioManager>();

            Transform sfxRoot = GetOrCreateChild(root.transform, "SFX Source Pool");
            const int sourceCount = 12;
            AudioSource[] sources = new AudioSource[sourceCount];
            for (int i = 0; i < sourceCount; i++)
            {
                Transform child = GetOrCreateChild(sfxRoot, $"SFX Source {i + 1:00}");
                AudioSource source = child.GetComponent<AudioSource>();
                if (source == null)
                    source = child.gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                sources[i] = source;
            }

            Transform musicRoot = GetOrCreateChild(root.transform, "Music Player");
            MusicPlayer musicPlayer = musicRoot.GetComponent<MusicPlayer>();
            if (musicPlayer == null)
                musicPlayer = musicRoot.gameObject.AddComponent<MusicPlayer>();
            AudioSource[] musicSources = musicRoot.GetComponents<AudioSource>();
            while (musicSources.Length < 2)
            {
                musicRoot.gameObject.AddComponent<AudioSource>();
                musicSources = musicRoot.GetComponents<AudioSource>();
            }

            SerializedObject musicData = new(musicPlayer);
            musicData.FindProperty("primarySource").objectReferenceValue = musicSources[0];
            musicData.FindProperty("secondarySource").objectReferenceValue = musicSources[1];
            musicData.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject managerData = new(manager);
            SerializedProperty sourceArray = managerData.FindProperty("sfxSources");
            sourceArray.arraySize = sources.Length;
            for (int i = 0; i < sources.Length; i++)
                sourceArray.GetArrayElementAtIndex(i).objectReferenceValue = sources[i];
            managerData.FindProperty("musicPlayer").objectReferenceValue = musicPlayer;
            managerData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateElectricLinePrefab()
        {
            GameObject root = new("ElectricLine", typeof(LineRenderer), typeof(ElectricLine));
            LineRenderer line = root.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 9;
            line.textureMode = LineTextureMode.Tile;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 2;
            line.sortingOrder = 52;
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SpriteLine.mat");

            SerializedObject data = new(root.GetComponent<ElectricLine>());
            data.FindProperty("lineRenderer").objectReferenceValue = line;
            data.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ElectricLinePrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(ElectricLinePrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(ElectricLinePrefabPath);
        }

        private static GameObject CreateHealProjectilePrefab()
        {
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            GameObject root = new("HealProjectile", typeof(SpriteRenderer), typeof(HealProjectile));
            SpriteRenderer projectile = root.GetComponent<SpriteRenderer>();
            projectile.sprite = circle;
            projectile.color = new Color(0.35f, 1f, 0.55f, 1f);
            projectile.sortingOrder = 62;

            GameObject impactObject = new("Impact", typeof(SpriteRenderer));
            impactObject.transform.SetParent(root.transform, false);
            SpriteRenderer impact = impactObject.GetComponent<SpriteRenderer>();
            impact.sprite = circle;
            impact.color = new Color(0.5f, 1f, 0.7f, 0f);
            impact.sortingOrder = 61;
            impact.enabled = false;

            SerializedObject data = new(root.GetComponent<HealProjectile>());
            data.FindProperty("projectileRenderer").objectReferenceValue = projectile;
            data.FindProperty("impactRenderer").objectReferenceValue = impact;
            data.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, HealProjectilePrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(HealProjectilePrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(HealProjectilePrefabPath);
        }

        private static void AssignBlockEffectPrefabs(
            GameObject electricLinePrefab,
            GameObject healProjectilePrefab)
        {
            GameObject supportRoot = PrefabUtility.LoadPrefabContents(SupportPrefabPath);
            SupportBlock support = supportRoot.GetComponent<SupportBlock>();
            SerializedObject supportData = new(support);
            supportData.FindProperty("electricLinePrefab").objectReferenceValue =
                electricLinePrefab != null ? electricLinePrefab.GetComponent<ElectricLine>() : null;
            supportData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(supportRoot, SupportPrefabPath);
            PrefabUtility.UnloadPrefabContents(supportRoot);

            GameObject healerRoot = PrefabUtility.LoadPrefabContents(HealerPrefabPath);
            HealerBlock healer = healerRoot.GetComponent<HealerBlock>();
            SerializedObject healerData = new(healer);
            healerData.FindProperty("healProjectilePrefab").objectReferenceValue =
                healProjectilePrefab != null ? healProjectilePrefab.GetComponent<HealProjectile>() : null;
            healerData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(healerRoot, HealerPrefabPath);
            PrefabUtility.UnloadPrefabContents(healerRoot);
        }

        private static void AddCanvasGroupToBlockButton()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BlockButtonPrefabPath);
            if (root.GetComponent<CanvasGroup>() == null)
                root.AddComponent<CanvasGroup>();
            RectTransform rect = root.transform as RectTransform;
            rect.sizeDelta = new Vector2(104f, 104f);
            PrefabUtility.SaveAsPrefabAsset(root, BlockButtonPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void SetupSupplyPresentation()
        {
            PreparationUI preparation = Object.FindFirstObjectByType<PreparationUI>(FindObjectsInactive.Include);
            if (preparation == null)
                throw new System.InvalidOperationException("PreparationUI was not found.");

            SerializedObject preparationData = new(preparation);
            Button rerollButton = preparationData.FindProperty("rerollButton").objectReferenceValue as Button;
            Button confirmButton = FindButton("Confirm Button");
            if (rerollButton == null || confirmButton == null)
                throw new System.InvalidOperationException("Reroll or Confirm button was not found.");

            Transform canvasParent = preparation.transform.parent;
            Transform existing = canvasParent.Find("Supply Presentation");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject("Supply Presentation", typeof(RectTransform), typeof(CanvasGroup), typeof(SupplyPresentationUI));
            root.transform.SetParent(canvasParent, false);
            root.transform.SetAsLastSibling();
            SetLayerRecursively(root, 5);
            Stretch(root.GetComponent<RectTransform>());
            CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();

            GameObject background = GetOrCreateUIChild(root.transform, "Background Panel", typeof(Image));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.015f, 0.03f, 0.05f, 0.82f);

            GameObject content = GetOrCreateUIChild(root.transform, "Content Root");
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(900f, 280f);

            GameObject container = GetOrCreateUIChild(content.transform, "Block Container");
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0f, 28f);
            containerRect.sizeDelta = new Vector2(760f, 110f);

            ConfigureButton(rerollButton, content.transform, new Vector2(-110f, -88f), new Vector2(190f, 52f));
            ConfigureButton(confirmButton, content.transform, new Vector2(110f, -88f), new Vector2(190f, 52f));
            ClearPersistentListeners(confirmButton);

            Button startWaveButton = SetupStartWaveButton(canvasParent);

            GameObject dock = GetOrCreateUIChild(root.transform, "Dock Target");
            RectTransform dockRect = dock.GetComponent<RectTransform>();
            dockRect.anchorMin = dockRect.anchorMax = new Vector2(0.5f, 0f);
            dockRect.anchoredPosition = new Vector2(0f, 88f);
            dockRect.sizeDelta = new Vector2(900f, 168f);

            GridLayoutGroup oldLayout = preparation.GetComponent<GridLayoutGroup>();
            if (oldLayout != null)
                Object.DestroyImmediate(oldLayout);
            ContentSizeFitter oldFitter = preparation.GetComponent<ContentSizeFitter>();
            if (oldFitter != null)
                Object.DestroyImmediate(oldFitter);

            SupplyPresentationUI supply = root.GetComponent<SupplyPresentationUI>();
            SerializedObject supplyData = new(supply);
            supplyData.FindProperty("presentationRoot").objectReferenceValue = root.GetComponent<RectTransform>();
            supplyData.FindProperty("presentationGroup").objectReferenceValue = rootGroup;
            supplyData.FindProperty("backgroundPanel").objectReferenceValue = backgroundRect;
            supplyData.FindProperty("backgroundImage").objectReferenceValue = backgroundImage;
            supplyData.FindProperty("contentRoot").objectReferenceValue = contentRect;
            supplyData.FindProperty("blockContainer").objectReferenceValue = containerRect;
            supplyData.FindProperty("dockTarget").objectReferenceValue = dockRect;
            supplyData.FindProperty("confirmButton").objectReferenceValue = confirmButton;
            supplyData.FindProperty("confirmLabel").objectReferenceValue =
                confirmButton.GetComponentInChildren<TMP_Text>(true);
            supplyData.FindProperty("rerollButton").objectReferenceValue = rerollButton;
            AssignCue(supplyData.FindProperty("blockLandingSound"), LandingClipPath, 0.75f);
            AssignCue(supplyData.FindProperty("rareBlockSound"), RareClipPath, 0.7f);
            AssignCue(supplyData.FindProperty("confirmSound"), ConfirmClipPath, 0.8f);
            supplyData.ApplyModifiedPropertiesWithoutUndo();

            preparationData.Update();
            preparationData.FindProperty("inventoryRoot").objectReferenceValue = containerRect;
            preparationData.FindProperty("supplyPresentation").objectReferenceValue = supply;
            preparationData.FindProperty("confirmButton").objectReferenceValue = confirmButton;
            preparationData.FindProperty("startWaveButton").objectReferenceValue = startWaveButton;
            preparationData.FindProperty("startWaveButtonVisibility").objectReferenceValue =
                startWaveButton.GetComponent<UIShowHide>();
            preparationData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button SetupStartWaveButton(Transform canvasParent)
        {
            Transform existing = canvasParent.Find("Start Wave Button");
            GameObject root = existing != null
                ? existing.gameObject
                : new GameObject(
                    "Start Wave Button",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(CanvasGroup),
                    typeof(UIShowHide));
            root.transform.SetParent(canvasParent, false);
            SetLayerRecursively(root, 5);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-32f, -95f);
            rect.sizeDelta = new Vector2(230f, 64f);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.12f, 0.72f, 0.48f, 0.98f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;

            Transform labelTransform = root.transform.Find("Text");
            GameObject labelObject = labelTransform != null
                ? labelTransform.gameObject
                : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            Stretch(label.rectTransform);
            label.text = "START WAVE";
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font != null)
                label.font = font;

            UIShowHide ui = root.GetComponent<UIShowHide>();
            SerializedObject uiData = new(ui);
            uiData.FindProperty("visualRoot").objectReferenceValue = rect;
            uiData.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            uiData.FindProperty("button").objectReferenceValue = button;
            uiData.ApplyModifiedPropertiesWithoutUndo();
            ClearPersistentListeners(button);
            return button;
        }

        private static void SetupWaveStartPresentation()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            WaveStartPresentationController controller =
                gameManager.GetComponentInChildren<WaveStartPresentationController>(true);
            if (controller == null)
            {
                Transform presentation = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    gameManager.transform,
                    "Presentation");
                Transform waveStart = GameManagerStructureRefactorSetup.GetOrCreateChild(
                    presentation,
                    "Wave Start Presentation");
                controller = waveStart.gameObject.AddComponent<WaveStartPresentationController>();
            }

            GameObject pulsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorePulsePrefabPath);
            SerializedObject controllerData = new(controller);
            controllerData.FindProperty("corePulsePrefab").objectReferenceValue =
                pulsePrefab != null ? pulsePrefab.GetComponent<CoreEnergyPulseView>() : null;
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            ShockwaveCountdownUI countdown =
                Object.FindFirstObjectByType<ShockwaveCountdownUI>(FindObjectsInactive.Include);
            if (countdown == null)
                return;
            SerializedObject countdownData = new(countdown);
            GameObject visual = countdownData.FindProperty("visualRoot").objectReferenceValue as GameObject;
            if (visual != null)
            {
                CanvasGroup group = visual.GetComponent<CanvasGroup>();
                if (group == null)
                    group = visual.AddComponent<CanvasGroup>();
                countdownData.FindProperty("canvasGroup").objectReferenceValue = group;
                countdownData.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupButtonInteractionsInScene()
        {
            AudioClip click = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickClipPath);
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ConfigureButtonInteraction(button, click);
        }

        private static void SetupButtonInteractionsInPrefabs()
        {
            AudioClip click = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickClipPath);
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    ConfigureButtonInteraction(button, click);
                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupButtonInteractionsInAdditionalScenes()
        {
            string[] scenePaths =
            {
                "Assets/Scenes/TitleScene.unity",
                "Assets/Scenes/TutorialScene.unity"
            };

            foreach (string scenePath in scenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                    continue;

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                SetupAudioManager();
                SetupButtonInteractionsInScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void ConfigureButtonInteraction(Button button, AudioClip click)
        {
            UIButtonInteraction interaction = button.GetComponent<UIButtonInteraction>();
            if (interaction == null)
                interaction = button.gameObject.AddComponent<UIButtonInteraction>();
            SerializedObject data = new(interaction);
            data.FindProperty("button").objectReferenceValue = button;
            data.FindProperty("animationTarget").objectReferenceValue = button.transform as RectTransform;
            AssignCue(data.FindProperty("clickSound"), click, 0.72f);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignCue(SerializedProperty cue, string clipPath, float volume)
        {
            AssignCue(cue, AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath), volume);
        }

        private static void AssignCue(SerializedProperty cue, AudioClip clip, float volume)
        {
            if (cue == null)
                return;
            SerializedProperty clips = cue.FindPropertyRelative("clips");
            clips.arraySize = clip != null ? 1 : 0;
            if (clip != null)
                clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            cue.FindPropertyRelative("volume").floatValue = volume;
            cue.FindPropertyRelative("pitchRange").vector2Value = new Vector2(0.97f, 1.03f);
            cue.FindPropertyRelative("spatialBlend").floatValue = 0f;
        }

        private static void ValidateBlockPrefab<T>(string path, string propertyName) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            T component = prefab != null ? prefab.GetComponent<T>() : null;
            if (component == null
                || new SerializedObject(component).FindProperty(propertyName).objectReferenceValue == null)
            {
                throw new System.InvalidOperationException($"{path} is missing {propertyName}.");
            }
        }

        private static Button FindButton(string name)
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.name == name)
                    return button;
            }

            return null;
        }

        private static void ClearPersistentListeners(Button button)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        private static void ConfigureButton(
            Button button,
            Transform parent,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect = button.transform as RectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing;

            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static GameObject GetOrCreateUIChild(
            Transform parent,
            string name,
            params System.Type[] extraComponents)
        {
            Transform existing = parent.Find(name);
            GameObject child = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            foreach (System.Type type in extraComponents)
            {
                if (child.GetComponent(type) == null)
                    child.AddComponent(type);
            }
            return child;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
