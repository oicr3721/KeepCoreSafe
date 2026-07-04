using System.Collections.Generic;
using DG.Tweening;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    public sealed class EnemyRewardUI : MonoBehaviour
    {
        public static EnemyRewardUI Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform coinRoot;
        [SerializeField] private RectTransform coinPrefab;
        [SerializeField] private RectTransform placePointTarget;

        [Header("Pooling")]
        [SerializeField, Min(0)] private int initialPoolSize = 12;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float dropHeight = 28f;
        [SerializeField, Min(0f)] private float dropDuration = 0.08f;
        [SerializeField, Min(0f)] private float flyDuration = 0.42f;
        [SerializeField, Min(0f)] private float targetPunchScale = 0.14f;
        [SerializeField, Min(0f)] private float targetPunchDuration = 0.16f;

        private readonly Queue<RectTransform> pool = new();
        private Camera worldCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            if (coinRoot == null)
                coinRoot = transform as RectTransform;
            worldCamera = Camera.main;

            for (int i = 0; i < initialPoolSize; i++)
                pool.Enqueue(CreateCoin());
        }

        public static bool TryPlayReward(Vector3 worldPosition, float amount)
        {
            if (Instance == null || !Instance.isActiveAndEnabled || !Instance.CanPlay())
                return false;

            Instance.PlayReward(worldPosition, amount);
            return true;
        }

        private bool CanPlay()
        {
            return canvas != null
                   && coinRoot != null
                   && coinPrefab != null
                   && placePointTarget != null
                   && worldCamera != null;
        }

        private void PlayReward(Vector3 worldPosition, float amount)
        {
            RectTransform coin = pool.Count > 0 ? pool.Dequeue() : CreateCoin();
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            Vector2 deathScreenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                placePointTarget.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                coinRoot,
                deathScreenPosition,
                uiCamera,
                out Vector2 deathPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                coinRoot,
                targetScreenPosition,
                uiCamera,
                out Vector2 targetPosition);

            coin.DOKill(false);
            coin.gameObject.SetActive(true);
            coin.SetAsLastSibling();
            coin.anchoredPosition = deathPosition + Vector2.up * dropHeight;
            coin.localScale = Vector3.one;

            DOTween.Sequence()
                .SetTarget(coin)
                .SetUpdate(true)
                .Append(coin.DOAnchorPos(deathPosition, dropDuration).SetEase(Ease.OutBounce))
                .Append(coin.DOAnchorPos(targetPosition, flyDuration).SetEase(Ease.InCubic))
                .Join(coin.DOScale(0.55f, flyDuration).SetEase(Ease.InQuad))
                .OnComplete(() => CompleteReward(coin, amount));
        }

        private void CompleteReward(RectTransform coin, float amount)
        {
            GameManager.PlacePoint.AddValue(amount);
            if (placePointTarget != null && targetPunchScale > 0f)
            {
                placePointTarget.DOKill(true);
                placePointTarget.DOPunchScale(
                        Vector3.one * targetPunchScale,
                        targetPunchDuration,
                        5,
                        0.5f)
                    .SetUpdate(true);
            }

            Release(coin);
        }

        private RectTransform CreateCoin()
        {
            if (coinPrefab == null || coinRoot == null)
                return null;

            RectTransform coin = Instantiate(coinPrefab, coinRoot);
            coin.gameObject.SetActive(false);
            return coin;
        }

        private void Release(RectTransform coin)
        {
            if (coin == null)
                return;

            coin.DOKill(false);
            coin.gameObject.SetActive(false);
            coin.localScale = Vector3.one;
            pool.Enqueue(coin);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
