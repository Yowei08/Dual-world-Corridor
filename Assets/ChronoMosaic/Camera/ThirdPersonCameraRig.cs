// ThirdPersonCameraRig.cs
// 3D 模式的第三人称轨道相机：鼠标环绕 + 跟随阻尼 + 球体检测防穿墙。
// 挂在主相机上。2D 模式下整个 GameObject 会被转场控制器关掉。

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCameraRig : MonoBehaviour
{
    [Header("目标")]
    public Transform target;
    [Tooltip("以角色脚底为原点，抬到胸口高度")]
    public Vector3 pivotOffset = new Vector3(0f, 1.45f, 0f);

    [Header("环绕")]
    [Tooltip("房间只有 20m 宽，4.2m 已经会在走廊里频繁贴墙；先给 3.6")]
    public float distance = 3.6f;
    public float minDistance = 1.0f;
    public float maxDistance = 6f;
    public float yawSensitivity = 180f;
    public float pitchSensitivity = 120f;
    public Vector2 pitchLimits = new Vector2(-25f, 65f);
    public bool invertY = false;
    [Tooltip("右肩偏移，0 为正中；0.3~0.5 能看清角色与正前方")]
    public float shoulderOffset = 0.35f;

    [Header("平滑")]
    [Range(0f, 0.4f)] public float pivotDamping = 0.07f;
    [Range(0f, 0.3f)] public float distanceDamping = 0.05f;
    [Tooltip("无鼠标输入多久后镜头缓慢回到角色背后，0 = 关闭")]
    public float autoAlignDelay = 1.8f;
    public float autoAlignSpeed = 1.4f;

    [Header("碰撞")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.25f;
    [Tooltip("被墙夹住时允许的最小距离，再近就要做角色淡出")]
    public float minClippedDistance = 0.8f;

    [Header("调试")]
    public bool lookEnabled = true;
    public bool autoAlign = true;
    public bool CameraClipped { get; private set; }

    private Camera cam;
    private float yaw;
    private float pitch = 12f;
    private float currentDistance;
    private float distanceVelocity;
    private Vector3 smoothPivot;
    private Vector3 pivotVelocity;
    private float lastLookTime = -99f;

    public float Yaw { get { return yaw; } }
    public Quaternion OrbitRotation { get { return Quaternion.Euler(pitch, yaw, 0f); } }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        currentDistance = distance;
        if (target != null)
        {
            smoothPivot = target.position + pivotOffset;
            yaw = target.eulerAngles.y;
        }
    }

    /// <summary>切进 2D 前调用，冻结鼠标环绕，防止转场时镜头乱飘。</summary>
    public void SetLookEnabled(bool value)
    {
        lookEnabled = value;
    }

    /// <summary>把镜头瞬间摆到角色背后，用于重开局或穿模复位。</summary>
    public void SnapBehind()
    {
        if (target == null) return;
        yaw = target.eulerAngles.y;
        pitch = 12f;
        smoothPivot = target.position + pivotOffset;
        currentDistance = distance;
        transform.position = smoothPivot - OrbitRotation * Vector3.forward * currentDistance;
        transform.rotation = OrbitRotation;
    }

    public void AddDistance(float delta)
    {
        distance = Mathf.Clamp(distance + delta, minDistance, maxDistance);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (Time.deltaTime <= 0f) return;

        // ---- 环绕输入 ----
        if (lookEnabled)
        {
            Vector2 look = JamInput.Look;
            if (look.sqrMagnitude > 0.0001f)
            {
                yaw += look.x * yawSensitivity * Time.deltaTime;
                pitch -= (invertY ? -look.y : look.y) * pitchSensitivity * Time.deltaTime;
                lastLookTime = Time.time;
            }
        }
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        // ---- 无操作时缓慢回正到角色背后（只在角色移动时）----
        if (autoAlign && lookEnabled && Time.time - lastLookTime > autoAlignDelay)
        {
            if (JamInput.Move.sqrMagnitude > 0.01f)
            {
                float t = 1f - Mathf.Exp(-autoAlignSpeed * Time.deltaTime);
                yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, t);
            }
        }

        // ---- 枢轴跟随 ----
        Vector3 desiredPivot = target.position + pivotOffset;
        smoothPivot = Vector3.SmoothDamp(smoothPivot, desiredPivot, ref pivotVelocity, pivotDamping);

        // ---- 碰撞：从枢轴向目标位置做球扫 ----
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = rot * Vector3.back;
        Vector3 shoulder = rot * Vector3.right * shoulderOffset;
        Vector3 desired = smoothPivot + back * distance + shoulder;
        Vector3 dir = desired - smoothPivot;
        float len = dir.magnitude;

        CameraClipped = false;
        if (len > 0.01f)
        {
            dir /= len;
            RaycastHit hit;
            if (Physics.SphereCast(smoothPivot, collisionRadius, dir, out hit, len, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float safe = Mathf.Max(minClippedDistance, hit.distance - 0.05f);
                if (safe < len - 0.01f) CameraClipped = true;
                len = safe;
            }
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, len, ref distanceVelocity, distanceDamping);
        transform.position = smoothPivot + back * currentDistance + shoulder * (currentDistance / Mathf.Max(distance, 0.01f));
        transform.rotation = rot;
    }
}
