using UnityEngine;

/// <summary>
/// player3D掉落状态
/// 从跳跃最高点下来、或者从地面上走出去都会进这里，只受重力
/// </summary>
public class FallState3D : I_State
{
    PlayerBlackboard blackboard;

    public FallState3D(Blackboard blackboard)
    {
        this.blackboard = blackboard as PlayerBlackboard;
    }

    public void OnEnter()
    {
        blackboard.moveSpeed = 0;
        // 是走下来的不是跳下来的：这段算把地面那一跳用掉了
        blackboard.MarkFell();
    }

    public void OnExit()
    {

    }

    public void OnUpdate()
    {
        blackboard.verticalSpeed -= blackboard.gravity * blackboard.deltaTime;

        // 掉落时不给操作位移，但保留从地面继承的水平惯性（缓慢衰减），落地判定只看 onGround
        blackboard.moveSpeed = 0;
        blackboard.DecayAirVelocity();
        Vector3 motion = blackboard.horizontalVelocity * blackboard.deltaTime
            + Vector3.up * (blackboard.verticalSpeed * blackboard.deltaTime);
        PlayerProbe3D.Move(blackboard, motion);
    }
}