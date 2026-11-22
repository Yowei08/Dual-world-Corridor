// SideViewCamera2D.cs
// 2D 正交侧视相机：固定在 +X，永远朝 -X 看，画面右 = +Z。
// 只跟随 Z（前进方向）与 Y（高度），X 恒定，保证“剖开建筑”的侧视图不会晃动。

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SideViewCamera2D : MonoBehaviour
{
    [Header("目标")]
    public Transform target;
    [Tooltip("必须大于房间最大 X，且小于所有远景背景板到相机的距离")]
    public float cameraX = 35f;

    [Header("构图")]
    [Tooltip("正交半高。12m 挑高的房间给 6.5，玩家永远落在画面下三分之一")]
    public float orthoSize = 6.5f;
    [Tooltip("镜头中心相对脚底的抬高量。13m 画幅下取 4.2，地面线落在画面下沿 15%")]
    public float heightOffset = 4.2f;
    [Tooltip("垂直限制，防止镜头拍到没做的天花板或地板下方")]
    public Vector2 verticalRange = new Vector2(4.2f, 9.0f);

    [Header("跟随")]
    [Range(0f, 0.5f)] public float followDamping = 0.14f;
    [Tooltip("朝移动方向前瞻，避免玩家看不到前方机关")]
    public float lookAhead = 2.2f;
    [Tooltip("前瞻跟随速度，越小越软")]
    public float lookAheadDamping = 0.35f;
    [Tooltip("速度换算成前瞻的系数")]
    public float lookAheadPerSpeed = 0.45f;

    private Camera cam;
    private Vector3 velocity;
    private float smoothAhead;
    private Vector3 lastTargetPos;
    private bool hasLast;

    public Camera Cam { get { return cam; } }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        if (target != null) lastTargetPos = target.position;
        hasLast = target != null;
    }

    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic) cam.orthographicSize = orthoSize;
    }

    /// <summary>切换瞬间调用，避免镜头从远处“飞过来”被玩家看到。</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        smoothAhead = 0f;
        velocity = Vector3.zero;
        Vector3 p = BuildTargetPosition(0f);
        transform.position = p;
        lastTargetPos = target.position;
        hasLast = true;
    }

    private Vector3 BuildTargetPosition(float ahead)
    {
        float y = Mathf.Clamp(target.position.y + heightOffset, verticalRange.x, verticalRange.y);
        float z = target.position.z + ahead;
        return new Vector3(cameraX, y, z);
    }

    private void LateUpdate()
    {
        if (target == null || Time.deltaTime <= 0f) return;

        // 用位移算 Z 速度，不依赖 Rigidbody，对 CharacterController 同样有效
        float vz = 0f;
        if (hasLast)
        {
            vz = (target.position.z - lastTargetPos.z) / Time.deltaTime;
        }
        lastTargetPos = target.position;
        hasLast = true;

        float wanted = Mathf.Clamp(vz * lookAheadPerSpeed, -lookAhead, lookAhead);
        smoothAhead = Mathf.Lerp(smoothAhead, wanted, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(lookAheadDamping, 0.01f)));

        Vector3 desired = BuildTargetPosition(smoothAhead);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, followDamping);
        transform.rotation = Quaternion.Euler(0f, -90f, 0f);
    }
}
