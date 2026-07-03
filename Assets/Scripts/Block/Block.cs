using System;
using DG.Tweening;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public abstract class Block : MonoBehaviour
    {
        [SerializeField]
        private BlockData data;

        private GameManager gameManager;
        private GridManager gridManager;
        private bool isDead;
        private DamageFeedback damageFeedback;
        private bool isBeingDismantled;

        public BlockData Data => data;
        public int MaxHP => data != null ? data.MaxHP : 1;
        public int CurrentHP { get; private set; }
        public int Cost => data != null ? data.Cost : 0;
        public BlockProperty BlockProperty => data != null ? data.Properties : BlockProperty.None;
        public Vector2Int GridPosition { get; private set; }
        public bool HasGridPosition { get; private set; }

        public event Action<int, int> HealthChanged;
        public event Action<Block> Died;

        protected virtual void Awake()
        {
            EnsureCollider();
            EnsureHealthBar();
        }

        protected virtual void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            gridManager = FindFirstObjectByType<GridManager>();

            if (data == null)
            {
                Debug.LogError($"{name} has no BlockData.", this);
            }
        }

        public void Initialize(BlockData blockData)
        {
            data = blockData;
            CurrentHP = data.MaxHP;
            ApplySprite();
            NotifyHealthChanged();
        }

        protected virtual void Update()
        {
            if (GameManager.Phase == GamePhase.Combat)
            {
                OnCombatUpdate(Time.deltaTime);
            }
        }

        protected virtual void OnCombatUpdate(float deltaTime)
        {
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHP <= 0)
            {
                return;
            }

            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            damageFeedback?.Play();
            NotifyHealthChanged();

            if (CurrentHP == 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHP <= 0)
            {
                return;
            }

            int healedHP = Mathf.Min(MaxHP, CurrentHP + amount);
            if (healedHP == CurrentHP)
            {
                return;
            }

            CurrentHP = healedHP;
            NotifyHealthChanged();
        }

        public virtual void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        public void PlayPlacementAnimation()
        {
            transform.DOKill();
            Vector3 finalScale = transform.localScale;
            transform.localScale = Vector3.zero;

            DOTween.Sequence()
                .SetTarget(transform)
                .Append(transform.DOScale(finalScale * 1.08f, 0.16f).SetEase(Ease.OutBack))
                .Append(transform.DOScale(finalScale, 0.08f).SetEase(Ease.OutQuad));
        }

        public void PlayDismantleAnimation(Action onComplete)
        {
            if (isBeingDismantled)
                return;

            isBeingDismantled = true;
            transform.DOKill();
            if (TryGetComponent(out Collider2D blockCollider))
                blockCollider.enabled = false;

            SpriteRenderer visual = GetComponentInChildren<SpriteRenderer>();
            Sequence sequence = DOTween.Sequence().SetTarget(transform);
            sequence.Append(transform.DOShakeRotation(
                0.12f,
                new Vector3(0f, 0f, 12f),
                8,
                20f,
                true,
                ShakeRandomnessMode.Harmonic));
            sequence.Append(transform.DOScale(Vector3.zero, 0.16f).SetEase(Ease.InBack));
            if (visual != null)
                sequence.Join(visual.DOFade(0f, 0.14f));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        internal void SetGridPosition(Vector2Int position)
        {
            GridPosition = position;
            HasGridPosition = true;
        }

        internal void ClearGridPosition()
        {
            HasGridPosition = false;
        }

        protected float GetAdjustedCooldown(float baseCooldown)
        {
            if (!HasGridPosition || gridManager == null)
            {
                return baseCooldown;
            }

            foreach (Block adjacentBlock in gridManager.GetBlocks())
            {
                if (adjacentBlock is SupportBlock supportBlock
                    && supportBlock.Data != null
                    && supportBlock.Data.AffectsOffset(GridPosition - supportBlock.GridPosition))
                {
                    return baseCooldown * supportBlock.Data.CooldownMultiplier;
                }
            }

            return baseCooldown;
        }

        private void EnsureHealthBar()
        {
            if (!TryGetComponent(out BlockHealthBar _))
            {
                gameObject.AddComponent<BlockHealthBar>();
            }
        }

        private void EnsureCollider()
        {
            if (!TryGetComponent(out BoxCollider2D _))
            {
                gameObject.AddComponent<BoxCollider2D>();
            }
        }

        private void NotifyHealthChanged()
        {
            HealthChanged?.Invoke(CurrentHP, MaxHP);
        }

        private void ApplySprite()
        {
            SpriteRenderer renderer = DamageFeedback.GetOrCreateVisualRenderer(gameObject);

            renderer.sprite = data.Sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 1;

            if (!TryGetComponent(out damageFeedback))
                damageFeedback = gameObject.AddComponent<DamageFeedback>();

            damageFeedback.Initialize(renderer, Color.white);
        }

        protected virtual void OnDestroy()
        {
            transform.DOKill();
        }

    }
}
