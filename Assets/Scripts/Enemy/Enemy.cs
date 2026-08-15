using System;
using System.Collections;
using System.Collections.Generic;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        private static readonly int MoveXParameter = Animator.StringToHash("MoveX");
        private static readonly int MoveYParameter = Animator.StringToHash("MoveY");
        [SerializeField]
        private EnemyData data;

        [Header("Prefab References")]
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private DamageFeedback damageFeedback;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform hitPoint;

        [Header("Visual Movement")]
        [SerializeField, Range(0f, 0.4f)]
        private float minimumPersonalOffsetRatio = 0.2f;

        [SerializeField, Range(0f, 0.4f)]
        private float maximumPersonalOffsetRatio = 0.25f;

        [SerializeField, Min(0.001f)]
        private float minimumSnapDistance = 0.02f;

        private bool isDead;
        private bool isShockwaveEliminationPlaying;
        private bool isMovingToCell;
        private Vector2Int movementDestination;
        private Vector2 personalCellOffset;
        private Vector2Int prevMove;
        protected GridManager GridManager { get; private set; }

        public EnemyData Data => data;
        public int MaxHP => data != null ? data.MaxHP : 1;
        public int CurrentHP { get; private set; }
        public bool IsDead => isDead;
        public Vector2 PersonalCellOffset => personalCellOffset;
        public Transform HitPoint => hitPoint;
        protected IReadOnlyList<Vector2Int> InitialPathCells { get; private set; } =
            Array.Empty<Vector2Int>();
        protected Block InitialRouteTarget { get; private set; }

        public event Action<Enemy> Died;

        protected virtual void Awake()
        {
            if (animator == null && visualRenderer != null)
                animator = visualRenderer.GetComponent<Animator>();

        }

        protected virtual void Start()
        {
            GridManager = GridManager.Instance;
            if (data == null) Debug.LogError($"{name} has no EnemyData.", this);
            if (GridManager != null)
                GeneratePersonalCellOffset();
        }

        public void Initialize(
            EnemyData enemyData,
            IReadOnlyList<Vector2Int> initialPathCells = null,
            Block initialRouteTarget = null)
        {
            data = enemyData;
            InitialPathCells = initialPathCells ?? Array.Empty<Vector2Int>();
            InitialRouteTarget = initialRouteTarget;
            CurrentHP = data.MaxHP;
            ApplySprite();
        }

        protected virtual void Update()
        {
            if (!isShockwaveEliminationPlaying && GameManager.Phase == GamePhase.Combat)
            {
                OnCombatUpdate(Time.deltaTime);
            }
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || isDead || isShockwaveEliminationPlaying)
            {
                return;
            }

            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            damageFeedback?.Play();
            if (CurrentHP == 0)
                Die();
            else
                OnDamaged(amount);
        }

        protected virtual void OnCombatUpdate(float deltaTime) { }
        protected virtual void OnDamaged(int amount) { }

        protected bool ContinueCellMovement(float deltaTime)
        {
            if (!isMovingToCell || GridManager == null)
                return false;

            Vector2 destination = GetCellWorldPosition(movementDestination);
            Vector2 currentPosition = transform.position;
            float destinationDistance = Vector2.Distance(currentPosition, destination);
            float snapDistance = Mathf.Max(minimumSnapDistance, Data.MoveSpeed * deltaTime);
            if (destinationDistance <= snapDistance)
            {
                transform.position = destination;
                isMovingToCell = false;
            }
            else
            {
                transform.position = Vector2.MoveTowards(
                    currentPosition,
                    destination,
                    Data.MoveSpeed * deltaTime);
            }

            return true;
        }

        protected bool TryBeginCellMovement(Vector2Int destination)
        {
            if (isMovingToCell || GridManager == null)
                return false;

            if (!GridManager.Grid.IsWithinBounds(destination)
                || !GridManager.IsCellEmpty(destination))
                return false;

            if (TryGetCurrentCell(out Vector2Int current)
                && current == destination
                && Vector2.Distance(transform.position, GetCellWorldPosition(destination)) < 0.05f)
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

            position = GridManager.WorldToGrid(transform.position - (Vector3)personalCellOffset);
            return GridManager.Grid.IsWithinBounds(position);
        }

        protected void StopMoving(bool resetPresentation = true)
        {
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

        protected void Die(bool awardEnergy = true)
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            UpdateMovementPresentation(Vector2Int.zero);
            if (awardEnergy && !isShockwaveEliminationPlaying)
                GameManager.Instance?.AwardEnemyEnergy(transform.position, data.EnergyOnDeath);
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        public void EliminateByShockwave(float silhouetteDuration)
        {
            if (isDead || isShockwaveEliminationPlaying)
                return;

            isShockwaveEliminationPlaying = true;
            StopMoving();
            damageFeedback?.Cancel();
            if (visualRenderer != null)
                visualRenderer.color = Color.black;

            StartCoroutine(CompleteShockwaveElimination(
                Mathf.Max(0f, silhouetteDuration)));
        }

        private IEnumerator CompleteShockwaveElimination(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            Die();
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
                direction = GetCellWorldPosition(destination) - (Vector2)transform.position;

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
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (GridManager == null)
                return;

            // 현재 위치 (초록)
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.12f);

            if (!isMovingToCell)
                return;

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
