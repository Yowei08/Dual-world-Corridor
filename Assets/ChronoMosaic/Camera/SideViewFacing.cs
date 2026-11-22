// SideViewFacing.cs
// 2D 模式下角色是“侧面剪影”，必须始终侧对镜头，并按左右移动翻面。
// 挂在角色根节点，visual 指向模型子物体（不要指向根，否则会和移动转向打架）。

using UnityEngine;

public class SideViewFacing : MonoBehaviour
{
    public Transform visual;
    [Tooltip("模型正面朝向为 +Z 时填 0；美术做成 -Z 就填 180")]
    public float modelYawOffset = 0f;
    public float turnSpeed = 14f;

    private float facing = 1f;
    private Vector3 lastPos;
    private bool hasLast;

    private void Awake()
    {
        if (visual == null) visual = transform;
        lastPos = transform.position;
        hasLast = true;
    }

    private void LateUpdate()
    {
        if (visual == null || Time.deltaTime <= 0f) return;

        if (!DimensionTransitionController.Is2D)
        {
            // 3D 由移动脚本控制根节点朝向，视觉子物体保持中立
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.identity,
                1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            hasLast = false;
            return;
        }

        Vector3 p = transform.position;
        if (hasLast)
        {
            float dz = p.z - lastPos.z;
            if (Mathf.Abs(dz) > 0.006f) facing = dz > 0f ? 1f : -1f;
        }
        lastPos = p;
        hasLast = true;

        float yaw = modelYawOffset + (facing > 0f ? 0f : 180f);
        Quaternion target = Quaternion.Euler(0f, yaw, 0f);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, target,
            1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }
}
