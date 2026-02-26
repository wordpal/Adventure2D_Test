using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("事件监听")]
    public PlayerStatBar playerStatBar;
    public CharacterEventSO healthEvent;
    public SceneLoadEventSO sceneLoadEvent;
    public VoidEventSO unLoadEventSO;
    public VoidEventSO loadDataEvent;
    public VoidEventSO GameOverEvent;
    public VoidEventSO returnToMenuEvent;
    public FloatEventSO syncVolumeEvent;

    [Header("广播")]
    public VoidEventSO pauseEvent;

    [Header("组件")]
    public GameObject gameOverPanel;
    public GameObject restartBtn;
    public GameObject mobileTouch;
    public GameObject pausePanel;
    public Button settingsBtn;
    public Slider VolumeSlider;


    private void Awake()
    {
        //编译时判断平台
#if UNITY_STANDALONE
    mobileTouch.gameObject.SetActive(false);
#endif

        settingsBtn.onClick.AddListener(TogglePausePanel);  //监听Settings
    }
    private void OnEnable()
    {
        healthEvent.OnEventRaised += OnHealthEvent;
        sceneLoadEvent.LoadRequestEvent += OnSceneLoadEvent;
        loadDataEvent.OnEventEaised += OnLoadDataEvent;
        GameOverEvent.OnEventEaised += OnGameOverEvent;
        returnToMenuEvent.OnEventEaised += OnLoadDataEvent;
        syncVolumeEvent.OnEventEaised += OnSyncVolumeEvent;
    }

    private void OnDisable()
    {
        healthEvent.OnEventRaised -= OnHealthEvent;
        sceneLoadEvent.LoadRequestEvent -= OnSceneLoadEvent;
        unLoadEventSO.OnEventEaised -= OnUnLoadEvent;
        loadDataEvent.OnEventEaised -= OnLoadDataEvent;
        GameOverEvent.OnEventEaised -= OnGameOverEvent;
        returnToMenuEvent.OnEventEaised -= OnLoadDataEvent;
        syncVolumeEvent.OnEventEaised -= OnSyncVolumeEvent;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePausePanel();
        }
    }
    public void TogglePausePanel()  //同时给pausePanel的ReturnToMenuButton用
    {
        if (pausePanel.activeInHierarchy)
        {
            pausePanel.gameObject.SetActive(false);
            Time.timeScale = 1; //时钟正常运行
        }
        else
        {
            pauseEvent.RaiseEvent();    //在游戏停止前执行
            pausePanel.gameObject.SetActive(true);
            Time.timeScale = 0; //时钟停止运行
        }
    }

    private void OnHealthEvent(Character character)
    {
        var percentage = character.currentHealth / character.maxHealth;
        playerStatBar.OnhealthChange(percentage);
        playerStatBar.OnPowerChange(character);
    }

    private void OnSceneLoadEvent(GameSceneSO scene, Vector3 arg1, bool arg2)
    {
        //只有location场景才加载playerStatBar
        var isLocation = scene.sceneType == SceneType.Location;
        playerStatBar.gameObject.SetActive(isLocation);
    }

    private void OnUnLoadEvent()
    {
        //卸载场景时PlayerStatBar也卸载
        playerStatBar.gameObject.SetActive(false);
    }

    private void OnGameOverEvent()
    {
        gameOverPanel.gameObject.SetActive(true);   //死亡后弹出面板
        EventSystem.current.SetSelectedGameObject(restartBtn);  //Restart为首选项
    }

    private void OnLoadDataEvent()
    {
        gameOverPanel.gameObject.SetActive(false);  //加载场景时保证面板关闭
    }

    private void OnSyncVolumeEvent(float amount)
    {
        VolumeSlider.value = VolumeMapper.DbToSlider(amount);
    }
}
