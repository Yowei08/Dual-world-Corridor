// MechanismPlayTest.cs
// 在 Play 模式下把七个机关各跑一遍，重点验证"策划案规定的行为"而不是"脚本没报错"。
// 菜单：Tools → Chrono Mosaic → 5. 机关自动测试
// 命令行：-executeMethod MechanismPlayTest.StartTest（不要加 -quit）

#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MechanismPlayTest
{
    private const string ActiveKey = "CM_MECH_TEST_ACTIVE";

    private static StringBuilder sb;
    private static int pass;
    private static int fail;
    private static bool started;
    private static float playStartTime;

    static MechanismPlayTest()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Chrono Mosaic/5. 机关自动测试", false, 5)]
    public static void StartTest()
    {
        sb = new StringBuilder();
        pass = 0;
        fail = 0;
        started = false;

        MechanismLabBuilder.BuildAndSave();

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

        if (Time.realtimeSinceStartup - playStartTime > 60f)
        {
            Line("[FAIL] 超时：机关测试未在 60 秒内结束");
            Finish();
        }
    }

    private static void Line(string text)
    {
        if (sb == null) sb = new StringBuilder();
        sb.AppendLine(text);
        Debug.Log("[MECHTEST] " + text);
    }

    private static void Check(string name, bool ok, string detail)
    {
        if (ok) pass++; else fail++;
        Line((ok ? "[PASS] " : "[FAIL] ") + name + (string.IsNullOrEmpty(detail) ? "" : "  -> " + detail));
    }

    private static int CountEchoes()
    {
#if UNITY_2022_2_OR_NEWER
        return Object.FindObjectsByType<TimedDestroy>(FindObjectsSortMode.None).Length;
#else
        return Object.FindObjectsOfType<TimedDestroy>().Length;
#endif
    }

    private static T Find<T>(string path) where T : Component
    {
        GameObject go = GameObject.Find(path);
        return go != null ? go.GetComponent<T>() : null;
    }

    private static IEnumerator Run(DimensionTransitionController ctrl)
    {
        yield return null;
        yield return new WaitForSeconds(0.5f);

        JumpPad pad = Find<JumpPad>("MechanismLab/JumpPad");
        PastEcho echo = Find<PastEcho>("MechanismLab/PastEcho");
        PressurePlate plateA = Find<PressurePlate>("MechanismLab/PlateA");
        PressurePlate plateB = Find<PressurePlate>("MechanismLab/PlateB");
        GateController gate = Find<GateController>("MechanismLab/Gate");
        GearPair gears = Find<GearPair>("MechanismLab/GearPair");
        Interactable3D lever = Find<Interactable3D>("MechanismLab/GearLever");
        GateController gearGate = Find<GateController>("MechanismLab/GearGate");
        GrowthTree tree = Find<GrowthTree>("MechanismLab/GrowthTree");
        WaterSprinkler can = Find<WaterSprinkler>("MechanismLab/WaterSprinkler");
        Interactable3D canButton = Find<Interactable3D>("MechanismLab/WaterSprinkler");
        ChainWeight chain = Find<ChainWeight>("MechanismLab/ChainWeight");
        PressurePlate chainPlate = Find<PressurePlate>("MechanismLab/ChainPlate");
        PrismBeam prism = Find<PrismBeam>("MechanismLab/Prism");
        LightReceiver receiver = Find<LightReceiver>("MechanismLab/LightReceiver");
        TestPlayerMotor motor = ctrl.player.GetComponent<TestPlayerMotor>();
        TestSword sword = ctrl.player.GetComponent<TestSword>();

        Check("机关全部就位",
            pad != null && echo != null && plateA != null && plateB != null && gate != null &&
            gears != null && lever != null && gearGate != null && tree != null && can != null &&
            canButton != null && chain != null && chainPlate != null && prism != null && receiver != null,
            "");

        if (tree == null || gears == null || can == null) { Finish(); yield break; }

        // ================= 3D 阶段 =================
        Teleport(ctrl, new Vector3(-7f, 0.1f, 3f));
        yield return new WaitForSeconds(0.9f);
        Check("3D：弹簧板不触发", pad.FireCount == 0, "fireCount=" + pad.FireCount);

        chain.OnSwordHit(1, chain.transform.position);
        yield return null;
        Check("3D：砍链被拒绝", !chain.IsCut, "");

        int echoBefore = CountEchoes();
        echo.Spawn();
        yield return new WaitForSeconds(0.2f);
        Check("3D：生成残影被拒绝", CountEchoes() == echoBefore, "");

        yield return new WaitForSeconds(0.3f);
        Check("3D：光束点亮接收器", receiver.IsLit, "");

        // ---- 双墙齿轮：3D 里隔 19m 咬不上 ----
        Check("齿轮初始未启动", !gears.driverRunning, "");
        Teleport(ctrl, new Vector3(-7f, 0.1f, 1.6f));
        yield return new WaitForSeconds(0.3f);
        Check("拉杆在 3D 可交互", lever != null && lever.CanInteract, "");
        if (lever != null) lever.Interact();
        yield return new WaitForSeconds(0.5f);
        Check("拉杆启动主动轮", gears.driverRunning, "");
        Check("3D：隔 19m 不咬合（间隙误差很大）",
            !gears.IsMeshed && gears.GapError > 10f,
            "gapError=" + gears.GapError.ToString("0.0") + " 投影距=" + gears.ProjectedDistance.ToString("0.00"));
        Check("3D：从动轮不转", gears.DrivenDegrees < 1f, "deg=" + gears.DrivenDegrees.ToString("0.0"));

        // ---- 十年树木：3D 里还没长桥 ----
        Check("3D：玻璃墙碰撞体关闭（2D 专属）", !tree.glassCollider.enabled, "");
        Check("3D：枯木桥尚未激活", tree.bridge3D == null || !tree.bridge3D.activeSelf, "");
        Check("3D：花洒不能推（2D 专属）", can != null && !can.CanPush, "");

        // ---- 双板门 ----
        GameObject weightA = DropWeight(plateA.transform.position);
        yield return new WaitForSeconds(1.1f);
        Check("单板按下：门不开", plateA.IsPressed && !gate.IsOpen, "A=" + plateA.IsPressed + " open=" + gate.IsOpen);

        GameObject weightB = DropWeight(plateB.transform.position);
        yield return new WaitForSeconds(1.1f);
        Check("双板按下：门打开", plateB.IsPressed && gate.IsOpen, "B=" + plateB.IsPressed + " open=" + gate.IsOpen);
        Check("门打开时移除拦路碰撞", gate.blocker == null || !gate.blocker.enabled, "");

        MoveWeightAway(weightA);
        yield return new WaitForSeconds(0.6f);
        Check("松开一块板：门关闭", !gate.IsOpen, "");
        MoveWeightAway(weightB);
        yield return new WaitForSeconds(0.5f);
        Check("全部松开：门保持关闭", !gate.IsOpen, "");

        // ================= 切到 2D（在 x=-7 走廊）=================
        Teleport(ctrl, new Vector3(-7f, 0.1f, 6.5f));
        yield return new WaitForSeconds(0.4f);
        ctrl.RequestSwitch(true);
        float wait = 0f;
        while (ctrl.busy && wait < 3f) { wait += Time.deltaTime; yield return null; }
        Check("已切到 2D", ctrl.is2D, "");

        // ---- 双墙齿轮：2D 投影相切，动力穿过 19m ----
        yield return new WaitForSeconds(0.4f);
        Check("2D：两轮投影相切咬合",
            gears.IsMeshed && gears.GapError < 0.2f,
            "间隙误差=" + gears.GapError.ToString("0.000") + " 投影距=" + gears.ProjectedDistance.ToString("0.00"));
        yield return new WaitForSeconds(2.2f);
        Check("2D：从动轮被带着转", gears.DrivenDegrees > 60f, "deg=" + gears.DrivenDegrees.ToString("0"));
        Check("2D：转够圈数后门打开", gears.Completed && gearGate.IsOpen,
            "completed=" + gears.Completed + " gate=" + gearGate.IsOpen);

        // ---- 十年树木：玻璃挡路 → 推花洒 → 碎裂 ----
        Check("2D：玻璃墙挡路（碰撞体启用）", tree.glassCollider.enabled, "");
        Check("2D：花洒可推", can.CanPush, "");
        Teleport(ctrl, new Vector3(-7f, 0.1f, 14.6f));
        yield return new WaitForSeconds(0.3f);
        if (canButton != null) canButton.Interact();
        yield return new WaitForSeconds(0.9f);
        Check("花洒推到树苗旁并浇水", tree.IsWatered, "arrived=" + can.Arrived);
        Check("2D：玻璃墙碰撞体已关（挡住的路通了）", !tree.glassCollider.enabled, "");
        yield return new WaitForSeconds(0.3f);
        Check("2D：玻璃网格已隐藏", tree.glassWall2D == null || !tree.glassWall2D.activeSelf, "");

        // ---- 断链配重 ----
        Teleport(ctrl, new Vector3(-7f, 0.1f, 16.6f));
        FaceTowards(ctrl, new Vector3(-7f, 0.1f, 18f));
        yield return new WaitForSeconds(0.3f);
        int hits2D = sword != null ? sword.Swing() : 0;
        yield return null;
        Check("2D：挥刀砍断铁链", chain.IsCut, "hits=" + hits2D);
        yield return new WaitForSeconds(1.6f);
        Check("配重落地压住踏板", chainPlate.IsPressed, "y=" + chain.transform.position.y.ToString("0.00"));

        echo.Spawn();
        yield return new WaitForSeconds(0.2f);
        Check("2D：生成残影成功", CountEchoes() > 0, "");
        Check("2D：光束熄灭", !receiver.IsLit, "");

        // ================= 切回 3D =================
        Teleport(ctrl, new Vector3(-7f, 0.1f, 19f));
        yield return new WaitForSeconds(0.4f);
        ctrl.RequestSwitch(false);
        wait = 0f;
        while (ctrl.busy && wait < 3f) { wait += Time.deltaTime; yield return null; }
        yield return new WaitForSeconds(1.4f);

        Check("已切回 3D", !ctrl.is2D, "");
        Check("3D：枯木桥出现", tree.bridge3D != null && tree.bridge3D.activeSelf, "");
        Check("3D：枯木桥长成并可以踩", tree.Grow01 >= 1f && tree.bridgeCollider.enabled,
            "grow=" + tree.Grow01.ToString("0.00"));
        Check("3D：齿轮又咬不上了（不是开关，是几何）", !gears.IsMeshed,
            "gapError=" + gears.GapError.ToString("0.0"));
        Check("3D：光束重新点亮", receiver.IsLit, "");

        Finish();
    }

    private static GameObject DropWeight(Vector3 platePosition)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = "TestWeight";
        w.transform.position = platePosition + Vector3.up * 1.3f;
        w.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

        Rigidbody rb = w.AddComponent<Rigidbody>();
        rb.mass = 1f;

        PressureWeight pw = w.AddComponent<PressureWeight>();
        pw.weight = 1f;
        pw.dimension = DimensionMode.Both;
        return w;
    }

    private static void MoveWeightAway(GameObject w)
    {
        if (w == null) return;
        Rigidbody rb = w.GetComponent<Rigidbody>();
        if (rb != null) rb.position = new Vector3(0f, 60f, 0f);
        else w.transform.position = new Vector3(0f, 60f, 0f);
    }

    private static void FaceTowards(DimensionTransitionController ctrl, Vector3 target)
    {
        Vector3 dir = target - ctrl.player.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) ctrl.player.rotation = Quaternion.LookRotation(dir.normalized);
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
        File.WriteAllText(Path.Combine(root, "mechtest-report.txt"), sb.ToString(), Encoding.UTF8);
        Debug.Log("[MECHTEST] " + result);

        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(fail == 0 ? 0 : 1);
    }
}
#endif
