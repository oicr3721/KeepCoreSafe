using DG.Tweening;
using UnityEngine;

public abstract class AnimatedUI : MonoBehaviour
{
    protected Tween currentTween;

    public bool IsPlaying =>
        currentTween != null && currentTween.IsActive() && currentTween.IsPlaying();


    public Tween Show()
    {
        currentTween?.Kill();

        BeforeShow();
        gameObject.SetActive(true);

        currentTween = CreateShowTween()
            .OnComplete(() =>
            {
                AfterShow();
            });

        return currentTween;
    }

    public Tween Hide()
    {
        currentTween?.Kill();

        BeforeHide();

        currentTween = CreateHideTween()
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                AfterHide();
            });

        return currentTween;
    }

    /// <summary>
    /// Show 시작 직전에 호출
    /// (초기 상태 세팅)
    /// </summary>
    protected virtual void BeforeShow() { }

    /// <summary>
    /// Hide 완료 후 호출
    /// </summary>
    protected virtual void AfterShow() { }

    /// <summary>
    /// Hide 시작 직전에 호출
    /// </summary>
    protected virtual void BeforeHide() { }

    /// <summary>
    /// Hide 완료 후 호출
    /// </summary>
    protected virtual void AfterHide() { }

    protected abstract Tween CreateShowTween();
    protected abstract Tween CreateHideTween();
}