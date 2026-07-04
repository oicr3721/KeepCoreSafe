using KeepCoreSafe.Blocks;
using KeepCoreSafe.Audio;
using UnityEngine;
using UnityEngine.Serialization;

namespace KeepCoreSafe.Data
{
    public class BlockData : ScriptableObject
    {
        [SerializeField]
        private string displayName;

        [SerializeField, TextArea(2, 5)]
        private string description;

        [SerializeField, Min(1)]
        private int maxHP = 100;

        [FormerlySerializedAs("cost")]
        [SerializeField, Min(0)]
        private int dismantleValue;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private Block prefab;

        [Tooltip("Optional targeting/filtering tags in addition to the data type's primary role.")]
        [SerializeField]
        private BlockProperty additionalProperties;

        [Header("Audio")]
        [Tooltip("Played when this block is destroyed by damage. Dismantling uses its own cue.")]
        [SerializeField] private AudioCue destroyedSound = new();

        public string DisplayName => displayName;
        public string Description => description;
        public int MaxHP => maxHP;
        public int DismantleValue => dismantleValue;
        public Sprite Sprite => sprite;
        public Block Prefab => prefab;
        public AudioCue DestroyedSound => destroyedSound;
        public virtual Color VisualColor => Color.white;
        public virtual BlockProperty Properties => additionalProperties;
        public virtual float EffectRange => 0f;
        public virtual AdjacencyDirection AffectedDirections => AdjacencyDirection.None;

        public virtual bool AffectsOffset(Vector2Int offset)
        {
            return false;
        }
    }
}
