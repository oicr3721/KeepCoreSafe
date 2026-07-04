using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "AttackBlockData", menuName = "Keep Core Safe/Block Data/Attack")]
    public sealed class AttackBlockData : TimedAreaBlockData
    {
        [SerializeField, Min(0)]
        private int attackValue;

        public int AttackValue => attackValue;
        public override BlockProperty Properties => base.Properties | BlockProperty.Attack;
    }
}
