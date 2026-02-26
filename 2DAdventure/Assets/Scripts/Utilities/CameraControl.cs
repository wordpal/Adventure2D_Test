using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("事件监听")]
    public VoidEventSO afterSceneLoadEvent; //注册事件

    public CinemachineImpulseSource impulseSource;
    public VoidEventSO caremaShakeEvent;
    
    private CinemachineConfiner2D confiner2D;


    private void Awake()
    {
        confiner2D = GetComponent<CinemachineConfiner2D>();
    }

    private void OnEnable()
    {
        caremaShakeEvent.OnEventEaised += OnCameraShakeEvent;
        afterSceneLoadEvent.OnEventEaised += OnAfterSceneLoadEvent;
    }

    private void OnDisable()
    {
        caremaShakeEvent.OnEventEaised -= OnCameraShakeEvent;
        afterSceneLoadEvent.OnEventEaised -= OnAfterSceneLoadEvent;
    }

    private void OnAfterSceneLoadEvent()
    {
        GetNewCameraBounds();   //获取新边界
    }

    private void OnCameraShakeEvent()
    {
        impulseSource.GenerateImpulse();
    }

    //private void Start()
    //{
    //    GetNewCameraBounds();
    //}

    private void GetNewCameraBounds()
    {
        var obj = GameObject.FindGameObjectWithTag("Bounds");
        if (obj == null)
        {
            Debug.Log("no find bound");
            return;
        }
        confiner2D.m_BoundingShape2D = obj.GetComponent<Collider2D>();

        confiner2D.InvalidateCache();
    }

}
