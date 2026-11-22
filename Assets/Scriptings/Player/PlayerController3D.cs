using UnityEngine;

/// <summary>
/// Player3D控制器
/// </summary>
public class PlayerController3D : MonoBehaviour
{
    // 状态机
    private FSM<E_PlayerStateType, PlayerBlackboard> playerFSM3D;
    // 玩家黑板
    private PlayerBlackboard playerBlackboard;
    public GameObject player3D;

    public InputCenter input;

    private void Awake()
    {
        playerBlackboard = new PlayerBlackboard();
        playerFSM3D = new FSM<E_PlayerStateType, PlayerBlackboard>(playerBlackboard);

        // 初始化playerBlackboard
        playerBlackboard.player3D = player3D;

        if (playerBlackboard.input == null)
        {
            playerBlackboard.input = input;
        }

        AddState();
        AddTransition();
        playerFSM3D.SwitchState(E_PlayerStateType.Idle);
    }

    private void Start()
    {

    }

    private void AddState()
    {
        playerFSM3D.AddState(E_PlayerStateType.Idle, new IdleState3D());
        playerFSM3D.AddState(E_PlayerStateType.Move, new MoveState3D());
    }

    private void AddTransition()
    {
        playerFSM3D.AddTransition(E_PlayerStateType.Idle, E_PlayerStateType.Move, () =>
        {
            return input != null && (input.haveMoveInput);
        });
        playerFSM3D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Idle, () =>
        {
            return playerBlackboard.moveSpeed <= 0;
        });
    }

    private void Update()
    {
        playerFSM3D.OnUpdate();
        PlayerPositionManage.playerPosition.WritePosition(transform.position.x, transform.position.y);
    }
}
