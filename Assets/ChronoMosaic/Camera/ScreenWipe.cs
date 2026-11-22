// ScreenWipe.cs
// 全屏水彩遮罩：0 → 1 → 0 的一次性播放，峰值正好盖住 0.25 s 的相机硬切。
// 挂在 Canvas 下的全屏 Image 上（Image 需要覆盖整个屏幕，颜色随意）。
// 有材质就驱动 _Progress（做不规则水彩边缘），没有就退化成整屏透明度。

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenWipe : MonoBehaviour
{
    public CanvasGroup group;
    public Image overlay;
    [Tooltip("可选：带 _Progress 的水彩遮罩材质；为空则用纯色淡入淡出")]
    public Material wipeMaterial;

    [Tooltip("归一化时间 → 透明度。0.32~0.62 保持全遮，确保换相机那帧看不见")]
    public AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.32f, 1f),
        new Keyframe(0.62f, 1f),
        new Keyframe(1f, 0f));

    private Coroutine running;
    private Material runtimeMaterial;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (overlay == null) overlay = GetComponent<Image>();
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
        if (wipeMaterial != null && overlay != null)
        {
            runtimeMaterial = new Material(wipeMaterial);
            overlay.material = runtimeMaterial;
        }
    }

    public void Play(float duration)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run(duration));
    }

    private IEnumerator Run(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            Apply(curve.Evaluate(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        Apply(0f);
        running = null;
    }

    private void Apply(float v)
    {
        if (group != null) group.alpha = v;
        if (runtimeMaterial != null) runtimeMaterial.SetFloat("_Progress", v);
        if (overlay != null && runtimeMaterial == null)
        {
            Color c = overlay.color;
            c.a = v;
            overlay.color = c;
        }
    }
}
