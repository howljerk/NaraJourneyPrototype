using UnityEngine;
using DG.Tweening;

public static class VerticalScrollerUtils
{
    public const float kStopScrollSpeed = 0f;
    public const float kFallingScrollSpeed = 5f;

    private static Tween s_CurrentScrollerTween;
    private static float s_CurrentScrollerDestSpeed = 0f;

    public static void StartScrolling(VerticalScroller scroller, float scrollSpeed)
    {
        if (Mathf.Approximately(s_CurrentScrollerDestSpeed, scrollSpeed))
            return;

        if (s_CurrentScrollerTween != null)
            s_CurrentScrollerTween.Kill();

        float speedDiff = Mathf.Abs(scrollSpeed - scroller.GetScrollSpeed());
        //Get to a speed of 10 units/sec in 1 second
        float timeToGetToScroll = speedDiff / 10f;

        s_CurrentScrollerDestSpeed = scrollSpeed;
        s_CurrentScrollerTween = DOTween.To(scroller.GetScrollSpeed,
                                            scroller.SetScrollSpeed,
                                            s_CurrentScrollerDestSpeed,
                                            timeToGetToScroll);
        s_CurrentScrollerTween.onComplete = () => s_CurrentScrollerTween = null;
    }
}
