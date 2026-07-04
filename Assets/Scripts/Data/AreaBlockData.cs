using UnityEngine;

namespace KeepCoreSafe.Data
{
    public abstract class AreaBlockData : BlockData
    {
        [SerializeField, Min(0f)]
        private float effectRange = 1f;

        [SerializeField]
        private AdjacencyDirection affectedDirections;

        public override float EffectRange => effectRange;
        public override AdjacencyDirection AffectedDirections => affectedDirections;

        public override bool AffectsOffset(Vector2Int offset)
        {
            return GridEffectArea.ContainsOffset(offset, affectedDirections, effectRange);
        }
    }
}
