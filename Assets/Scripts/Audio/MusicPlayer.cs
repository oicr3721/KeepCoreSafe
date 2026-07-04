using DG.Tweening;
using UnityEngine;

namespace KeepCoreSafe.Audio
{
    public sealed class MusicPlayer : MonoBehaviour
    {
        [Tooltip("Two sources are used to crossfade between stage tracks.")]
        [SerializeField] private AudioSource primarySource;
        [SerializeField] private AudioSource secondarySource;

        private AudioSource activeSource;
        private MusicTrackData currentTrack;

        private void Awake()
        {
            activeSource = primarySource;
            Configure(primarySource);
            Configure(secondarySource);
        }

        public void Play(MusicTrackData track)
        {
            if (track == null || track.Clip == null || track == currentTrack)
                return;

            AudioSource next = activeSource == primarySource ? secondarySource : primarySource;
            if (next == null)
                return;

            next.DOKill(false);
            activeSource?.DOKill(false);
            next.clip = track.Clip;
            next.loop = track.Loop;
            next.pitch = 1f;
            next.volume = 0f;
            next.Play();

            float duration = track.CrossfadeDuration;
            next.DOFade(track.Volume, duration).SetUpdate(true);
            if (activeSource != null)
            {
                AudioSource previous = activeSource;
                previous.DOFade(0f, duration)
                    .SetUpdate(true)
                    .OnComplete(previous.Stop);
            }

            activeSource = next;
            currentTrack = track;
        }

        public void Stop(float fadeDuration = 0.25f)
        {
            currentTrack = null;
            FadeOut(primarySource, fadeDuration);
            FadeOut(secondarySource, fadeDuration);
        }

        private static void Configure(AudioSource source)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private static void FadeOut(AudioSource source, float duration)
        {
            if (source == null)
                return;

            source.DOKill(false);
            source.DOFade(0f, duration).SetUpdate(true).OnComplete(source.Stop);
        }

        private void OnDestroy()
        {
            primarySource?.DOKill(false);
            secondarySource?.DOKill(false);
        }
    }
}
