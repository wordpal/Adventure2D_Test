using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

//Bee不需要和Ground的碰撞体，可以穿越

public class Bee : Enemy
{
    [Header("移动范围")]
    public float patrolRadius;

    protected override void Awake()
    {
        base.Awake();
        patrolState = new BeePatrolState();
        chaseState = new BeeChaseState();
    }
    public override bool FindPlayer()
    {
        var obj = Physics2D.OverlapCircle(transform.position, checkDistance, attackLayer);
        if (obj)
        {
            attacker = obj.transform;//记录 attacker
        }
        return obj;
    }

    public override void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, checkDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spwanPoint, patrolRadius);
    }

    public override Vector3 GetNowPoint()
    {
        var targetX = Random.Range(-patrolRadius, patrolRadius);
        var targetY = Random.Range(-patrolRadius, patrolRadius);

        return spwanPoint + new Vector3(targetX, targetY);
    }

    public override void Move()
    {
        //写到状态机的physicsUpdate里，不写在这里
    }

}
