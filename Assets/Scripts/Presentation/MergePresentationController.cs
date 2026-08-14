using System;
using System.Collections.Generic;
using DG.Tweening;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class MergePresentationController : MonoBehaviour
    {
        public readonly struct SourceVisual
        {
            public SourceVisual(SpriteRenderer renderer)
            {
                Sprite = renderer != null ? renderer.sprite : null;
                Color = renderer != null ? renderer.color : Color.white;
                Position = renderer != null ? renderer.transform.position : Vector3.zero;
                Scale = renderer != null ? renderer.transform.lossyScale : Vector3.one;
                SortingLayerId = renderer != null ? renderer.sortingLayerID : 0;
                SortingOrder = renderer != null ? renderer.sortingOrder : 0;
            }

            public Sprite Sprite { get; }
            public Color Color { get; }
            public Vector3 Position { get; }
            public Vector3 Scale { get; }
            public int SortingLayerId { get; }
            public int SortingOrder { get; }
            public bool IsValid => Sprite != null;
        }

        private sealed class SequenceContext
        {
            public readonly List<GameObject> SpawnedObjects = new();
            public readonly List<SpriteRenderer> SourceRenderers = new();
            public GridManager.InteractionLock InteractionLock;
            public Block ResultBlock;
            public SpriteRenderer ResultRenderer;
            public Material ResultMaterial;
            public Color ResultColor;
            public bool ResultRendererEnabled;
            public Sequence TweenSequence;
            public bool Released;
        }

        private static readonly int MaskAmountId = Shader.PropertyToID("_MaskAmount");

        [Header("Prefab and Material References")]
        [SerializeField] private Material whiteMaskMaterial;
        [SerializeField] private CoreEnergyPulseView energyPulsePrefab;
        [SerializeField] private ShockwaveRingView shockwavePrefab;
        [SerializeField] private ParticleSystem burstParticlesPrefab;
        [SerializeField] private Transform effectRoot;

        [Header("Source Mask")]
        [SerializeField, Min(0f)] private float maskTransitionDuration = 0.1f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveDuration = 0.2f;
        [SerializeField] private Ease moveEase = Ease.InOutCubic;

        [Header("Energy Compression")]
        [SerializeField, Min(0f)] private float compressionDuration = 0.15f;
        [SerializeField, Range(0.05f, 1f)] private float compressionScale = 0.62f;
        [SerializeField, Min(0f)] private float compressionGlowScale = 1.1f;

        [Header("Merge Burst")]
        [SerializeField, Min(0.01f)] private float burstDuration = 0.1f;
        [SerializeField, Min(0f)] private float flashIntensity = 1f;
        [SerializeField, Min(0f)] private float shockwaveScale = 2.2f;
        [SerializeField, Min(0)] private int particleCount = 18;
        [SerializeField, Min(0.01f)] private float particleLifetime = 0.34f;
        [Tooltip("Played through AudioManager at the exact moment the special-block burst begins.")]
        [SerializeField] private AudioClip specialBlockMergeSound;

        [Header("Result Reveal")]
        [SerializeField, Min(0f)] private float resultRevealDelay = 0.1f;
        [SerializeField, Min(0f)] private float resultRevealDuration = 0.2f;
        [SerializeField, Min(0f)] private float resultEmissionIntensity = 1.25f;

        [Header("Camera Shake")]
        [SerializeField, Min(0f)] private float cameraShakeStrength = 0.06f;
        [SerializeField, Min(0f)] private float cameraShakeDuration = 0.08f;

        private readonly HashSet<SequenceContext> activeContexts = new();

        public bool Play(
            IReadOnlyList<SourceVisual> sources,
            Vector3 mergePoint,
            Block resultBlock,
            GridManager.InteractionLock interactionLock,
            Action onBurst = null)
        {
            if (sources == null
                || sources.Count == 0
                || resultBlock == null
                || resultBlock.VisualRenderer == null
                || whiteMaskMaterial == null)
            {
                interactionLock?.Dispose();
                return false;
            }

            SequenceContext context = CreateContext(resultBlock, interactionLock);
            foreach (SourceVisual source in sources)
            {
                if (source.IsValid)
                    CreateSourceProxy(context, source);
            }

            if (context.SourceRenderers.Count == 0)
            {
                Release(context);
                return false;
            }

            activeContexts.Add(context);
            context.TweenSequence = BuildSequence(context, mergePoint, onBurst);
            return true;
        }

        private SequenceContext CreateContext(Block resultBlock, GridManager.InteractionLock interactionLock)
        {
            SpriteRenderer renderer = resultBlock.VisualRenderer;
            SequenceContext context = new()
            {
                InteractionLock = interactionLock,
                ResultBlock = resultBlock,
                ResultRenderer = renderer,
                ResultMaterial = renderer.sharedMaterial,
                ResultColor = renderer.color,
                ResultRendererEnabled = renderer.enabled
            };

            resultBlock.SetPresentationHealthBarVisible(false);
            renderer.enabled = false;
            return context;
        }

        private void CreateSourceProxy(SequenceContext context, SourceVisual source)
        {
            GameObject proxy = new("Merge Source Visual", typeof(SpriteRenderer));
            proxy.transform.SetParent(effectRoot != null ? effectRoot : transform, true);
            proxy.transform.position = source.Position;
            proxy.transform.localScale = source.Scale;

            SpriteRenderer renderer = proxy.GetComponent<SpriteRenderer>();
            renderer.sprite = source.Sprite;
            renderer.color = source.Color;
            renderer.sharedMaterial = whiteMaskMaterial;
            renderer.sortingLayerID = source.SortingLayerId;
            renderer.sortingOrder = Mathf.Max(source.SortingOrder, 40);
            SetMaskAmount(renderer, 0f);

            context.SpawnedObjects.Add(proxy);
            context.SourceRenderers.Add(renderer);
        }

        private Sequence BuildSequence(SequenceContext context, Vector3 mergePoint, Action onBurst)
        {
            Sequence sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .SetAutoKill(true);

            for (int i = 0; i < context.SourceRenderers.Count; i++)
            {
                SpriteRenderer captured = context.SourceRenderers[i];
                Tween maskTween = DOTween.To(
                    () => GetMaskAmount(captured),
                    value => SetMaskAmount(captured, value),
                    1f,
                    maskTransitionDuration);
                if (i == 0)
                    sequence.Append(maskTween);
                else
                    sequence.Join(maskTween);
            }

            for (int i = 0; i < context.SourceRenderers.Count; i++)
            {
                Tween moveTween = context.SourceRenderers[i].transform
                    .DOMove(mergePoint, moveDuration)
                    .SetEase(moveEase);
                if (i == 0)
                    sequence.Append(moveTween);
                else
                    sequence.Join(moveTween);
            }

            sequence.AppendCallback(() => PlayCompressionGlow(context, mergePoint));
            for (int i = 0; i < context.SourceRenderers.Count; i++)
            {
                SpriteRenderer renderer = context.SourceRenderers[i];
                Tween compressionTween = renderer.transform.DOScale(
                        renderer.transform.localScale * compressionScale,
                    compressionDuration)
                    .SetEase(Ease.InCubic);
                if (i == 0)
                    sequence.Append(compressionTween);
                else
                    sequence.Join(compressionTween);
            }

            sequence.AppendCallback(() =>
            {
                HideSources(context);
                PlayBurst(context, mergePoint);
                ShowMaskedResult(context);
                onBurst?.Invoke();
            });
            sequence.AppendInterval(resultRevealDelay);
            sequence.AppendCallback(() => PlayResultActivation(context, mergePoint));
            sequence.Append(DOTween.To(
                    () => GetMaskAmount(context.ResultRenderer),
                    value => SetMaskAmount(context.ResultRenderer, value),
                    0f,
                    resultRevealDuration)
                .SetEase(Ease.OutCubic));
            sequence.OnComplete(() => Release(context));
            sequence.OnKill(() => Release(context));
            return sequence;
        }

        private void PlayCompressionGlow(SequenceContext context, Vector3 position)
        {
            CoreEnergyPulseView pulse = Spawn(context, energyPulsePrefab, position);
            pulse?.Play(compressionDuration, 2, 0.08f, compressionGlowScale);
        }

        private void PlayBurst(SequenceContext context, Vector3 position)
        {
            if (IsSpecialBlock(context.ResultBlock))
                AudioManager.Play(specialBlockMergeSound);

            CoreEnergyPulseView flash = Spawn(context, energyPulsePrefab, position);
            flash?.Play(
                burstDuration,
                1,
                0.05f,
                Mathf.Max(0.4f, shockwaveScale * 0.55f),
                flashIntensity);

            ShockwaveRingView shockwave = Spawn(context, shockwavePrefab, position);
            shockwave?.Play(burstDuration, shockwaveScale);

            ParticleSystem particles = Spawn(context, burstParticlesPrefab, position);
            if (particles != null)
            {
                ParticleSystem.MainModule main = particles.main;
                main.startLifetime = particleLifetime;
                particles.Emit(particleCount);
            }

            if (cameraShakeStrength > 0f && cameraShakeDuration > 0f)
                GameCameraController.Instance?.PlayImpactShake(cameraShakeStrength, cameraShakeDuration);
        }

        private static bool IsSpecialBlock(Block block)
        {
            if (block == null)
                return false;

            const BlockProperty specialProperties =
                BlockProperty.Attack | BlockProperty.Support | BlockProperty.Healer;
            return (block.BlockProperty & specialProperties) != 0;
        }

        private void ShowMaskedResult(SequenceContext context)
        {
            if (context.ResultRenderer == null)
                return;

            context.ResultRenderer.sharedMaterial = whiteMaskMaterial;
            context.ResultRenderer.color = context.ResultColor;
            SetMaskAmount(context.ResultRenderer, 1f);
            context.ResultRenderer.enabled = true;
        }

        private void PlayResultActivation(SequenceContext context, Vector3 position)
        {
            CoreEnergyPulseView pulse = Spawn(context, energyPulsePrefab, position);
            pulse?.Play(
                Mathf.Max(0.01f, resultRevealDuration),
                1,
                0.15f,
                0.9f,
                resultEmissionIntensity);
        }

        private T Spawn<T>(SequenceContext context, T prefab, Vector3 position) where T : Component
        {
            if (prefab == null)
                return null;

            T instance = Instantiate(prefab, position, Quaternion.identity, effectRoot != null ? effectRoot : transform);
            context.SpawnedObjects.Add(instance.gameObject);
            return instance;
        }

        private static void HideSources(SequenceContext context)
        {
            foreach (SpriteRenderer renderer in context.SourceRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        private void Release(SequenceContext context)
        {
            if (context == null || context.Released)
                return;

            context.Released = true;
            if (context.ResultRenderer != null)
            {
                context.ResultRenderer.sharedMaterial = context.ResultMaterial;
                context.ResultRenderer.color = context.ResultColor;
                context.ResultRenderer.enabled = context.ResultRendererEnabled;
                context.ResultRenderer.SetPropertyBlock(null);
            }

            if (context.ResultBlock != null)
                context.ResultBlock.SetPresentationHealthBarVisible(true);

            context.InteractionLock?.Dispose();
            foreach (GameObject spawnedObject in context.SpawnedObjects)
            {
                if (spawnedObject != null)
                    Destroy(spawnedObject);
            }

            activeContexts.Remove(context);
        }

        private static float GetMaskAmount(SpriteRenderer renderer)
        {
            if (renderer == null)
                return 0f;

            MaterialPropertyBlock properties = new();
            renderer.GetPropertyBlock(properties);
            return properties.GetFloat(MaskAmountId);
        }

        private static void SetMaskAmount(SpriteRenderer renderer, float amount)
        {
            if (renderer == null)
                return;

            MaterialPropertyBlock properties = new();
            renderer.GetPropertyBlock(properties);
            properties.SetFloat(MaskAmountId, Mathf.Clamp01(amount));
            renderer.SetPropertyBlock(properties);
        }

        private void OnDestroy()
        {
            SequenceContext[] contexts = new SequenceContext[activeContexts.Count];
            activeContexts.CopyTo(contexts);
            foreach (SequenceContext context in contexts)
            {
                context.TweenSequence?.Kill(false);
                Release(context);
            }
        }
    }
}
