using UnityEngine;

/// <summary>
/// boss脚本
/// </summary>
public class Boss : Monster
{

    private void Start()
    {
        health = 3;
        damage = 1;
    }

    private void Update()
    {
        //monsterFSM.OnUpdate();
    }

    public override void TakeDamage(int damage)
    {
        if (health > 0)
        {
            health -= damage;
            Debug.Log(health);
        }
        else
        {
            Die();
        }
    }

    public override void Die()
    {
        
    }
}
