using KeepCoreSafe.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class AudioVolumeSettingsUI : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        private void OnEnable()
        {
            ConfigureSlider(bgmSlider, AudioManager.MusicVolume, HandleBgmVolumeChanged);
            ConfigureSlider(sfxSlider, AudioManager.SfxVolume, HandleSfxVolumeChanged);
        }

        private void OnDisable()
        {
            if (bgmSlider != null)
                bgmSlider.onValueChanged.RemoveListener(HandleBgmVolumeChanged);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
        }

        private static void ConfigureSlider(
            Slider slider,
            float value,
            UnityEngine.Events.UnityAction<float> listener)
        {
            if (slider == null)
                return;

            slider.minValue = AudioManager.MinimumVolume;
            slider.maxValue = AudioManager.MaximumVolume;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(AudioManager.ClampVolume(value));
            slider.onValueChanged.RemoveListener(listener);
            slider.onValueChanged.AddListener(listener);
        }

        private static void HandleBgmVolumeChanged(float value)
        {
            AudioManager.SetMusicVolume(value);
        }

        private static void HandleSfxVolumeChanged(float value)
        {
            AudioManager.SetSfxVolume(value);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (bgmSlider == null)
                Debug.LogWarning($"{nameof(AudioVolumeSettingsUI)} on {name} needs a BGM Slider reference.", this);
            if (sfxSlider == null)
                Debug.LogWarning($"{nameof(AudioVolumeSettingsUI)} on {name} needs an SFX Slider reference.", this);
        }
#endif
    }
}
