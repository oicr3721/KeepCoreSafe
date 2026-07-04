using UnityEngine;

namespace KeepCoreSafe.Combat
{
    public sealed class DamageFeedback : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float duration = 0.16f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Hit Readability")]
        [SerializeField] private Color flashColor = Color.black;
        [SerializeField, Range(0.01f, 0.8f)] private float flashPortion = 0.22f;
        [SerializeField, Min(0f)] private float shakeStrength = 0.08f;
        [SerializeField, Min(0f)] private float scalePunch = 0.12f;

        private SpriteRenderer targetRenderer;
        private Color baseColor = Color.white;
        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale;
        private float remaining;

        public void Initialize(SpriteRenderer renderer, Color normalColor)
        {
            targetRenderer = renderer;
            baseColor = normalColor;
            baseLocalPosition = renderer.transform.localPosition;
            baseLocalScale = renderer.transform.localScale;
            renderer.color = normalColor;
        }

        public void Play()
        {
            if (targetRenderer == null)
                return;

            targetRenderer.transform.localPosition = baseLocalPosition;
            targetRenderer.transform.localScale = baseLocalScale;
            targetRenderer.color = baseColor;
            remaining = duration;
        }

        public void Cancel()
        {
            ResetVisual();
        }

        private void Update()
        {
            if (remaining <= 0f || targetRenderer == null)
                return;

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            remaining = Mathf.Max(0f, remaining - deltaTime);
            float progress = 1f - remaining / duration;
            float envelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.35f);
            Vector2 shake = Random.insideUnitCircle * (shakeStrength * envelope);
            targetRenderer.transform.localPosition = baseLocalPosition + (Vector3)shake;
            targetRenderer.transform.localScale = baseLocalScale
                * (1f + Mathf.Sin(progress * Mathf.PI) * scalePunch);

            float safeFlashPortion = Mathf.Max(0.01f, flashPortion);
            if (progress < safeFlashPortion)
                targetRenderer.color = Color.Lerp(baseColor, flashColor, progress / safeFlashPortion);
            else
                targetRenderer.color = Color.Lerp(
                    flashColor,
                    baseColor,
                    (progress - safeFlashPortion) / (1f - safeFlashPortion));

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
            targetRenderer.transform.localScale = baseLocalScale;
            targetRenderer.color = baseColor;
            remaining = 0f;
        }
    }
}
