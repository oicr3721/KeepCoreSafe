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

        private int nextSourceIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ConfigureSources();
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

        public static void PlayAt(AudioCue cue, Vector3 worldPosition)
        {
            Instance?.PlayInternal(cue, worldPosition, true);
        }

        public void PlayMusic(MusicTrackData track)
        {
            musicPlayer?.Play(track);
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
            source.volume = cue.Volume;
            source.pitch = cue.RandomPitch;
            source.spatialBlend = hasPosition ? cue.SpatialBlend : 0f;
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

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            GameManager.WaveStarted -= HandleWaveStarted;
            Instance = null;
        }
    }
}
