using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "WallBlockData", menuName = "Keep Core Safe/Block Data/Wall")]
    public sealed class WallBlockData : BlockData
    {
        public override BlockProperty Properties => base.Properties | BlockProperty.Wall;
    }
}
