using UnityEngine;

/// <summary>
/// player3D站立状态
/// </summary>
public class IdleState3D : I_State
{
    PlayerBlackboard blackboard;

    public IdleState3D(Blackboard blackboard)
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
        StickToGround();
    }

    /// <summary>
    /// 站着也贴地：给一点向下的位移，下坡/坡面上脚下不会悬空，也就不会被误判成掉落
    /// </summary>
    private void StickToGround()
    {
        PlayerProbe3D.Move(blackboard, Vector3.down * (blackboard.maxSpeed * blackboard.deltaTime));
    }
}