using UnityEngine;

/// <summary>
/// Player2D控制器
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
        playerFSM2D.AddState(E_PlayerStateType.Idle, new IdleState2D());
        playerFSM2D.AddState(E_PlayerStateType.Move, new MoveState2D(playerBlackboard));
    }

    private void AddTransition()
    {
        playerFSM2D.AddTransition(E_PlayerStateType.Idle, E_PlayerStateType.Move, () =>
        {
            return input != null && (input.moveLeft || input.moveRight);
        });
        playerFSM2D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Idle, () =>
        {
            return playerBlackboard.moveSpeed <= 0;
        });
    }

    private void Update()
    {
        playerFSM2D.OnUpdate();
        PlayerPositionManage.playerPosition.WritePosition(transform.position.x, transform.position.y);
    }
}
