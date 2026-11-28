using UnityEngine;

/// <summary>
/// 攻击特效：扩散光环（砸地面）
/// 挂在特效 prefab 根节点上，由 BossAttack 驱动
/// </summary>
public class BossAttackRingVisual : MonoBehaviour
{
    [Tooltip("画环用的 LineRenderer（留空自动在子物体里找）")]
    public LineRenderer line;

    [Tooltip("3D 时环画在 XZ 平面，2D 时画在 XY 平面")]
    public bool is3D = true;

    [Tooltip("离地高度，避免和地面 z-fighting")]
    public float yOffset = 0.08f;

    private const int Steps = 64;

    private void Awake()
    {
        if (line == null)
        {
            line = GetComponentInChildren<LineRenderer>();
        }
    }

    // 画一个从中心扩散的圆环
    public void Draw(Vector3 center, float radius)
    {
        if (line == null)
        {
            return;
        }

        line.positionCount = Steps + 1;

        for (int i = 0; i <= Steps; i++)
        {
            float a = Mathf.PI * 2f * i / Steps;
            Vector3 p;

            if (is3D)
            {
                p = center + new Vector3(Mathf.Cos(a) * radius, yOffset, Mathf.Sin(a) * radius);
            }
            else
            {
                p = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            }

            line.SetPosition(i, p);
        }
    }

    public void SetColor(Color c)
    {
        if (line == null)
        {
            return;
        }

        line.startColor = c;
        line.endColor = c;
    }

    public void Hide()
    {
        if (line != null)
        {
            line.positionCount = 0;
        }
    }
}