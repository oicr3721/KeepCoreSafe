using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "HealerBlockData", menuName = "Keep Core Safe/Block Data/Healer")]
    public sealed class HealerBlockData : TimedAreaBlockData
    {
        [SerializeField, Min(0)]
        private int healValue;

        public int HealValue => healValue;
        public override BlockProperty Properties => base.Properties | BlockProperty.Healer;
    }
}
