// SwordContracts.cs
// 近战可击打物统一接口。玩家的挥砍脚本只要：
//   hit.collider.GetComponentInParent<ISwordHittable>()?.OnSwordHit(damage, point);

using UnityEngine;

public interface ISwordHittable
{
    void OnSwordHit(int damage, Vector3 point);
}
