using JetBrains.Annotations;
using UnityEngine;

public class BossAttack : I_State
{
    MonsterBlackboard blackboard;
    public BossAttack(Blackboard blackboard)
    {
        this.blackboard = blackboard as MonsterBlackboard;
    }
    // 当前释放的攻击
    private int nowSkill; // skill = 1-3

    public void OnEnter()
    {
        Debug.Log("BossAttack");
        blackboard.attackEnd = false;
        nowSkill = Random.Range(1, 4);
    }

    public void OnExit()
    {
        // 清除一些召唤物
        // 设置CD

    }

    public void OnUpdate()
    {
        Attack1();
    }

    //  攻击方式1（拍地板）
    private void Attack1()
    {
        if (nowSkill != 1)
        {
            return;
        }
        // Boss3D逻辑

        // Boss2D逻辑
    }
}
