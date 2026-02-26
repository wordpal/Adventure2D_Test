using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Event/SceneLoadEventSO")]
public class SceneLoadEventSO : ScriptableObject
{
    public UnityAction<GameSceneSO, Vector3, bool> LoadRequestEvent;

    /// <summary>
    /// 场景加载请求
    /// </summary>
    /// <param name="sceneToGo">要去的场景</param>
    /// <param name="posToGo">要去的位置</param>
    /// <param name="fadeScreen">是否渐入渐出</param>
    public void RasieLoadRequestEvent(GameSceneSO sceneToGo, Vector3 posToGo, bool fadeScreen)  
    {
        LoadRequestEvent?.Invoke(sceneToGo, posToGo, fadeScreen);
    }

}
