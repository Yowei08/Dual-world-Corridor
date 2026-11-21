using UnityEngine;

/// <summary>
/// 2D玩家与3D玩家转换的控制中心
/// </summary>
public class PlayerCenter : MonoBehaviour
{
    public GameObject player2D;
    public GameObject player3D;
    // false = 2D true = 3D
    private bool nowWorld = true;

    private void Start()
    {
        UpdateWorld();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchWorld();
        }
    }

    public void UpdateWorld()
    {
        player2D.SetActive(!nowWorld);
        player3D.SetActive(nowWorld);
    }

    public void SwitchWorld()
    {
        nowWorld = !nowWorld;
        // 切换到2D
        if (nowWorld == false)
        {
            Vector2 target = PlayerPositionManage.playerPosition.GetPosition2D();
            player2D.transform.position = new Vector3(target.x, target.y, player2D.transform.position.z);
        }
        // 否则切换到3D
        else
        {
            // 射线检测地板
            RaycastHit2D hit = Physics2D.Raycast(player2D.transform.position, Vector2.down);
            if (hit.collider != null)
            {
                GameObject target = hit.collider.gameObject;
                GameObject obj = target.GetComponent<Import3D>().object3D;
                PlayerPositionManage.playerPosition.WritePosition(obj.transform.position.z);
            }
            player3D.transform.position = PlayerPositionManage.playerPosition.GetPosition3D();
        }
        UpdateWorld();
    }
}
