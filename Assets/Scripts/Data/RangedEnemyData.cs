using KeepCoreSafe.Combat;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "RangedEnemyData", menuName = "Keep Core Safe/Enemy Data/Ranged")]
    public sealed class RangedEnemyData : EnemyData
    {
        [SerializeField, Min(1f)]
        private float attackRange = 3f;

        [SerializeField, Min(0f)]
        private float attackRangeTolerance = 0.2f;

        [SerializeField, Min(0.1f)]
        private float projectileSpeed = 7f;

        [SerializeField, Min(0f)]
        private float projectileArcHeight = 0.7f;

        [SerializeField]
        private MissileProjectile projectilePrefab;

        public float AttackRange => attackRange;
        public float AttackRangeTolerance => attackRangeTolerance;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileArcHeight => projectileArcHeight;
        public MissileProjectile ProjectilePrefab => projectilePrefab;
    }
}
