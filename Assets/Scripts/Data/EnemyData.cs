using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Keep Core Safe/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField]
        private string displayName;

        [SerializeField, Min(1)]
        private int maxHP = 60;

        [SerializeField, Min(0.1f)]
        private float moveSpeed = 1.6f;

        [SerializeField, Min(1)]
        private int attackDamage = 12;

        [SerializeField, Min(0.1f)]
        private float attackCooldown = 1f;

        [SerializeField, Min(0f)]
        private float attackRange = 0.08f;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private BlockProperty[] targetPriority = { BlockProperty.Core, BlockProperty.Wall };

        public string DisplayName => displayName;
        public int MaxHP => maxHP;
        public float MoveSpeed => moveSpeed;
        public int AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public float AttackRange => attackRange;
        public Sprite Sprite => sprite;

        public int GetPriority(BlockProperty property)
        {
            for (int i = 0; i < targetPriority.Length; i++)
            {
                if ((property & targetPriority[i]) != 0) return i;
            }

            return targetPriority.Length;
        }

#if UNITY_EDITOR
        public void ConfigurePrototype(
            string name,
            int hp,
            float speed,
            int damage,
            float cooldown,
            float range,
            Sprite enemySprite,
            BlockProperty[] priorities)
        {
            displayName = name;
            maxHP = hp;
            moveSpeed = speed;
            attackDamage = damage;
            attackCooldown = cooldown;
            attackRange = range;
            sprite = enemySprite;
            targetPriority = priorities;
        }
#endif
    }
}
