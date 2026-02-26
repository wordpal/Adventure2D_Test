using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Event/FadeEventSO")]
public class FadeEventSO : ScriptableObject
{
    public UnityAction<Color, float> OnEventRaised;
    //private bool fadeIn;

    //直接封装成函数

    /// <summary>
    /// 画布变黑
    /// </summary>
    /// <param name="duration"></param>
    public void FadeIn(float duration)
    {
        RaiseEvent(Color.black, duration);
    }

    /// <summary>
    /// 画布变透明
    /// </summary>
    /// <param name="duration"></param>
    public void FadeOut(float duration)
    {
        RaiseEvent(Color.clear, duration);
    }

    public void RaiseEvent(Color color, float duration)
    {
        OnEventRaised(color, duration);
    }

}
