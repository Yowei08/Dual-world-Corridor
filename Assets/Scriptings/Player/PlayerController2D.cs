using UnityEngine;

/// <summary>
/// Player2D控制器
/// </summary>
public class PlayerController2D : MonoBehaviour
{
    private int moveDir = 0; // 玩家移动方向(-1为向左，1为向右）
    private float moveSpeed = PlayerBlackboard.moveSpeed; // 玩家移动速度

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.D))
        {
            moveDir = SetMoveDir(1);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            moveDir = SetMoveDir(-1);
        }
        else
        {
            moveDir = SetMoveDir(0);
        }
        Move();
    }

    // 设置玩家移动方向
    public int SetMoveDir(int dir)
    {
        return dir;
    }

    // 玩家移动方法
    public void Move()
    {
        transform.position += new Vector3(moveDir, 0, 0) * moveSpeed * Time.deltaTime;
        PlayerPositionManage.playerPosition.WritePosition(transform.position.x, transform.position.y);
    }
}
