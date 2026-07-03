using System;
using System.Collections.Generic;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        private static readonly List<Enemy> ActiveEnemies = new();
        private static readonly List<Collider2D> ActiveEnemyColliders = new();

        [SerializeField]
        private EnemyData data;

        [Header("Visual Movement")]
        [SerializeField, Range(0f, 0.4f)]
        private float personalOffsetRatio = 0.25f;

        [SerializeField, Range(0.1f, 1f)]
        private float separationRadiusInCells = 0.55f;

        [SerializeField, Range(0f, 0.5f)]
        private float separationStrength = 0.2f;

        private bool isDead;
        private DamageFeedback damageFeedback;
        private bool isMovingToCell;
        private Vector2Int movementDestination;
        private Vector2 personalCellOffset;

        protected Rigidbody2D Body { get; private set; }
        protected Collider2D CollisionCollider { get; private set; }
        protected GridManager GridManager { get; private set; }

        public EnemyData Data => data;
        public int MaxHP => data != null ? data.MaxHP : 1;
        public int CurrentHP { get; private set; }
        public bool IsDead => isDead;
        public Vector2 PersonalCellOffset => personalCellOffset;

        public event Action<Enemy> Died;

        private bool pathDirty;

        protected virtual void Awake()
        {
            EnsurePhysicsComponents();
            ActiveEnemies.Add(this);
        }

        protected virtual void Start()
        {
            GridManager = GridManager.Instance;
            if (data == null) Debug.LogError($"{name} has no EnemyData.", this);
            if (GridManager != null)
            {
                GeneratePersonalCellOffset();
                GridManager.GridChanged += OnGridChanged;
            }
        }

        public void Initialize(EnemyData enemyData)
        {
            data = enemyData;
            CurrentHP = data.MaxHP;
            ApplySprite();
        }

        protected virtual void Update()
        {
            if (GameManager.Phase == GamePhase.Combat)
            {
                OnCombatUpdate(Time.deltaTime);
            }
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || isDead)
            {
                return;
            }

            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            damageFeedback?.Play();
            if (CurrentHP == 0)
            {
                Die();
            }
        }

        private void OnGridChanged()
        {
            pathDirty = true;
        }

        protected virtual void OnCombatUpdate(float deltaTime)
        {
            if (pathDirty && !isMovingToCell)
            {
                pathDirty = false;
                RebuildPlan();
            }
        }

        protected bool ContinueCellMovement(float deltaTime)
        {
            if (!isMovingToCell || GridManager == null)
                return false;

            Vector2 destination = GetCellWorldPosition(movementDestination);
            Vector2 offset = destination - Body.position;
            float snapDistance = Mathf.Max(0.02f, Data.MoveSpeed * Time.fixedDeltaTime);
            if (offset.sqrMagnitude <= snapDistance * snapDistance)
            {
                Body.position = destination;
                Body.linearVelocity = Vector2.zero;
                isMovingToCell = false;
            }
            else
            {
                Vector2 desiredVelocity = offset.normalized * Data.MoveSpeed;
                Vector2 separationVelocity = CalculateSeparationVelocity();
                Body.linearVelocity = Vector2.ClampMagnitude(
                    desiredVelocity + separationVelocity,
                    Data.MoveSpeed);
            }

            return true;
        }

        protected bool TryBeginCellMovement(Vector2Int destination)
        {
            if (isMovingToCell || GridManager == null)
                return false;

            if (!GridManager.Grid.IsWithinBounds(destination)
                || !GridManager.IsCellEmpty(destination))
            {
                return false;
            }

            if (TryGetCurrentCell(out Vector2Int current)
                && current == destination
                && Vector2.Distance(Body.position, GetCellWorldPosition(destination)) < 0.05f)
            {
                return false;
            }

            movementDestination = destination;
            isMovingToCell = true;
            return true;
        }

        protected bool TryGetCurrentCell(out Vector2Int position)
        {
            position = default;
            if (GridManager == null)
                return false;

            position = GridManager.WorldToGrid(Body.position - personalCellOffset);
            return GridManager.Grid.IsWithinBounds(position);
        }

        protected void StopMoving()
        {
            Body.linearVelocity = Vector2.zero;
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            Body.linearVelocity = Vector2.zero;
            GameManager.PlacePoint.AddValue(1f);
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void EnsurePhysicsComponents()
        {
            if (!TryGetComponent(out CircleCollider2D circleCollider))
            {
                circleCollider = gameObject.AddComponent<CircleCollider2D>();
            }

            CollisionCollider = circleCollider;
            IgnoreOtherEnemies();

            if (!TryGetComponent(out Rigidbody2D body))
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }

            Body = body;
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = 0f;
            Body.freezeRotation = true;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void ApplySprite()
        {
            SpriteRenderer renderer = DamageFeedback.GetOrCreateVisualRenderer(gameObject);

            renderer.sprite = data.Sprite;
            renderer.color = data.VisualColor;
            renderer.sortingOrder = 2;

            if (!TryGetComponent(out damageFeedback))
                damageFeedback = gameObject.AddComponent<DamageFeedback>();

            damageFeedback.Initialize(renderer, data.VisualColor);
        }

        private void IgnoreOtherEnemies()
        {
            for (int i = ActiveEnemyColliders.Count - 1; i >= 0; i--)
            {
                Collider2D otherCollider = ActiveEnemyColliders[i];
                if (otherCollider == null)
                {
                    ActiveEnemyColliders.RemoveAt(i);
                    continue;
                }

                Physics2D.IgnoreCollision(CollisionCollider, otherCollider, true);
            }

            ActiveEnemyColliders.Add(CollisionCollider);
        }

        private void GeneratePersonalCellOffset()
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.right;

            float radius = GridManager.CellSize * personalOffsetRatio;
            personalCellOffset = direction * UnityEngine.Random.Range(radius * 0.8f, radius);
        }

        private Vector2 CalculateSeparationVelocity()
        {
            float radius = GridManager.CellSize * separationRadiusInCells;
            float radiusSquared = radius * radius;
            Vector2 separation = Vector2.zero;

            foreach (Enemy other in ActiveEnemies)
            {
                if (other == null || other == this || other.isDead || other.Body == null)
                    continue;

                Vector2 away = Body.position - other.Body.position;
                float distanceSquared = away.sqrMagnitude;
                if (distanceSquared <= 0.0001f || distanceSquared >= radiusSquared)
                    continue;

                float distance = Mathf.Sqrt(distanceSquared);
                separation += away / distance * (1f - distance / radius);
            }

            return Vector2.ClampMagnitude(separation, 1f)
                   * (Data.MoveSpeed * separationStrength);
        }

        private Vector2 GetCellWorldPosition(Vector2Int cell)
        {
            return (Vector2)GridManager.GridToWorld(cell) + personalCellOffset;
        }

        protected virtual void OnDestroy()
        {
            ActiveEnemies.Remove(this);
            ActiveEnemyColliders.Remove(CollisionCollider);
            if (GridManager != null)
                GridManager.GridChanged -= OnGridChanged;
        }

        protected virtual void RebuildPlan() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEnemyColliders()
        {
            ActiveEnemies.Clear();
            ActiveEnemyColliders.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (GridManager == null)
                return;

            // 현재 위치 (초록)
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.12f);

            // 목적지 (빨강)
            Gizmos.color = Color.red;

            Vector3 destination = GetCellWorldPosition(movementDestination);
            Gizmos.DrawSphere(destination, 0.15f);

            // 현재 -> 목적지
            Gizmos.DrawLine(transform.position, destination);
        }
#endif

    }
}
