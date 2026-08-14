using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "BasicBlockData", menuName = "Keep Core Safe/Block Data/Basic")]
    public sealed class BasicBlockData : BlockData
    {
        public override Color VisualColor => Color != null ? Color.Color : UnityEngine.Color.white;
        public override BlockProperty Properties => base.Properties | BlockProperty.Wall;
    }
}
