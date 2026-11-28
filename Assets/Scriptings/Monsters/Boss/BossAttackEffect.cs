using UnityEngine;

public class BossAttackEffect : MonoBehaviour
{
    private bool hasAttacked = false;
    private int damage = 1;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnCollisionEnter(Collider obj)
    {
        if (hasAttacked)
        {
            return;
        }
    }
}
