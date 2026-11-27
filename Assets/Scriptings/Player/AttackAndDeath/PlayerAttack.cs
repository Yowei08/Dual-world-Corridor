using UnityEngine;

// 玩家攻击脚本，player3D专属
public class PlayerAttack : MonoBehaviour
{
    // 玩家攻击力
    private int damage = 1;
    // 攻击冷却时间
    private float CD = 0.4f;
    private float nextAttackTime = 0f;
    // 攻击范围
    private float size = 2.2f;
    // 攻击角度
    private float angle = 110;
    // 顿帧时间
    private float pause = 0.05f;

    // 攻击判定生成位置
    public Transform point;
    private InputCenter input { get { return InputCenter.Instance; }  }


    private void Start()
    {
        if (point == null)
        {
            point = FindAnyObjectByType<PlayerController3D>().transform;
        }   
    }

    private void Update()
    {
        if (input.attack && Time.time >= nextAttackTime)
        {
            Attack();
        }
    }

    // 攻击方法，生成攻击判定
    private void Attack()
    {
        // CD计时
        nextAttackTime = Time.time + CD;

        // 初步筛选范围内物体
        Collider[] hits = Physics.OverlapSphere(point.position, size);
        // 精细筛选扇形内物体
        float halfAngle = angle * 0.5f;
        foreach (Collider hit in hits)
        {
            // 计算攻击点指向物体的方向
            Vector3 dirToMonster = (hit.transform.position - point.position).normalized;
            // 计算方向与玩家正面方向的夹角
            float angle = Vector3.Angle(transform.forward, dirToMonster);
            // 在范围内
            if (angle <= halfAngle)
            {
                // 击中敌人的情况
                if (hit.TryGetComponent<GetHit>(out GetHit component))
                {
                    component.TakeDamage(damage);
                }

                // 击中交互物品的情况

            }
        }
    }
}
