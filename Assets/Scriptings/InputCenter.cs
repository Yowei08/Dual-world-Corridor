using UnityEngine;


/// <summary>
/// 管理所有输入
/// </summary>
public class InputCenter : MonoBehaviour
{
    private static InputCenter instance;

    // 单例：随时访问都有值（没赋值就找，找不到就建），避免字段初始化器拿到 null
    public static InputCenter Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<InputCenter>(FindObjectsInactive.Include);

            if (instance == null)
            {
                GameObject go = new GameObject("InputCenter(Auto)");
                instance = go.AddComponent<InputCenter>();
            }

            return instance;
        }
        private set
        {
            instance = value;
        }
    }

    // 移动相关
    public bool haveMoveInput = false;
    public bool moveForward = false;
    public bool moveBack = false;
    public bool moveRight = false;
    public bool moveLeft = false;

    // 跳跃相关
    public bool jump = false;

    // 玩家攻击
    public bool attack = false;
    private bool inAttacking = false;

    private void Awake()
    {
        // 全局只留一个：重复的只销毁组件，不销毁挂着的物体
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        haveMoveInput = CheckMoveInput();
        jump = CheckJumpInput();
        SwitchWorld();
        Attack();
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
            WorldSwitchManage.Instance.UseEvent();
        }
    }

    private void Attack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (inAttacking)
            {
                return;
            }
            attack = true;
            inAttacking = true;
        }
        attack = false;
    }
}
