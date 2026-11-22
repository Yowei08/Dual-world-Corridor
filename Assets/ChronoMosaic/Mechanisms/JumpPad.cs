// JumpPad.cs
// 弹簧板：站在上面就把玩家弹起来。按维度开关（策划案里 2D 才能踩）。
// 用法：把本脚本挂在一个带 Trigger Collider 的物体上，玩家根节点实现 IJumpPadReceiver。

using UnityEngine;
using UnityEngine.Events;

/// <summary>能被弹簧板弹起来的东西（一般就是玩家控制器）。</summary>
public interface IJumpPadReceiver
{
    /// <summary>请求以指定高度（米）起跳，实现方自己换算成初速度。</summary>
    void Launch(float heightMeters);
}

[DisallowMultipleComponent]
public class JumpPad : MonoBehaviour
{
    [Header("规则")]
    public DimensionMode activeIn = DimensionMode.Past2D;
    [Tooltip("弹起高度（米），3.2 大约等于两次跳")]
    public float launchHeight = 3.2f;
    [Tooltip("两次触发的最短间隔，防止一帧内连弹")]
    public float rearmTime = 0.45f;

    [Header("表现")]
    public Transform padVisual;
    public float compressDepth = 0.12f;
    public Renderer padRenderer;
    public Color idleColor = new Color(0.30f, 0.55f, 0.45f);
    public Color fireColor = new Color(0.45f, 1f, 0.78f);

    [Header("事件")]
    public UnityEvent onFire;

    public int FireCount { get; private set; }
    public bool Ready { get { return DimensionRules.ActiveIn(activeIn) && Time.time >= nextFireTime; } }

    private float nextFireTime;
    private float flashUntil;
    private Vector3 homePos;
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (padVisual != null) homePos = padVisual.localPosition;
        mpb = new MaterialPropertyBlock();
        Paint(idleColor);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!DimensionRules.ActiveIn(activeIn)) return;
        if (Time.time < nextFireTime) return;

        IJumpPadReceiver receiver = other.GetComponentInParent<IJumpPadReceiver>();
        if (receiver == null) return;

        nextFireTime = Time.time + rearmTime;
        FireCount++;
        flashUntil = Time.time + 0.18f;
        receiver.Launch(launchHeight);
        if (onFire != null) onFire.Invoke();
    }

    private void Update()
    {
        if (padVisual != null)
        {
            bool pressed = Time.time < flashUntil;
            Vector3 target = pressed ? homePos + Vector3.down * compressDepth : homePos;
            padVisual.localPosition = Vector3.Lerp(padVisual.localPosition, target,
                1f - Mathf.Exp(-18f * Time.deltaTime));
        }

        Paint(Time.time < flashUntil ? fireColor : idleColor);
    }

    private void Paint(Color c)
    {
        if (padRenderer == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        padRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        mpb.SetColor(ColorId, c);
        padRenderer.SetPropertyBlock(mpb);
    }
}
