using UnityEngine;

public class SlimeKingPatrolState : BaseState
{
    private float moveDirX;

    public override void OnEnter(Enemy emeny)
    {
        currentEnemy = emeny;
        currentEnemy.currentSpeed = currentEnemy.normalSpeed;
        currentEnemy.anim.SetBool("chase", false);
        currentEnemy.anim.SetBool("walk", true);
    }

    public override void LogicUpdate()
    {
        if (currentEnemy.FindPlayer())
        {
            currentEnemy.SwitchState(NPCState.Chase);
            return;
        }

        // 和 Snail 一样：碰到悬崖或者面朝侧碰到墙就待机一会，然后 Enemy.TimeCounter 会负责掉头
        if (!currentEnemy.physicsCheck.isGround ||
            (currentEnemy.physicsCheck.touchLeftWall && currentEnemy.faceDir.x < 0) ||
            (currentEnemy.physicsCheck.touchRightWall && currentEnemy.faceDir.x > 0))
        {
            currentEnemy.wait = true;
            currentEnemy.anim.SetBool("walk", false);
        }
        else
        {
            currentEnemy.anim.SetBool("walk", true);
        }

        moveDirX = currentEnemy.faceDir.x;
    }

    public override void PhysicsUpdate()
    {
        if (!currentEnemy.isHurt && !currentEnemy.isDead && !currentEnemy.wait)
        {
            currentEnemy.rb.velocity = new Vector2(currentEnemy.currentSpeed * moveDirX * Time.deltaTime, currentEnemy.rb.velocity.y);
        }
    }

    public override void OnExit()
    {
        currentEnemy.anim.SetBool("walk", false);
    }
}
