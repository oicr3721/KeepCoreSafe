using KeepCoreSafe.Blocks;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Localization;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    public class BlockData : ScriptableObject
    {
        [SerializeField, Tooltip("Stable analytics identifier. Falls back to the asset name when empty.")]
        private string analyticsId;

        [SerializeField]
        [Tooltip("Localization key for the block display name.")]
        private string displayName;

        [SerializeField, TextArea(2, 5)]
        [Tooltip("Localization key for the block description.")]
        private string description;

        [SerializeField, Min(1)]
        private int maxHP = 100;

        [SerializeField]
        private Sprite sprite;

        [Header("Health Visuals")]
        [SerializeField]
        [Tooltip("Sprites ordered from highest to lowest health. Empty or null entries use the base Sprite.")]
        private Sprite[] healthStageSprites = System.Array.Empty<Sprite>();

        [Header("Block Color")]
        [SerializeField] private BlockColorData color;

        [SerializeField]
        private Block prefab;

        [Tooltip("Optional targeting/filtering tags in addition to the data type's primary role.")]
        [SerializeField]
        private BlockProperty additionalProperties;

        [Header("Audio")]
        [Tooltip("Played when this block is destroyed by damage. Dismantling uses its own cue.")]
        [SerializeField] private AudioCue destroyedSound = new();

        public string DisplayName => LocalizationManager.Get(displayName, displayName);
        public string Description => LocalizationManager.Get(description, description);
        public string DisplayNameKey => displayName;
        public string DescriptionKey => description;
        public string AnalyticsId => string.IsNullOrWhiteSpace(analyticsId) ? name : analyticsId;
        public int MaxHP => maxHP;
        public Sprite Sprite => sprite;
        public BlockColorData Color => color;
        public Color DestroyEffectColor => color != null ? color.Color : VisualColor;
        public Block Prefab => prefab;
        public AudioCue DestroyedSound => destroyedSound;
        public virtual Color VisualColor => UnityEngine.Color.white;
        public virtual BlockProperty Properties => additionalProperties;
        public virtual float EffectRange => 0f;
        public virtual AdjacencyDirection AffectedDirections => AdjacencyDirection.None;

        public virtual bool AffectsOffset(Vector2Int offset)
        {
            return false;
        }

        public Sprite GetHealthSprite(float healthRatio)
        {
            if (healthStageSprites == null || healthStageSprites.Length == 0)
                return sprite;

            float clampedRatio = Mathf.Clamp01(healthRatio);
            int stageIndex = Mathf.Clamp(
                Mathf.FloorToInt((1f - clampedRatio) * healthStageSprites.Length),
                0,
                healthStageSprites.Length - 1);
            Sprite stageSprite = healthStageSprites[stageIndex];
            return stageSprite != null ? stageSprite : sprite;
        }
    }
}
