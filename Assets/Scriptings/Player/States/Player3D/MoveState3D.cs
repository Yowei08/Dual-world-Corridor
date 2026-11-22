using UnityEngine;

/// <summary>
/// player3D移动状态
/// 移动方向以摄像机的前/右为准，玩家正面转向移动方向
/// </summary>
public class MoveState3D : I_State
{
    PlayerBlackboard blackboard;

    public MoveState3D(Blackboard blackboard)
    {
        this.blackboard = blackboard as PlayerBlackboard;
    }

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
        blackboard.moveDirX = 0;
        blackboard.moveDirZ = 0;

        if (blackboard.input == null)
        {
            return;
        }

        if (blackboard.input.moveRight)
        {
            blackboard.moveDirX = 1;
        }
        else if (blackboard.input.moveLeft)
        {
            blackboard.moveDirX = -1;
        }

        if (blackboard.input.moveForward)
        {
            blackboard.moveDirZ = 1;
        }
        else if (blackboard.input.moveBack)
        {
            blackboard.moveDirZ = -1;
        }
    }

    private void Move()
    {
        Vector3 direction = GetMoveDirection();

        if (direction == Vector3.zero)
        {
            blackboard.moveSpeed = 0;
            return;
        }

        // 斜着走要归一化，否则对角线比直线快，和2D的移动速度就对不上了
        direction = direction.normalized;
        blackboard.moveSpeed = maxSpeed;

        float distance = blackboard.moveSpeed * blackboard.deltaTime;

        // 向移动方向做膨胀胶囊检测，撞墙就拒绝这次移动
        if (PlayerProbe3D.WallCheck(blackboard, direction, distance))
        {
            blackboard.moveSpeed = 0;
            return;
        }

        // 转向：正面朝向移动方向
        FaceTo(direction);

        blackboard.verticalSpeed = 0;

        // 记下当前水平速度：起跳时直接继承它，所以在跑动中起跳会往前飘
        blackboard.horizontalVelocity = direction * blackboard.moveSpeed;

        // 水平位移 + 一点向下的贴地势：下坡时脚还踩在地面上，不会掉进掉落状态
        Vector3 motion = direction * distance + Vector3.down * (blackboard.maxSpeed * blackboard.deltaTime);
        PlayerProbe3D.Move(blackboard, motion);
    }

    /// <summary>
    /// 以摄像机的前/右为准算移动方向，再压平到水平面
    /// 摄像机没传就退回世界坐标轴
    /// </summary>
    private Vector3 GetMoveDirection()
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (blackboard.camera3D != null)
        {
            forward = blackboard.camera3D.forward;
            right = blackboard.camera3D.right;
        }

        // 摄像机俯视时前/右会带竖直分量，压平再用
        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
            right = Vector3.right;
        }

        forward.Normalize();
        right.Normalize();
        return forward * blackboard.moveDirZ + right * blackboard.moveDirX;
    }

    /// <summary>
    /// 转向：玩家正面转向移动方向，按turnSpeed转过去
    /// </summary>
    private void FaceTo(Vector3 direction)
    {
        if (blackboard.player3D == null)
        {
            return;
        }

        Transform body = blackboard.player3D.transform;
        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        body.rotation = Quaternion.RotateTowards(body.rotation, target, blackboard.turnSpeed * blackboard.deltaTime);
    }
}