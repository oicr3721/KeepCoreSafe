using System;
using UnityEngine;

namespace KeepCoreSafe.Audio
{
    [Serializable]
    public sealed class AudioCue
    {
        [Tooltip("One clip is selected randomly each time this cue is played.")]
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();

        [Tooltip("Volume multiplier applied to the selected clip.")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Tooltip("A random pitch is selected inside this range.")]
        [SerializeField] private Vector2 pitchRange = new(0.96f, 1.04f);

        [Tooltip("0 is a global 2D sound. 1 is a fully positional 3D sound.")]
        [SerializeField, Range(0f, 1f)] private float spatialBlend;

        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public float RandomPitch => UnityEngine.Random.Range(
            Mathf.Min(pitchRange.x, pitchRange.y),
            Mathf.Max(pitchRange.x, pitchRange.y));

        public bool TryGetRandomClip(out AudioClip clip)
        {
            clip = null;
            if (clips == null || clips.Length == 0)
                return false;

            int start = UnityEngine.Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                AudioClip candidate = clips[(start + offset) % clips.Length];
                if (candidate != null)
                {
                    clip = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
