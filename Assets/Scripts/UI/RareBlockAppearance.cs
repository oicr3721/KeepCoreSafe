using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    public sealed class RareBlockAppearance : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Image shine;
        [SerializeField, Min(0f)] private float duration = 0.45f;
        [SerializeField, Min(0f)] private float punchScale = 0.18f;
        [SerializeField, Min(0f)] private float rotation = 180f;

        public void Play()
        {
            if (target == null)
                target = transform as RectTransform;

            target.DOKill(true);
            target.localScale = Vector3.one;
            target.DOPunchScale(Vector3.one * punchScale, duration, 7, 0.55f)
                .SetUpdate(true);

            if (shine == null)
                return;

            shine.DOKill(false);
            shine.rectTransform.DOKill(false);
            Color color = shine.color;
            color.a = 0f;
            shine.color = color;
            shine.rectTransform.localRotation = Quaternion.identity;

            DOTween.Sequence()
                .SetTarget(shine)
                .SetUpdate(true)
                .Append(shine.DOFade(0.9f, duration * 0.25f))
                .Join(shine.rectTransform.DORotate(new Vector3(0f, 0f, rotation), duration))
                .Append(shine.DOFade(0f, duration * 0.45f));
        }

        private void OnDisable()
        {
            target?.DOKill(false);
            shine?.DOKill(false);
            shine?.rectTransform.DOKill(false);
        }
    }
}
