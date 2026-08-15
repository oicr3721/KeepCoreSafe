using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KeepCoreSafe.UI
{
    public sealed class ShopOfferCardMotion : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private RectTransform sourceRect;
        [SerializeField] private RectTransform positionRoot;
        [SerializeField] private RectTransform tiltRoot;
        [SerializeField] private RectTransform flipRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Flip")]
        [SerializeField, Min(0.05f)] private float defaultFlipDuration = 0.32f;

        [Header("Floating")]
        [SerializeField] private float floatAmplitude = 4f;
        [SerializeField, Min(0.1f)] private float floatCycleDuration = 1.8f;
        [SerializeField, Range(0f, 8f)] private float idleTiltAmount = 2.4f;
        [SerializeField, Range(0f, 5f)] private float idleTwistAmount = 1.1f;
        [SerializeField, Min(0.05f)] private float idleTiltSpeed = 1.35f;
        [SerializeField, Range(0f, 1f)] private float hoverIdleTiltMultiplier = 0.2f;
        [SerializeField, Min(0f)] private float idleRandomTimeOffset = 1.75f;
        [SerializeField, Min(0f)] private float idlePhaseMultiplier = 5.5f;

        [Header("Hover")]
        [SerializeField, Range(1f, 1.16f)] private float hoverScale = 1.07f;
        [SerializeField, Range(0f, 15f)] private float maxTiltX = 7f;
        [SerializeField, Range(0f, 15f)] private float maxTiltY = 7f;
        [SerializeField, Range(0f, 8f)] private float maxTiltZ = 1.6f;
        [SerializeField, Min(0.01f)] private float hoverScaleDuration = 0.15f;
        [SerializeField] private Ease hoverScaleEase = Ease.OutBack;
        [SerializeField, Min(1f)] private float tiltFollowSpeed = 22f;
        [SerializeField, Range(0f, 12f)] private float hoverPunchAngle = 5f;
        [SerializeField, Min(0.01f)] private float hoverPunchDuration = 0.15f;
        [SerializeField, Range(1, 30)] private int hoverPunchVibrato = 18;
        [SerializeField, Range(0f, 1f)] private float hoverPunchElasticity = 0.8f;

        [Header("Click Popup")]
        [SerializeField, Range(0.01f, 0.18f)] private float clickPopupScale = 0.055f;
        [SerializeField, Min(0.01f)] private float clickPopupDuration = 0.16f;
        [SerializeField, Range(1, 30)] private int clickPopupVibrato = 12;
        [SerializeField, Range(0f, 1f)] private float clickPopupElasticity = 0.75f;

        private Tween floatTween;
        private Tween scaleTween;
        private Tween selectionTween;
        private Tween hoverPunchTween;
        private Tween clickPopupTween;
        private bool isPointerOver;
        private bool inputEnabled;
        private bool isRevealed;
        private bool isSelected;
        private bool floatingAllowed;
        private bool hasMotionSeed;
        private float baseAnchoredY;
        private float motionTimeOffset;
        private Vector3 targetManualTilt;
        private Vector3 smoothedTilt;
        private bool basePositionCaptured;

        private void Update()
        {
            if (!isRevealed || !floatingAllowed || tiltRoot == null)
                return;

            ApplyCardMotion();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        public void ShowBackImmediate()
        {
            KillTweens();
            isPointerOver = false;
            isSelected = false;
            isRevealed = false;
            targetManualTilt = Vector3.zero;
            smoothedTilt = Vector3.zero;
            StopFloating(true);
            ResetVisualTransform();
        }

        public void ShowFrontImmediate(bool selected)
        {
            KillTweens();
            isPointerOver = false;
            isSelected = selected;
            isRevealed = true;
            targetManualTilt = Vector3.zero;
            smoothedTilt = Vector3.zero;
            ResetVisualTransform();
        }

        public Tween FlipToFront(Action showFront, Action onComplete, float duration = -1f)
        {
            duration = ResolveDuration(duration);
            PrepareForBlockingAnimation();
            SetFlipScaleX(1f);

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(flipRoot.DOScaleX(0f, duration * 0.5f).SetEase(Ease.InQuad))
                .AppendCallback(() => showFront?.Invoke())
                .Append(flipRoot.DOScaleX(1f, duration * 0.5f).SetEase(Ease.OutBack, 1.25f));
            sequence.OnComplete(() =>
            {
                isRevealed = true;
                onComplete?.Invoke();
            });
            return sequence;
        }

        public Tween FlipToBack(Action swapToBack, Action onComplete, float duration = -1f)
        {
            duration = ResolveDuration(duration);
            PrepareForBlockingAnimation();
            SetFlipScaleX(1f);

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Append(flipRoot.DOScaleX(0f, duration * 0.5f).SetEase(Ease.InQuad))
                .AppendCallback(() => swapToBack?.Invoke())
                .Append(flipRoot.DOScaleX(1f, duration * 0.5f).SetEase(Ease.OutQuad));
            sequence.OnComplete(() =>
            {
                isRevealed = false;
                onComplete?.Invoke();
            });
            return sequence;
        }

        public void StartFloating(float phaseOffset)
        {
            if (!isRevealed || positionRoot == null)
                return;

            floatingAllowed = true;
            if (!hasMotionSeed)
            {
                motionTimeOffset = phaseOffset * idlePhaseMultiplier
                                   + UnityEngine.Random.Range(0f, idleRandomTimeOffset);
                hasMotionSeed = true;
            }

            floatTween?.Kill(false);
            positionRoot.anchoredPosition = new Vector2(
                positionRoot.anchoredPosition.x,
                baseAnchoredY);
            floatTween = positionRoot
                .DOAnchorPosY(baseAnchoredY + floatAmplitude, floatCycleDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(phaseOffset)
                .SetUpdate(true)
                .SetTarget(this);
        }

        public void StopFloating(bool resetPosition)
        {
            floatingAllowed = false;
            floatTween?.Kill(false);
            floatTween = null;
            if (resetPosition && positionRoot != null)
            {
                positionRoot.anchoredPosition = new Vector2(
                    positionRoot.anchoredPosition.x,
                    baseAnchoredY);
            }
        }

        public void PrepareForReroll()
        {
            SetInputEnabled(false);
            KillTweens();
            StopFloating(true);
            isPointerOver = false;
            targetManualTilt = Vector3.zero;
            smoothedTilt = Vector3.zero;
            ResetVisualTransform();
        }

        public void PlayClickPopup()
        {
            if (tiltRoot == null)
                return;

            clickPopupTween?.Kill(false);
            clickPopupTween = tiltRoot
                .DOPunchScale(
                    Vector3.one * clickPopupScale,
                    clickPopupDuration,
                    clickPopupVibrato,
                    clickPopupElasticity)
                .SetUpdate(true)
                .SetTarget(this);
        }

        public void PrepareSupplyReveal(float verticalOffset)
        {
            KillTweens();
            CaptureBasePosition();
            ResetVisualTransform();
            positionRoot.anchoredPosition = new Vector2(positionRoot.anchoredPosition.x, baseAnchoredY - verticalOffset);
            tiltRoot.localScale = Vector3.one * 0.88f;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        public Tween PlaySupplyReveal(float duration)
        {
            Sequence result = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Join(positionRoot.DOAnchorPosY(baseAnchoredY, duration).SetEase(Ease.OutCubic))
                .Join(tiltRoot.DOScale(Vector3.one, duration).SetEase(Ease.OutBack));
            if (canvasGroup != null)
                result.Join(canvasGroup.DOFade(1f, duration * 0.65f));
            result.Append(tiltRoot.DOPunchScale(Vector3.one * 0.045f, 0.14f, 6, 0.5f));
            return result;
        }

        public Tween PlaySupplySelected(float duration)
        {
            PrepareForBlockingAnimation();
            return DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Join(flipRoot.DOLocalRotate(new Vector3(0f, 360f, 0f), duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.InOutCubic))
                .Join(tiltRoot.DOScale(1.16f, duration * 0.45f).SetEase(Ease.OutBack))
                .Append(tiltRoot.DOScale(1f, duration * 0.35f).SetEase(Ease.OutCubic));
        }

        public Tween PlaySupplyUnselected(float duration, float backwardOffset)
        {
            PrepareForBlockingAnimation();
            Sequence result = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Join(tiltRoot.DOScale(0.84f, duration).SetEase(Ease.OutCubic))
                .Join(positionRoot.DOAnchorPosY(baseAnchoredY - backwardOffset, duration).SetEase(Ease.OutCubic));
            if (canvasGroup != null)
                result.Join(canvasGroup.DOFade(0.42f, duration));
            return result;
        }

        public Tween PlaySupplyExit(float duration, float distance)
        {
            StopFloating(false);
            Sequence result = DOTween.Sequence().SetUpdate(true).SetTarget(this)
                .Join(positionRoot.DOAnchorPosY(baseAnchoredY - distance, duration).SetEase(Ease.InCubic))
                .Join(tiltRoot.DOScale(0.72f, duration).SetEase(Ease.InCubic));
            if (canvasGroup != null)
                result.Join(canvasGroup.DOFade(0f, duration));
            return result;
        }

        public void PointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            scaleTween?.Kill(false);
            scaleTween = tiltRoot
                .DOScale(Vector3.one * hoverScale, hoverScaleDuration)
                .SetEase(hoverScaleEase)
                .SetUpdate(true)
                .SetTarget(this);
            PlayHoverPunch();
            UpdateTilt(eventData);
        }

        public void PointerMove(PointerEventData eventData)
        {
            UpdateTilt(eventData);
        }

        public void PointerExit()
        {
            isPointerOver = false;
            targetManualTilt = Vector3.zero;
            scaleTween?.Kill(false);
            scaleTween = tiltRoot
                .DOScale(Vector3.one, hoverScaleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this);
        }

        public void KillTweens()
        {
            floatTween?.Kill(false);
            scaleTween?.Kill(false);
            selectionTween?.Kill(false);
            hoverPunchTween?.Kill(false);
            clickPopupTween?.Kill(false);
            if (positionRoot != null)
                positionRoot.DOKill(false);
            if (tiltRoot != null)
                tiltRoot.DOKill(false);
            if (flipRoot != null)
                flipRoot.DOKill(false);
        }

        private void PrepareForBlockingAnimation()
        {
            SetInputEnabled(false);
            KillTweens();
            StopFloating(true);
            isPointerOver = false;
            targetManualTilt = Vector3.zero;
            smoothedTilt = Vector3.zero;
            ResetVisualTransform();
        }

        private void ResetVisualTransform()
        {
            if (positionRoot != null)
            {
                CaptureBasePosition();
                positionRoot.anchoredPosition = new Vector2(positionRoot.anchoredPosition.x, baseAnchoredY);
            }

            if (tiltRoot != null)
            {
                tiltRoot.localRotation = Quaternion.identity;
                tiltRoot.localScale = Vector3.one;
            }

            if (flipRoot != null)
            {
                flipRoot.localRotation = Quaternion.identity;
                SetFlipScaleX(1f);
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private void CaptureBasePosition()
        {
            if (basePositionCaptured || positionRoot == null)
                return;
            baseAnchoredY = positionRoot.anchoredPosition.y;
            basePositionCaptured = true;
        }

        private void UpdateTilt(PointerEventData eventData)
        {
            if (sourceRect == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sourceRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            Rect rect = sourceRect.rect;
            float normalizedX = Mathf.Clamp(rect.width <= 0f ? 0f : localPoint.x / (rect.width * 0.5f), -1f, 1f);
            float normalizedY = Mathf.Clamp(rect.height <= 0f ? 0f : localPoint.y / (rect.height * 0.5f), -1f, 1f);
            targetManualTilt = new Vector3(
                normalizedY * maxTiltX,
                -normalizedX * maxTiltY,
                (normalizedY - normalizedX) * 0.5f * maxTiltZ);
        }

        private void ApplyCardMotion()
        {
            float time = Time.unscaledTime * idleTiltSpeed + motionTimeOffset;
            bool hovering = isPointerOver && inputEnabled && !isSelected;
            float idleWeight = hovering ? hoverIdleTiltMultiplier : 1f;

            Vector3 idleTilt = new(
                Mathf.Sin(time) * idleTiltAmount * idleWeight,
                Mathf.Cos(time * 0.91f + 0.37f) * idleTiltAmount * idleWeight,
                Mathf.Sin(time * 0.63f + 1.7f) * idleTwistAmount * idleWeight);

            Vector3 targetTilt = targetManualTilt + idleTilt;
            float t = 1f - Mathf.Exp(-tiltFollowSpeed * Time.unscaledDeltaTime);
            smoothedTilt = new Vector3(
                Mathf.LerpAngle(smoothedTilt.x, targetTilt.x, t),
                Mathf.LerpAngle(smoothedTilt.y, targetTilt.y, t),
                Mathf.LerpAngle(smoothedTilt.z, targetTilt.z, t));
            tiltRoot.localRotation = Quaternion.Euler(smoothedTilt);
        }

        private void PlayHoverPunch()
        {
            if (flipRoot == null || hoverPunchAngle <= 0f)
                return;

            hoverPunchTween?.Kill(false);
            flipRoot.localRotation = Quaternion.identity;
            hoverPunchTween = flipRoot
                .DOPunchRotation(
                    Vector3.forward * hoverPunchAngle,
                    hoverPunchDuration,
                    hoverPunchVibrato,
                    hoverPunchElasticity)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private float ResolveDuration(float duration)
        {
            return duration > 0f ? duration : defaultFlipDuration;
        }

        private void SetFlipScaleX(float x)
        {
            if (flipRoot == null)
                return;

            Vector3 scale = flipRoot.localScale;
            scale.x = x;
            flipRoot.localScale = scale;
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sourceRect == null || positionRoot == null || tiltRoot == null || flipRoot == null)
            {
                Debug.LogWarning(
                    $"{nameof(ShopOfferCardMotion)} on {name} has missing prefab references.",
                    this);
            }
        }
#endif
    }
}
