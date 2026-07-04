using System;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        private static readonly int MoveXParameter = Animator.StringToHash("MoveX");
        private static readonly int MoveYParameter = Animator.StringToHash("MoveY");
        private static readonly List<Enemy> ActiveEnemies = new();
        private static readonly List<Collider2D> ActiveEnemyColliders = new();

        [SerializeField]
        private EnemyData data;

        [Header("Prefab References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D collisionCollider;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private DamageFeedback damageFeedback;
        [SerializeField] private Animator animator;

        [Header("Visual Movement")]
        [SerializeField, Range(0f, 0.4f)]
        private float minimumPersonalOffsetRatio = 0.2f;

        [SerializeField, Range(0f, 0.4f)]
        private float maximumPersonalOffsetRatio = 0.25f;

        [SerializeField, Range(0.1f, 1f)]
        private float separationRadiusInCells = 0.55f;

        [SerializeField, Range(0f, 0.5f)]
        private float separationStrength = 0.2f;

        [SerializeField, Min(0.001f)]
        private float minimumSnapDistance = 0.02f;

        private bool isDead;
        private bool isMovingToCell;
        private Vector2Int movementDestination;
        private Vector2 personalCellOffset;
        private Vector2Int prevMove;

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
            if (animator == null && visualRenderer != null)
                animator = visualRenderer.GetComponent<Animator>();

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
            float snapDistance = Mathf.Max(minimumSnapDistance, Data.MoveSpeed * Time.fixedDeltaTime);
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
            UpdateMovementPresentation(CalculateAnimationMove(destination));
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

        protected void StopMoving(bool resetPresentation = true)
        {
            Body.linearVelocity = Vector2.zero;
            if (resetPresentation)
                UpdateMovementPresentation(Vector2Int.zero);
        }

        protected void FaceAttackTarget(Block target)
        {
            if (target == null)
                return;

            Vector2 direction = TryGetCurrentCell(out Vector2Int currentCell)
                ? target.GridPosition - currentCell
                : target.transform.position - transform.position;
            UpdateMovementPresentation(ToCardinalAnimationMove(direction));
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            Body.linearVelocity = Vector2.zero;
            UpdateMovementPresentation(Vector2Int.zero);
            if (!EnemyRewardUI.TryPlayReward(transform.position, 1f))
                GameManager.PlacePoint.AddValue(1f);
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void EnsurePhysicsComponents()
        {
            if (collisionCollider == null)
                collisionCollider = GetComponent<Collider2D>();
            if (body == null)
                body = GetComponent<Rigidbody2D>();

            Body = body;
            CollisionCollider = collisionCollider;
            if (Body == null || CollisionCollider == null)
            {
                Debug.LogError($"{name} prefab is missing Rigidbody2D or Collider2D.", this);
                enabled = false;
                return;
            }

            IgnoreOtherEnemies();
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = 0f;
            Body.freezeRotation = true;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void ApplySprite()
        {
            if (visualRenderer == null)
            {
                Debug.LogError($"{name} prefab has no visual renderer assigned.", this);
                return;
            }

            visualRenderer.sprite = data.Sprite;
            damageFeedback?.Initialize(visualRenderer, Color.white);
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

            float minimumRadius = GridManager.CellSize * minimumPersonalOffsetRatio;
            float maximumRadius = GridManager.CellSize * maximumPersonalOffsetRatio;
            personalCellOffset = direction * UnityEngine.Random.Range(
                Mathf.Min(minimumRadius, maximumRadius),
                Mathf.Max(minimumRadius, maximumRadius));
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

        private Vector2Int CalculateAnimationMove(Vector2Int destination)
        {
            Vector2 direction;
            if (TryGetCurrentCell(out Vector2Int currentCell))
                direction = destination - currentCell;
            else
                direction = GetCellWorldPosition(destination) - Body.position;

            return ToCardinalAnimationMove(direction);
        }

        private static Vector2Int ToCardinalAnimationMove(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                && !Mathf.Approximately(direction.x, 0f))
            {
                return new Vector2Int(direction.x > 0f ? 1 : -1, 0);
            }

            if (!Mathf.Approximately(direction.y, 0f))
                return new Vector2Int(0, direction.y > 0f ? 1 : -1);

            return Vector2Int.zero;
        }

        private void UpdateMovementPresentation(Vector2Int move)
        {
            if (move == prevMove)
                return;

            prevMove = move;
            if (animator != null)
            {
                animator.SetInteger(MoveXParameter, move.x);
                animator.SetInteger(MoveYParameter, move.y);
            }

            if (visualRenderer != null)
                visualRenderer.flipX = move.x < 0;
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
