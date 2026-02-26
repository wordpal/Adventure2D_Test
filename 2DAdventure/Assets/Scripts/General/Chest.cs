using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

//(ISaveable功能待开发)
public class Chest : MonoBehaviour, IInterecatable, ISaveable
{
    [Header("广播")]
    public FloatEventSO healthRecoverEvent;

    [Header("变量参数")]
    public float healthRecoverValue;

    public Sprite closeSprite;
    public Sprite openSprite;

    private bool isDone;
    private SpriteRenderer spriteRenderer;


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();    
    }

    private void OnEnable()
    {
        spriteRenderer.sprite = isDone ? openSprite : closeSprite;

        ISaveable saveable = this;  //登记自己
        saveable.RegisterSaveData();
    }

    private void OnDisable()
    {
        ISaveable saveable = this;  //取消登记
        saveable.UnRegisterSaveData();
    }

    public void TriggerAction()
    {
        //Debug.Log("Opening Chest");
        if (!isDone)
        {
            OpenChest();
            healthRecoverEvent.RaiseEvent(healthRecoverValue);
            //TODO:奖励回血

        }
    }

    public void OpenChest()
    {
        spriteRenderer.sprite = openSprite;
        isDone = true;
        //this.gameObject.tag = "Untagged";
        this.GetComponent<BoxCollider2D>().enabled = false;
    }

    #region Save&load相关
    public DataDefination GetDataID()
    {
        return GetComponent<DataDefination>();
    }

    public void GetSaveData(Data data)
    {
        //if (data.boolValueDict.ContainsKey(GetDataID().ID))
        //{
        //    data.boolValueDict[GetDataID().ID] = isDone;
        //}
        //else
        //{
        //    data.boolValueDict.Add(GetDataID().ID, isDone);
        //}
    }

    public void LoadData(Data data)
    {
        //if(data.boolValueDict.ContainsKey(GetDataID().ID))
        //{
        //    //已经打开过
        //    if (data.boolValueDict[GetDataID().ID])
        //    {
        //        OpenChest();
        //    }
        //}
    }
    #endregion
}
