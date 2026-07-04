using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class ShockwaveRingView : MonoBehaviour
    {
        [SerializeField] private LineRenderer ringRenderer;
        [SerializeField, ColorUsage(true, true)]
        private Color startColor = new(0.7f, 2.4f, 1.6f, 1f);

        [SerializeField, ColorUsage(true, true)]
        private Color endColor = new(0.2f, 0.8f, 1.2f, 0f);

        private float elapsed;
        private float duration;
        private float maximumDiameter;
        private bool isPlaying;

        public void Play(float playDuration, float diameter)
        {
            duration = Mathf.Max(0.01f, playDuration);
            maximumDiameter = Mathf.Max(0.01f, diameter);
            elapsed = 0f;
            isPlaying = true;
            ApplyFrame(0f);
        }

        private void Update()
        {
            if (!isPlaying)
                return;

            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            ApplyFrame(elapsed / duration);
            if (elapsed >= duration)
                isPlaying = false;
        }

        private void ApplyFrame(float normalizedTime)
        {
            float easedExpansion = 1f - Mathf.Pow(1f - normalizedTime, 4f);
            transform.localScale = Vector3.one * (maximumDiameter * easedExpansion);

            if (ringRenderer == null)
                return;

            Color color = Color.Lerp(startColor, endColor, normalizedTime);
            ringRenderer.startColor = color;
            ringRenderer.endColor = color;
            ringRenderer.widthMultiplier = Mathf.Lerp(0.16f, 0.035f, normalizedTime);
        }
    }
}
