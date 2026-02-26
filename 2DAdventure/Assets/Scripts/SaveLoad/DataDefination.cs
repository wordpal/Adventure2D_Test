using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//GUID数据定义文件
public class DataDefination : MonoBehaviour
{
    public PersistentType persistentType;
    public string ID;

    private void OnValidate()
    {
        if (persistentType == PersistentType.ReadWrite)
        {
            if (ID == null)
                ID = System.Guid.NewGuid().ToString();
        }
        else
        {
            ID = string.Empty;
        }  
    }
}
