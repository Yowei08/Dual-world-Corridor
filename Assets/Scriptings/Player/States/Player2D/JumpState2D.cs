using UnityEngine;

/// <summary>
/// player2D跳跃状态
/// 和3D用同一个起跳速度公式，所以两边跳跃高度相同；上升撞到头顶就把竖直速度清掉，下一帧进掉落
/// 2段跳开着的话，上升途中再按一次跳还能再起一段
/// </summary>
public class JumpState2D : I_State
{
    PlayerBlackboard blackboard;

    public JumpState2D(Blackboard blackboard)
    {
        this.blackboard = blackboard as PlayerBlackboard;
    }

    public void OnEnter()
    {
        // 起跳速度由跳跃高度和重力反推，3D用的是同一个公式，所以两边跳得一样高
        blackboard.TakeJump();
        blackboard.moveSpeed = 0;
    }

    public void OnExit()
    {

    }

    public void OnUpdate()
    {
        // 上升途中再按跳：2段跳开着就再起一段
        if (blackboard.CanAirJump())
        {
            blackboard.TakeJump();
        }

        blackboard.jumpTimer += blackboard.deltaTime;
        blackboard.verticalSpeed -= blackboard.gravity * blackboard.deltaTime;

        // 继承的水平速度在空中缓慢衰减：跑动中起跳会往前飘一段
        PlayerProbe2D.MoveAir(blackboard);

        if (blackboard.verticalSpeed > 0f)
        {
            if (PlayerProbe2D.MoveUp(blackboard, blackboard.verticalSpeed * blackboard.deltaTime))
            {
                blackboard.verticalSpeed = 0f;
            }
        }
        else
        {
            PlayerProbe2D.MoveDown(blackboard, -blackboard.verticalSpeed * blackboard.deltaTime);
        }
    }
}