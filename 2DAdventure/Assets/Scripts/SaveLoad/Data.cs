using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data
{
    public string sceneToSave;

    public Dictionary<string, SerializeVector3> characterPosDict = new Dictionary<string, SerializeVector3>();
    public Dictionary<string, float> floatValueDict = new Dictionary<string, float>();
    public Dictionary<string, bool> boolValueDict = new Dictionary<string, bool>();

    public void SaveGameScene(GameSceneSO savescene)
    {
        sceneToSave = JsonUtility.ToJson(savescene);
    }

    public GameSceneSO GetSavedScene()
    {
        var newScene = ScriptableObject.CreateInstance<GameSceneSO>();  //严谨的创建被注册的有周期的SO文件
        JsonUtility.FromJsonOverwrite(sceneToSave, newScene);
        return newScene;
    }
}

//可序列化的Vector3
public class SerializeVector3
{
    public float x, y, z;

    public SerializeVector3(Vector3 pos)
    {
        x = pos.x;
        y = pos.y;
        z = pos.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
