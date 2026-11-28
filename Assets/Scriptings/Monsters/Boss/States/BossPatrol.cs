using Unity.VisualScripting;
using UnityEngine;

// Boss巡查
public class BossPatrol : I_State
{
    MonsterBlackboard blackboard;
    public BossPatrol(Blackboard blackboard)
    {
        this.blackboard = blackboard as MonsterBlackboard;
    }

    public void OnEnter()
    {
        Debug.Log("BossPatrol");
        blackboard.start = false;
        blackboard.CD = int.MaxValue;
    }

    public void OnExit()
    {
        // 设置CD为5秒
        blackboard.CD = 5;
    }

    public void OnUpdate()
    {
        // 计算玩家与boss距离
        float dis2D = (blackboard.player2DTransform.position - blackboard.monster2D.transform.position).magnitude;
        float dis3D = (blackboard.player3DTransform.position - blackboard.monster3D.transform.position).magnitude;

        // 开始战斗
        if (dis2D <= 20 || dis3D <= 20)
        {
            blackboard.start = true;
        }
    }
}
