using System.Collections;
using UnityEngine;

public class SlimeKingSkillState : BaseState
{
    private SlimeKing slime;

    private bool hasJumped;
    private bool wasInAir;
    private bool landed;

    public override void OnEnter(Enemy emeny)
    {
        currentEnemy = emeny;
        slime = currentEnemy as SlimeKing;

        hasJumped = false;
        wasInAir = false;
        landed = false;

        if (slime != null && slime.attackHitbox != null)
            slime.attackHitbox.SetActive(false);

        // 进入攻击时先停止水平速度，避免滑行
        currentEnemy.rb.velocity = new Vector2(0, currentEnemy.rb.velocity.y);

        // 触发攻击动画（Bee 的参数：attack trigger）
        currentEnemy.anim.SetTrigger("attack");
        currentEnemy.anim.SetBool("walk", false);
        currentEnemy.anim.SetBool("chase", false);
    }

    public override void LogicUpdate()
    {
        if (slime == null)
        {
            currentEnemy.SwitchState(NPCState.Chase);
            return;
        }

        // 攻击前：确保面向玩家
        if (!landed && currentEnemy.attacker != null)
        {
            if (currentEnemy.attacker.position.x - currentEnemy.transform.position.x > 0)
                currentEnemy.transform.localScale = new Vector3(-1, 1, 1);
            else if (currentEnemy.attacker.position.x - currentEnemy.transform.position.x < 0)
                currentEnemy.transform.localScale = new Vector3(1, 1, 1);
        }

        // 起跳：只执行一次
        if (!hasJumped && currentEnemy.physicsCheck.isGround)
        {
            hasJumped = true;
            currentEnemy.rb.AddForce(Vector2.up * slime.jumpForce, ForceMode2D.Impulse);
            wasInAir = true;
        }

        // 落地判定：曾经离地 + 再次接地 + 下落阶段
        if (!landed && wasInAir && currentEnemy.physicsCheck.isGround && currentEnemy.rb.velocity.y <= 0.01f)
        {
            landed = true;
            currentEnemy.rb.velocity = new Vector2(0, currentEnemy.rb.velocity.y);

            // 落地瞬间开启伤害 hitbox
            if (slime.attackHitbox != null)
                currentEnemy.StartCoroutine(EnableHitboxForSeconds(slime.attackHitbox, slime.damageWindow));

            // 进入僵直：使用 Enemy.wait 机制
            currentEnemy.wait = true;
            currentEnemy.waitTimeCounter = slime.attackStunTime;

            // 进入攻击冷却（与 wait 分离，避免 waitTime 被复用）
            slime.attackCooldownCounter = slime.attackCooldown;
        }

        // 僵直结束后决定回到 Chase 或 Patrol
        if (landed && !currentEnemy.wait)
        {
            if (currentEnemy.lostTimeCounter > 0)
                currentEnemy.SwitchState(NPCState.Chase);
            else
                currentEnemy.SwitchState(NPCState.Patrol);
        }
    }

    public override void PhysicsUpdate()
    {
        // Skill 状态下不进行水平移动（只跳起/落下）
    }

    public override void OnExit()
    {
        if (slime != null && slime.attackHitbox != null)
            slime.attackHitbox.SetActive(false);

        currentEnemy.anim.SetBool("walk", false);
        currentEnemy.anim.SetBool("chase", false);
    }

    private IEnumerator EnableHitboxForSeconds(GameObject hitbox, float seconds)
    {
        hitbox.SetActive(true);
        yield return new WaitForSeconds(seconds);
        hitbox.SetActive(false);
    }
}
