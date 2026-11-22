using UnityEngine;

/// <summary>
/// player2D移动状态
/// </summary>
public class MoveState2D : I_State
{
    PlayerBlackboard blackboard;

    public MoveState2D(Blackboard blackboard)
    {
        this.blackboard = blackboard as PlayerBlackboard;
    }

    private int moveDir = 0; // 移动方向
    private float maxSpeed; // 移动速度

    public void OnEnter()
    {
        maxSpeed = blackboard.maxSpeed;
    }

    public void OnExit()
    {
        
    }

    public void OnUpdate()
    {
        SetDir();
        Move();
    }

    private void SetDir()
    {
        if (blackboard.input.moveRight)
        {
            moveDir = 1;
        }
        else if (blackboard.input.moveLeft)
        {
            moveDir = -1;
        }
        else
        {
            moveDir = 0;
        }
    }

    private void Move()
    {
        if (moveDir == 0)
        {
            blackboard.moveSpeed = 0;
            return;
        }
        blackboard.moveSpeed = maxSpeed;
        blackboard.player2D.transform.position += new Vector3(moveDir, 0, 0) * blackboard.moveSpeed * Time.deltaTime;
    }
}
