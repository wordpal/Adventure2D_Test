using UnityEngine;
using static UnityEngine.GraphicsBuffer;
public class BeeChaseState : BaseState
{
    private Vector3 target;
    private Vector3 moveDir;
    private Attack attack;
    private bool isAttack;
    private float attackRateCounter;//由于只有Bee有attackrate,故在此状态机内设置计时器
    public override void OnEnter(Enemy emeny)
    {
        currentEnemy = emeny;
        currentEnemy.currentSpeed = currentEnemy.chaseSpeed;
        attack = currentEnemy.GetComponent<Attack>();
        currentEnemy.lostTimeCounter = currentEnemy.lostTime;
        currentEnemy.anim.SetBool("chase", true);
    }
    public override void LogicUpdate()
    {
        if (currentEnemy.lostTimeCounter <= 0)
        {
            currentEnemy.SwitchState(NPCState.Patrol);
        }
        //Player以脚底为中心，所以要向上移动一些
        target = new Vector3(currentEnemy.attacker.position.x, currentEnemy.attacker.position.y + 1.5f, 0);

        //判断攻击距离
        if (Mathf.Abs(currentEnemy.transform.position.x - target.x) < attack.attackRange && Mathf.Abs(currentEnemy.transform.position.y - target.y) < attack.attackRange)
        {
            isAttack = true;
            //不在被攻击时，速度就设为0，否则被击飞
            if (!currentEnemy.isHurt)
                currentEnemy.rb.velocity = Vector2.zero;

            //计时器
            attackRateCounter -= Time.deltaTime;
            if (attackRateCounter <= 0) 
            {
                attackRateCounter = attack.attackRate;
                currentEnemy.anim.SetTrigger("attack");
            }
        }
        else
        {
            isAttack = false;
        }

        //追踪目标
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
        if (!currentEnemy.isHurt && !currentEnemy.isDead && !isAttack)
        {
            currentEnemy.rb.velocity = moveDir * currentEnemy.currentSpeed * Time.deltaTime;
        }
    }
    public override void OnExit()
    {
        currentEnemy.anim.SetBool("chase", false);
    }
}
