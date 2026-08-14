using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class DelayedFillGauge : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private Slider delayedSlider;
        [SerializeField] private Slider currentSlider;
        [SerializeField] private Image delayedFill;

        [Header("Change Feedback")]
        [SerializeField] private Color increaseColor = new(1f, 0.88f, 0.2f, 1f);
        [SerializeField] private Color decreaseColor = new(1f, 0.25f, 0.18f, 1f);
        [SerializeField, Min(0f)] private float delay = 0.18f;
        [SerializeField, Min(0.01f)] private float followDuration = 0.3f;

        private Tween followTween;
        private float displayedValue;
        private bool initialized;

        public void SetRange(float minimum, float maximum)
        {
            maximum = Mathf.Max(minimum + 0.0001f, maximum);
            ConfigureSlider(delayedSlider, minimum, maximum);
            ConfigureSlider(currentSlider, minimum, maximum);
        }

        public void SetValue(float value, bool immediate = false)
        {
            if (currentSlider == null || delayedSlider == null)
                return;

            value = Mathf.Clamp(value, currentSlider.minValue, currentSlider.maxValue);
            followTween?.Kill(false);
            if (!initialized || immediate)
            {
                initialized = true;
                displayedValue = value;
                currentSlider.value = value;
                delayedSlider.value = value;
                return;
            }

            bool increasing = value >= displayedValue;
            if (delayedFill != null)
                delayedFill.color = increasing ? increaseColor : decreaseColor;

            Slider following;
            if (increasing)
            {
                delayedSlider.value = value;
                following = currentSlider;
            }
            else
            {
                currentSlider.value = value;
                following = delayedSlider;
            }

            displayedValue = value;
            followTween = DOVirtual.DelayedCall(delay, () =>
                {
                    followTween = DOTween.To(
                            () => following.value,
                            next => following.value = next,
                            value,
                            followDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true)
                        .SetTarget(this);
                }, true)
                .SetTarget(this);
        }

        private static void ConfigureSlider(Slider slider, float minimum, float maximum)
        {
            if (slider == null)
                return;
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.wholeNumbers = false;
        }

        private void OnDisable()
        {
            followTween?.Kill(false);
            followTween = null;
        }
    }
}
