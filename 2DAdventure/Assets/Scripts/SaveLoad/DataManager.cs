using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using System.IO;


[DefaultExecutionOrder(-100)]   //在游戏开始前先创造saveableList
public class DataManager : MonoBehaviour
{
    [Header("是否处于开发者模式")]
    public bool inDeveloperModule;

    public static DataManager instance;
    [Header("事件监听")]
    public VoidEventSO saveDataEvent;
    public VoidEventSO loadDataEvent;

    [Header("组件")]
    public GameObject NoSave;

    private List<ISaveable> saveableList = new List<ISaveable>();
    private Data saveData;

    private string jsonFolder;  //保存路径


    private void Awake()
    {
        //保证全局单例
        if (instance == null)
            instance = this;        
        else
            Destroy(this.gameObject);

        saveData = new Data();  //data不是monoBhv，必须new才能使用

        jsonFolder = Application.persistentDataPath + "/SAVE DATA/";

        ReadSavedData();    //游戏开始时读取有没有save
    }

    private void OnEnable()
    {
        saveDataEvent.OnEventEaised += Save;
        loadDataEvent.OnEventEaised += Load;
    }

    private void OnDisable()
    {
        saveDataEvent.OnEventEaised -= Save;
        loadDataEvent.OnEventEaised -= Load;
    }

    private void Update()
    {
        if (inDeveloperModule && Keyboard.current.lKey.wasPressedThisFrame)
        {
            Load();
        }
    }

    public void RegisterSaveData(ISaveable saveable)
    {
        if (!saveableList.Contains(saveable))
        {
            saveableList.Add(saveable);
        }
    }

    public void UnRegisterSaveData(ISaveable saveable)
    {
        if (saveableList.Contains(saveable))
        {
            saveableList.Remove(saveable);
        }
    }

    public void Save()
    {
        foreach (var saveable in saveableList)
        {
            saveable.GetSaveData(saveData);     //对每一个saveable进行save
        }

        //存储到内存中
        var resultPath = jsonFolder + "data.sav";

        var jsonData = JsonConvert.SerializeObject(saveData);

        if (!File.Exists(resultPath))
        {
            Directory.CreateDirectory(jsonFolder);
        }

        File.WriteAllText(resultPath, jsonData);

        //foreach (var item in saveData.characterPosDict)
        //{
        //    Debug.Log(item.Key + " is in " + item.Value);
        //}
    }

    public void Load()
    {
        var resultPath = jsonFolder + "data.sav";
        if (!File.Exists(resultPath))
        {
            StartCoroutine(LoadButNoAnySave());
            return;
        }

        ReadSavedData(); // 关键：先把文件读回 saveData

        foreach (var saveable in saveableList)
        {
            saveable.LoadData(saveData);     //对每一个saveable进行load
//            Debug.Log(saveable);
        }
    }

    private IEnumerator LoadButNoAnySave()
    {
        NoSave.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        NoSave.gameObject.SetActive(false);
    }

    private void ReadSavedData()
    {
        var resultPath = jsonFolder + "data.sav";

        if (File.Exists(resultPath))
        {
            var stringData = File.ReadAllText(resultPath);
            var jsonData = JsonConvert.DeserializeObject<Data>(stringData);

            saveData = jsonData;
        }
    }

}
