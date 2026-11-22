// WaterSprinkler.cs —— 2D 里的铜花洒
// 策划案 §5.1：2D 推花洒给 Z = 17.5 的树苗浇水。
// 玩家按 E 推，花洒沿 Z 滑到树苗旁，出水 → 树喝水 → 玻璃碎、3D 长桥。
//
// 为什么不做成"按一下就浇水"：推的过程本身就是玩家理解"时间因果"的节拍。

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterSprinkler : MonoBehaviour
{
    [Header("规则")]
    public DimensionMode pushIn = DimensionMode.Past2D;
    public GrowthTree tree;
    [Tooltip("推到哪个 Z（= 树苗位置）")]
    public float targetZ = 15.8f;
    public float slideSpeed = 2.5f;

    [Header("表现")]
    public Transform spout;
    public LineRenderer stream;
    public float streamTime = 1.2f;

    public bool Pushed { get; private set; }
    public bool Arrived { get; private set; }

    private float streamUntil = -1f;

    public bool CanPush
    {
        get { return !Pushed && DimensionRules.ActiveIn(pushIn) && !DimensionRules.Busy; }
    }

    public void Push()
    {
        if (!CanPush) return;
        Pushed = true;
        streamUntil = Time.time + streamTime;
        if (stream != null) stream.enabled = true;
    }

    private void Update()
    {
        if (!Pushed) return;

        Vector3 p = transform.position;
        p.z = Mathf.MoveTowards(p.z, targetZ, slideSpeed * Time.deltaTime);
        transform.position = p;

        if (stream != null)
        {
            if (Time.time < streamUntil && spout != null)
            {
                stream.SetPosition(0, spout.position);
                stream.SetPosition(1, spout.position + Vector3.forward * 0.9f + Vector3.down * 0.35f);
            }
            else if (stream.enabled)
            {
                stream.enabled = false;
            }
        }

        if (!Arrived && Mathf.Abs(p.z - targetZ) < 0.05f)
        {
            Arrived = true;
            if (tree != null) tree.Water();
        }
    }
}
