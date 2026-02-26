using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

[RequireComponent(typeof(Rigidbody2D),typeof(Animator),typeof(PhysicsCheck))]
public class Enemy : MonoBehaviour
{

    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;
    [HideInInspector] public PhysicsCheck physicsCheck;

    [Header("基本属性")]
    public float normalSpeed;
    public float chaseSpeed;
    [HideInInspector] public float currentSpeed;
    public Vector3 faceDir;
    public float hurtForce;
    public Transform attacker;
    public Vector3 spwanPoint;

    [Header("检测玩家")]
    public Vector2 centerOffset;
    public Vector2 checksize;
    public float checkDistance;
    public LayerMask attackLayer;

    [Header("计时器")]
    public float waitTime;
    public float waitTimeCounter;
    public bool wait;
    public float lostTime;
    public float lostTimeCounter;

    [Header("状态")]
    public bool isHurt;
    public bool isDead;


    protected BaseState patrolState;
    protected BaseState chaseState;
    protected BaseState currentState;
    protected BaseState skillState;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        physicsCheck = GetComponent<PhysicsCheck>();

        currentSpeed = normalSpeed;
        //waitTimeCounter = waitTime;
        spwanPoint = transform.position;
    }

    //当这个物体被激活得时候执行
    private void OnEnable()
    {
        currentState = patrolState;
        currentState.OnEnter(this);
    }

    private void Update()
    {
        //朝向与scale相反
        faceDir = new Vector3(-transform.localScale.x, 0, 0);

        currentState.LogicUpdate();
        TimeCounter();
    }

    private void FixedUpdate()
    {
        if (!isHurt & !isDead & !wait)
            Move();
        currentState.PhysicsUpdate();
    }

    private void OnDisable()
    {
        currentState.OnExit();
    }

    public virtual void Move()
    {
        if(!anim.GetCurrentAnimatorStateInfo(0).IsName("SnailPreMove")&&!anim.GetCurrentAnimatorStateInfo(0).IsName("SnailRecover"))//只有Snail有
            rb.velocity = new Vector2(currentSpeed * faceDir.x * Time.deltaTime, rb.velocity.y);
    }

    /// <summary>
    /// 计数器
    /// </summary>
    private void TimeCounter()
    {
        if (wait)
        {
            waitTimeCounter -= Time.deltaTime;
            if (waitTimeCounter < 0)
            {
                wait = false;
                waitTimeCounter = waitTime;
                transform.localScale = new Vector3(faceDir.x, 1, 1);
            }
        }

        if (!FindPlayer() && lostTimeCounter > 0)
        {
            lostTimeCounter -= Time.deltaTime;
        }
        //else
        //{
        //    lostTimeCounter = lostTime;
        //}
    }

    public virtual bool FindPlayer()
    {
        var obj = Physics2D.BoxCast(transform.position + (Vector3)centerOffset, checksize, 0, faceDir, checkDistance, attackLayer);
        if (obj)
            lostTimeCounter = lostTime; //从最后一次丢失目标的时候开始计时
        return obj;
    }

    public virtual Vector3 GetNowPoint()    //Bee覆写用
    {
        return transform.position;
    }

    #region 事件执行方法

    public void OnTakeDamage(Transform attackTrans)
    {
        attacker = attackTrans;
        if (attackTrans.position.x - transform.position.x > 0)  //攻击者在右边
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        if (attackTrans.position.x - transform.position.x < 0)  //攻击者在左边
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        isHurt = true;
        anim.SetTrigger("hurt");
        //受伤被击退
        rb.velocity = new Vector2(0, rb.velocity.y);
        Vector2 dir = new Vector2(transform.position.x - attackTrans.position.x, 0).normalized;
        StartCoroutine(OnHurt(dir));
    }

    private IEnumerator OnHurt(Vector2 dir) //协同程序返回值;迭代器
    {
        rb.AddForce(dir * hurtForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.45f);
        isHurt = false;     //大概等动画播放完修改isHurt
    }

    public void OnDie()
    {
        gameObject.layer = 2;
        anim.SetBool("dead", true);
        isDead = true;
    }

    public void DestroyAfterAnimation()
    {
        Destroy(this.gameObject);
    }

    public void SwitchState(NPCState state)
    {
        var newState = state switch
        {
            NPCState.Patrol => patrolState,
            NPCState.Chase => chaseState,
            NPCState.Skill => skillState,
            _ => null
        };
        currentState.OnExit();
        currentState = newState;
        currentState.OnEnter(this);
    }

    #endregion

    public virtual void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position + (Vector3)centerOffset + new Vector3(checkDistance * -transform.localScale.x, 0, 0), 0.2f);
    }
}
