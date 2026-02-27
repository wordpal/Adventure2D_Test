using UnityEngine;

// 史莱姆王：巡逻（和 Snail 一样）+ 追击（Bee 的 chase 思路）+ 砸地攻击（落地瞬间开启子物体 Attack hitbox）
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(PhysicsCheck))]
public class SlimeKing : Enemy
{
    [Header("攻击 Hitbox（子物体）")]
    public GameObject attackHitbox;

    [Header("攻击参数")]
    public float approachDistance = 1.2f; // 跑到玩家旁边的距离阈值
    public float jumpForce = 8f;          // 起跳冲量
    public float damageWindow = 0.1f;     // 落地后 hitbox 开启时间
    public float attackStunTime = 0.8f;   // 攻击后僵直时间（使用 Enemy.wait）

    [Header("攻击判定")]
    public float attackCooldown = 1.2f;   // 每次攻击后的冷却（额外计时，不依赖 wait）

    [HideInInspector] public float attackCooldownCounter;

    protected override void Awake()
    {
        base.Awake();
        patrolState = new SlimeKingPatrolState();
        chaseState = new SlimeKingChaseState();
        skillState = new SlimeKingSkillState();

        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }

    public override bool FindPlayer()
    {
        var hit = Physics2D.BoxCast(transform.position + (Vector3)centerOffset, checksize, 0, faceDir, checkDistance, attackLayer);
        if (hit)
        {
            attacker = hit.transform;
            lostTimeCounter = lostTime;
        }
        return hit;
    }

    public override void Move()
    {
        // 史莱姆王的移动全部写在状态机的 PhysicsUpdate 中，避免与 Enemy.Move 的默认逻辑冲突。
    }

    public override void OnDie()
    {
        Time.timeScale = 0.2f; //时钟变慢
        base.OnDie();
    }

    public override void DestroyAfterAnimation()
    {
        Time.timeScale = 1; //回复正常
        base.DestroyAfterAnimation();
    }
}
