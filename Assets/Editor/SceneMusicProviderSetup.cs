using KeepCoreSafe.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class SceneMusicProviderSetup
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/TitleScene.unity",
            "Assets/Scenes/TutorialScene.unity",
            "Assets/Scenes/PrologueScene.unity",
            "Assets/Scenes/GameScene.unity"
        };
        private const string AudioMixerPath = "Assets/Audio/DefaultAudioMixer.mixer";

        [MenuItem("Keep Core Safe/Setup Scene Music Providers")]
        public static void Setup()
        {
            foreach (string scenePath in ScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                EnsureSceneProvider(scenePath);
                if (scenePath.EndsWith("PrologueScene.unity"))
                    EnsurePrologueMusicPlayer();
                EnsureMixerRouting();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("SCENE_MUSIC_PROVIDER_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate Scene Music Providers")]
        public static void Validate()
        {
            foreach (string scenePath in ScenePaths)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (Object.FindFirstObjectByType<SceneMusicProvider>(FindObjectsInactive.Include) == null)
                    throw new System.InvalidOperationException($"SceneMusicProvider is missing in {scenePath}.");
            }

            EditorSceneManager.OpenScene("Assets/Scenes/PrologueScene.unity", OpenSceneMode.Single);
            AudioManager audioManager = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            SerializedObject audioData = new(audioManager);
            if (audioData.FindProperty("musicPlayer").objectReferenceValue == null)
                throw new System.InvalidOperationException("Prologue AudioManager has no MusicPlayer reference.");

            Debug.Log("SCENE_MUSIC_PROVIDER_VALIDATION_COMPLETE");
        }

        private static void EnsureSceneProvider(string scenePath)
        {
            if (Object.FindFirstObjectByType<SceneMusicProvider>(FindObjectsInactive.Include) != null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (mainCamera == null)
                throw new System.InvalidOperationException($"A Camera was not found in {scenePath}.");

            Undo.AddComponent<SceneMusicProvider>(mainCamera.gameObject);
        }

        private static void EnsurePrologueMusicPlayer()
        {
            AudioManager audioManager = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (audioManager == null)
                throw new System.InvalidOperationException("Prologue AudioManager was not found.");

            SerializedObject audioData = new(audioManager);
            SerializedProperty musicPlayerProperty = audioData.FindProperty("musicPlayer");
            if (musicPlayerProperty.objectReferenceValue != null)
                return;

            MusicPlayer musicPlayer = audioManager.GetComponent<MusicPlayer>();
            if (musicPlayer == null)
                musicPlayer = Undo.AddComponent<MusicPlayer>(audioManager.gameObject);

            AudioSource primary = Undo.AddComponent<AudioSource>(audioManager.gameObject);
            AudioSource secondary = Undo.AddComponent<AudioSource>(audioManager.gameObject);
            primary.playOnAwake = false;
            secondary.playOnAwake = false;

            SerializedObject playerData = new(musicPlayer);
            playerData.FindProperty("primarySource").objectReferenceValue = primary;
            playerData.FindProperty("secondarySource").objectReferenceValue = secondary;
            playerData.ApplyModifiedPropertiesWithoutUndo();

            musicPlayerProperty.objectReferenceValue = musicPlayer;
            audioData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureMixerRouting()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerPath);
            if (mixer == null)
                return;

            AudioMixerGroup[] bgmGroups = mixer.FindMatchingGroups("BGM");
            AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
            AudioMixerGroup bgmGroup = bgmGroups.Length > 0 ? bgmGroups[0] : null;
            AudioMixerGroup sfxGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;

            AudioManager audioManager = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (audioManager != null && sfxGroup != null)
            {
                SerializedObject audioData = new(audioManager);
                SerializedProperty sources = audioData.FindProperty("sfxSources");
                for (int i = 0; i < sources.arraySize; i++)
                {
                    if (sources.GetArrayElementAtIndex(i).objectReferenceValue is AudioSource source)
                        source.outputAudioMixerGroup = sfxGroup;
                }
            }

            MusicPlayer musicPlayer = Object.FindFirstObjectByType<MusicPlayer>(FindObjectsInactive.Include);
            if (musicPlayer == null || bgmGroup == null)
                return;

            SerializedObject playerData = new(musicPlayer);
            if (playerData.FindProperty("primarySource").objectReferenceValue is AudioSource primary)
                primary.outputAudioMixerGroup = bgmGroup;
            if (playerData.FindProperty("secondarySource").objectReferenceValue is AudioSource secondary)
                secondary.outputAudioMixerGroup = bgmGroup;
        }
    }
}
