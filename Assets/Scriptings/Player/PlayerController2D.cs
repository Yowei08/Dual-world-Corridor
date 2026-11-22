using UnityEngine;

/// <summary>
/// Player2D控制器
/// 每帧先做脚下胶囊判定，再跑状态机：onGround 是跳跃、移动、掉落的唯一依据
/// </summary>
public class PlayerController2D : MonoBehaviour
{
    // 状态机
    private FSM<E_PlayerStateType, PlayerBlackboard> playerFSM2D;
    // 玩家黑板
    private PlayerBlackboard playerBlackboard;
    public GameObject player2D;

    public InputCenter input;

    private void Awake()
    {
        playerBlackboard = new PlayerBlackboard();
        playerFSM2D = new FSM<E_PlayerStateType, PlayerBlackboard>(playerBlackboard);

        // 初始化playerBlackboard
        playerBlackboard.player2D = player2D;

        // 控制器就挂在玩家身上，没拖引用就直接用自己的物体
        if (playerBlackboard.player2D == null)
        {
            playerBlackboard.player2D = gameObject;
        }

        if (playerBlackboard.input == null)
        {
            playerBlackboard.input = input;
        }

        AddState();
        AddTransition();
        playerFSM2D.SwitchState(E_PlayerStateType.Idle);
    }

    private void Start()
    {

    }

    private void AddState()
    {
        playerFSM2D.AddState(E_PlayerStateType.Idle, new IdleState2D(playerBlackboard));
        playerFSM2D.AddState(E_PlayerStateType.Move, new MoveState2D(playerBlackboard));
        playerFSM2D.AddState(E_PlayerStateType.Jump, new JumpState2D(playerBlackboard));
        playerFSM2D.AddState(E_PlayerStateType.Fall, new FallState2D(playerBlackboard));
    }

    private void AddTransition()
    {
        // 站立：能跳就跳，脚下一空就掉，有输入就进移动
        playerFSM2D.AddTransition(E_PlayerStateType.Idle, E_PlayerStateType.Jump, CanJump);
        playerFSM2D.AddTransition(E_PlayerStateType.Idle, E_PlayerStateType.Fall, IsAirborne);
        playerFSM2D.AddTransition(E_PlayerStateType.Idle, E_PlayerStateType.Move, HasMoveInput);

        // 移动：同样先看跳跃和踩空，没输入就回站立
        playerFSM2D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Jump, CanJump);
        playerFSM2D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Fall, IsAirborne);
        playerFSM2D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Idle, NoMoveInput);

        // 跳跃：过了起跳锁定期、竖直速度不再向上，才允许进掉落，所以不会刚起跳就掉
        playerFSM2D.AddTransition(E_PlayerStateType.Jump, E_PlayerStateType.Fall, CanFallFromJump);

        // 掉落：踩到地面就按有没有输入回到站立/移动；2段跳开着时空中还能再跳一段（切回Jump）
        playerFSM2D.AddTransition(E_PlayerStateType.Fall, E_PlayerStateType.Jump, CanAirJump);
        playerFSM2D.AddTransition(E_PlayerStateType.Fall, E_PlayerStateType.Jump, CanJump);
        playerFSM2D.AddTransition(E_PlayerStateType.Fall, E_PlayerStateType.Move, CanLandMove);
        playerFSM2D.AddTransition(E_PlayerStateType.Fall, E_PlayerStateType.Idle, CanLand);
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

    // 2D玩家的移动输入只有左右
    private bool HasMoveInput()
    {
        return playerBlackboard.input != null && (playerBlackboard.input.moveLeft || playerBlackboard.input.moveRight);
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
        playerBlackboard.onGround = PlayerProbe2D.GroundCheck(playerBlackboard);
        // 落地重置跳跃段数，跳跃输入松开解锁，2段跳才不会重复触发
        playerBlackboard.RefreshJumpState();
        playerFSM2D.OnUpdate();
    }
}