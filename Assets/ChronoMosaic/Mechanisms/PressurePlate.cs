// PressurePlate.cs
// 压力踏板：踩下 → onPressed；离开 → onReleased。
// 支持只在一个维度生效（2D 机关 / 3D 机关），这是“残影双开”谜题的基础件。

using UnityEngine;
using UnityEngine.Events;

public class PressurePlate : MonoBehaviour
{
    [Header("规则")]
    public DimensionMode activeIn = DimensionMode.Both;
    [Tooltip("需要累计多少重量才触发")]
    public float requiredWeight = 1f;
    [Tooltip("触发后是否锁死（一次性机关用）")]
    public bool latch = false;

    [Header("表现")]
    public Transform plateVisual;
    public float pressDepth = 0.08f;
    public float pressSpeed = 10f;
    public Renderer plateRenderer;
    public Color idleColor = new Color(0.35f, 0.35f, 0.38f);
    public Color pressedColor = new Color(0.95f, 0.72f, 0.25f);

    [Header("事件")]
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    public bool IsPressed { get; private set; }

    private float accumulated;
    private Vector3 visualHome;
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (plateVisual != null) visualHome = plateVisual.localPosition;
        mpb = new MaterialPropertyBlock();
        ApplyColor(idleColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        PressureWeight w = other.GetComponentInParent<PressureWeight>();
        if (w == null || !w.IsActive) return;
        accumulated += w.weight;
        Refresh();
    }

    private void OnTriggerExit(Collider other)
    {
        PressureWeight w = other.GetComponentInParent<PressureWeight>();
        if (w == null) return;
        accumulated = Mathf.Max(0f, accumulated - w.weight);
        Refresh();
    }

    private void Update()
    {
        // 切维度时，另一侧的重量瞬间“不存在”了
        if (!DimensionRules.ActiveIn(activeIn) && IsPressed)
        {
            accumulated = 0f;
            Refresh();
        }

        if (plateVisual != null)
        {
            Vector3 target = IsPressed ? visualHome + Vector3.down * pressDepth : visualHome;
            plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, target,
                1f - Mathf.Exp(-pressSpeed * Time.deltaTime));
        }
    }

    private void Refresh()
    {
        if (latch && IsPressed) return;
        bool now = accumulated >= requiredWeight;
        if (now == IsPressed) return;
        IsPressed = now;
        ApplyColor(now ? pressedColor : idleColor);
        if (now) { if (onPressed != null) onPressed.Invoke(); }
        else { if (onReleased != null) onReleased.Invoke(); }
    }

    private void ApplyColor(Color c)
    {
        if (plateRenderer == null) return;
        plateRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        mpb.SetColor(ColorId, c);
        plateRenderer.SetPropertyBlock(mpb);
    }

    public void ForceRelease()
    {
        accumulated = 0f;
        Refresh();
    }
}
