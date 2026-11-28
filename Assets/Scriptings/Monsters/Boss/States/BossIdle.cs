using UnityEngine;

public class BossIdle : I_State
{
    MonsterBlackboard blackboard;

    public BossIdle(Blackboard blackboard)
    {
        this.blackboard = blackboard as MonsterBlackboard;
    }

    public void OnEnter()
    {
        Debug.Log("BossIdle");
    }

    public void OnExit()
    {
        
    }

    public void OnUpdate()
    {
        // 攻击CD计时
        if (blackboard.CD > 0)
        {
            blackboard.CD -= Time.deltaTime;
        }
    }
}
