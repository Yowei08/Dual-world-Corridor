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
    // 转换前z坐标
    private float lastPosZ = 0;

    // false = 2D true = 3D
    private bool nowWorld;


    private void Start()
    {
        groundMask = LayerMask.GetMask("Ground");

        // 初始化坐标
        if (player2D != null && player3D != null)
        {
            PlayerPositionManage.playerPosition.WritePosition(player3D.transform.position.z);
            Vector2 pos = PlayerPositionManage.playerPosition.GetPosition2D();
            player2D.transform.position = new Vector3(pos.x, pos.y, player2D.transform.position.z);
            player3D.transform.position = PlayerPositionManage.playerPosition.GetPosition3D();
            // 初始化地面记录和3D玩家z坐标记录
            lastGround = WriteLastGround();
        }

        // 加入到OnCheck事件
        WorldSwitchManage.WorldSwitch.AddCheck(GroundCheck);
        // 加入到OnSwitch事件
        WorldSwitchManage.WorldSwitch.AddEvent(SwitchWorld);
        nowWorld = WorldSwitchManage.WorldSwitch.nowWorld;

        UpdateWorld();
    }

    private void Update()
    {

    }

    // 更新玩家显示
    private void UpdateWorld()
    {
        if (player2D != null)
        {
            player2D.SetActive(!nowWorld);
        }
        if (player3D != null)
        {
            player3D.SetActive(nowWorld);
        }
    }

    // 地面检测
    private bool GroundCheck()
    {
        // 2D->3D，检测3D是否有地面
        if (nowWorld == false)
        {
            RaycastHit2D hit = Physics2D.Raycast(player2D.transform.position, Vector3.down, Mathf.Infinity, groundMask);
            // 如果找到了地面
            if (hit.collider != null)
            {
                GameObject ground = null; // ground代表2D平台指向3D的对应平台
                GameObject obj = hit.collider.gameObject;
                if (obj.TryGetComponent<Import3D>(out Import3D component))
                {
                    ground = component.object3D;
                }

                // 如果找到的2D平台有指向3D的对应平台
                if (ground != null)
                {
                    // 对比lastGround确定玩家所处平台是否变化
                    // 如果相同，玩家读取保存的z坐标
                    if (ground == lastGround)
                    {
                        PlayerPositionManage.playerPosition.WritePosition(lastPosZ);
                        return true;
                    }
                    PlayerPositionManage.playerPosition.WritePosition(ground.transform.position.z);
                    return true;
                }
            }
            return false;
        }
        // 3D->2D，检查2D是否有地面
        if (nowWorld == true)
        {
            // 当前坐标直接打入2D平面内向下发射射线寻找平台
            Vector2 pos = PlayerPositionManage.playerPosition.GetPosition2D();
            RaycastHit2D hit = Physics2D.Raycast(new Vector3(pos.x, pos.y, player2D.transform.position.z), Vector3.down, Mathf.Infinity, groundMask);
            if (hit.collider != null)
            {
                // 记录当前3D地面和3D玩家z坐标
                lastGround = WriteLastGround() != null ? WriteLastGround() : lastGround;
                return true;
            }
            return false;
        }
        return false;
    }

    // 进入2D前，记录当前3D中的地面
    private GameObject WriteLastGround()
    {
        RaycastHit hit;
        GameObject ground = null;
        if (Physics.Raycast(player3D.transform.position, Vector3.down, out hit, Mathf.Infinity, groundMask))
        {
            ground = hit.collider.gameObject;
        }
        lastPosZ = ground != null ? player3D.transform.position.z : lastPosZ;
        return ground;
    }

    // 世界切换逻辑
    private void SwitchWorld()
    {
        nowWorld = WorldSwitchManage.WorldSwitch.nowWorld;
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
