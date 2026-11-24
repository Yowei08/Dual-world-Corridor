using UnityEngine;

/// <summary>
/// 用于存储Player的共享数据
/// 2D与3D共用同一份数值与同一套检测口径，改一处两边同时生效，保证手感严格一致
/// </summary>
public class PlayerBlackboard : Blackboard
{
    // 共享组件
    public GameObject player2D;
    public GameObject player3D;
    public CharacterController controller3D; // 3D玩家身上的胶囊控制器，移动全靠它
    public Transform camera3D; // 3D世界用的摄像机，移动方向以它的前/右为准
    public InputCenter input;

    // 玩家数值
    public float moveSpeed = 0;// 玩家当前移动速度
    public float maxSpeed = 5.5f; // 移动速度上限（米/秒），2D与3D共用
    public float turnSpeed = 720f; // 转身速度（度/秒），玩家正面转向移动方向

    // 跳跃与重力：2D与3D都用这两项推起跳速度，所以跳跃高度一定是同一个数
    public float jumpHeight = 1.6f; // 跳跃高度（米），2D与3D共用
    public float gravity = 25f; // 重力加速度（正数，方向向下）
    public float jumpMinTime = 0.1f; // 起跳后的锁定期，没过完不允许判定掉落

    // 2段跳：开关就这一个，关掉只有1段
    public bool doubleJump = false; // 2段跳开关（现在关着）
    public int jumpCount = 0; // 这次离地后用掉了几段跳
    public bool jumpConsumed = false; // 这一帧的跳跃输入是否已经被用掉

    // 胶囊体尺寸：2D与3D的身体用同一份尺寸，判定口径才对得上
    public float capsuleRadius = 0.3f; // 胶囊半径
    public float capsuleHeight = 1.8f; // 胶囊总高
    public float groundCheckGap = 0.08f; // 脚底胶囊向下检测的距离（要比胶囊控制器停在地面时留的间隙大，否则永远判不到地面）
    public float wallCheckSkin = 0.02f; // 墙体检测时胶囊的膨胀量
    public float slopeLimit = 46f; // 可走坡度的最大角度，超过就算墙
    // 地面检测层：只认 Ground 层（可站立平台）。机关/装饰不在这一层，玩家就不会踩上去
    public LayerMask groundLayer = GroundMask();
    public LayerMask wallLayer = WallMask(); // 撞墙检测层：只有真正的墙在Wall层

    // 运行时状态：由探针和状态机共同维护
    public float deltaTime = 0.02f; // 本帧步长，状态统一用它，方便按固定步长回放
    public bool onGround = false; // 脚底胶囊是否踩到地面，跳跃/移动/掉落的唯一依据
    public float verticalSpeed = 0; // 竖直速度，向上为正
    public float jumpTimer = 0; // 起跳后经过的时间，配合jumpMinTime挡住"刚起跳就掉落"
    public Vector3 horizontalVelocity = Vector3.zero; // 水平速度：地面上就是当前移动速度，起跳后继承下来
    public float airDrag = 4f; // 空中水平速度的衰减（米/秒²，越小跳出去飘得越远）
    public Vector2 groundNormal = Vector2.up; // 2D脚下的地面法线，斜坡移动要用
    public int moveDir2D = 0; // 2D水平输入方向 -1/0/1
    public bool facingRight = true; // 2D玩家当前朝向，true=朝右（翻转用）
    public int moveDirX = 0; // 3D左右输入方向 -1/0/1
    public int moveDirZ = 0; // 3D前后输入方向 -1/0/1

    /// <summary>
    /// Ground层的遮罩：按名字取，取不到退回全层
    /// </summary>
    private static LayerMask GroundMask()
    {
        int mask = LayerMask.GetMask("Ground");
        return mask != 0 ? mask : Physics.AllLayers;
    }

    /// <summary>
    /// Wall层的遮罩：按名字取，取不到就退回第8层
    /// </summary>
    private static LayerMask WallMask()
    {
        int mask = LayerMask.GetMask("Wall");
        return mask != 0 ? mask : 1 << 8;
    }

    /// <summary>
    /// 起跳速度：由跳跃高度和重力反推（v = √(2gh)）
    /// 2D与3D都取这个值，所以两边跳得一样高
    /// </summary>
    public float JumpSpeed
    {
        get { return Mathf.Sqrt(2f * gravity * jumpHeight); }
    }

    /// <summary>
    /// 离地后一共能用几段跳：2段跳开着是2，关着是1
    /// </summary>
    public int JumpMax
    {
        get { return doubleJump ? 2 : 1; }
    }

    /// <summary>
    /// 空中还能不能再跳一段：2段跳开着、段数没用完、这一帧的跳跃输入还在
    /// </summary>
    public bool CanAirJump()
    {
        return doubleJump
            && jumpCount < JumpMax
            && input != null
            && input.jump
            && !jumpConsumed;
    }

    /// <summary>
    /// 真的跳一次：给竖直速度、清锁定时间、记一段跳、把这一帧的跳跃输入用掉
    /// </summary>
    public void TakeJump()
    {
        verticalSpeed = JumpSpeed;
        jumpTimer = 0f;
        jumpCount += 1;
        jumpConsumed = true;
    }

    /// <summary>
    /// 从地面走出去（不是跳出去的）：这段算把地面那一跳用掉了，空中只剩2段跳那一下
    /// </summary>
    public void MarkFell()
    {
        if (jumpCount == 0)
        {
            jumpCount = 1;
        }
    }

    /// <summary>
    /// 空中的水平速度缓慢衰减：2D/3D共用同一个公式，所以跳出去的弧线一样
    /// </summary>
    public void DecayAirVelocity()
    {
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, airDrag * deltaTime);
    }

    /// <summary>
    /// 每帧开头调用：落地就把段数清零，跳跃输入松开了就解锁
    /// </summary>
    public void RefreshJumpState()
    {
        if (onGround)
        {
            jumpCount = 0;
        }

        if (input == null || !input.jump)
        {
            jumpConsumed = false;
        }
    }
}