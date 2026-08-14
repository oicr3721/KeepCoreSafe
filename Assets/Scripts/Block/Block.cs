using System;
using DG.Tweening;
using KeepCoreSafe.Audio;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Presentation;
using KeepCoreSafe.UI;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    public abstract class Block : MonoBehaviour
    {
        [SerializeField]
        private BlockData data;

        [Header("Prefab References")]
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private BoxCollider2D blockCollider;
        [SerializeField] private DamageFeedback damageFeedback;
        [SerializeField] private BlockHealthBar healthBarPrefab;

        [Header("Placement Animation")]
        [SerializeField, Min(1f)] private float placementOvershoot = 1.08f;
        [SerializeField, Min(0f)] private float placementGrowDuration = 0.16f;
        [SerializeField, Min(0f)] private float placementSettleDuration = 0.08f;

        [Header("Dismantle Animation")]
        [SerializeField, Min(0f)] private float dismantleShakeDuration = 0.12f;
        [SerializeField, Min(0f)] private float dismantleShakeAngle = 12f;
        [SerializeField, Min(1)] private int dismantleShakeVibrato = 8;
        [SerializeField, Range(0f, 180f)] private float dismantleShakeRandomness = 20f;
        [SerializeField, Min(0f)] private float dismantleScaleDuration = 0.16f;
        [SerializeField, Min(0f)] private float dismantleFadeDuration = 0.14f;

        private bool isDead;
        private bool isBeingDismantled;
        private BlockHealthBar healthBar;

        public BlockData Data => data;

        public ObservableValue HP = new();

        public BlockProperty BlockProperty => data != null ? data.Properties : BlockProperty.None;
        public Vector2Int GridPosition { get; private set; }
        public bool HasGridPosition { get; private set; }
        public SpriteRenderer VisualRenderer => visualRenderer;

        //public event Action<int, int> HealthChanged;
        public event Action<Block> Died;

        protected virtual void Awake()
        {
            if (blockCollider == null)
                blockCollider = GetComponent<BoxCollider2D>();
        }

        protected virtual void Start()
        {
            if (data == null)
            {
                Debug.LogError($"{name} has no BlockData.", this);
            }
        }

        public void Initialize(BlockData blockData)
        {
            HP.OnValueChanged -= HandleHealthChanged;
            data = blockData;
            HP.Initialize(data.MaxHP, data.MaxHP);
            ApplyBaseVisual();
            CreateHealthBar();
            HP.OnValueChanged += HandleHealthChanged;
            UpdateHealthVisual(1f);
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
            if (amount <= 0 || HP.CurrentValue <= 0)
            {
                return;
            }

            HP.SubtractValue(amount);
            damageFeedback?.Play();

            if (HP.CurrentValue == 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || HP.CurrentValue <= 0)
            {
                return;
            }

            HP.AddValue(amount);
        }

        public virtual void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            AudioManager.PlayAt(data?.DestroyedSound, transform.position);
            if (data != null)
                BlockDestroyEffectManager.Instance?.PlayAt(transform.position, data.DestroyEffectColor);
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
                .Append(transform.DOScale(finalScale * placementOvershoot, placementGrowDuration).SetEase(Ease.OutBack))
                .Append(transform.DOScale(finalScale, placementSettleDuration).SetEase(Ease.OutQuad));
        }

        public void PlayRareAppearance()
        {
            if (visualRenderer == null || data == null)
                return;

            visualRenderer.DOKill(false);
            Color originalColor = data.VisualColor;
            visualRenderer.color = originalColor;
            DOTween.Sequence()
                .SetTarget(visualRenderer)
                .Append(visualRenderer.DOColor(new Color(1f, 0.9f, 0.25f, 1f), 0.12f))
                .Append(visualRenderer.DOColor(Color.white, 0.1f))
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => visualRenderer.color = originalColor);
        }

        public void PlayDismantleAnimation(Action onComplete)
        {
            if (isBeingDismantled)
                return;

            isBeingDismantled = true;
            transform.DOKill();
            if (blockCollider != null)
                blockCollider.enabled = false;

            Sequence sequence = DOTween.Sequence().SetTarget(transform);
            sequence.Append(transform.DOShakeRotation(
                dismantleShakeDuration,
                new Vector3(0f, 0f, dismantleShakeAngle),
                dismantleShakeVibrato,
                dismantleShakeRandomness,
                true,
                ShakeRandomnessMode.Harmonic));
            sequence.Append(transform.DOScale(Vector3.zero, dismantleScaleDuration).SetEase(Ease.InBack));
            if (visualRenderer != null)
                sequence.Join(visualRenderer.DOFade(0f, dismantleFadeDuration));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        public void SetPresentationHealthBarVisible(bool visible)
        {
            if (healthBar != null)
                healthBar.gameObject.SetActive(visible);
        }

        public void UpdateHealthVisual(float healthRatio)
        {
            float clampedRatio = Mathf.Clamp01(healthRatio);
            if (visualRenderer != null && data != null)
                visualRenderer.sprite = data.GetHealthSprite(clampedRatio);

            healthBar?.UpdateHealthVisual(clampedRatio);
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
            if (!HasGridPosition || GridManager.Instance == null)
            {
                return baseCooldown;
            }

            foreach (Block adjacentBlock in GridManager.Instance.GetBlocks())
            {
                if (adjacentBlock is SupportBlock supportBlock
                    && supportBlock.Data is SupportBlockData supportData
                    && supportData.AffectsOffset(GridPosition - supportBlock.GridPosition))
                {
                    return baseCooldown * supportData.CooldownMultiplier;
                }
            }

            return baseCooldown;
        }

        private void CreateHealthBar()
        {
            if (healthBar == null && healthBarPrefab != null)
            {
                healthBar = Instantiate(healthBarPrefab, GameDefaultUI.BlockHPBarRoot);
                healthBar.Initialize(this);
            }
            else if (healthBarPrefab == null)
            {
                Debug.LogError($"{name} has no BlockHealthBar prefab assigned.", this);
            }
        }

        private void HandleHealthChanged(float currentHealth, float maximumHealth)
        {
            float healthRatio = maximumHealth > 0f
                ? currentHealth / maximumHealth
                : 0f;
            UpdateHealthVisual(healthRatio);
        }

        private void ApplyBaseVisual()
        {
            if (visualRenderer == null)
            {
                Debug.LogError($"{name} has no visual renderer assigned.", this);
                return;
            }

            visualRenderer.color = data.VisualColor;
            damageFeedback?.Initialize(visualRenderer, data.VisualColor);
        }

        protected virtual void OnDestroy()
        {
            HP.OnValueChanged -= HandleHealthChanged;
            transform.DOKill();
            if(healthBar != null)
                Destroy(healthBar.gameObject);
        }

    }
}
