using UnityEngine;

/// <summary>
/// boss脚本
/// </summary>
public class Boss : Monster
{
    public GameObject boss2D;
    public GameObject boss3D;

    // 玩家坐标
    public Transform player2DTransform;
    public Transform player3DTransform;

    private bool canBeAttack = false;


    private void Start()
    {
        health = 3; // boss生命值初始化
        damage = 1; // boss伤害初始化

        // 初始化状态机
        blackboard = new MonsterBlackboard();
        FSM = new FSM<E_MonsterStateType, MonsterBlackboard>(blackboard);

        blackboard.monster2D = boss2D;
        blackboard.monster3D = boss3D;
        blackboard.player2DTransform = player2DTransform;
        blackboard.player3DTransform = player3DTransform;

        AddState();
        AddTransition();

        FSM.SwitchState(E_MonsterStateType.Patrol);
    }

    // 添加状态
    private void AddState()
    {
        FSM.AddState(E_MonsterStateType.Patrol, new BossPatrol(blackboard));
        FSM.AddState(E_MonsterStateType.Idle, new BossIdle(blackboard));
        FSM.AddState(E_MonsterStateType.Attack, new BossAttack(blackboard));
    }

    // 添加转换规则
    private void AddTransition()
    {
        FSM.AddTransition(E_MonsterStateType.Patrol, E_MonsterStateType.Idle, () =>
        {
            return blackboard.start;
        });
        // CD小于等于0自动攻击
        FSM.AddTransition(E_MonsterStateType.Idle, E_MonsterStateType.Attack, () =>
        {
            return blackboard.CD <= 0;
        });
        // 攻击结束回到Idle
        FSM.AddTransition(E_MonsterStateType.Attack, E_MonsterStateType.Idle, () =>
        {
            return blackboard.attackEnd;
        });
    }

    private void Update()
    {
        if (FSM != null)
        {
            FSM.OnUpdate();
        }

        // boss3阶段
        switch (health)
        {
            case 3:
                break;
            case 2:
                break;
            case 1:
                break;
        }
    }

    private void CleanProtect()
    {
        canBeAttack = true;
    }

    // 敌人受击方法
    public override void TakeDamage(int damage)
    {
        if (!canBeAttack)
        {
            return;
        }
        if (health > 0)
        {
            health -= damage;
            canBeAttack = false;
        }
        else
        {
            // 敌人生命值为0执行死亡
            Die();
        }
    }

    // 敌人死亡方法
    public override void Die()
    {
        gameObject.SetActive(false);
    }

    // 重置
    public void Reset()
    {
        health = 3;
        FSM.SwitchState(E_MonsterStateType.Patrol);
    }
}
