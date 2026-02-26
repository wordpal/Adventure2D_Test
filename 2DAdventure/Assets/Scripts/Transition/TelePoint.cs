using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TelePoint : MonoBehaviour, IInterecatable
{
    public SceneLoadEventSO loadEventSO;
    public Vector3 positionToGo;
    public GameSceneSO sceneToGo;
    public void TriggerAction()
    {
        Debug.Log("传送!");
        this.GetComponent<BoxCollider2D>().enabled = false; //触发一次后无法再次触发
        loadEventSO.RasieLoadRequestEvent(sceneToGo, positionToGo, true);   //广播呼叫请求
    }
}
