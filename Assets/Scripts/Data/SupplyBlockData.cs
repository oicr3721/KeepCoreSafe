using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "SupplyBlockData", menuName = "Keep Core Safe/Block System/Supply Event Block")]
    public sealed class SupplyBlockData : BlockData
    {
        public override Color VisualColor => Color != null ? Color.Color : UnityEngine.Color.white;
        public override BlockProperty Properties => base.Properties | BlockProperty.Supply;
    }
}
