using System;
using System.Collections.Generic;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Enemies
{
    public abstract class Enemy : MonoBehaviour
    {
        private static readonly List<Collider2D> ActiveEnemyColliders = new();

        [SerializeField]
        private EnemyData data;

        private GameManager gameManager;
        private bool isDead;

        protected Rigidbody2D Body { get; private set; }
        protected Collider2D CollisionCollider { get; private set; }

        public EnemyData Data => data;
        public int MaxHP => data != null ? data.MaxHP : 1;
        public int CurrentHP { get; private set; }

        public event Action<Enemy> Died;

        protected virtual void Awake()
        {
            EnsurePhysicsComponents();
        }

        protected virtual void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (data == null) Debug.LogError($"{name} has no EnemyData.", this);
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
            if (CurrentHP == 0)
            {
                Die();
            }
        }

        protected abstract void OnCombatUpdate(float deltaTime);

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
            if (!TryGetComponent(out SpriteRenderer renderer))
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = data.Sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 2;
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

        private void OnDestroy()
        {
            ActiveEnemyColliders.Remove(CollisionCollider);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEnemyColliders()
        {
            ActiveEnemyColliders.Clear();
        }
    }
}
