// PlayModeAutoTest.cs
// 在真正的 Play 模式下跑一遍相机/转场/落点，验证物理与协程行为。
// 菜单：Tools → Chrono Mosaic → 4. Play 模式自动测试
// 命令行：-executeMethod PlayModeAutoTest.StartTest（不要加 -quit）

#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeAutoTest
{
    private const string ActiveKey = "CM_PLAYTEST_ACTIVE";
    private const string ScenePath = "Assets/Scenes/CameraTest.unity";

    private static StringBuilder sb;
    private static int pass;
    private static int fail;
    private static bool started;
    private static float playStartTime;

    static PlayModeAutoTest()
    {
        if (SessionState.GetBool(ActiveKey, false))
        {
            EditorApplication.update += Tick;
        }
    }

    [MenuItem("Tools/Chrono Mosaic/3. Play 模式自动测试", false, 3)]
    public static void StartTest()
    {
        sb = new StringBuilder();
        pass = 0;
        fail = 0;
        started = false;

        // 从空场景开始，避免把上一次测试残留的东西一起存进场景
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ChronoMosaicSceneBuilder.Build();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);

        SessionState.SetBool(ActiveKey, true);
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) return;

        if (!started)
        {
            DimensionTransitionController ctrl = DimensionTransitionController.Instance;
            if (ctrl == null) return;
            started = true;
            playStartTime = Time.realtimeSinceStartup;
            ctrl.StartCoroutine(Run(ctrl));
            return;
        }

        // 看门狗：25 秒还没结束就判定超时
        if (Time.realtimeSinceStartup - playStartTime > 25f)
        {
            Line("[FAIL] 超时：测试未在 25 秒内结束");
            Finish();
        }
    }

    private static void Line(string text)
    {
        if (sb == null) sb = new StringBuilder();
        sb.AppendLine(text);
        Debug.Log("[PLAYTEST] " + text);
    }

    private static void Check(string name, bool ok, string detail)
    {
        if (ok) pass++; else fail++;
        Line((ok ? "[PASS] " : "[FAIL] ") + name + (string.IsNullOrEmpty(detail) ? "" : "  -> " + detail));
    }

    private static IEnumerator Run(DimensionTransitionController ctrl)
    {
        yield return null;
        yield return new WaitForSeconds(0.4f);

        Check("控制器实例存在", ctrl != null, "");
        Check("起始处于 3D", !ctrl.is2D, "");
        Check("3D 相机在渲染", ctrl.camera3D.gameObject.activeSelf, "");
        Check("2D 相机未渲染", !ctrl.camera2D.gameObject.activeSelf, "");
        Check("遮罩初始透明", ctrl.blend < 0.01f, "blend=" + ctrl.blend.ToString("0.00"));

        // A. 3D 站地板
        Teleport(ctrl, new Vector3(0f, 0.1f, 5f));
        yield return new WaitForSeconds(0.4f);
        Check("3D 站地板：可切换", ctrl.footRing.CanSwitch, "snapX=" + ctrl.footRing.SnapX.ToString("0.00"));
        Check("3D 站地板：吸附在自己脚下", Mathf.Abs(ctrl.footRing.SnapX) < 0.4f,
            "snapX=" + ctrl.footRing.SnapX.ToString("0.00"));

        // 截图放在计时之前，避免同步渲染阻塞污染转场时长测量
        yield return new WaitForSeconds(0.2f);
        CaptureAndCheck("3D 视角成图", ctrl.camera3D.GetComponent<Camera>(), "shot_3d.png");
        yield return null;
        yield return null;

        // B. 3D 站深渊上方
        Teleport(ctrl, new Vector3(0f, 0.1f, 11f));
        yield return new WaitForSeconds(0.4f);
        Check("3D 深渊上方：禁切", !ctrl.footRing.CanSwitch, "snapX=" + ctrl.footRing.SnapX.ToString("0.00"));

        // C. 深渊上方请求切 2D 应被拒绝（脚下没有立足点）
        ctrl.RequestSwitch(true);
        yield return null;
        Check("深渊上方切 2D 被拒绝", !ctrl.busy && !ctrl.is2D, "");

        // D. 回地板边缘，切 2D
        Teleport(ctrl, new Vector3(0f, 0.1f, 6.5f));
        yield return new WaitForSeconds(0.35f);

        float t0 = Time.realtimeSinceStartup;
        ctrl.RequestSwitch(true);
        Check("请求后立即进入 busy", ctrl.busy, "");

        float elapsed = 0f;
        while (ctrl.busy && elapsed < 3f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = Time.realtimeSinceStartup - t0;

        Check("转场时长接近 0.65s", Mathf.Abs(elapsed - ctrl.totalDuration) < 0.3f, "实际 " + elapsed.ToString("0.00") + "s");
        Check("已切到 2D", ctrl.is2D, "");
        Check("2D 相机激活", ctrl.camera2D.gameObject.activeSelf, "");
        Check("3D 相机隐藏", !ctrl.camera3D.gameObject.activeSelf, "");
        Check("blend 归位到 1", Mathf.Abs(ctrl.blend - 1f) < 0.02f, "blend=" + ctrl.blend.ToString("0.00"));

        Camera cam2 = ctrl.camera2D.GetComponent<Camera>();
        yield return null;
        yield return null;
        CaptureAndCheck("2D 视角成图", cam2, "shot_2d.png");
        Check("2D 相机为正交", cam2.orthographic, "");
        Check("2D 相机 X = 35", Mathf.Abs(cam2.transform.position.x - 35f) < 0.01f, "x=" + cam2.transform.position.x);
        Check("2D 相机朝向 -X", Quaternion.Angle(cam2.transform.rotation, Quaternion.Euler(0f, -90f, 0f)) < 0.2f,
            cam2.transform.rotation.eulerAngles.ToString());
        Check("2D 相机跟随 Z", Mathf.Abs(cam2.transform.position.z - ctrl.player.position.z) < 1.6f,
            "camZ=" + cam2.transform.position.z.ToString("0.0") + " playerZ=" + ctrl.player.position.z.ToString("0.0"));

        // E. 2D 走到红毯中段：3D 那边是深渊，必须拒绝
        Teleport(ctrl, new Vector3(0f, 0.1f, 11f));
        yield return new WaitForSeconds(0.35f);
        Check("2D 红毯(3D 是深渊)：禁切回", !ctrl.footRing.CanSwitch, "snapX=" + ctrl.footRing.SnapX.ToString("0.00"));
        ctrl.RequestSwitch(false);
        yield return null;
        Check("红毯上请求切 3D 被拒绝", ctrl.is2D && !ctrl.busy, "");

        // F. 2D 走到平台：3D 有地面，应允许并吸附
        Teleport(ctrl, new Vector3(0f, 0.1f, 19f));
        yield return new WaitForSeconds(0.35f);
        float expectX = ctrl.footRing.SnapX;
        Check("2D 平台(3D 有地面)：可切回", ctrl.footRing.CanSwitch, "snapX=" + expectX.ToString("0.00"));

        ctrl.RequestSwitch(false);
        elapsed = 0f;
        while (ctrl.busy && elapsed < 3f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        yield return null;

        Check("已切回 3D", !ctrl.is2D, "");
        Check("3D 相机激活", ctrl.camera3D.gameObject.activeSelf, "");
        Check("2D 相机隐藏", !ctrl.camera2D.gameObject.activeSelf, "");
        Check("X 已吸附到安全点", Mathf.Abs(ctrl.player.position.x - expectX) < 0.35f,
            "x=" + ctrl.player.position.x.ToString("0.00") + " expect=" + expectX.ToString("0.00"));
        Check("3D 相机距离正确", Mathf.Abs(Vector3.Distance(ctrl.camera3D.transform.position,
            ctrl.player.position + ctrl.camera3D.pivotOffset) - ctrl.camera3D.distance) < 0.6f, "");

        // G. 连续快速切换不应卡死
        for (int i = 0; i < 3; i++)
        {
            Teleport(ctrl, new Vector3(0f, 0.1f, 5f));
            yield return new WaitForSeconds(0.25f);
            ctrl.RequestSwitch(!ctrl.is2D);
            yield return new WaitForSeconds(0.8f);
        }
        Check("连续切换后状态自洽", !ctrl.busy && ctrl.is2D == DimensionTransitionController.Is2D, "");

        Finish();
    }

    // 渲染一帧到贴图并落盘，同时统计非背景像素比例，防止"全黑/全空"却测试通过
    private static void CaptureAndCheck(string label, Camera cam, string fileName)
    {
        const int w = 960;
        const int h = 540;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevTarget = cam.targetTexture;

        try
        {
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;

            Color32[] px = tex.GetPixels32();
            double sum = 0.0, sum2 = 0.0;
            for (int i = 0; i < px.Length; i++)
            {
                double lum = px[i].r * 0.299 + px[i].g * 0.587 + px[i].b * 0.114;
                sum += lum;
                sum2 += lum * lum;
            }
            double mean = sum / px.Length;
            double std = System.Math.Sqrt(System.Math.Max(0.0, sum2 / px.Length - mean * mean));

            string root = Directory.GetParent(Application.dataPath).FullName;
            File.WriteAllBytes(Path.Combine(root, fileName), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);

            Check(label, std > 4.0,
                "亮度 std=" + std.ToString("0.0") + " mean=" + mean.ToString("0.0") + "  -> " + fileName);
        }
        catch (System.Exception e)
        {
            Check(label, false, e.Message);
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
        }
    }

    private static void Teleport(DimensionTransitionController ctrl, Vector3 position)
    {
        CharacterController cc = ctrl.playerCharacter;
        if (cc != null) cc.enabled = false;
        if (ctrl.player != null) ctrl.player.position = position;
        if (ctrl.playerBody != null) ctrl.playerBody.position = position;
        if (cc != null) cc.enabled = true;
    }

    private static void Finish()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string result = "RESULT: " + (fail == 0 ? "PASS" : "FAIL") + "  passed=" + pass + " failed=" + fail;
        if (sb == null) sb = new StringBuilder();
        sb.AppendLine(result);
        File.WriteAllText(Path.Combine(root, "playtest-report.txt"), sb.ToString(), Encoding.UTF8);
        Debug.Log("[PLAYTEST] " + result);

        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(fail == 0 ? 0 : 1);
    }
}
#endif
