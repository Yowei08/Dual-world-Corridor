using System.Collections;
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
    private bool isPausing;

    // 攻击判定生成位置
    public Transform point;
    private InputCenter input { get { return InputCenter.Instance; }  }


    private void Start()
    {
        // 设置攻击判定坐标点
        if (point == null)
        {
            point = FindFirstObjectByType<PlayerController3D>().transform;
        }   
    }

    private void Update()
    {
        if (input.attack && Time.time >= nextAttackTime)
        {
            Debug.Log("Attack");
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
            // 忽略玩家自身
            if (hit.TryGetComponent<PlayerHealth>(out PlayerHealth self))
            {
                continue;
            }

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
                    Pause();
                }

                // 击中交互物品的情况

            }
        }
    }

    // 顿帧管理
    private void Pause()
    {
        if (!isPausing)
        {
            StartCoroutine(PauseCoroutine());
        }
    }

    private IEnumerator PauseCoroutine()
    {
        isPausing = true;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(pause);
        Time.timeScale = originalTimeScale;
        isPausing = false;
    }

    // 让攻击范围可见：选中这个组件（或在 Scene 视图打开 Gizmos）就能看到
    private void OnDrawGizmosSelected()
    {
        DrawAttackRange();
    }

    /// <summary>
    /// 画出攻击范围：判定球（size）+ 正面 angle 度的锥形
    /// 和 Attack() 用的完全是同一组数据（point / size / angle / transform.forward）
    /// </summary>
    public void DrawAttackRange()
    {
        Vector3 origin = point != null ? point.position : transform.position;
        Vector3 forward = transform.forward;
        Vector3 up = transform.up;
        float half = angle * 0.5f;

        // 判定球
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.25f);
        Gizmos.DrawSphere(origin, size);
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 1f);
        Gizmos.DrawWireSphere(origin, size);

        // 锥形的四条边界线（水平 + 垂直两个剖面，对应 Vector3.Angle 的立体角度）
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 1f);
        Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(-half, up) * forward * size);
        Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(half, up) * forward * size);
        Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(-half, transform.right) * forward * size);
        Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(half, transform.right) * forward * size);

        // 水平面上的弧线
        int steps = 24;
        Vector3 prev = origin + Quaternion.AngleAxis(-half, up) * forward * size;

        for (int i = 1; i <= steps; i++)
        {
            float a = Mathf.Lerp(-half, half, (float)i / steps);
            Vector3 cur = origin + Quaternion.AngleAxis(a, up) * forward * size;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
}
