using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

public class PhysicsCheck : MonoBehaviour
{
    private CapsuleCollider2D capsuleCollider;
    private PlayerController playerController;
    private Rigidbody2D rb;

    [Header("检测参数")]
    public bool manual;     //是否手动调整左右墙壁检测偏移
    public bool isPlayer;   //只有player需要获得PlayerController组件进行爬墙判断
    public float checkReduis;
    public Vector2 bottomOffset;
    public Vector2 leftOffset;
    public Vector2 rightOffset;
    public LayerMask groundLayer;

    [Header("状态")]
    public bool isGround;
    public bool touchLeftWall;
    public bool touchRightWall;
    public bool onWall;

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        if (!manual)
        {
            rightOffset = new Vector2((capsuleCollider.bounds.size.x + capsuleCollider.offset.x) / 2, capsuleCollider.bounds.size.y / 2);
            leftOffset = new Vector2(-rightOffset.x, capsuleCollider.bounds.size.y / 2);
        }

        if (isPlayer)
        {
            playerController = GetComponent<PlayerController>();
        }

    }

    // Update is called once per frame
    private void Update()
    {
        Check();
    }

    public void Check()
    {
        //落地动画比其他状态时要高一些
        if (onWall)
            isGround = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(bottomOffset.x * transform.localScale.x, bottomOffset.y), checkReduis, groundLayer);
        else
            isGround = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(bottomOffset.x * transform.localScale.x, 0), checkReduis, groundLayer);


        touchLeftWall = Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, checkReduis, groundLayer);
        touchRightWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, checkReduis, groundLayer);

        //只有靠墙且不在地上且按键朝相应方向时才爬墙
        if (isPlayer)
        {
            onWall = (touchLeftWall && playerController.inputDirection.x < 0f || touchRightWall && playerController.inputDirection.x > 0f) && rb.velocity.y < 0;
        }
    }

    private void OnDrawGizmosSelected()
    {
        float faceDir = transform.localScale.x >= 0 ? 1f : -1f;

        Vector2 bottom = new Vector2(bottomOffset.x * faceDir, bottomOffset.y);

        Gizmos.DrawWireSphere((Vector2)transform.position + bottom, checkReduis);
        Gizmos.DrawWireSphere((Vector2)transform.position + leftOffset, checkReduis);
        Gizmos.DrawWireSphere((Vector2)transform.position + rightOffset, checkReduis);
    }
}
