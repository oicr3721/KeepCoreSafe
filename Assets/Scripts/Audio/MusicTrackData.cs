using UnityEngine;

namespace KeepCoreSafe.Audio
{
    [CreateAssetMenu(fileName = "MusicTrack", menuName = "Keep Core Safe/Audio/Music Track")]
    public sealed class MusicTrackData : ScriptableObject
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
        [SerializeField] private bool loop = true;
        [SerializeField, Min(0f)] private float crossfadeDuration = 0.5f;

        public AudioClip Clip => clip;
        public float Volume => volume;
        public bool Loop => loop;
        public float CrossfadeDuration => crossfadeDuration;
    }
}
