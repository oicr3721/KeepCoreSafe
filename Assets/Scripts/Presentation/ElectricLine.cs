using System;
using DG.Tweening;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ElectricLine : MonoBehaviour
    {
        [Header("Line")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField, Range(3, 24)] private int pointCount = 9;
        [SerializeField, Min(0f)] private float baseWidth = 0.055f;
        [SerializeField, ColorUsage(true, true)]
        private Color color = new(0.3f, 0.85f, 1.8f, 0.9f);

        [Header("Electric Motion")]
        [SerializeField, Min(0f)] private float noiseAmplitude = 0.055f;
        [SerializeField, Min(0f)] private float noiseFrequency = 14f;
        [SerializeField, Min(0f)] private float noiseSpeed = 9f;
        [SerializeField, Min(0f)] private float pulseSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.2f;
        [SerializeField] private Vector2 uvScrollSpeed = new(2.5f, 0f);

        [Header("Transition")]
        [SerializeField, Min(0f)] private float revealDuration = 0.08f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.12f;

        private Transform source;
        private Transform target;
        private Material runtimeMaterial;
        private Action<ElectricLine> returnToPool;
        private float visibility;
        private float collapse;
        private float motionTime;
        private bool isPlaying;
        private bool isReleasing;

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = Mathf.Max(3, pointCount);
            runtimeMaterial = lineRenderer.material;
        }

        public void Play(Transform sourceTransform, Transform targetTransform, Action<ElectricLine> onReleased)
        {
            transform.DOKill(false);
            source = sourceTransform;
            target = targetTransform;
            returnToPool = onReleased;
            visibility = 0f;
            collapse = 0f;
            motionTime = UnityEngine.Random.value * 10f;
            isPlaying = true;
            isReleasing = false;
            lineRenderer.enabled = true;

            DOTween.To(() => visibility, value => visibility = value, 1f, revealDuration)
                .SetTarget(transform)
                .SetEase(Ease.OutQuad);
            UpdateLine();
        }

        public void Release()
        {
            if (!isPlaying || isReleasing)
                return;

            isReleasing = true;
            transform.DOKill(false);
            DOTween.To(() => collapse, value => collapse = value, 1f, releaseDuration)
                .SetTarget(transform)
                .SetEase(Ease.InCubic)
                .OnComplete(ReturnToPool);
        }

        private void Update()
        {
            if (!isPlaying)
                return;

            if (source == null || target == null)
            {
                Release();
                return;
            }

            motionTime += Time.deltaTime;
            UpdateLine();
        }

        private void UpdateLine()
        {
            if (source == null || target == null || lineRenderer == null)
                return;

            Vector3 start = source.position;
            Vector3 targetPosition = Vector3.Lerp(target.position, start, collapse);
            Vector2 direction = targetPosition - start;
            Vector2 perpendicular = direction.sqrMagnitude > 0.0001f
                ? new Vector2(-direction.y, direction.x).normalized
                : Vector2.up;

            int count = Mathf.Max(3, pointCount);
            if (lineRenderer.positionCount != count)
                lineRenderer.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                Vector3 point = Vector3.Lerp(start, targetPosition, t);
                if (i > 0 && i < count - 1)
                {
                    float noise = Mathf.PerlinNoise(
                        t * noiseFrequency + motionTime * noiseSpeed,
                        motionTime * 0.37f) * 2f - 1f;
                    point += (Vector3)(perpendicular * (noise * noiseAmplitude));
                }

                lineRenderer.SetPosition(i, point);
            }

            float pulse = 1f + Mathf.Sin(motionTime * pulseSpeed) * pulseAmount;
            lineRenderer.widthMultiplier = baseWidth * pulse * visibility * (1f - collapse);
            Color visibleColor = color;
            visibleColor.a *= visibility * (1f - collapse);
            lineRenderer.startColor = visibleColor;
            lineRenderer.endColor = visibleColor;

            if (runtimeMaterial != null)
                runtimeMaterial.mainTextureOffset += uvScrollSpeed * Time.deltaTime;
        }

        private void ReturnToPool()
        {
            isPlaying = false;
            isReleasing = false;
            lineRenderer.enabled = false;
            source = null;
            target = null;
            Action<ElectricLine> callback = returnToPool;
            returnToPool = null;
            callback?.Invoke(this);
        }

        private void OnDisable()
        {
            transform.DOKill(false);
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            transform.DOKill(false);
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
        }
    }
}
