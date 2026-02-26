using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour, ISaveable
{
    private GameSceneSO sceneToGo;
    private Vector3 positionToGo;
    private bool fadeScreen;
    private bool isLoading;

    private Rigidbody2D playerRb;

    public Transform playerTrans;
    public Vector3 firstPosition;
    public Vector3 menuPosition;

    [Header("事件监听")]
    public SceneLoadEventSO loadEventSO;
    public VoidEventSO newGameEvent;
    public VoidEventSO returnToMenuEvent;

    [Header("广播")]
    public VoidEventSO afterSceneLoadedEvent;
    public FadeEventSO fadeEvent;
    public VoidEventSO unLoadEventSO;

    [Header("场景")]
    public GameSceneSO firstLoadScene;
    public GameSceneSO menuScene;

    public float fadeDuration;
    public GameSceneSO currentLoadedScene;


    private void Awake()
    {
        if (playerTrans != null)
            playerRb = playerTrans.GetComponent<Rigidbody2D>();
    }

    //TODO:MainMemu
    private void Start()
    {
        //NewGame();  //游戏开始时加载第一个场景
        loadEventSO.RasieLoadRequestEvent(menuScene, menuPosition, true);
    }

    private void OnEnable()
    {
        loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
        newGameEvent.OnEventEaised += NewGame;
        returnToMenuEvent.OnEventEaised += OnReturnToMenuEvent;

        ISaveable saveable = this;  //登记自己
        saveable.RegisterSaveData();
    }


    private void OnDisable()
    {
        loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
        newGameEvent.OnEventEaised -= NewGame;
        returnToMenuEvent.OnEventEaised -= OnReturnToMenuEvent;

        ISaveable saveable = this;  //取消登记
        saveable.UnRegisterSaveData();
    }

    private void NewGame()
    {
        sceneToGo = firstLoadScene;
        playerTrans.position = firstPosition;
        //OnLoadRequestEvent(sceneToGo, firstPosition, true);
        loadEventSO.RasieLoadRequestEvent(sceneToGo, firstPosition, true);
    }

    private void OnReturnToMenuEvent()
    {
        sceneToGo = menuScene;
        positionToGo = menuPosition;
        OnLoadRequestEvent(sceneToGo, positionToGo, true);
    }


    /// <summary>
    /// 场景事件加载请求
    /// </summary>
    /// <param name="locationToGo"></param>
    /// <param name="posToGo"></param>
    /// <param name="fadeScreen"></param>

    private void OnLoadRequestEvent(GameSceneSO locationToGo, Vector3 posToGo, bool fadeScreen)
    {
        if (isLoading)
            return;
        isLoading = true;
        
        sceneToGo = locationToGo;
        positionToGo = posToGo;
        this.fadeScreen = fadeScreen;

        if (currentLoadedScene != null) 
        {
            StartCoroutine(UnloadPreviousScene());  //先卸载旧场景
        }
        else
        {
            LoadNewScene(); //直接加载新场景
        }

        //菜单界面人物不动
        if (playerRb == null && playerTrans != null)
            playerRb = playerTrans.GetComponent<Rigidbody2D>();

        if (playerRb != null && sceneToGo != null && sceneToGo.sceneType == SceneType.Location)
        {
            playerRb.simulated = true;
        }
        else
        {
            if (playerRb != null)
                playerRb.simulated = false;
        }

    }

    private IEnumerator UnloadPreviousScene()
    {
        if (fadeScreen)
        {
            //实现渐入渐出
            fadeEvent.FadeIn(fadeDuration);
        }
        unLoadEventSO.RaiseEvent();
        yield return new WaitForSeconds(fadeDuration);
        yield return currentLoadedScene.sceneReference.UnLoadScene();   //等候场景卸载
        playerTrans.gameObject.SetActive(false);

        LoadNewScene();
    }

    private void LoadNewScene()
    {
        var loadingObject = sceneToGo.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
        loadingObject.Completed += OnLoadCompleted;     //加载场景在大游戏中可能比较久，故分步
    }

    private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> obj)
    {
        currentLoadedScene = sceneToGo;
        playerTrans.position = positionToGo;
        playerTrans.gameObject.SetActive(true);

        if (playerRb == null && playerTrans != null)
            playerRb = playerTrans.GetComponent<Rigidbody2D>();

        if (playerRb != null && currentLoadedScene != null)
            playerRb.simulated = currentLoadedScene.sceneType == SceneType.Location;

        if (fadeScreen)
        {
            //实现渐入渐出
            fadeEvent.FadeOut(fadeDuration);
        }

        isLoading = false;

        if (currentLoadedScene.sceneType == SceneType.Location)
        {
            //menu界面不需要执行
            afterSceneLoadedEvent.RaiseEvent(); //加载完场景之后执行
        }
    }
    #region Save&Load相关
    public DataDefination GetDataID()
    {
        return GetComponent<DataDefination>();
    }

    public void GetSaveData(Data data)
    {
        data.SaveGameScene(currentLoadedScene);
    }

    public void LoadData(Data data)
    {
        var playerID = playerTrans.GetComponent<DataDefination>().ID;   //用player确定是否进行了save 
        if (data.characterPosDict.ContainsKey(playerID))
        {
            positionToGo = data.characterPosDict[playerID].ToVector3(); //character也会改变位置
            sceneToGo = data.GetSavedScene();

            OnLoadRequestEvent(sceneToGo, positionToGo, true);
        }

    }

    #endregion
}
