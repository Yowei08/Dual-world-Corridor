using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private static int health = 3;
    private float limit = -5f ;
    public bool p;
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
            SetPosition(true);
            health = 3;
        }
    }

    private void SetPosition(bool isStart)
    {
        Vector3 pos = PlayerSavePointManage.Manage.Get(isStart).position;
        // 表示2D
        if (p == false)
        {
            player.transform.position = new Vector3(pos.x, pos.y, player.transform.position.z);
        }
        // 表示2D
        else if (p == true)
        {
            player.transform.position = pos;
        }
    }
}
