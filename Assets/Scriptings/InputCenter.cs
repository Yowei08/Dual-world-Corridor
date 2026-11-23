using UnityEngine;


/// <summary>
/// 管理所有输入
/// </summary>
public class InputCenter : MonoBehaviour
{
    // 移动相关
    public bool haveMoveInput = false;
    public bool moveForward = false;
    public bool moveBack = false;
    public bool moveRight = false;
    public bool moveLeft = false;

    // 跳跃相关
    public bool jump = false;

    private void Start()
    {
        
    }

    private void Update()
    {
        haveMoveInput = CheckMoveInput();
        jump = CheckJumpInput();
        SwitchWorld();
    }

    private bool CheckJumpInput()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    private bool CheckMoveInput()
    {
        if (Input.GetKey(KeyCode.W))
        {
            moveForward = true;
        }
        else
            moveForward = false;
        if (Input.GetKey(KeyCode.S))
        {
            moveBack = true;
        }
        else
            moveBack = false;
        if (Input.GetKey(KeyCode.D))
        {
            moveRight = true;
        }
        else moveRight = false;
        if (Input.GetKey(KeyCode.A))
        {
            moveLeft = true;
        }
        else
            moveLeft = false;

        if (moveForward || moveBack || moveRight || moveLeft)
        {
            return true;
        }
        else
            return false;
    }

    private void SwitchWorld()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            WorldSwitchManage.WorldSwitch.UseEvent();
        }
    }
}
