using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    public class BlockData : ScriptableObject
    {
        [SerializeField]
        private string displayName;

        [SerializeField, Min(1)]
        private int maxHP = 100;

        [SerializeField, Min(0)]
        private int cost;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private Block prefab;

        [Tooltip("Optional targeting/filtering tags in addition to the data type's primary role.")]
        [SerializeField]
        private BlockProperty additionalProperties;

        public string DisplayName => displayName;
        public int MaxHP => maxHP;
        public int Cost => cost;
        public Sprite Sprite => sprite;
        public Block Prefab => prefab;
        public virtual BlockProperty Properties => additionalProperties;
        public virtual float EffectRange => 0f;
        public virtual AdjacencyDirection AffectedDirections => AdjacencyDirection.None;

        public virtual bool AffectsOffset(Vector2Int offset)
        {
            return false;
        }
    }
}
