using UnityEngine;

/// <summary>
/// player2D掉落状态
/// 下落前先探脚下，碰到面就停在面上，不会穿地也不会漏掉落地判定
/// </summary>
public class FallState2D : I_State
{
    PlayerBlackboard blackboard;

    public FallState2D(Blackboard blackboard)
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
        PlayerProbe2D.MoveAir(blackboard);
        PlayerProbe2D.MoveDown(blackboard, -blackboard.verticalSpeed * blackboard.deltaTime);
    }
}