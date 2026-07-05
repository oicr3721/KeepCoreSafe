using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KeepCoreSafe.Audio;

namespace KeepCoreSafe.Tutorial
{
    public sealed class TutorialGlitchTransition : MonoBehaviour
    {
        [SerializeField] private Image redFlash;
        [SerializeField] private TMP_Text noiseLabel;
        [SerializeField] private CanvasGroup blackout;
        [SerializeField, Min(0.5f)] private float duration = 1.8f;
        [SerializeField, Min(0.5f)] private float blackoutDuration = 2f;
        [SerializeField] private AudioCue glitchSound = new();

        public void Play()
        {
            gameObject.SetActive(true);
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            AudioManager.Play(glitchSound);
            float elapsed = 0f;
            if (blackout != null)
                blackout.alpha = 0f;
            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float interval = Mathf.Lerp(0.16f, 0.025f, progress);
                if (redFlash != null)
                {
                    Color color = redFlash.color;
                    color.a = UnityEngine.Random.Range(0.08f, Mathf.Lerp(0.25f, 0.85f, progress));
                    redFlash.color = color;
                }
                if (noiseLabel != null)
                {
                    noiseLabel.text = BuildNoise(UnityEngine.Random.Range(18, 48));
                    noiseLabel.rectTransform.anchoredPosition = UnityEngine.Random.insideUnitCircle * (22f * progress);
                }

                yield return new WaitForSecondsRealtime(interval);
                elapsed += interval;
            }

            if (blackout != null)
                blackout.alpha = 1f;
            if (redFlash != null)
                redFlash.color = Color.clear;
            if (noiseLabel != null)
                noiseLabel.text = string.Empty;

            AudioManager.Instance.StopMusic();
            yield return new WaitForSecondsRealtime(blackoutDuration);
            SceneLoader.Load(SceneType.Prologue);
        }

        private static string BuildNoise(int length)
        {
            const string glyphs = "01#@$%ERROR_SIGNAL_LOST_";
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = glyphs[UnityEngine.Random.Range(0, glyphs.Length)];
            return new string(result);
        }
    }
}
