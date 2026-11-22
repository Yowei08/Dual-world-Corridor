// PrismBeam.cs
// 聚光棱镜：3D 里用手轮调角度，2D 侧视时光束才“正好”打到对面墙的水晶上。
// 判定用真实射线，视觉用 LineRenderer，两边一定对得上。

using UnityEngine;

public class PrismBeam : MonoBehaviour
{
    [Header("规则")]
    public DimensionMode activeIn = DimensionMode.Present3D;
    [Tooltip("调角度的手轮/棱镜本体")]
    public Transform pivot;
    public Transform beamOrigin;
    public float minAngle = -30f;
    public float maxAngle = 30f;
    public float angleStep = 5f;
    public float range = 40f;
    public LayerMask hitMask = ~0;

    [Header("表现")]
    public LineRenderer beamLine;
    public Light beamLight;

    [Header("状态")]
    public float currentAngle;

    private float targetAngle;
    private LightReceiver currentReceiver;

    private void Awake()
    {
        targetAngle = currentAngle;
        if (beamLine != null) beamLine.useWorldSpace = true;
    }

    private void Update()
    {
        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, 90f * Time.deltaTime);
        if (pivot != null) pivot.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

        bool active = DimensionRules.ActiveIn(activeIn);
        if (beamLine != null) beamLine.enabled = active;
        if (beamLight != null) beamLight.enabled = active;

        if (!active || beamOrigin == null)
        {
            ClearReceiver();
            return;
        }

        RaycastHit hit;
        Vector3 end = beamOrigin.position + beamOrigin.forward * range;
        LightReceiver hitReceiver = null;

        if (Physics.Raycast(beamOrigin.position, beamOrigin.forward, out hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            hitReceiver = hit.collider.GetComponentInParent<LightReceiver>();
        }

        if (beamLine != null)
        {
            beamLine.positionCount = 2;
            beamLine.SetPosition(0, beamOrigin.position);
            beamLine.SetPosition(1, end);
        }

        if (hitReceiver != currentReceiver)
        {
            ClearReceiver();
            currentReceiver = hitReceiver;
            if (currentReceiver != null) currentReceiver.Receive(true);
        }
    }

    private void ClearReceiver()
    {
        if (currentReceiver != null)
        {
            currentReceiver.Receive(false);
            currentReceiver = null;
        }
    }

    public void RotateByStep(int direction)
    {
        targetAngle = Mathf.Clamp(targetAngle + angleStep * Mathf.Sign(direction), minAngle, maxAngle);
    }

    public void SetAngle(float angle)
    {
        targetAngle = Mathf.Clamp(angle, minAngle, maxAngle);
    }

    public bool IsAligned(LightReceiver receiver)
    {
        return receiver != null && currentReceiver == receiver;
    }
}
