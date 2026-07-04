using KeepCoreSafe.Blocks;
using KeepCoreSafe.Enemies;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    public class EnemyData : ScriptableObject
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

        [SerializeField, Min(0.05f)]
        private float repathInterval = 0.4f;

        [SerializeField, Min(0)]
        private int maxPreferredPathExtraCells = 4;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private Enemy prefab;

        [SerializeField]
        private BlockProperty[] targetPriority = { BlockProperty.Core, BlockProperty.Wall };

        public string DisplayName => displayName;
        public int MaxHP => maxHP;
        public float MoveSpeed => moveSpeed;
        public int AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public float RepathInterval => repathInterval;
        public int MaxPreferredPathExtraCells => maxPreferredPathExtraCells;
        public Sprite Sprite => sprite;
        public Enemy Prefab => prefab;

        public int GetPriority(BlockProperty property)
        {
            for (int i = 0; i < targetPriority.Length; i++)
            {
                if ((property & targetPriority[i]) != 0)
                    return i;
            }

            return targetPriority.Length;
        }
    }
}
