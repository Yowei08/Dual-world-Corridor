// TestSword.cs
// 只用于"能挥刀"的最小攻击脚本：左键在身前打一个球，命中所有 ISwordHittable。
// 正式项目里换成你们自己的连招系统，然后把本文件删掉。

using UnityEngine;

public class TestSword : MonoBehaviour
{
    public float reach = 1.6f;
    public float radius = 0.9f;
    public int damage = 1;
    public LayerMask mask = ~0;

    public int SwingCount { get; private set; }
    public int HitCount { get; private set; }

    private void Update()
    {
        if (DimensionTransitionController.IsBusy) return;
        if (JamInput.AttackDown) Swing();
    }

    /// <summary>挥一刀，返回命中的可击打物体数量。</summary>
    public int Swing()
    {
        SwingCount++;
        Vector3 center = transform.position + Vector3.up * 1f + transform.forward * reach * 0.6f;
        Collider[] hits = Physics.OverlapSphere(center, radius, mask, QueryTriggerInteraction.Ignore);

        int connected = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            ISwordHittable target = hits[i].GetComponentInParent<ISwordHittable>();
            if (target == null) continue;
            target.OnSwordHit(damage, hits[i].ClosestPoint(center));
            connected++;
        }
        HitCount += connected;
        return connected;
    }
}
