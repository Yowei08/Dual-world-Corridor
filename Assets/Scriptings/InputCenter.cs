using UnityEngine;

/// <summary>
/// 世界转换委托
/// </summary>
public delegate void SwitchWorld();

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

    // 世界切换事件
    public event SwitchWorld switchWorld;

    private void Start()
    {
        
    }

    private void Update()
    {
        haveMoveInput = CheckMoveInput();
        SwitchWorld();
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
            switchWorld?.Invoke();
        }
    }

    public void AddEvent(SwitchWorld switchWorld)
    {
        this.switchWorld += switchWorld;
    }
}
