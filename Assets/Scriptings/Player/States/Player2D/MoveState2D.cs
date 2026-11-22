using UnityEngine;

/// <summary>
/// player2D移动状态
/// 2D玩家没有刚体，位移手算：斜坡上把水平位移投影到坡面切线，走完再贴回地面
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
        moveDir = 0;

        if (blackboard.input != null)
        {
            if (blackboard.input.moveRight)
            {
                moveDir = 1;
            }
            else if (blackboard.input.moveLeft)
            {
                moveDir = -1;
            }
        }

        blackboard.moveDir2D = moveDir;

        if (moveDir != 0)
        {
            FaceTo();
        }
    }

    /// <summary>
    /// 翻转：让玩家正面朝向移动方向（朝左就把水平缩放取反）
    /// </summary>
    private void FaceTo()
    {
        if (blackboard.player2D == null)
        {
            return;
        }

        blackboard.facingRight = moveDir > 0;

        Vector3 scale = blackboard.player2D.transform.localScale;
        float absX = Mathf.Abs(scale.x);
        blackboard.player2D.transform.localScale = new Vector3(blackboard.facingRight ? absX : -absX, scale.y, scale.z);
    }

    private void Move()
    {
        if (moveDir == 0)
        {
            blackboard.moveSpeed = 0;
            return;
        }

        blackboard.moveSpeed = maxSpeed;

        float distance = blackboard.moveSpeed * blackboard.deltaTime;
        Vector2 direction = new Vector2(moveDir, 0f);

        // 向移动方向做膨胀胶囊检测，撞墙就拒绝这次移动
        if (PlayerProbe2D.WallCheck(blackboard, direction, distance))
        {
            blackboard.moveSpeed = 0;
            return;
        }

        Vector2 delta = direction * distance;

        if (blackboard.onGround)
        {
            // 陡坡当墙，不允许往上走
            if (Vector2.Angle(blackboard.groundNormal, Vector2.up) > blackboard.slopeLimit)
            {
                blackboard.moveSpeed = 0;
                return;
            }

            // 斜坡：位移投影到坡面切线，水平速度和3D一样，竖直方向跟着坡面走
            delta -= blackboard.groundNormal * Vector2.Dot(delta, blackboard.groundNormal);
        }

        blackboard.verticalSpeed = 0;

        // 记下当前水平速度：起跳时直接继承它，所以在跑动中起跳会往前飘
        blackboard.horizontalVelocity = new Vector3(moveDir * blackboard.moveSpeed, 0f, 0f);

        PlayerProbe2D.Translate(blackboard, delta);

        // 贴回坡面：下坡时脚下会悬空一截，补掉它才不会被判成掉落
        PlayerProbe2D.MoveDown(blackboard, Mathf.Abs(distance) + blackboard.groundCheckGap);
    }
}