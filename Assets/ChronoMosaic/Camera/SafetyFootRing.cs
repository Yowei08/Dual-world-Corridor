// SafetyFootRing.cs
// 脚下的“安全光圈”：沿整条 X 轴做步进地面探测，决定现在能不能切回 3D、会落在哪。
// 规则完全按策划案 2.2：优先原 X → 否则吸到最近的完整平台 → 否则禁切。
//
// 用法：挂在一个空物体上（或玩家子物体），把 Ring 指向脚下光圈 Renderer。

using UnityEngine;

public class SafetyFootRing : MonoBehaviour
{
    [Header("探测主体")]
    public Transform probe;
    [Tooltip("probe 原点不在脚底时用来补偿（脚底为正）")]
    public float feetOffset = 0f;

    [Header("判定层")]
    [Tooltip("3D 可落点：只有两个维度都实心的地面才算数（Shared_Both）")]
    public LayerMask groundMask = ~0;
    [Tooltip("脚下“有东西”的层：站住即可，2D 红毯也算。不填则等于 groundMask")]
    public LayerMask standMask = 0;
    [Tooltip("头顶净空检测用的遮挡层（墙体、机关）")]
    public LayerMask obstructionMask = ~0;

    [Header("探测范围")]
    [Tooltip("房间半宽，第一展厅是 10")]
    public float halfWidth = 10f;
    public float step = 0.25f;
    public float probeUp = 1.2f;
    public float probeDown = 2.2f;
    public float probeRadius = 0.3f;
    [Tooltip("命中面与脚底的高度差在此范围内视为同一平面")]
    public float samePlaneTolerance = 0.45f;
    [Tooltip("窄于此宽度的平台不参与吸附，避免站到栏杆上")]
    public float minPlatformWidth = 0.7f;
    public float headroomHeight = 1.7f;
    public float headroomRadius = 0.3f;

    [Header("光圈表现")]
    public Renderer ringRenderer;
    [Tooltip("额外同步颜色的部件：2D 侧向的那一片，保证两个视角都能看到光圈")]
    public Renderer[] additionalRingRenderers;
    public Color safeColor = new Color(0.22f, 0.78f, 1f, 1f);
    public Color blockedColor = new Color(0.55f, 0.12f, 0.12f, 1f);
    public float pulseSpeed = 6f;
    public float updateInterval = 0.05f;

    [Header("状态（只读）")]
    public bool canSwitch;
    public bool hasTarget;
    public float snapX;
    public float platformWidth;

    public bool CanSwitch { get { return canSwitch; } }
    public bool HasTarget { get { return hasTarget; } }
    public float SnapX { get { return snapX; } }

    private float timer;
    private float deniedUntil;
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (probe == null) return;
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;
        Evaluate();
    }

    /// <summary>转场被拒绝时调用，让光圈红闪一下。</summary>
    public void FlashDenied()
    {
        deniedUntil = Time.time + 0.45f;
    }

    /// <summary>立即重算一次（切维度后建议调用）。</summary>
    public void RefreshNow()
    {
        timer = 0f;
        Evaluate();
    }

    private void Evaluate()
    {
        float y = probe.position.y + feetOffset;
        float z = probe.position.z;
        float currentX = probe.position.x;

        if (!IsGrounded(currentX, y, z))
        {
            Apply(false, 0f, 0f);
            return;
        }

        bool inRun = false;
        float runStart = 0f;
        float bestScore = float.MaxValue;
        float bestCenter = 0f;
        float bestWidth = 0f;
        bool found = false;

        for (float x = -halfWidth; x <= halfWidth + 0.0001f; x += step)
        {
            bool ok = HasGroundAt(x, y, z);
            if (ok && !inRun)
            {
                inRun = true;
                runStart = x;
            }

            bool isLast = x + step > halfWidth + 0.0001f;
            if (inRun && (!ok || isLast))
            {
                float runEnd = ok ? x : x - step;
                float width = Mathf.Max(0f, runEnd - runStart) + step;
                float center = (runStart + runEnd) * 0.5f;

                if (width >= minPlatformWidth && HasHeadroom(center, y, z))
                {
                    // 包含当前 X 的平台优先级最高（保证“优先保留原 X”）
                    bool contains = Mathf.Abs(currentX - center) <= width * 0.5f;
                    float score = Mathf.Abs(center - currentX) + (contains ? -1000f : 0f);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCenter = center;
                        bestWidth = width;
                        found = true;
                    }
                }
                inRun = false;
            }
        }

        Apply(found, bestCenter, bestWidth);
    }

    private void Apply(bool ok, float center, float width)
    {
        canSwitch = ok;
        hasTarget = ok;
        snapX = center;
        platformWidth = width;

        bool denied = Time.time < deniedUntil;
        Color c = (ok && !denied) ? safeColor : blockedColor;
        if (ok && !denied)
        {
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * pulseSpeed);
            c *= pulse;
        }

        Paint(ringRenderer, c);
        if (additionalRingRenderers != null)
        {
            for (int i = 0; i < additionalRingRenderers.Length; i++) Paint(additionalRingRenderers[i], c);
        }
    }

    private void Paint(Renderer target, Color c)
    {
        if (target == null) return;
        // Awake 之前也可能被外部调用（例如别的脚本在自己 Awake 里 RefreshNow）
        if (mpb == null) mpb = new MaterialPropertyBlock();
        target.enabled = true;
        target.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        mpb.SetColor(ColorId, c);
        mpb.SetColor(EmissionColorId, c * 0.6f);
        target.SetPropertyBlock(mpb);
    }

    private bool IsGrounded(float x, float y, float z)
    {
        // 站得稳 ≠ 有 3D 落点：2D 里站在红毯上也必须能按 Q（否则断桥关永远过不去）
        LayerMask stand = standMask.value != 0 ? standMask : groundMask;
        return Physics.Raycast(new Vector3(x, y + 0.25f, z), Vector3.down, 0.6f,
            stand, QueryTriggerInteraction.Ignore);
    }

    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private readonly Collider[] headroomHits = new Collider[8];

    private bool HasGroundAt(float x, float y, float z)
    {
        Vector3 origin = new Vector3(x, y + probeUp, z);
        int count = Physics.SphereCastNonAlloc(origin, probeRadius, Vector3.down, groundHits,
            probeUp + probeDown, groundMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider == null) continue;
            if (IsSelf(hit.collider.transform)) continue;   // 别把自己的胶囊当成地面
            if (Mathf.Abs(hit.point.y - y) <= samePlaneTolerance) return true;
        }
        return false;
    }

    private bool HasHeadroom(float x, float y, float z)
    {
        Vector3 bottom = new Vector3(x, y + headroomRadius + 0.05f, z);
        Vector3 top = new Vector3(x, y + headroomHeight, z);
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, headroomRadius, headroomHits,
            obstructionMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = headroomHits[i];
            if (c == null) continue;
            if (IsSelf(c.transform)) continue;   // 别把玩家自己当障碍物
            return false;
        }
        return true;
    }

    private bool IsSelf(Transform t)
    {
        if (probe == null || t == null) return false;
        return t == probe || t.IsChildOf(probe);
    }

    private void OnDrawGizmosSelected()
    {
        if (probe == null) return;
        float y = probe.position.y + feetOffset;
        Gizmos.color = canSwitch ? Color.cyan : Color.red;
        Gizmos.DrawLine(new Vector3(probe.position.x, y, probe.position.z),
                        new Vector3(snapX, y, probe.position.z));
        if (hasTarget) Gizmos.DrawWireSphere(new Vector3(snapX, y, probe.position.z), 0.4f);
    }
}
