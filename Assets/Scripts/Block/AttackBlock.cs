using System.Collections.Generic;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class AttackBlock : CombatBlock
    {
        private readonly List<Enemy> candidates = new();

        [Header("Laser")]
        [SerializeField] private LineRenderer laser;
        [SerializeField, Min(0.01f)] private float laserDuration = 0.14f;
        [SerializeField, Min(0.1f)] private float laserDrawSpeed = 2.5f;
        [SerializeField, Range(0f, 1f)] private float laserFadeStart = 0.55f;
        [SerializeField] private Color laserStartColor = Color.white;
        [SerializeField] private Color laserEndColor = new Color(1f, 0.18f, 0.08f, 1f);
        [SerializeField, Min(0f)] private float initialWidthMultiplier = 1.4f;
        [SerializeField, Min(0f)] private float finalWidthMultiplier = 0.25f;
        [SerializeField] private Transform laserAttachPoint;

        private Enemy currentTarget;
        private float cooldownRemaining;
        private float laserRemaining;
        private Vector3 laserEndPosition;

        private AttackBlockData AttackData => Data as AttackBlockData;

        protected override void Awake()
        {
            base.Awake();
            if (laser != null)
                laser.enabled = false;
        }

        protected override void Update()
        {
            base.Update();
            if (GameManager.Phase != GamePhase.Combat && laser != null)
            {
                laserRemaining = 0f;
                laser.enabled = false;
            }
        }

        protected override void OnCombatUpdate(float deltaTime)
        {
            if (AttackData == null)
                return;

            UpdateLaser(deltaTime);
            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f)
                return;

            if (currentTarget == null || currentTarget.IsDead)
                currentTarget = FindRandomEnemyInEffectArea();

            if (currentTarget == null)
                return;

            laserEndPosition = currentTarget.HitPoint.position;
            currentTarget.TakeDamage(AttackData.AttackValue);
            AudioManager.PlayAt(AttackData.AttackSound, transform.position);
            PlayLaser();
            cooldownRemaining = GetAdjustedCooldown(AttackData.ActionCooldown);
        }

        private Enemy FindRandomEnemyInEffectArea()
        {
            candidates.Clear();
            if (GridManager.Instance == null || !HasGridPosition)
                return null;

            IReadOnlyCollection<Enemy> activeEnemies = GameManager.Instance?.ActiveEnemies;
            if (activeEnemies == null)
                return null;

            foreach (Enemy enemy in activeEnemies)
            {
                if (enemy == null || enemy.IsDead)
                    continue;

                Vector2Int enemyCell = GridManager.Instance.WorldToGrid(enemy.transform.position);
                Vector2Int offset = enemyCell - GridPosition;
                if (GridEffectArea.ContainsOffset(
                        offset,
                        AttackData.AffectedDirections,
                        AttackData.EffectRange))
                    candidates.Add(enemy);
            }

            return candidates.Count == 0
                ? null
                : candidates[Random.Range(0, candidates.Count)];
        }

        private void PlayLaser()
        {
            if (laser == null)
                return;

            laserRemaining = laserDuration;
            laser.enabled = true;
            laser.SetPosition(0, laserAttachPoint.position);
            laser.SetPosition(1, laserAttachPoint.position);
        }

        private void UpdateLaser(float deltaTime)
        {
            if (laserRemaining <= 0f || laser == null)
                return;

            laserRemaining = Mathf.Max(0f, laserRemaining - deltaTime);
            float progress = 1f - laserRemaining / laserDuration;
            Vector3 end = currentTarget != null ? currentTarget.HitPoint.position : laserEndPosition;
            laserEndPosition = end;
            laser.SetPosition(0, laserAttachPoint.position);
            laser.SetPosition(1, Vector3.Lerp(
                laserAttachPoint.position,
                end,
                Mathf.Clamp01(progress * laserDrawSpeed)));

            float fade = 1f - Mathf.InverseLerp(laserFadeStart, 1f, progress);
            Color startColor = laserStartColor;
            Color endColor = laserEndColor;
            startColor.a *= fade;
            endColor.a *= fade;
            laser.startColor = startColor;
            laser.endColor = endColor;
            laser.widthMultiplier = Mathf.Lerp(
                initialWidthMultiplier,
                finalWidthMultiplier,
                progress);

            if (laserRemaining <= 0f)
                laser.enabled = false;
        }
    }
}
