// LightReceiver.cs
// 聚光棱镜的接收水晶：被光束照到 → onLit；光束移开 → onUnlit。

using UnityEngine;
using UnityEngine.Events;

public class LightReceiver : MonoBehaviour
{
    public UnityEvent onLit;
    public UnityEvent onUnlit;

    [Header("表现")]
    public Renderer crystalRenderer;
    public Color dimColor = new Color(0.25f, 0.3f, 0.35f);
    public Color litColor = new Color(1f, 0.85f, 0.35f);

    public bool IsLit { get; private set; }

    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        SetColor(dimColor, 0f);
    }

    public void Receive(bool lit)
    {
        if (lit == IsLit) return;
        IsLit = lit;
        SetColor(lit ? litColor : dimColor, lit ? 1.2f : 0f);
        if (lit) { if (onLit != null) onLit.Invoke(); }
        else { if (onUnlit != null) onUnlit.Invoke(); }
    }

    private void SetColor(Color c, float emission)
    {
        if (crystalRenderer == null) return;
        crystalRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        mpb.SetColor(ColorId, c);
        mpb.SetColor(EmissionId, c * emission);
        crystalRenderer.SetPropertyBlock(mpb);
    }
}
