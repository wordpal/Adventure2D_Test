using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class Character : MonoBehaviour, ISaveable
{
    [Header("事件监听")]
    public VoidEventSO newGameEvent;
    public FloatEventSO healthRecoverEvent;

    [Header("基本属性")]
    public float maxHealth;
    public float currentHealth;
    public float maxPower;
    public float currentPower;
    public float powerRecoverSpeed;

    [Header("受伤无敌")]
    public float invulnerableDuration;
    public float invulnerableCounter;
    public bool invulnerable;

    public UnityEvent<Character> OnHealthChange; 
    public UnityEvent<Transform> OnTakeDamage;
    public UnityEvent OnDie;

    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentPower = maxPower;
    }

    public void OnEnable()
    {
        newGameEvent.OnEventEaised += NewGame;
        healthRecoverEvent.OnEventEaised += OnhealthRecoverEvent;
        ISaveable saveable = this;  //登记自己
        saveable.RegisterSaveData();
    }

    private void OnDisable()
    {
        newGameEvent.OnEventEaised -= NewGame;
        healthRecoverEvent.OnEventEaised -= OnhealthRecoverEvent;
        ISaveable saveable = this;  //除去自己
        saveable.UnRegisterSaveData();
    }



    private void NewGame()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentPower = maxPower;
        OnHealthChange?.Invoke(this);//初始血量显示为满
    }

    private void OnhealthRecoverEvent(float healthRecoverValue)
    {
        //不是Player就不加血
        if (LayerMask.LayerToName(gameObject.layer) != "Player")
        {
            return;
        }
//        Debug.Log("Recover!");
        currentHealth = math.min(currentHealth + healthRecoverValue, maxHealth);
        OnHealthChange?.Invoke(this);
    }

    private void Update()
    {
        if (invulnerable) 
        {
            invulnerableCounter -= Time.deltaTime;
            if (invulnerableCounter <= 0)
            {
                invulnerable = false;
            }
        }

        if (currentPower < maxPower)
        {
            currentPower += powerRecoverSpeed * Time.deltaTime;
        }
        else
        {
            currentPower = maxPower;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Water") && currentHealth > 0)
        {
            //下水直接死亡，更新血量，播放音效
            currentHealth = 0;
            OnHealthChange?.Invoke(this);
            OnDie?.Invoke();
        }
    }

    public void TakeDamage(Attack attacker)
    {
        if (invulnerable)
        {
            return;
        }
        if (currentHealth - attacker.damage > 0)
        {
            currentHealth -= attacker.damage;
            TriggleInvulnerable();
            //执行受伤
            OnTakeDamage?.Invoke(attacker.transform);
        }
        else if (!isDead)
        {
            //执行死亡
            currentHealth = 0;
            OnDie?.Invoke();
            isDead = true;
        }

        OnHealthChange?.Invoke(this);
    }

    /// <summary>
    /// 触发受伤无敌
    /// </summary>



    public void TriggleInvulnerable() 
    {
        if (!invulnerable)
        {
            invulnerable = true;
            invulnerableCounter = invulnerableDuration;
        }
    }

    public void OnSlide(int cost)
    {
        currentPower -= cost;
        OnHealthChange?.Invoke(this);
    }

    #region Save&load相关函数
    public DataDefination GetDataID()
    {
        return GetComponent<DataDefination>();
    }

    public void GetSaveData(Data data)
    {
        if (data.characterPosDict.ContainsKey(GetDataID().ID))
        {
            data.characterPosDict[GetDataID().ID] = new SerializeVector3(transform.position);
            data.floatValueDict[GetDataID().ID + "health"] = currentHealth;
            data.floatValueDict[GetDataID().ID + "power"] = currentPower;
        }
        else
        {
            data.characterPosDict.Add(GetDataID().ID, new SerializeVector3(transform.position));
            data.floatValueDict.Add(GetDataID().ID + "health", currentHealth);
            data.floatValueDict.Add(GetDataID().ID + "power", currentPower);
        }
    }

    public void LoadData(Data data)
    {
        if (data.characterPosDict.ContainsKey(GetDataID().ID))
        {
            transform.position = data.characterPosDict[GetDataID().ID].ToVector3();
            currentHealth = data.floatValueDict[GetDataID().ID + "health"];
            currentPower = data.floatValueDict[GetDataID().ID + "power"];
        }

        //通知UI更新
        OnHealthChange?.Invoke(this);   
    }
    #endregion
}
