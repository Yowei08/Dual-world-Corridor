using UnityEngine;

/// <summary>
/// player3D跳跃状态
/// 起跳之后只管往上走，竖直速度还不够下落时不会离开这个状态，所以不会"刚起跳就掉落"
/// 2段跳开着的话，上升途中再按一次跳还能再起一段
/// </summary>
public class JumpState3D : I_State
{
    PlayerBlackboard blackboard;

    public JumpState3D(Blackboard blackboard)
    {
        this.blackboard = blackboard as PlayerBlackboard;
    }

    public void OnEnter()
    {
        // 起跳速度由跳跃高度和重力反推，2D用的是同一个公式，所以两边跳得一样高
        blackboard.TakeJump();
        blackboard.moveSpeed = 0;
    }

    public void OnExit()
    {

    }

    public void OnUpdate()
    {
        // 上升途中再按跳：2段跳开着就再起一段（TakeJump会用掉这一帧的跳跃输入，不会重复触发）
        if (blackboard.CanAirJump())
        {
            blackboard.TakeJump();
        }

        blackboard.jumpTimer += blackboard.deltaTime;
        blackboard.verticalSpeed -= blackboard.gravity * blackboard.deltaTime;

        // 继承的水平速度在空中缓慢衰减：跑动中起跳会往前飘一段
        blackboard.DecayAirVelocity();
        Vector3 motion = blackboard.horizontalVelocity * blackboard.deltaTime
            + Vector3.up * (blackboard.verticalSpeed * blackboard.deltaTime);
        PlayerProbe3D.Move(blackboard, motion);
    }
}