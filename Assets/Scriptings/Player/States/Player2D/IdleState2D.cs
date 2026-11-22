using UnityEngine;

/// <summary>
/// player2D默认状态
/// </summary>
public class IdleState2D : I_State
{
    PlayerBlackboard blackboard;

    public IdleState2D(Blackboard blackboard)
    {
        this.blackboard = blackboard as PlayerBlackboard;
    }

    public void OnEnter()
    {
        blackboard.moveSpeed = 0;
        blackboard.verticalSpeed = 0;
    }

    public void OnExit()
    {

    }

    public void OnUpdate()
    {
        blackboard.moveSpeed = 0;
        // 站着没有水平惯性
        blackboard.horizontalVelocity = Vector3.zero;
        // 站着也贴地：检测距离比脚底间隙宽松，不补这一下人会悬在地面上方
        PlayerProbe2D.MoveDown(blackboard, blackboard.groundCheckGap);
    }
}