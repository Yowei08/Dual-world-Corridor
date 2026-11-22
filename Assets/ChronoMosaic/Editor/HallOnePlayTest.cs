// HallOnePlayTest.cs —— 第一展厅白膜的通关验证
// 按策划案 §5.1 的玩家流跑一遍：红毯过坑 → 十年树木 → 双墙齿轮 → 错位断桥 + X 吸附。
// 菜单：Tools → Chrono Mosaic → 7. 第一展厅自动测试
// 命令行：-executeMethod HallOnePlayTest.StartTest（不要加 -quit）

#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HallOnePlayTest
{
    private const string ActiveKey = "CM_HALL_TEST_ACTIVE";

    private static StringBuilder sb;
    private static int pass;
    private static int fail;
    private static bool started;
    private static float playStartTime;

    static HallOnePlayTest()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Chrono Mosaic/7. 第一展厅自动测试", false, 7)]
    public static void StartTest()
    {
        sb = new StringBuilder();
        pass = 0;
        fail = 0;
        started = false;

        HallOneBuilder.BuildAndSave();

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

        if (Time.realtimeSinceStartup - playStartTime > 90f)
        {
            Line("[FAIL] 超时：第一展厅测试未在 90 秒内结束");
            Finish();
        }
    }

    private static void Line(string text)
    {
        if (sb == null) sb = new StringBuilder();
        sb.AppendLine(text);
        Debug.Log("[HALLTEST] " + text);
    }

    private static void Check(string name, bool ok, string detail)
    {
        if (ok) pass++; else fail++;
        Line((ok ? "[PASS] " : "[FAIL] ") + name + (string.IsNullOrEmpty(detail) ? "" : "  -> " + detail));
    }

    private static T Find<T>(string path) where T : Component
    {
        GameObject go = GameObject.Find(path);
        return go != null ? go.GetComponent<T>() : null;
    }

    private static bool HasGround(float x, float y, float z)
    {
        return Physics.Raycast(new Vector3(x, y + 5f, z), Vector3.down, 12f,
            LayerMask.GetMask("Shared_Both"), QueryTriggerInteraction.Ignore);
    }

    /// <summary>3D 里能不能站：枯木桥是 Present3D_Only，不能只查 Shared_Both。</summary>
    private static bool HasGround3D(float x, float y, float z)
    {
        return Physics.Raycast(new Vector3(x, y + 5f, z), Vector3.down, 12f,
            LayerMask.GetMask("Shared_Both", "Present3D_Only"), QueryTriggerInteraction.Ignore);
    }

    private static IEnumerator Run(DimensionTransitionController ctrl)
    {
        yield return null;
        yield return new WaitForSeconds(0.5f);

        GrowthTree tree = Find<GrowthTree>("Hall01/GrowthTree");
        WaterSprinkler can = Find<WaterSprinkler>("Hall01/WaterSprinkler");
        Interactable3D canButton = Find<Interactable3D>("Hall01/WaterSprinkler");
        GearPair gears = Find<GearPair>("Hall01/GearPair");
        Interactable3D lever = Find<Interactable3D>("Hall01/GearLever");
        GateController door = Find<GateController>("Hall01/ExitDoor");

        Check("关键物件都在", tree != null && can != null && gears != null && lever != null && door != null, "");
        if (tree == null || gears == null || door == null) { Finish(); yield break; }

        // ---------- 0. 入口 ----------
        Teleport(ctrl, new Vector3(0f, 0.1f, 2f));
        yield return new WaitForSeconds(0.4f);
        Check("入口地板可站", HasGround(0f, 0f, 2f), "");
        Check("入口处允许切维度", ctrl.footRing != null && ctrl.footRing.CanSwitch, "");

        // 策划案第一句就是"宽 6 m 深渊跳不过"：用角色的跳跃参数算一遍最大跳跃距离
        TestPlayerMotor motor = ctrl.player.GetComponent<TestPlayerMotor>();
        if (motor != null)
        {
            float airTime = 2f * Mathf.Sqrt(2f * motor.jumpHeight / Mathf.Abs(motor.gravity));
            float jumpDistance = motor.speed * airTime;
            Check("6 m 深渊跳不过去（算出来的）", jumpDistance < 6f,
                "最大跳跃 " + jumpDistance.ToString("0.00") + " m（速度 " + motor.speed +
                " × 滞空 " + airTime.ToString("0.00") + " s）");
        }

        // ---------- 1. 红毯过坑 ----------
        Check("深渊在 3D 里没有地板", !HasGround(0f, 0f, 11f), "");
        Teleport(ctrl, new Vector3(0f, 0.1f, 7f));
        yield return new WaitForSeconds(0.3f);
        ctrl.RequestSwitch(true);
        yield return WaitSwitch(ctrl);
        Check("入口处切到 2D", ctrl.is2D, "");

        Teleport(ctrl, new Vector3(0f, 0.1f, 11f));
        yield return new WaitForSeconds(0.4f);
        Check("坑上方光圈暗红（禁切回 3D）", ctrl.footRing != null && !ctrl.footRing.CanSwitch,
            "canSwitch=" + (ctrl.footRing != null ? ctrl.footRing.CanSwitch.ToString() : "null"));

        Teleport(ctrl, new Vector3(0f, 0.1f, 15.5f));
        yield return new WaitForSeconds(0.4f);
        Check("走到中间平台后恢复可切", ctrl.footRing != null && ctrl.footRing.CanSwitch, "");
        ctrl.RequestSwitch(false);
        yield return WaitSwitch(ctrl);
        Check("在中间平台切回 3D", !ctrl.is2D, "");

        // ---------- 2. 十年树木 ----------
        Check("3D：枯木桥未出现", tree.bridge3D == null || !tree.bridge3D.activeSelf, "");
        Check("3D：2D 玻璃碰撞体关闭", !tree.glassCollider.enabled, "");

        ctrl.RequestSwitch(true);
        yield return WaitSwitch(ctrl);
        Check("浇水前：2D 玻璃墙挡住去路", tree.glassCollider.enabled, "");
        Check("2D：花洒可推", can.CanPush, "");

        Teleport(ctrl, new Vector3(0f, 0.1f, 15.2f));
        yield return new WaitForSeconds(0.3f);
        if (canButton != null) canButton.Interact();
        yield return new WaitForSeconds(1.6f);
        Check("推花洒到 Z=17.5 完成浇水", tree.IsWatered, "arrived=" + can.Arrived);
        Check("浇水后玻璃碎（路通了）", !tree.glassCollider.enabled, "");

        Teleport(ctrl, new Vector3(0f, 0.1f, 16.5f));
        yield return new WaitForSeconds(0.3f);
        ctrl.RequestSwitch(false);
        yield return WaitSwitch(ctrl);
        yield return new WaitForSeconds(1.4f);
        Check("3D：枯木桥长成", tree.bridge3D != null && tree.bridge3D.activeSelf && tree.Grow01 >= 1f,
            "grow=" + tree.Grow01.ToString("0.00"));
        Check("3D：可以踩上枯木桥", HasGround3D(0f, 0f, 21f), "");

        // ---------- 3. 双墙齿轮 ----------
        Teleport(ctrl, new Vector3(0f, 0.1f, 26f));
        yield return new WaitForSeconds(0.4f);
        Check("拉杆在 3D 可交互", lever != null && lever.CanInteract, "");
        if (lever != null) lever.Interact();
        yield return new WaitForSeconds(0.5f);
        Check("拉杆启动主动轮", gears.driverRunning, "");
        Check("3D：隔 19 m 咬不上", !gears.IsMeshed && gears.GapError > 10f,
            "gapError=" + gears.GapError.ToString("0.0"));
        Check("3D：门仍然关着", !door.IsOpen, "");

        ctrl.RequestSwitch(true);
        yield return WaitSwitch(ctrl);
        yield return new WaitForSeconds(0.4f);
        Check("2D：两轮投影相切咬合", gears.IsMeshed && gears.GapError < 0.2f,
            "gapError=" + gears.GapError.ToString("0.000") + " 投影距=" + gears.ProjectedDistance.ToString("0.00"));
        yield return new WaitForSeconds(7f);
        Check("2D：转满一圈后大门打开", gears.Completed && door.IsOpen,
            "deg=" + gears.DrivenDegrees.ToString("0") + " completed=" + gears.Completed + " door=" + door.IsOpen);
        Check("门开时拦路碰撞体关闭", door.blocker == null || !door.blocker.enabled, "");

        // ---------- 4. 错位断桥 + X 吸附 ----------
        ctrl.RequestSwitch(false);
        yield return WaitSwitch(ctrl);
        Teleport(ctrl, new Vector3(-5f, 0.1f, 39f));
        yield return new WaitForSeconds(0.4f);
        Check("站在左断桥上", HasGround(-5f, 0f, 39f), "");
        Check("左断桥上允许切 2D", ctrl.footRing.CanSwitch, "");

        ctrl.RequestSwitch(true);
        yield return WaitSwitch(ctrl);
        Teleport(ctrl, new Vector3(-5f, 0.1f, 44f));
        yield return new WaitForSeconds(0.5f);
        Check("2D 走到右断桥的 Z（仍锁在 X=-5）", Mathf.Abs(ctrl.player.position.x + 5f) < 0.3f,
            "x=" + ctrl.player.position.x.ToString("0.00"));
        Check("右断桥上方允许切回 3D", ctrl.footRing.CanSwitch, "");

        ctrl.RequestSwitch(false);
        yield return WaitSwitch(ctrl);
        yield return new WaitForSeconds(0.2f);
        Check("切回 3D 时 X 轴吸附到右断桥", Mathf.Abs(ctrl.player.position.x - 5f) < 0.5f,
            "x=" + ctrl.player.position.x.ToString("0.00"));
        Check("落在右断桥实体上", HasGround(5f, 0f, 44f), "");

        BoxESN();
        Finish();
    }

    private static void BoxESN()
    {
        GameObject baseGO = GameObject.Find("Hall01/Elevator_Base");
        Line("[INFO] 电梯底座 " + (baseGO != null ? "存在" : "缺失"));
    }

    private static IEnumerator WaitSwitch(DimensionTransitionController ctrl)
    {
        float t = 0f;
        yield return null;
        while (ctrl.busy && t < 3f) { t += Time.deltaTime; yield return null; }
    }

    private static void Teleport(DimensionTransitionController ctrl, Vector3 position)
    {
        CharacterController cc = ctrl.playerCharacter;
        if (cc != null) cc.enabled = false;
        ctrl.player.position = position;
        if (cc != null) cc.enabled = true;
    }

    private static void Finish()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string result = "RESULT: " + (fail == 0 ? "PASS" : "FAIL") + "  passed=" + pass + " failed=" + fail;
        if (sb == null) sb = new StringBuilder();
        sb.AppendLine(result);
        File.WriteAllText(Path.Combine(root, "halltest-report.txt"), sb.ToString(), Encoding.UTF8);
        Debug.Log("[HALLTEST] " + result);

        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(fail == 0 ? 0 : 1);
    }
}
#endif
