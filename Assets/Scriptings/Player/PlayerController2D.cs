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

    private void Start()
    {
        playerBlackboard = new PlayerBlackboard();
        playerFSM2D = new FSM<E_PlayerStateType, PlayerBlackboard>(playerBlackboard);

        // 初始化playerBlackboard
        playerBlackboard.player2D = player2D;

        AddState();
        AddTransition();
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
            return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
        });
        playerFSM2D.AddTransition(E_PlayerStateType.Move, E_PlayerStateType.Idle, () =>
        {
            return playerBlackboard.moveSpeed <= 0;
        });
    }

    private void Update()
    {
        playerFSM2D.OnUpdate();
    }
}
