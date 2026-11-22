// DimensionCollider.cs
// 只在某个维度生效的碰撞体。
// 典型用法：2D 红毯桥的碰撞体，切回 3D 必须关掉，否则玩家能踩着它走过深渊。

using UnityEngine;

[DisallowMultipleComponent]
public class DimensionCollider : MonoBehaviour
{
    public DimensionMode activeIn = DimensionMode.Past2D;
    public Collider[] colliders;

    private void Reset()
    {
        colliders = GetComponentsInChildren<Collider>(true);
    }

    private void Awake()
    {
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>(true);
    }

    private void Update()
    {
        Apply(DimensionRules.Is2D);
    }

    /// <summary>显式指定维度，测试与初始化时可直接调用。</summary>
    public void Apply(bool is2D)
    {
        if (colliders == null) return;
        bool on = activeIn == DimensionMode.Both || (activeIn == DimensionMode.Past2D) == is2D;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = on;
        }
    }
}
