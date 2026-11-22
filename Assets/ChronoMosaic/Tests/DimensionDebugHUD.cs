// DimensionDebugHUD.cs
// 调试面板：一眼看到 3D/2D、混合值、能不能切、会吸附到哪个 X。
// 左上角显示，Esc 隐藏/显示。正式包里记得删。

using UnityEngine;

public class DimensionDebugHUD : MonoBehaviour
{
    public bool visible = true;
    private float fps;

    private void Update()
    {
        fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);
        if (JamInput.CancelDown) visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible) return;
        DimensionTransitionController ctrl = DimensionTransitionController.Instance;

        GUILayout.BeginArea(new Rect(12f, 12f, 300f, 200f), GUI.skin.box);
        GUILayout.Label("<b>Chrono-Mosaic 调试</b>", Rich());
        GUILayout.Label("FPS: " + Mathf.RoundToInt(fps), Rich());

        if (ctrl == null)
        {
            GUILayout.Label("场景里没有 DimensionTransitionController", Rich());
            GUILayout.EndArea();
            return;
        }

        string dim = ctrl.is2D
            ? "<color=#F5B942>2D 过去</color>"
            : "<color=#6FD3FF>3D 现在</color>";
        GUILayout.Label("维度: " + dim, Rich());
        GUILayout.Label("混合值 _DimBlend: " + ctrl.blend.ToString("0.00"), Rich());
        GUILayout.Label("转场中: " + (ctrl.busy ? "是" : "否"), Rich());

        if (ctrl.footRing != null)
        {
            string can = ctrl.footRing.CanSwitch
                ? "<color=#38BDF8>可切换</color>"
                : "<color=#EF4444>禁切</color>";
            GUILayout.Label("落点: " + can, Rich());
            GUILayout.Label("吸附 X: " + ctrl.footRing.SnapX.ToString("0.00")
                + "  宽 " + ctrl.footRing.platformWidth.ToString("0.0") + " m", Rich());
        }

        GUILayout.Label("WASD 移动 / 鼠标转视角 / Q 切换", Rich());
        GUILayout.EndArea();
    }

    private static GUIStyle Rich()
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.richText = true;
        s.fontSize = 13;
        return s;
    }
}
