using UnityEngine;

//3D进入2D需要读取2D玩家脚下的位置是否存在地面
//2D进入3D需要读取3D玩家脚下的位置是否存在地面，且读取2D脚下地面指向3D的地面对象，额外判断是否存在

/// <summary>
/// 2D玩家与3D玩家转换的控制中心
/// </summary>
public class PlayerCenter : MonoBehaviour
{
    public GameObject player2D;
    public GameObject player3D;

    private LayerMask groundMask;

    // 针对2D转换到3D的z轴判断额外内容
    // 存储切换前地面
    private GameObject lastGround;
    // 转换前坐标
    private Vector3 lastPosition = Vector3.zero;

    // false = 2D true = 3D
    private bool nowWorld = true;

    private void Start()
    {
        groundMask = LayerMask.GetMask("Ground");

        UpdateWorld();

        // 初始化坐标
        PlayerPositionManage.playerPosition.WritePosition(player3D.transform.position.z);
        Vector2 pos = PlayerPositionManage.playerPosition.GetPosition2D();
        player2D.transform.position = new Vector3(pos.x, pos.y, player2D.transform.position.z);
        player3D.transform.position = PlayerPositionManage.playerPosition.GetPosition3D();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchWorld();
        }
    }

    // 更新玩家显示
    private void UpdateWorld()
    {
        player2D.SetActive(!nowWorld);
        player3D.SetActive(nowWorld);
    }

    private bool GroundCheck()
    {
        // 2D检查3D
        if (nowWorld == false)
        {
            RaycastHit2D hit = Physics2D.Raycast(player2D.transform.position, Vector3.down, Mathf.Infinity, groundMask);
            if (hit.collider != null)
            {
                GameObject ground = null;
                GameObject obj = hit.collider.gameObject;
                if (obj.TryGetComponent<Import3D>(out Import3D component))
                {
                    ground = component.object3D;
                }
                if (ground != null)
                {
                    PlayerPositionManage.playerPosition.WritePosition(ground.transform.position.z);
                    return true;
                }
            }
            return false;
        }
        // 3D检查2D
        if (nowWorld == true)
        {
            Vector2 pos = PlayerPositionManage.playerPosition.GetPosition2D();
            RaycastHit2D hit = Physics2D.Raycast(new Vector3(pos.x, pos.y, player2D.transform.position.z), Vector3.down, Mathf.Infinity, groundMask);
            if (hit.collider != null)
            {
                return true;
            }
            return false;
        }
        return false;
    }

    public void SwitchWorld()
    {
        if (GroundCheck() == false)
        {
            return;
        }
        nowWorld = !nowWorld;
        // 切换到2D
        if (nowWorld == false)
        {
            Vector2 target = PlayerPositionManage.playerPosition.GetPosition2D();
            player2D.transform.position = new Vector3(target.x, target.y, player2D.transform.position.z);
        }
        // 切换到3D
        else
        {
            Vector3 target = PlayerPositionManage.playerPosition.GetPosition3D();
            player3D.transform.position = target;
        }
        UpdateWorld();
    }
}
