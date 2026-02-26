using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Sign : MonoBehaviour
{
    private Animator anim;
    private bool canPress;
    private PlayerInputControl playerInput;
    private IInterecatable targetItem;

    public GameObject signSprite;
    public Transform PlayerTrans;


    private void Awake()
    {
        //anim = GetComponentInChildren 
        anim = signSprite.GetComponent<Animator>();
        playerInput = new PlayerInputControl();
        playerInput.Enable();
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += OnActionChange;
        playerInput.Gameplay.Confirm.started += OnConfirm;
    }

    private void OnDisable()
    {
        canPress = false;   //切换场景人物关闭的时候自动关闭交互
    }

    private void Update()
    {
        signSprite.GetComponent<Renderer>().enabled = canPress;
        signSprite.transform.localScale = PlayerTrans.localScale;
    }

    private void OnConfirm(InputAction.CallbackContext context)
    {
        if (canPress)
        {
            targetItem.TriggerAction();
            GetComponent<AudioDefination>()?.PlayAudioClip();
        }
    }

    /// <summary>
    /// 换交互设备的同时更换动画
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="actionChange"></param>

    private void OnActionChange(object obj, InputActionChange actionChange)
    {
        if (actionChange == InputActionChange.ActionStarted)
        {
            var d = ((InputAction)obj).activeControl.device;
            switch (d)
            {
                case Keyboard:
                    anim.Play("keyboard");
                    break;

                case Gamepad:  //ps4设备
                    anim.Play("ps4");
                    break;
            }
        }
    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Interecatable"))
        {
            canPress = true;
            targetItem = collision.GetComponent<IInterecatable>();
        }   
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Interecatable"))
        {
            canPress = false;
        }
    }
}
//交互控制文件
