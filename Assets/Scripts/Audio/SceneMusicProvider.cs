using UnityEngine;

namespace KeepCoreSafe.Audio
{
    [DefaultExecutionOrder(-50)]
    public sealed class SceneMusicProvider : MonoBehaviour
    {
        [Header("Scene Background Music")]
        [Tooltip("BGM requested when this scene starts. Leave empty to keep the current music playing.")]
        [SerializeField] private MusicTrackData sceneMusic;

        [Tooltip("Cross-fade time used when changing from the current BGM to this scene's BGM.")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.6f;

        private void Start()
        {
            if (sceneMusic != null)
                AudioManager.RequestMusic(sceneMusic, fadeDuration);
        }
    }
}
