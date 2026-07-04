using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "BasicBlockData", menuName = "Keep Core Safe/Block Data/Basic")]
    public sealed class BasicBlockData : BlockData
    {
        [SerializeField] private BlockColorData color;

        public BlockColorData Color => color;
        public override Color VisualColor => color != null ? color.Color : UnityEngine.Color.white;
        public override BlockProperty Properties => base.Properties | BlockProperty.Wall;
    }
}
