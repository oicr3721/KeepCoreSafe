using System;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Audio
{
    [DefaultExecutionOrder(-100)]
    public sealed class AudioManager : MonoBehaviour
    {
        [Serializable]
        private struct StageMusic
        {
            [Tooltip("This track is used from this wave onward, until a later entry replaces it.")]
            [Min(0)] public int minimumWave;
            public MusicTrackData track;
        }

        public static AudioManager Instance { get; private set; }

        [Header("Sound Effect Source Pool")]
        [Tooltip("Pre-created AudioSources used by every gameplay sound effect.")]
        [SerializeField] private AudioSource[] sfxSources = Array.Empty<AudioSource>();

        [Header("Background Music")]
        [SerializeField] private MusicPlayer musicPlayer;
        [SerializeField] private MusicTrackData defaultMusic;
        [SerializeField] private StageMusic[] stageMusic = Array.Empty<StageMusic>();

        [Header("Volume")]
        [Tooltip("Initial BGM volume multiplier used before a saved setting exists.")]
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.3f;
        [Tooltip("Initial SFX volume multiplier used before a saved setting exists.")]
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.4f;

        private const string MusicVolumePrefKey = "KeepCoreSafe.Audio.MusicVolume";
        private const string SfxVolumePrefKey = "KeepCoreSafe.Audio.SfxVolume";

        public const float MinimumVolume = 0f;
        public const float MaximumVolume = 1f;
        public const float DefaultVolume = 1f;

        private int nextSourceIndex;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;

        public static float MusicVolume =>
            Instance != null
                ? Instance.musicVolume
                : ClampVolume(PlayerPrefs.GetFloat(MusicVolumePrefKey, DefaultVolume));

        public static float SfxVolume =>
            Instance != null
                ? Instance.sfxVolume
                : ClampVolume(PlayerPrefs.GetFloat(SfxVolumePrefKey, DefaultVolume));

        public static float ClampVolume(float volume)
        {
            return Mathf.Clamp(volume, MinimumVolume, MaximumVolume);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Instance.ApplySceneConfiguration(defaultMusic, stageMusic);
                DestroyDuplicateMusicPlayer();
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            PreserveSeparateMusicPlayer();
            LoadPersistedVolumes();
            ConfigureSources();
            musicPlayer?.SetVolumeMultiplier(musicVolume);
        }

        private void Start()
        {
            GameManager.WaveStarted += HandleWaveStarted;
            musicPlayer?.Play(defaultMusic);
        }

        public static void Play(AudioCue cue)
        {
            Instance?.PlayInternal(cue, Vector3.zero, false);
        }

        public static void Play(AudioClip clip)
        {
            Instance?.PlayClipInternal(clip);
        }

        public static void PlayAt(AudioCue cue, Vector3 worldPosition)
        {
            Instance?.PlayInternal(cue, worldPosition, true);
        }

        public void PlayMusic(MusicTrackData track)
        {
            musicPlayer?.Play(track);
        }

        public void StopMusic()
        {
            musicPlayer?.Stop();
        }

        public static void RequestMusic(MusicTrackData track, float fadeDuration)
        {
            Instance?.musicPlayer?.Play(track, fadeDuration);
        }

        public static void SetMusicVolume(float volume)
        {
            if (Instance != null)
            {
                Instance.SetMusicVolumeInternal(volume, true);
                return;
            }

            PlayerPrefs.SetFloat(MusicVolumePrefKey, ClampVolume(volume));
            PlayerPrefs.Save();
        }

        public static void SetSfxVolume(float volume)
        {
            if (Instance != null)
            {
                Instance.SetSfxVolumeInternal(volume, true);
                return;
            }

            PlayerPrefs.SetFloat(SfxVolumePrefKey, ClampVolume(volume));
            PlayerPrefs.Save();
        }

        private void PlayInternal(AudioCue cue, Vector3 worldPosition, bool hasPosition)
        {
            if (cue == null || !cue.TryGetRandomClip(out AudioClip clip))
                return;

            AudioSource source = GetAvailableSource();
            if (source == null)
                return;

            source.Stop();
            source.transform.position = hasPosition ? worldPosition : transform.position;
            source.clip = clip;
            source.volume = cue.Volume * sfxVolume;
            source.pitch = cue.RandomPitch;
            source.spatialBlend = hasPosition ? cue.SpatialBlend : 0f;
            source.loop = false;
            source.Play();
        }

        private void PlayClipInternal(AudioClip clip)
        {
            if (clip == null)
                return;

            AudioSource source = GetAvailableSource();
            if (source == null)
                return;

            source.Stop();
            source.transform.position = transform.position;
            source.clip = clip;
            source.volume = sfxVolume;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.loop = false;
            source.Play();
        }

        private AudioSource GetAvailableSource()
        {
            if (sfxSources == null || sfxSources.Length == 0)
                return null;

            for (int offset = 0; offset < sfxSources.Length; offset++)
            {
                int index = (nextSourceIndex + offset) % sfxSources.Length;
                AudioSource source = sfxSources[index];
                if (source != null && !source.isPlaying)
                {
                    nextSourceIndex = (index + 1) % sfxSources.Length;
                    return source;
                }
            }

            AudioSource fallback = sfxSources[nextSourceIndex];
            nextSourceIndex = (nextSourceIndex + 1) % sfxSources.Length;
            return fallback;
        }

        private void HandleWaveStarted(int waveIndex)
        {
            MusicTrackData selected = defaultMusic;
            int selectedMinimum = -1;
            foreach (StageMusic entry in stageMusic)
            {
                if (entry.track != null
                    && entry.minimumWave <= waveIndex
                    && entry.minimumWave >= selectedMinimum)
                {
                    selected = entry.track;
                    selectedMinimum = entry.minimumWave;
                }
            }

            musicPlayer?.Play(selected);
        }

        private void ApplySceneConfiguration(
            MusicTrackData sceneDefaultMusic,
            StageMusic[] sceneStageMusic)
        {
            defaultMusic = sceneDefaultMusic;
            stageMusic = sceneStageMusic != null
                ? (StageMusic[])sceneStageMusic.Clone()
                : Array.Empty<StageMusic>();
        }

        private void PreserveSeparateMusicPlayer()
        {
            if (musicPlayer == null)
                return;

            Transform musicRoot = musicPlayer.transform.root;
            if (musicRoot != transform.root)
                DontDestroyOnLoad(musicRoot.gameObject);
        }

        private void DestroyDuplicateMusicPlayer()
        {
            if (musicPlayer == null || musicPlayer == Instance.musicPlayer)
                return;

            Transform musicRoot = musicPlayer.transform.root;
            if (musicRoot != transform.root)
                Destroy(musicRoot.gameObject);
        }

        private void ConfigureSources()
        {
            foreach (AudioSource source in sfxSources)
            {
                if (source == null)
                    continue;

                source.playOnAwake = false;
                source.loop = false;
            }
        }

        private void LoadPersistedVolumes()
        {
            musicVolume = PlayerPrefs.GetFloat(
                MusicVolumePrefKey,
                ClampVolume(defaultMusicVolume));
            sfxVolume = PlayerPrefs.GetFloat(
                SfxVolumePrefKey,
                ClampVolume(defaultSfxVolume));
            musicVolume = ClampVolume(musicVolume);
            sfxVolume = ClampVolume(sfxVolume);
        }

        private void SetMusicVolumeInternal(float volume, bool save)
        {
            musicVolume = ClampVolume(volume);
            musicPlayer?.SetVolumeMultiplier(musicVolume);
            if (!save)
                return;

            PlayerPrefs.SetFloat(MusicVolumePrefKey, musicVolume);
            PlayerPrefs.Save();
        }

        private void SetSfxVolumeInternal(float volume, bool save)
        {
            sfxVolume = ClampVolume(volume);
            if (!save)
                return;

            PlayerPrefs.SetFloat(SfxVolumePrefKey, sfxVolume);
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            GameManager.WaveStarted -= HandleWaveStarted;
            Instance = null;
        }
    }
}
