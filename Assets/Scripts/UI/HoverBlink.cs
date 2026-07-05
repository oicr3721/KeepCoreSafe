using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HoverBlink : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Blink")]
    [SerializeField] private float hoverAlpha = 0.35f;
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease ease = Ease.Linear;

    [SerializeField] private CanvasGroup canvasGroup;
    private Tween blinkTween;

    private void Awake()
    {
        if(canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        blinkTween?.Kill();

        blinkTween = canvasGroup
            .DOFade(hoverAlpha, duration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        blinkTween?.Kill();

        canvasGroup
            .DOFade(1f, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        blinkTween?.Kill();
        canvasGroup.alpha = 1f;
    }
}