using DG.Tweening;
using UnityEngine;

public class FadeUI : AnimatedUI
{
    [SerializeField] CanvasGroup canvasGroup;

    [SerializeField] private float showDuration = 0.3f;
    [SerializeField] private float hideDuration = 0.2f;

    protected override Tween CreateShowTween()
    {
        return canvasGroup.DOFade(1, showDuration);
    }

    protected override Tween CreateHideTween()
    {
        return canvasGroup.DOFade(0, hideDuration);
    }

    protected override void BeforeShow()
    {
        gameObject.SetActive(false);
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = true;
    }

    protected override void AfterShow()
    {
        canvasGroup.alpha = 1;
    }

    protected override void BeforeHide()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = false;
    }

    protected override void AfterHide()
    {
        canvasGroup.alpha = 0;
    }
}
