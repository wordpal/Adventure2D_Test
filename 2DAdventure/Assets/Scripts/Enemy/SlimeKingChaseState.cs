using UnityEngine;

public class SlimeKingChaseState : BaseState
{
    private SlimeKing slime;
    private float moveDirX;

    public override void OnEnter(Enemy emeny)
    {
        currentEnemy = emeny;
        slime = currentEnemy as SlimeKing;

        currentEnemy.currentSpeed = currentEnemy.chaseSpeed;
        currentEnemy.lostTimeCounter = currentEnemy.lostTime;
        currentEnemy.anim.SetBool("walk", false);
        currentEnemy.anim.SetBool("chase", true);

        if (slime != null)
            slime.attackCooldownCounter = 0;
    }

    public override void LogicUpdate()
    {
        if (currentEnemy.lostTimeCounter <= 0)
        {
            currentEnemy.SwitchState(NPCState.Patrol);
            return;
        }

        // 追击时持续尝试刷新 attacker（FindPlayer 会更新 lostTimeCounter/attacker）
        currentEnemy.FindPlayer();

        if (slime != null)
            slime.attackCooldownCounter -= Time.deltaTime;

        if (currentEnemy.attacker != null)
        {
            // 朝向玩家
            if (currentEnemy.attacker.position.x - currentEnemy.transform.position.x > 0)
                currentEnemy.transform.localScale = new Vector3(-1, 1, 1);
            else if (currentEnemy.attacker.position.x - currentEnemy.transform.position.x < 0)
                currentEnemy.transform.localScale = new Vector3(1, 1, 1);

            // 距离足够近时切到攻击（Skill）
            if (slime != null && slime.attackCooldownCounter <= 0)
            {
                float dist = Mathf.Abs(currentEnemy.transform.position.x - currentEnemy.attacker.position.x);
                if (dist <= slime.approachDistance && currentEnemy.physicsCheck.isGround)
                {
                    currentEnemy.SwitchState(NPCState.Skill);
                    return;
                }
            }
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
        currentEnemy.anim.SetBool("chase", false);
    }
}
