using KeepCoreSafe.Audio;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
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
    public static class TutorialPrologueSetup
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string TutorialScenePath = "Assets/Scenes/TutorialScene.unity";
        private const string PrologueScenePath = "Assets/Scenes/PrologueScene.unity";
        private const string TutorialDifficultyPath = "Assets/Resources/Data/Systems/TutorialDifficultyData.asset";
        private const string FontPath = "Assets/Fonts/LimgulMono16 SDF 1.asset";
        private const string WhiteSquarePath = "Assets/Sprites/WhiteSquare.png";
        private const string PrologueEarthPath = "Assets/Sprites/PrologueEarthDestruction.png";

        [MenuItem("Keep Core Safe/Setup Tutorial and Prologue")]
        public static void Setup()
        {
            SetupTutorialScene();
            SetupPrologueScene();
            ConfigureTitleStart();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TUTORIAL_PROLOGUE_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Tutorial and Prologue")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<TutorialDirector>(FindObjectsInactive.Include) == null
                || Object.FindFirstObjectByType<TypewriterText>(FindObjectsInactive.Include) == null)
            {
                throw new System.InvalidOperationException("Tutorial scene references are incomplete.");
            }

            EditorSceneManager.OpenScene(PrologueScenePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<PrologueDirector>(FindObjectsInactive.Include) == null
                || Object.FindFirstObjectByType<TypewriterText>(FindObjectsInactive.Include) == null)
            {
                throw new System.InvalidOperationException("Prologue scene references are incomplete.");
            }

            Debug.Log("TUTORIAL_PROLOGUE_VALIDATION_COMPLETE");
        }

        private static void SetupTutorialScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TutorialScenePath) == null)
            {
                if (!AssetDatabase.CopyAsset(GameScenePath, TutorialScenePath))
                    throw new System.InvalidOperationException("Failed to create TutorialScene from GameScene.");
                AssetDatabase.ImportAsset(TutorialScenePath, ImportAssetOptions.ForceSynchronousImport);
            }

            Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            BlockSupplyController supply = gameManager.GetComponentInChildren<BlockSupplyController>(true);
            WaveManager wave = gameManager.GetComponentInChildren<WaveManager>(true);
            WaveDifficultyController difficulty = gameManager.GetComponentInChildren<WaveDifficultyController>(true);
            PlacementController placement = Object.FindFirstObjectByType<PlacementController>(FindObjectsInactive.Include);
            PreparationUI preparation = Object.FindFirstObjectByType<PreparationUI>(FindObjectsInactive.Include);

            BasicBlockData red = Load<BasicBlockData>("Assets/Resources/Data/Block/Basic/RedBasic.asset");
            BasicBlockData green = Load<BasicBlockData>("Assets/Resources/Data/Block/Basic/GreenBasic.asset");
            BasicBlockData blue = Load<BasicBlockData>("Assets/Resources/Data/Block/Basic/BlueBasic.asset");
            ConfigureScriptedSupply(supply, red, green);
            ConfigureStartingBlocks(placement, red, green, blue);
            ConfigureTutorialDifficulty(difficulty);

            Canvas canvas = Object.FindFirstObjectByType<GameDefaultUI>(FindObjectsInactive.Include)
                .GetComponentInParent<Canvas>();
            TMP_Text fontSource = Object.FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            TypewriterText typewriter = CreateTutorialDialogue(canvas.transform, fontSource, out GameObject dialogueRoot);
            TutorialGridHighlight highlight = CreateHighlight();
            TutorialGlitchTransition glitch = CreateGlitch(canvas.transform, fontSource);

            TutorialDirector director = gameManager.GetComponent<TutorialDirector>();
            if (director == null)
                director = gameManager.gameObject.AddComponent<TutorialDirector>();
            SerializedObject directorData = new(director);
            directorData.FindProperty("placementController").objectReferenceValue = placement;
            directorData.FindProperty("supplyController").objectReferenceValue = supply;
            directorData.FindProperty("preparationUI").objectReferenceValue = preparation;
            directorData.FindProperty("waveManager").objectReferenceValue = wave;
            directorData.FindProperty("dialogueRoot").objectReferenceValue = dialogueRoot;
            directorData.FindProperty("typewriter").objectReferenceValue = typewriter;
            directorData.FindProperty("gridHighlight").objectReferenceValue = highlight;
            directorData.FindProperty("glitchTransition").objectReferenceValue = glitch;
            directorData.FindProperty("redBlock").objectReferenceValue = red;
            directorData.FindProperty("greenBlock").objectReferenceValue = green;
            directorData.ApplyModifiedPropertiesWithoutUndo();

            ShopEventController shop = gameManager.GetComponentInChildren<ShopEventController>(true);
            if (shop != null) shop.enabled = false;
            ShopEventUI shopUI = Object.FindFirstObjectByType<ShopEventUI>(FindObjectsInactive.Include);
            if (shopUI != null) shopUI.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureScriptedSupply(
            BlockSupplyController supply,
            BasicBlockData red,
            BasicBlockData green)
        {
            SerializedObject data = new(supply);
            data.FindProperty("useScriptedSupply").boolValue = true;
            SerializedProperty blocks = data.FindProperty("scriptedBlocks");
            blocks.arraySize = 3;
            blocks.GetArrayElementAtIndex(0).objectReferenceValue = red;
            blocks.GetArrayElementAtIndex(1).objectReferenceValue = red;
            blocks.GetArrayElementAtIndex(2).objectReferenceValue = green;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStartingBlocks(
            PlacementController placement,
            BasicBlockData red,
            BasicBlockData green,
            BasicBlockData blue)
        {
            SerializedObject data = new(placement);
            data.FindProperty("useScriptedStartingBlocks").boolValue = true;
            SerializedProperty blocks = data.FindProperty("scriptedStartingBlocks");
            blocks.arraySize = 4;
            SetStartingBlock(blocks.GetArrayElementAtIndex(0), Vector2Int.up, red);
            SetStartingBlock(blocks.GetArrayElementAtIndex(1), Vector2Int.down, green);
            SetStartingBlock(blocks.GetArrayElementAtIndex(2), Vector2Int.left, blue);
            SetStartingBlock(blocks.GetArrayElementAtIndex(3), Vector2Int.right, green);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStartingBlock(
            SerializedProperty property,
            Vector2Int offset,
            BasicBlockData data)
        {
            property.FindPropertyRelative("offset").vector2IntValue = offset;
            property.FindPropertyRelative("data").objectReferenceValue = data;
        }

        private static void ConfigureTutorialDifficulty(WaveDifficultyController controller)
        {
            WaveDifficultyData data = Load<WaveDifficultyData>(TutorialDifficultyPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WaveDifficultyData>();
                AssetDatabase.CreateAsset(data, TutorialDifficultyPath);
            }

            SerializedObject difficulty = new(data);
            difficulty.FindProperty("firstWaveRequiredEnergy").intValue = 14;
            difficulty.FindProperty("lateGameRequiredEnergy").intValue = 14;
            difficulty.FindProperty("firstWaveEnemyCount").vector2IntValue = new Vector2Int(2, 3);
            difficulty.FindProperty("lateGameEnemyCount").vector2IntValue = new Vector2Int(2, 3);
            difficulty.FindProperty("firstWaveSpawnInterval").floatValue = 0.9f;
            difficulty.FindProperty("lateGameSpawnInterval").floatValue = 0.9f;
            difficulty.FindProperty("enemyGrowthPerExtraWave").floatValue = 0f;
            difficulty.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject controllerData = new(controller);
            controllerData.FindProperty("difficultyData").objectReferenceValue = data;
            controllerData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TypewriterText CreateTutorialDialogue(
            Transform canvas,
            TMP_Text fontSource,
            out GameObject root)
        {
            root = GetOrCreateUI(canvas, "Tutorial Dialogue", typeof(Image), typeof(Button));
            Stretch(root.GetComponent<RectTransform>());
            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.12f);

            GameObject panel = GetOrCreateUI(root.transform, "Dialogue Panel", typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0f);
            panelRect.anchorMax = new Vector2(0.92f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 28f);
            panelRect.sizeDelta = new Vector2(0f, 190f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.06f, 0.09f, 0.97f);

            GameObject textObject = GetOrCreateUI(panel.transform, "Lily Dialogue", typeof(TextMeshProUGUI), typeof(TypewriterText));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            Stretch(textRect);
            textRect.offsetMin = new Vector2(36f, 26f);
            textRect.offsetMax = new Vector2(-36f, -24f);
            TMP_Text label = textObject.GetComponent<TMP_Text>();
            ApplyFont(label, fontSource);
            label.fontSize = 25f;
            label.color = new Color(0.88f, 1f, 0.94f, 1f);
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;

            TypewriterText typewriter = textObject.GetComponent<TypewriterText>();
            SerializedObject writerData = new(typewriter);
            writerData.FindProperty("textLabel").objectReferenceValue = label;
            writerData.FindProperty("inputButton").objectReferenceValue = root.GetComponent<Button>();
            writerData.FindProperty("charactersPerSecond").floatValue = 34f;
            writerData.ApplyModifiedPropertiesWithoutUndo();
            root.transform.SetAsLastSibling();
            return typewriter;
        }

        private static TutorialGridHighlight CreateHighlight()
        {
            TutorialGridHighlight existing =
                Object.FindFirstObjectByType<TutorialGridHighlight>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            GameObject root = new("Tutorial Grid Highlight", typeof(SpriteRenderer), typeof(TutorialGridHighlight));
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = Load<Sprite>(WhiteSquarePath);
            renderer.sortingOrder = 70;
            SerializedObject data = new(root.GetComponent<TutorialGridHighlight>());
            data.FindProperty("highlightRenderer").objectReferenceValue = renderer;
            data.ApplyModifiedPropertiesWithoutUndo();
            return root.GetComponent<TutorialGridHighlight>();
        }

        private static TutorialGlitchTransition CreateGlitch(Transform canvas, TMP_Text fontSource)
        {
            GameObject root = GetOrCreateUI(canvas, "Tutorial Glitch Transition", typeof(TutorialGlitchTransition));
            Stretch(root.GetComponent<RectTransform>());
            GameObject red = GetOrCreateUI(root.transform, "Red Flash", typeof(Image));
            Stretch(red.GetComponent<RectTransform>());
            red.GetComponent<Image>().color = new Color(1f, 0f, 0f, 0f);
            GameObject noise = GetOrCreateUI(root.transform, "Error Noise", typeof(TextMeshProUGUI));
            Stretch(noise.GetComponent<RectTransform>());
            TMP_Text noiseText = noise.GetComponent<TMP_Text>();
            ApplyFont(noiseText, fontSource);
            noiseText.fontSize = 44f;
            noiseText.color = new Color(1f, 0.15f, 0.12f, 0.9f);
            noiseText.alignment = TextAlignmentOptions.Center;
            noiseText.raycastTarget = false;
            GameObject black = GetOrCreateUI(root.transform, "Blackout", typeof(Image), typeof(CanvasGroup));
            Stretch(black.GetComponent<RectTransform>());
            black.GetComponent<Image>().color = Color.black;
            black.GetComponent<CanvasGroup>().alpha = 0f;

            TutorialGlitchTransition transition = root.GetComponent<TutorialGlitchTransition>();
            SerializedObject data = new(transition);
            data.FindProperty("redFlash").objectReferenceValue = red.GetComponent<Image>();
            data.FindProperty("noiseLabel").objectReferenceValue = noiseText;
            data.FindProperty("blackout").objectReferenceValue = black.GetComponent<CanvasGroup>();
            data.ApplyModifiedPropertiesWithoutUndo();
            root.transform.SetAsLastSibling();
            root.SetActive(false);
            return transition;
        }

        private static void SetupPrologueScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            GameObject canvasObject = new("Prologue Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject input = GetOrCreateUI(canvas.transform, "Input Background", typeof(Image), typeof(Button));
            Stretch(input.GetComponent<RectTransform>());
            input.GetComponent<Image>().color = Color.black;

            GameObject earth = GetOrCreateUI(canvas.transform, "Earth Illustration Slot", typeof(Image), typeof(CanvasGroup));
            RectTransform earthRect = earth.GetComponent<RectTransform>();
            earthRect.anchorMin = earthRect.anchorMax = new Vector2(0.5f, 1f);
            earthRect.pivot = new Vector2(0.5f, 1f);
            earthRect.anchoredPosition = new Vector2(0f, -55f);
            earthRect.sizeDelta = new Vector2(920f, 470f);
            Image earthImage = earth.GetComponent<Image>();
            EnsureSpriteImporter(PrologueEarthPath);
            earthImage.sprite = Load<Sprite>(PrologueEarthPath);
            earthImage.color = Color.white;
            earthImage.raycastTarget = false;
            earth.GetComponent<CanvasGroup>().alpha = 0f;

            GameObject textObject = GetOrCreateUI(canvas.transform, "Prologue Text", typeof(TextMeshProUGUI), typeof(TypewriterText));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.16f, 0.08f);
            textRect.anchorMax = new Vector2(0.84f, 0.5f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            TMP_Text label = textObject.GetComponent<TMP_Text>();
            ApplyFont(label, null);
            label.fontSize = 30f;
            label.color = new Color(0.88f, 0.94f, 1f, 1f);
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            TypewriterText writer = textObject.GetComponent<TypewriterText>();
            SerializedObject writerData = new(writer);
            writerData.FindProperty("textLabel").objectReferenceValue = label;
            writerData.FindProperty("inputButton").objectReferenceValue = input.GetComponent<Button>();
            writerData.FindProperty("charactersPerSecond").floatValue = 30f;
            writerData.ApplyModifiedPropertiesWithoutUndo();

            GameObject blackout = GetOrCreateUI(canvas.transform, "Blackout", typeof(Image), typeof(CanvasGroup));
            Stretch(blackout.GetComponent<RectTransform>());
            blackout.GetComponent<Image>().color = Color.black;
            blackout.GetComponent<Image>().raycastTarget = false;
            blackout.GetComponent<CanvasGroup>().alpha = 0f;

            GameObject directorObject = new("Prologue Director", typeof(PrologueDirector));
            PrologueDirector director = directorObject.GetComponent<PrologueDirector>();
            SerializedObject directorData = new(director);
            directorData.FindProperty("typewriter").objectReferenceValue = writer;
            directorData.FindProperty("earthIllustration").objectReferenceValue = earth.GetComponent<CanvasGroup>();
            directorData.FindProperty("blackout").objectReferenceValue = blackout.GetComponent<CanvasGroup>();
            directorData.ApplyModifiedPropertiesWithoutUndo();
            CreateAudioManager();

            EditorSceneManager.SaveScene(scene, PrologueScenePath);
        }

        private static void CreateAudioManager()
        {
            GameObject root = new("Audio Manager", typeof(AudioManager));
            AudioSource[] sources = new AudioSource[8];
            for (int i = 0; i < sources.Length; i++)
            {
                GameObject child = new($"SFX Source {i + 1:00}", typeof(AudioSource));
                child.transform.SetParent(root.transform, false);
                sources[i] = child.GetComponent<AudioSource>();
                sources[i].playOnAwake = false;
            }
            SerializedObject data = new(root.GetComponent<AudioManager>());
            SerializedProperty array = data.FindProperty("sfxSources");
            array.arraySize = sources.Length;
            for (int i = 0; i < sources.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = sources[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTitleStart()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity", OpenSceneMode.Single);
            SceneTransitionTrigger trigger = Object.FindFirstObjectByType<SceneTransitionTrigger>(FindObjectsInactive.Include);
            if (trigger != null)
            {
                SerializedObject data = new(trigger);
                data.FindProperty("sceneType").enumValueIndex = (int)SceneType.Tutorial;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true),
                new EditorBuildSettingsScene(TutorialScenePath, true),
                new EditorBuildSettingsScene(PrologueScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        private static GameObject GetOrCreateUI(Transform parent, string name, params System.Type[] components)
        {
            Transform existing = parent.Find(name);
            GameObject root = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            foreach (System.Type component in components)
            {
                if (root.GetComponent(component) == null)
                    root.AddComponent(component);
            }
            return root;
        }

        private static void ApplyFont(TMP_Text target, TMP_Text fallback)
        {
            TMP_FontAsset font = Load<TMP_FontAsset>(FontPath);
            if (font != null) target.font = font;
            else if (fallback != null) target.font = fallback.font;
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void EnsureSpriteImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer
                || importer.textureType == TextureImporterType.Sprite)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
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
