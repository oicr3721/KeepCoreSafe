using DG.Tweening;
using UnityEngine;
using TMPro;

namespace KeepCoreSafe.UI
{
    public class CountTextUI : MonoBehaviour
    {
        protected ObservableValue source;

        [SerializeField]
        protected TMP_Text tmp;

        [Header("Count Animation")]
        [SerializeField] private bool animateCount = true;
        [SerializeField, Min(0.01f)] private float minimumCountDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float maximumCountDuration = 0.45f;
        [SerializeField, Min(1f)] private float countStepsPerSecond = 45f;
        [SerializeField, Range(1f, 1.3f)] private float valueChangedPunchScale = 1.08f;
        [SerializeField, Min(0.01f)] private float valueChangedPunchDuration = 0.12f;

        [Header("Insufficient Value Feedback")]
        [SerializeField] private Color insufficientColor = new(1f, 0.18f, 0.12f, 1f);
        [SerializeField, Range(1f, 1.4f)] private float insufficientScale = 1.2f;
        [SerializeField, Min(0.01f)] private float insufficientFlashDuration = 0.08f;
        [SerializeField, Min(1)] private int insufficientFlashCount = 2;
        [SerializeField] private Vector3 insufficientShakeStrength = new(8f, 0f, 0f);
        [SerializeField, Min(0.01f)] private float insufficientShakeDuration = 0.18f;

        private RectTransform rectTransform;
        private Color normalColor = Color.white;
        private Vector3 normalScale = Vector3.one;
        private Vector2 normalAnchoredPosition;
        private int displayedValue;
        private Tween countTween;
        private Tween punchTween;
        private Sequence insufficientSequence;

        protected void Awake()
        {
            if (tmp == null)
                tmp = GetComponent<TMP_Text>();

            rectTransform = tmp != null
                ? tmp.rectTransform
                : transform as RectTransform;
            if (tmp != null)
                normalColor = tmp.color;
            if (rectTransform != null)
            {
                normalScale = rectTransform.localScale;
                normalAnchoredPosition = rectTransform.anchoredPosition;
            }
        }

        protected void Start()
        {
            Initialize(source);
        }

        protected void OnDestroy()
        {
            KillTweens();
            if (source == null) return;
            source.OnValueChanged -= Refresh;
        }

        protected virtual void Refresh(float current, float max)
        {
            AnimateToValue(current);
        }

        public virtual void Initialize(ObservableValue source)
        {
            if (source == null) return;

            if (this.source != null)
                this.source.OnValueChanged -= Refresh;

            this.source = source;
            displayedValue = ToDisplayValue(source.CurrentValue);
            SetText(displayedValue);

            this.source.OnValueChanged += Refresh;
        }

        public void PlayInsufficientFeedback()
        {
            if (tmp == null || rectTransform == null)
                return;

            insufficientSequence?.Kill(false);
            punchTween?.Kill(false);
            rectTransform.DOKill(false);
            tmp.DOKill(false);
            tmp.color = normalColor;
            rectTransform.localScale = normalScale;
            rectTransform.anchoredPosition = normalAnchoredPosition;

            insufficientSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (int i = 0; i < insufficientFlashCount; i++)
            {
                insufficientSequence
                    .Append(tmp.DOColor(insufficientColor, insufficientFlashDuration))
                    .Append(tmp.DOColor(normalColor, insufficientFlashDuration));
            }

            insufficientSequence.Join(rectTransform
                .DOScale(normalScale * insufficientScale, insufficientFlashDuration)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo));
            insufficientSequence.Join(rectTransform
                .DOShakeAnchorPos(
                    insufficientShakeDuration,
                    insufficientShakeStrength,
                    12,
                    70f,
                    false,
                    true));
            insufficientSequence.OnComplete(RestoreVisualState);
        }

        private void AnimateToValue(float current)
        {
            int targetValue = ToDisplayValue(current);
            countTween?.Kill(false);
            punchTween?.Kill(false);

            if (!animateCount || displayedValue == targetValue)
            {
                displayedValue = targetValue;
                SetText(displayedValue);
                PlayValueChangedPunch();
                return;
            }

            int startValue = displayedValue;
            int distance = Mathf.Abs(targetValue - startValue);
            float duration = Mathf.Clamp(
                distance / Mathf.Max(1f, countStepsPerSecond),
                minimumCountDuration,
                maximumCountDuration);

            countTween = DOTween.To(
                    () => displayedValue,
                    value =>
                    {
                        displayedValue = value;
                        SetText(displayedValue);
                    },
                    targetValue,
                    duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    displayedValue = targetValue;
                    SetText(displayedValue);
                    PlayValueChangedPunch();
                });
        }

        private void PlayValueChangedPunch()
        {
            if (rectTransform == null || valueChangedPunchScale <= 1f)
                return;

            punchTween?.Kill(false);
            rectTransform.localScale = normalScale;
            punchTween = rectTransform
                .DOPunchScale(
                    normalScale * (valueChangedPunchScale - 1f),
                    valueChangedPunchDuration,
                    8,
                    0.7f)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() => rectTransform.localScale = normalScale);
        }

        private void SetText(int value)
        {
            if (tmp != null)
                tmp.text = value.ToString();
        }

        private static int ToDisplayValue(float value)
        {
            return Mathf.RoundToInt(value);
        }

        private void RestoreVisualState()
        {
            if (tmp != null)
                tmp.color = normalColor;
            if (rectTransform != null)
            {
                rectTransform.localScale = normalScale;
                rectTransform.anchoredPosition = normalAnchoredPosition;
            }
        }

        private void KillTweens()
        {
            countTween?.Kill(false);
            punchTween?.Kill(false);
            insufficientSequence?.Kill(false);
            if (tmp != null)
                tmp.DOKill(false);
            if (rectTransform != null)
                rectTransform.DOKill(false);
        }
    }
}
