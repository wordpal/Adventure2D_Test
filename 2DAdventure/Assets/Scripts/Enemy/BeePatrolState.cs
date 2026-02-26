using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BeePatrolState : BaseState
{
    private Vector3 target;
    private Vector3 moveDir; 
    public override void OnEnter(Enemy emeny)
    {
        currentEnemy = emeny;
        currentEnemy.currentSpeed = currentEnemy.normalSpeed;
        target = currentEnemy.GetNowPoint();
    }
    public override void LogicUpdate()
    {
        if (currentEnemy.FindPlayer())
        {
            currentEnemy.SwitchState(NPCState.Chase);
        }

        if (Mathf.Abs(currentEnemy.transform.position.x - target.x) < 0.1 && Mathf.Abs(currentEnemy.transform.position.y - target.y) < 0.1)
        {
            currentEnemy.wait = true;
            target = currentEnemy.GetNowPoint();
        }

        moveDir = (target - currentEnemy.transform.position).normalized;
        if (moveDir.x > 0)
        {
            currentEnemy.transform.localScale = new Vector3(-1, 1, 1);
        }
        if (moveDir.x < 0)
        {
            currentEnemy.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public override void PhysicsUpdate()
    {
        if (!currentEnemy.isHurt && !currentEnemy.isDead && !currentEnemy.wait)
        {
            currentEnemy.rb.velocity = moveDir * currentEnemy.currentSpeed * Time.deltaTime;
        }
    }

    public override void OnExit()
    {
        
    }

}
