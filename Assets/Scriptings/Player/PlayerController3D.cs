using UnityEngine;

/// <summary>
/// Player3D控制器
/// 每帧先做脚下胶囊判定，再跑状态机：onGround 是跳跃、移动、掉落的唯一依据
/// </summary>
public class PlayerController3D : MonoBehaviour
{
    // 状态机
    private FSM<E_PlayerStateType, PlayerBlackboard> playerFSM3D;
    // 玩家黑板
    private PlayerBlackboard playerBlackboard;
    public GameObject player3D;

    public InputCenter input;

    // 3D摄像機，移动方向以它的前/右为准
    public Camera viewCamera;

    private void Awake()
    {
        playerBlackboard = new PlayerBlackboard();
        playerFSM3D = new FSM<E_PlayerStateType, PlayerBlackboard>(playerBlackboard);

        // 初始化playerBlackboard
        playerBlackboard.player3D = player3D;

        // 控制器就挂在玩家身上，没拖引用就直接用自己的物体
        if (playerBlackboard.player3D == null)
        {
            playerBlackboard.player3D = gameObject;
        }

        playerBlackboard.controller3D = GetComponent<CharacterController>();

        if (playerBlackboard.input == null)
        {
            playerBlackboard.input = input;
        }

        // 摄像机传进黑板，移动方向靠它（没拖就用MainCamera）
        SetCamera();

        // 黑板的胶囊尺寸就是身体的尺寸，身体和检测口径必须一致
        SyncCapsule();

        AddState();
        AddTransition();
        playerFSM3D.SwitchState(E_PlayerStateType.Idle);
    }

    private void Start()
    {

    }

    /// <summary>
    /// 世界切换会把这个玩家重新启用：把上一段留下的速度和状态清干净
    /// 不清的话会带着旧的竖直速度回来，还可能停在Jump里出不去，移动跳跃就全锁死了
    /// </summary>
    private void OnEnable()
    {
        ResetState();
    }

    /// <summary>
    /// 清速度、清跳跃计数、状态回到Idle
    /// </summary>
    private void ResetState()
    {
        if (playerBlackboard == null || playerFSM3D == null)
        {
            return;
        }

        playerBlackboard.moveSpeed = 0f;
        playerBlackboard.verticalSpeed = 0f;
        playerBlackboard.horizontalVelocity = Vector3.zero;
        playerBlackboard.jumpTimer = 0f;
        playerBlackboard.jumpCount = 0;
        playerBlackboard.jumpConsumed = false;
        playerFSM3D.SwitchState(E_PlayerStateType.Idle);
    }

    /// <summary>
    /// 把摄像机交给黑板：移动方向以摄像机的前/右为准
    /// 没拖就用MainCamera，再没有就退回世界坐标轴（会提示一次）
    /// </summary>
    private void SetCamera()
    {
        if (viewCamera != null)
        {
            playerBlackboard.camera3D = viewCamera.transform;
            return;
        }

        if (Camera.main != null)
        {
            playerBlackboard.camera3D = Camera.main.transform;
            return;
        }

        playerBlackboard.camera3D = null;
        Debug.LogWarning("[PlayerController3D] 没传viewCamera，3D移动会按世界坐标轴算");
    }

    /// <summary>
    /// 把黑板的胶囊尺寸套到CharacterController上
    /// 2D按同一份尺寸做检测，3D不对齐的话判定和身体就会错位
    /// </summary>
    private void SyncCapsule()
    {
        if (playerBlackboard.controller3D == null)
        {
            // 场景里没挂胶囊控制器就补一个，3D移动全靠它
            playerBlackboard.controller3D = gameObject.AddComponent<CharacterController>();
        }

        playerBlackboard.controller3D.center = Vector3.zero; // 原点就是胶囊中心，和2D玩家对齐
        playerBlackboard.controller3D.height = playerBlackboard.capsuleHeight;
        playerBlackboard.controller3D.radius = playerBlackboard.capsuleRadius;
        playerBlackboard.controller3D.slopeLimit = playerBlackboard.slopeLimit;
        // 碰撞间隙：默认0.08会让胶囊浮在地面上方8cm，2D是贴地的，这里收小到和2D一个量级
        playerBlackboard.controller3D.skinWidth = 0.02f;

        IgnoreOwnColliders();
    }

    /// <summary>
    /// 只处理子物体（模型）上的碰撞体：胶囊会把自己模型上的BoxCollider/SphereCollider当成墙，一步都走不动
    /// 同一个物体上的Collider胶囊不会碰（你自己加的碰撞体留着，给命中判定用）
    /// </summary>
    private void IgnoreOwnColliders()
    {
        Collider[] own = GetComponentsInChildren<Collider>();
        for (int i = 0; i < own.Length; i++)
        {
            if (own[i] == playerBlackboard.controller3D || own[i].transform == transform)
            {
                continue;
            }

            Physics.IgnoreCollision(playerBlackboard.controller3D, own[i]);
            own[i].enabled = false;
        }
    }

    private void AddState()
    {
        playerFSM3D.AddState(E_PlayerStateType.Idle, new IdleState3D(playerBlackboard));
        playerFSM3D.AddState(E_PlayerStateType.Move, new MoveState3D(playerBlackboard));
        playerFSM3D.AddState(E_PlayerStateType.Jump, new JumpState3D(playerBlackboard));
        playerFSM3D.AddState(E_PlayerStateType.Fall, new FallState3D(playerBlackboard));
    }

    private void AddTransition()
    {
        // 站立：能跳就跳，脚下一空就掉，有输入就进移动
        playerFSM3D.AddTransition(E_PlayerStateType.Idle, E_PlayerStateType.Jump, CanJump);
        playerFSM3D.AddTransition(E_PlayerStateType.Idle, E_PlayerStateType.Fall, IsAirborne);
        playerFSM3D.AddTransition(E_PlayerStateType.Idle, E_PlayerStateType.Move, HasMoveInput);

        // 移动：同样先看跳跃和踩空，没输入就回站立
        playerFSM3D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Jump, CanJump);
        playerFSM3D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Fall, IsAirborne);
        playerFSM3D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Idle, NoMoveInput);

        // 跳跃：过了起跳锁定期、竖直速度不再向上，才允许进掉落，所以不会刚起跳就掉
        playerFSM3D.AddTransition(E_PlayerStateType.Jump, E_PlayerStateType.Fall, CanFallFromJump);

        // 兜底：锁定期过完还踩着地却停在Jump（状态被重置过）就直接回地面状态，不然会卡在Jump里动不了
        playerFSM3D.AddTransition(E_PlayerStateType.Jump, E_PlayerStateType.Move, CanLandMoveFromJump);
        playerFSM3D.AddTransition(E_PlayerStateType.Jump, E_PlayerStateType.Idle, CanLandFromJump);

        // 掉落：踩到地面就按有没有输入回到站立/移动；2段跳开着时空中还能再跳一段（切回Jump）
        playerFSM3D.AddTransition(E_PlayerStateType.Fall, E_PlayerStateType.Jump, CanAirJump);
        playerFSM3D.AddTransition(E_PlayerStateType.Fall, E_PlayerStateType.Jump, CanJump);
        playerFSM3D.AddTransition(E_PlayerStateType.Fall, E_PlayerStateType.Move, CanLandMove);
        playerFSM3D.AddTransition(E_PlayerStateType.Fall, E_PlayerStateType.Idle, CanLand);
    }

    // 能跳：踩在地面上，并且这一帧的跳跃输入还没被用掉
    private bool CanJump()
    {
        return playerBlackboard.onGround
            && playerBlackboard.input != null
            && playerBlackboard.input.jump
            && !playerBlackboard.jumpConsumed;
    }

    // 空中再跳一段：2段跳开着才有
    private bool CanAirJump()
    {
        return playerBlackboard.CanAirJump();
    }

    // 脚下一空
    private bool IsAirborne()
    {
        return !playerBlackboard.onGround;
    }

    // 起跳锁定期过完，并且竖直速度不再向上，才算真的开始下落
    private bool CanFallFromJump()
    {
        return !playerBlackboard.onGround
            && playerBlackboard.jumpTimer >= playerBlackboard.jumpMinTime
            && playerBlackboard.verticalSpeed <= 0f;
    }

    // 起跳锁定期过完还踩着地：说明状态被重置过，允许回地面状态（防止卡在Jump里）
    private bool CanLandFromJump()
    {
        return playerBlackboard.onGround
            && playerBlackboard.jumpTimer >= playerBlackboard.jumpMinTime;
    }

    private bool CanLandMoveFromJump()
    {
        return CanLandFromJump() && HasMoveInput();
    }

    // 3D玩家的移动输入：WASD任意一个
    private bool HasMoveInput()
    {
        return playerBlackboard.input != null && playerBlackboard.input.haveMoveInput;
    }

    private bool NoMoveInput()
    {
        return !HasMoveInput();
    }

    // 落地：onGround 为真才算落地
    private bool CanLand()
    {
        return playerBlackboard.onGround;
    }

    private bool CanLandMove()
    {
        return playerBlackboard.onGround && HasMoveInput();
    }

    private void Update()
    {
        // 本帧步长写进黑板，2D与3D用同一套步长口径
        playerBlackboard.deltaTime = Time.deltaTime;
        Tick();
        PlayerPositionManage.playerPosition.WritePosition(transform.position.x, transform.position.y);
    }

    /// <summary>
    /// 一帧的移动：先刷新脚下胶囊，再跑状态机
    /// 单独抽出来，自检脚本可以按固定步长直接驱动，不受Update时序影响
    /// </summary>
    public void Tick()
    {
        // onGround 是跳跃、移动、掉落的唯一依据，每帧先算它
        playerBlackboard.onGround = PlayerProbe3D.GroundCheck(playerBlackboard);
        // 落地重置跳跃段数，跳跃输入松开解锁，2段跳才不会重复触发
        playerBlackboard.RefreshJumpState();
        playerFSM3D.OnUpdate();
    }
}