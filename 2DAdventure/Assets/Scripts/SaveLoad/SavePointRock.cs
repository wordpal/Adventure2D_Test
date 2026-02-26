using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SavePointRock : MonoBehaviour,IInterecatable
{
    [Header("广播")]
    public VoidEventSO SaveGameEvent;

    [Header("变量参数")]
    public SpriteRenderer spriteRenderer;
    public Sprite darkSprite;
    public Sprite lightSprite;
    public Light2D light2D;

    private bool isDone;

    private void Awake()
    {
        //spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        spriteRenderer.sprite = isDone ? lightSprite : darkSprite;
        light2D.gameObject.SetActive(isDone);
    }

    public void TriggerAction()
    {
        //Debug.Log("Opening Chest");
        if (!isDone)
        {
            saveGame();
        }
    }

    private void saveGame()
    {
        isDone = true;
        spriteRenderer.sprite = lightSprite;
        light2D.gameObject.SetActive(isDone);
        SaveGameEvent.RaiseEvent();

        this.GetComponent<BoxCollider2D>().enabled = false;
    }
}
