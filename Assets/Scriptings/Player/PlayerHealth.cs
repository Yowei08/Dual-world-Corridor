using UnityEngine;

public class PlayerHealth : MonoBehaviour, GetHit
{
    private static int health = 3;
    private float limit = -5f ;
    public bool is3D;
    public GameObject player;

    private void Start()
    {
        
    }

    private void Update()
    {
        Falling();
    }

    public void Falling()
    {
        if (player == null)
        {
            return;
        }

        if (player.transform.position.y <= limit)
        {
            if (health > 1)
            {
                SetPosition(false);
            }
            TakeDamage(1);
        }
    }

    public void TakeDamage(int damage)
    {
        if (health > 0)
        {
            health -= damage;
        }
        else
        {
            PlayerDie();
            SetPosition(true);
            health = 3;
        }
    }

    private void SetPosition(bool isStart)
    {
        Vector3 pos = PlayerSavePointManage.Instance.Get(isStart).position;
        player.SetActive(false);
        bool haveSet = false;
        // 表示2D
        if (is3D == false)
        {
            player.transform.position = new Vector3(pos.x, pos.y, player.transform.position.z);
            haveSet = true;
        }
        // 表示3D
        if (is3D == true)
        {
            player.transform.position = pos;
            haveSet = true;
        }
        // 暂时关闭玩家逻辑，进行传送，防止位移异常
        if (haveSet)
        {
            player.SetActive(true);
        }
    }

    private void PlayerDie()
    {
        EventManage.Instance.UseDieEvent();
    }
}
