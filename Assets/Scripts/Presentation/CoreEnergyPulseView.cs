using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class CoreEnergyPulseView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer pulseRenderer;
        [SerializeField, ColorUsage(true, true)]
        private Color pulseColor = new(0.65f, 2.2f, 1.35f, 1f);

        private float elapsed;
        private float duration;
        private int pulseCount;
        private float minimumScale;
        private float maximumScale;
        private bool isPlaying;

        public void Play(float playDuration, int pulses, float minScale, float maxScale)
        {
            duration = Mathf.Max(0.01f, playDuration);
            pulseCount = Mathf.Max(1, pulses);
            minimumScale = Mathf.Max(0f, minScale);
            maximumScale = Mathf.Max(minimumScale, maxScale);
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
            if (pulseRenderer == null)
                return;

            float pulseTime = normalizedTime * pulseCount;
            int pulseIndex = Mathf.Min(pulseCount - 1, Mathf.FloorToInt(pulseTime));
            float cycle = pulseTime - Mathf.Floor(pulseTime);
            float envelope = Mathf.Sin(cycle * Mathf.PI);
            float intensity = Mathf.Lerp(0.45f, 1f, (pulseIndex + 1f) / pulseCount);
            float scale = Mathf.Lerp(minimumScale, maximumScale, cycle);

            transform.localScale = Vector3.one * scale;
            Color color = pulseColor;
            color.a *= envelope * intensity;
            pulseRenderer.color = color;
        }
    }
}
