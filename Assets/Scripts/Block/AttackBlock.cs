using System.Collections.Generic;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public sealed class AttackBlock : Block
    {
        private const float LaserDuration = 0.14f;

        private readonly List<Enemy> candidates = new();
        private GridManager gridManager;
        private Enemy currentTarget;
        private LineRenderer laser;
        private Material laserMaterial;
        private float cooldownRemaining;
        private float laserRemaining;
        private Vector3 laserEndPosition;

        protected override void Awake()
        {
            base.Awake();
            CreateLaserRenderer();
        }

        protected override void Start()
        {
            base.Start();
            gridManager = FindFirstObjectByType<GridManager>();
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
            UpdateLaser(deltaTime);
            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f)
                return;

            if (currentTarget == null || currentTarget.IsDead)
                currentTarget = FindRandomEnemyInEffectArea();

            if (currentTarget == null)
                return;

            laserEndPosition = currentTarget.transform.position;
            currentTarget.TakeDamage(Data.AttackValue);
            PlayLaser();
            cooldownRemaining = GetAdjustedCooldown(Data.ActionCooldown);
        }

        private Enemy FindRandomEnemyInEffectArea()
        {
            candidates.Clear();
            if (gridManager == null || !HasGridPosition)
                return null;

            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (enemy == null || enemy.IsDead)
                    continue;

                Vector2Int enemyCell = gridManager.WorldToGrid(enemy.transform.position);
                Vector2Int offset = enemyCell - GridPosition;
                if (GridEffectArea.ContainsOffset(offset, Data.AffectedDirections, Data.EffectRange))
                    candidates.Add(enemy);
            }

            return candidates.Count == 0
                ? null
                : candidates[Random.Range(0, candidates.Count)];
        }

        private void CreateLaserRenderer()
        {
            laser = gameObject.AddComponent<LineRenderer>();
            laser.useWorldSpace = true;
            laser.positionCount = 2;
            laser.numCapVertices = 4;
            laser.sortingOrder = 12;
            laser.startWidth = 0.08f;
            laser.endWidth = 0.035f;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                laserMaterial = new Material(shader) { name = "Attack Laser Material" };
                laser.sharedMaterial = laserMaterial;
            }

            laser.enabled = false;
        }

        private void PlayLaser()
        {
            laserRemaining = LaserDuration;
            laser.enabled = true;
            laser.SetPosition(0, transform.position);
            laser.SetPosition(1, transform.position);
        }

        private void UpdateLaser(float deltaTime)
        {
            if (laserRemaining <= 0f || laser == null)
                return;

            laserRemaining = Mathf.Max(0f, laserRemaining - deltaTime);
            float progress = 1f - laserRemaining / LaserDuration;
            Vector3 end = currentTarget != null ? currentTarget.transform.position : laserEndPosition;
            laserEndPosition = end;
            laser.SetPosition(0, transform.position);
            laser.SetPosition(1, Vector3.Lerp(transform.position, end, Mathf.Clamp01(progress * 2.5f)));

            float fade = 1f - Mathf.InverseLerp(0.55f, 1f, progress);
            Color color = new Color(1f, 0.18f, 0.08f, fade);
            laser.startColor = Color.white * new Color(1f, 1f, 1f, fade);
            laser.endColor = color;
            laser.widthMultiplier = Mathf.Lerp(1.4f, 0.25f, progress);

            if (laserRemaining <= 0f)
                laser.enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (laserMaterial != null)
                Destroy(laserMaterial);
        }
    }
}
