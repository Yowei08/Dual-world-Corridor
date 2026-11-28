using UnityEngine;

/// <summary>
/// 攻击特效：光束（激光横扫 / 蓄力激光）
/// 挂在特效 prefab 根节点上，由 BossAttack 驱动；长度由外部给（会被墙挡住）
/// </summary>
public class BossAttackBeamVisual : MonoBehaviour
{
    [Tooltip("光束主线条（留空自动在子物体里找）")]
    public LineRenderer line;

    [Tooltip("可选的外发光线条")]
    public LineRenderer glow;

    private void Awake()
    {
        Resolve();
    }

    private void Resolve()
    {
        if (line == null)
        {
            LineRenderer[] all = GetComponentsInChildren<LineRenderer>(true);

            if (all.Length > 0)
            {
                line = all[0];
            }

            if (all.Length > 1 && glow == null)
            {
                glow = all[1];
            }
        }
    }

    // 从 from 画到 to，两段线一起更新
    public void Draw(Vector3 from, Vector3 to)
    {
        Resolve();
        SetLine(line, from, to);
        SetLine(glow, from, to);
    }

    private void SetLine(LineRenderer target, Vector3 from, Vector3 to)
    {
        if (target == null)
        {
            return;
        }

        target.positionCount = 2;
        target.SetPosition(0, from);
        target.SetPosition(1, to);
    }

    public void SetColor(Color c)
    {
        Resolve();

        if (line != null)
        {
            line.startColor = c;
            line.endColor = c;
        }

        if (glow != null)
        {
            Color g = c;
            g.a *= 0.35f;
            glow.startColor = g;
            glow.endColor = g;
        }
    }

    public void Hide()
    {
        Resolve();

        if (line != null) line.positionCount = 0;
        if (glow != null) glow.positionCount = 0;
    }
}