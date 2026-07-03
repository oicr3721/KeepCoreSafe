using KeepCoreSafe.Enemies;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class AttackBlock : Block
    {
        private float cooldownRemaining;

        protected override void OnCombatUpdate(float deltaTime)
        {
            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f)
            {
                return;
            }

            Enemy target = FindNearestEnemy();
            if (target != null)
            {
                target.TakeDamage(Data.AttackValue);
                cooldownRemaining = GetAdjustedCooldown(Data.ActionCooldown);
            }
        }

        private Enemy FindNearestEnemy()
        {
            Enemy nearestEnemy = null;
            float nearestDistance = Data.EffectRange;

            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            return nearestEnemy;
        }
    }
}
