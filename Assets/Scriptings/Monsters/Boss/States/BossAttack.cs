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

    // 攻击2

    public void OnEnter()
    {
        Debug.Log("BossAttack");
        blackboard.attackEnd = false;
        nowSkill = Random.Range(1, 4);
    }

    public void OnExit()
    {
        // 设置CD
    }

    public void OnUpdate()
    {

    }
}
