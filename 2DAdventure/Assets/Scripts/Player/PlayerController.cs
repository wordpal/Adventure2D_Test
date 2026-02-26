using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerInputControl inputControl;
    private Rigidbody2D rb;
    private PhysicsCheck physicsCheck;
    private SpriteRenderer rbSprite;
    private CapsuleCollider2D capsuleCollider;
    private PlayerAnimation playerAnimation;
    private Character character;


    public Vector2 inputDirection;

    [Header("事件监听")]
    public SceneLoadEventSO loadEvent;
    public VoidEventSO afterSceneLoadedEvent;
    public VoidEventSO loadDataEvent;
    public VoidEventSO returnToMenuEvent;


    [Header("基本属性")]
    public float speed;
    public float jumpForce;
    public float wallJumpForce;
    public float hurtForce;
    public float slideDistance;     
    public float slideSpeed;
    public int SildePowerCost; 


    private float runSpeed;
    private float walkSpeed => speed / 2.5f;
    private Vector2 originOffset;   //用作下蹲时碰撞体变化
    private Vector2 originSize;

    [Header("物理材质")]
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D wall;

    [Header("状态")]
    public bool isCrouch;
    public bool isHurt;
    public bool isDead;
    public bool isAttack;
    public bool wallJump;
    public bool isSlide;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsCheck = GetComponent<PhysicsCheck>();
        rbSprite = GetComponent<SpriteRenderer>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        playerAnimation = GetComponent<PlayerAnimation>();
        character = GetComponent<Character>(); 

        originOffset = capsuleCollider.offset;
        originSize = capsuleCollider.size;

        inputControl = new PlayerInputControl();
        inputControl.Gameplay.Jump.started += Jump;

        #region 强制走路

        runSpeed = speed;
        inputControl.Gameplay.WalkButton.performed += ctx =>
        {
            if (physicsCheck.isGround)
            {
                speed = walkSpeed;
            }
        };

        inputControl.Gameplay.WalkButton.canceled += ctx =>
        {
            if (physicsCheck.isGround)
            {
                speed = runSpeed;
            }
        };
        #endregion

        inputControl.Gameplay.Attack.started += PlayerAttack;

        inputControl.Gameplay.Slide.started += PlayerSlide;

        inputControl.Enable();
    }


    private void OnEnable()
    {
        loadEvent.LoadRequestEvent += OnLoadEvent;
        afterSceneLoadedEvent.OnEventEaised += OnAfterSceneLoadedEvent;
        loadDataEvent.OnEventEaised += OnLoadDataEvent;
        returnToMenuEvent.OnEventEaised += OnLoadDataEvent;
    }

    private void OnDisable()
    {
        inputControl.Disable();
        loadEvent.LoadRequestEvent -= OnLoadEvent;
        afterSceneLoadedEvent.OnEventEaised -= OnAfterSceneLoadedEvent;
        loadDataEvent.OnEventEaised -= OnLoadDataEvent;
        returnToMenuEvent.OnEventEaised -= OnLoadDataEvent;
    }

    private void Update()
    {
        inputDirection = inputControl.Gameplay.Move.ReadValue<Vector2>();
        CheckState();
    }

    private void FixedUpdate()
    {   
        //受伤和攻击时不能移动
        if (!isHurt && !isAttack)
            Move();
    }

    private void Move()
    {
        //人物移动，在下蹲和蹬墙跳时不能移动
        if (!isCrouch && !wallJump)
            rb.velocity = new Vector2(inputDirection.x * speed * Time.deltaTime, rb.velocity.y);

        //人物翻转
        int faceDir = (int)transform.localScale.x;
        if (inputDirection.x > 0)
            faceDir = 1;
        if (inputDirection.x < 0)
            faceDir = -1;
        transform.localScale = new Vector3(faceDir, 1, 1);

        //人物下蹲
        isCrouch = inputDirection.y < -0.5f && physicsCheck.isGround;
        if (isCrouch)
        {
            //修改碰撞体大小和位移
            capsuleCollider.offset = new Vector2(-0.05f, 0.85f);
            capsuleCollider.size = new Vector2(0.7f, 1.7f);
        }
        else
        {
            //还原之前的参数
            capsuleCollider.offset = originOffset;
            capsuleCollider.size = originSize; 
        }

    }

    #region 场景加载事件监听
    private void OnLoadEvent(GameSceneSO arg0, Vector3 arg1, bool arg2)
    {
        //加载时人物不能动
        inputControl.Gameplay.Disable();
    }

    private void OnAfterSceneLoadedEvent()
    {
        //加载完成后人物可以移动
        inputControl.Gameplay.Enable();
    }

    private void OnLoadDataEvent()
    {
        isDead = false;
    }
    #endregion

    private void Jump(InputAction.CallbackContext context)
    {
        //在地上才能进行跳跃
        if (physicsCheck.isGround) 
        {
            rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
            //播放跳跃音效
            GetComponent<AudioDefination>()?.PlayAudioClip();
        } else if (physicsCheck.onWall) 
        {
            //反方向给一个向上的力
            rb.AddForce((new Vector2(-inputDirection.x, 2f)) * wallJumpForce, ForceMode2D.Impulse);
            wallJump = true;
            //播放跳跃音效
            GetComponent<AudioDefination>()?.PlayAudioClip();
        }

        //跳跃能够打断滑铲,取消无敌状态
        isSlide = false;
        character.invulnerable = false;
        StopAllCoroutines();
    }

    private void PlayerAttack(InputAction.CallbackContext context)
    {
        playerAnimation.PlayAttack();
        isAttack = true;
    }

    private void PlayerSlide(InputAction.CallbackContext context)
    {
        if (!isSlide && physicsCheck.isGround && character.currentPower - SildePowerCost >= 0)
        {

            isSlide = true;
            var targetPos = new Vector3(transform.position.x + slideDistance * transform.localScale.x, transform.position.y);
            int slideDir = (int)transform.localScale.x;
            StartCoroutine(TriggerSlide(targetPos, slideDir));
            character.OnSlide(SildePowerCost);

        }

        //TODO：无敌问题，左右滑动问题
    }

    private IEnumerator TriggerSlide(Vector3 targetPos, int slideDir)
    {
        do 
        {
            yield return null;  //协程每次循环 先暂停到下一帧，然后下一帧再继续执行后面的检测和MovePosition。
            character.invulnerable = true;  //滑铲时无敌

            //碰到悬崖或者碰墙或者改变方向都会取消滑铲
            if (!physicsCheck.isGround ||physicsCheck.touchLeftWall && transform.localScale.x < 0f ||physicsCheck.touchRightWall && transform.localScale.x > 0f ||transform.localScale.x != slideDir)
            {
                break;
            }

            rb.MovePosition(new Vector2(transform.position.x + slideSpeed * transform.localScale.x, transform.position.y));
        } while (MathF.Abs(transform.position.x - targetPos.x) > 0.1f);
        isSlide = false;
        character.invulnerable = false;
    }

    #region UnityEvent
    public void GetHurt(Transform attacker)
    {
        isHurt = true;
        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(transform.position.x - attacker.position.x, 0).normalized;
        rb.AddForce(dir * hurtForce, ForceMode2D.Impulse);
    }

    public void PlayerDead()
    {
        isDead = true;
        inputControl.Gameplay.Disable();    //禁止游戏操作
    }
    #endregion

    private void CheckState()
    {
        capsuleCollider.sharedMaterial = physicsCheck.isGround ? normal : wall;
        //在墙上时缓慢下降
        if (physicsCheck.onWall)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y / 2);
        }

        //从开始下落时才能操纵下落方向
        if (wallJump && rb.velocity.y < 0f)
        {
            wallJump = false;
        }
    }
}
