// GrowthTree.cs —— 十年树木
// 策划案 §5.1 第 2 段：
//   Z ∈ [18,24] 的 2D 红毯上立着一堵完好的【玻璃隔断墙】挡住去路；
//   在 2D 把【花洒】推到 Z=17.5 的【树苗】上浇水 →
//   切回 3D，玻璃已碎，树苗长成【参天枯木桥】，沿枯木走过。
//
// 关键在"因果要看得见"：水在"过去"浇下去，树在"现在"已经长了十年。
// 所以桥只在 3D 出现（Present3D_Only），2D 里永远是那棵小树苗。

using UnityEngine;
using UnityEngine.Events;

public class GrowthTree : MonoBehaviour
{
    [Header("规则")]
    [Tooltip("只有在哪个维度才能浇水")]
    public DimensionMode waterIn = DimensionMode.Past2D;
    [Tooltip("浇水后，切回 3D 时长出枯木桥要多久")]
    public float growDuration = 1.0f;
    [Tooltip("桥长到多少比例才允许踩上去（防止半截树干上站人）")]
    [Range(0f, 1f)] public float walkableAt = 0.35f;

    [Header("物件")]
    [Tooltip("2D 树苗（Past2D_Only）")]
    public GameObject sapling2D;
    [Tooltip("2D 玻璃隔断墙（Past2D_Only），浇水后碎裂")]
    public GameObject glassWall2D;
    [Tooltip("玻璃墙的碰撞体，碎裂前挡路")]
    public Collider glassCollider;
    [Tooltip("3D 参天枯木桥（Present3D_Only），浇水后才出现")]
    public GameObject bridge3D;
    [Tooltip("枯木桥碰撞体，长到 walkableAt 才启用")]
    public Collider bridgeCollider;

    [Header("表现")]
    [Tooltip("玻璃碎裂视觉停留时间，之后整块隐藏")]
    public float shatterHold = 0.15f;
    public AudioSource audioSource;
    public AudioClip waterClip;
    public AudioClip shatterClip;
    public AudioClip growClip;

    [Header("事件")]
    public UnityEvent onWatered;
    public UnityEvent onGlassShattered;
    public UnityEvent onGrown;

    public bool IsWatered { get; private set; }
    public bool GlassShattered { get; private set; }
    /// <summary>0~1，枯木桥的成长进度</summary>
    public float Grow01 { get; private set; }

    private Vector3 bridgeScale = Vector3.one;
    private float grown;
    private float shatterTimer = -1f;
    private bool growEventFired;

    private void Awake()
    {
        if (bridge3D != null) bridgeScale = bridge3D.transform.localScale;
        Apply();
    }

    private void Update()
    {
        // 玻璃：先关碰撞（立刻生效），再延迟隐藏网格，读起来才像"碎了"
        if (shatterTimer >= 0f)
        {
            shatterTimer += Time.deltaTime;
            if (shatterTimer >= shatterHold)
            {
                shatterTimer = -1f;
                if (glassWall2D != null) glassWall2D.SetActive(false);
            }
        }

        // 生长只发生在 3D（2D 是"过去"，树还没长）
        if (IsWatered && !DimensionRules.Is2D && grown < growDuration)
            grown = Mathf.Min(growDuration, grown + Time.deltaTime);

        Grow01 = growDuration > 0f ? Mathf.Clamp01(grown / growDuration) : 1f;
        if (bridge3D != null && IsWatered && !DimensionRules.Is2D)
            bridge3D.transform.localScale = bridgeScale * Mathf.SmoothStep(0f, 1f, Grow01);

        if (!growEventFired && IsWatered && Grow01 >= 1f)
        {
            growEventFired = true;
            if (audioSource != null && growClip != null) audioSource.PlayOneShot(growClip);
            if (onGrown != null) onGrown.Invoke();
        }

        Apply();
    }

    private void Apply()
    {
        bool is2D = DimensionRules.Is2D;

        if (sapling2D != null) sapling2D.SetActive(is2D);

        bool bridgeOn = !is2D && IsWatered;
        if (bridge3D != null && bridge3D.activeSelf != bridgeOn) bridge3D.SetActive(bridgeOn);
        if (bridgeCollider != null) bridgeCollider.enabled = bridgeOn && Grow01 >= walkableAt;

        if (glassWall2D != null)
        {
            bool glassOn = is2D && !IsWatered && shatterTimer < 0f;
            if (glassOn && !glassWall2D.activeSelf) glassWall2D.SetActive(true);
        }
        if (glassCollider != null) glassCollider.enabled = is2D && !IsWatered;
    }

    /// <summary>由花洒调用。</summary>
    public void Water()
    {
        if (IsWatered) return;
        if (!DimensionRules.ActiveIn(waterIn)) return;

        IsWatered = true;
        grown = 0f;
        if (audioSource != null && waterClip != null) audioSource.PlayOneShot(waterClip);
        if (onWatered != null) onWatered.Invoke();

        ShaterGlass();
        Apply();
    }

    private void ShaterGlass()
    {
        if (GlassShattered) return;
        GlassShattered = true;
        shatterTimer = 0f;
        if (glassCollider != null) glassCollider.enabled = false;
        if (audioSource != null && shatterClip != null) audioSource.PlayOneShot(shatterClip);
        if (onGlassShattered != null) onGlassShattered.Invoke();
    }

    [ContextMenu("调试：直接浇水")]
    private void DebugWater() { Water(); }
}
