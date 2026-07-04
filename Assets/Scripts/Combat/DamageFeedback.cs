using UnityEngine;

namespace KeepCoreSafe.Combat
{
    public sealed class DamageFeedback : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float duration = 0.18f;
        [SerializeField, Min(0f)] private float shakeStrength = 0.1f;

        private SpriteRenderer targetRenderer;
        private Color baseColor = Color.white;
        private Vector3 baseLocalPosition;
        private float remaining;

        public void Initialize(SpriteRenderer renderer, Color normalColor)
        {
            targetRenderer = renderer;
            baseColor = normalColor;
            baseLocalPosition = renderer.transform.localPosition;
            renderer.color = normalColor;
        }

        public void Play()
        {
            if (targetRenderer == null)
                return;

            targetRenderer.transform.localPosition = baseLocalPosition;
            targetRenderer.color = baseColor;
            remaining = duration;
        }

        private void Update()
        {
            if (remaining <= 0f || targetRenderer == null)
                return;

            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            float progress = 1f - remaining / duration;
            float envelope = 1f - progress;
            Vector2 shake = Random.insideUnitCircle * (shakeStrength * envelope);
            shake.y = 0;
            targetRenderer.transform.localPosition = baseLocalPosition + (Vector3)shake;

            if (progress < 0.25f)
                targetRenderer.color = Color.Lerp(baseColor, Color.white, progress / 0.25f);
            else if (progress < 0.62f)
                targetRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.16f, 0.12f), (progress - 0.25f) / 0.37f);
            else
                targetRenderer.color = Color.Lerp(new Color(1f, 0.16f, 0.12f), baseColor, (progress - 0.62f) / 0.38f);

            if (remaining <= 0f)
                ResetVisual();
        }

        private void OnDisable()
        {
            ResetVisual();
        }

        private void ResetVisual()
        {
            if (targetRenderer == null)
                return;

            targetRenderer.transform.localPosition = baseLocalPosition;
            targetRenderer.color = baseColor;
            remaining = 0f;
        }
    }
}
