// MechanismLabBuilder.cs
// 相机测试房间 + 七个机关，每个机关按策划案的关键规则搭。
// 菜单：Tools → Chrono Mosaic → 4. 生成机关实验室

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class MechanismLabBuilder
{
    private const string LabRootName = "MechanismLab";
    public const string ScenePath = "Assets/Scenes/MechanismLab.unity";
    private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

    [MenuItem("Tools/Chrono Mosaic/4. 生成机关实验室", false, 4)]
    public static void BuildLab()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 复用相机测试房间：地板、墙、玩家、两个相机、维度系统、光圈、遮罩
        ChronoMosaicSceneBuilder.Build();

        GameObject old = GameObject.Find(LabRootName);
        if (old != null) Object.DestroyImmediate(old);

        GameObject root = new GameObject(LabRootName);
        Transform lab = root.transform;

        AddJumpPad(lab, new Vector3(-7f, 0f, 3f));
        AddEcho(lab, new Vector3(-3.5f, 0f, 3f));
        AddGate(lab);
        AddGears(lab);                       // 双墙齿轮：相切咬合 + 拉杆 + 门
        AddTree(lab);                        // 十年树木：玻璃墙 + 花洒 + 树苗 + 枯木桥
        AddChainRig(lab, new Vector3(-7f, 0f, 18f));
        AddPrism(lab, new Vector3(5f, 0f, 18f), new Vector3(1.5f, 1.5f, 18f));

        GameObject player = GameObject.Find("ChronoMosaicTest/Player");
        if (player != null && player.GetComponent<TestSword>() == null) player.AddComponent<TestSword>();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("[MechanismLab] 七个机关已摆好。");
    }

    public static void BuildAndSave()
    {
        BuildLab();
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();
    }

    // ---------------- 1. 弹簧板 ----------------
    private static void AddJumpPad(Transform parent, Vector3 pos)
    {
        GameObject pad = Box(parent, "JumpPad", pos + new Vector3(0f, 0.15f, 0f),
            new Vector3(1.8f, 0.3f, 1.8f), new Color(0.32f, 0.62f, 0.5f), LayerShared);

        BoxCollider col = pad.GetComponent<BoxCollider>();
        col.isTrigger = true;
        Rigidbody rb = pad.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        JumpPad jump = pad.AddComponent<JumpPad>();
        jump.activeIn = DimensionMode.Past2D;
        jump.launchHeight = 3.2f;
        jump.padVisual = pad.transform;
        jump.padRenderer = pad.GetComponent<Renderer>();
    }

    // ---------------- 2. 过去残影 ----------------
    private static void AddEcho(Transform parent, Vector3 pos)
    {
        GameObject host = new GameObject("PastEcho");
        host.transform.SetParent(parent, false);
        host.transform.position = pos;

        GameObject spawn = new GameObject("SpawnPoint");
        spawn.transform.SetParent(host.transform, false);
        spawn.transform.localPosition = new Vector3(0f, 1f, 0f);

        PastEcho echo = host.AddComponent<PastEcho>();
        echo.usableIn = DimensionMode.Past2D;
        echo.echoPrefab = CreateEchoPrefab();
        echo.spawnPoint = spawn.transform;
        echo.lifetime = 4f;
        echo.cooldown = 1.5f;
        echo.maxAlive = 2;
    }

    private static GameObject CreateEchoPrefab()
    {
        const string dir = "Assets/Generated";
        const string path = dir + "/PastEchoTemplate.prefab";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Generated");

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = "PastEchoTemplate";
        temp.transform.localScale = new Vector3(0.5f, 1.6f, 0.5f);
        Object.DestroyImmediate(temp.GetComponent<Collider>());
        temp.GetComponent<Renderer>().sharedMaterial = GetMaterial(new Color(0.55f, 0.78f, 1f), true);
        temp.AddComponent<TimedDestroy>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    // ---------------- 3. 双板门 ----------------
    private static void AddGate(Transform parent)
    {
        PressurePlate a = MakePlate(parent, "PlateA", new Vector3(0.5f, 0.25f, 2f));
        PressurePlate b = MakePlate(parent, "PlateB", new Vector3(3.0f, 0.25f, 2f));

        GameObject gate = Box(parent, "Gate", new Vector3(1.75f, 1.5f, 4.2f),
            new Vector3(3f, 3f, 0.35f), new Color(0.42f, 0.36f, 0.30f), LayerShared);

        GateController ctrl = gate.AddComponent<GateController>();
        ctrl.requiredPlates = new PressurePlate[] { a, b };
        ctrl.requireAll = true;
        ctrl.gateVisual = gate.transform;
        ctrl.openOffset = new Vector3(0f, 3.4f, 0f);
        ctrl.blocker = gate.GetComponent<Collider>();
    }

    private static PressurePlate MakePlate(Transform parent, string name, Vector3 pos)
    {
        GameObject plate = Box(parent, name, pos, new Vector3(1.6f, 0.5f, 1.6f),
            new Color(0.35f, 0.35f, 0.38f), LayerShared);
        plate.GetComponent<BoxCollider>().isTrigger = true;

        PressurePlate p = plate.AddComponent<PressurePlate>();
        p.activeIn = DimensionMode.Both;
        p.requiredWeight = 1f;
        p.plateVisual = plate.transform;
        p.plateRenderer = plate.GetComponent<Renderer>();
        return p;
    }

    // ---------------- 4. 双墙齿轮 ----------------
    // 策划案：左墙主动轮 / 右墙从动轮相隔 19 m，3D 里永远咬不上；
    // 切 2D 投影后两轮外切，动力穿过 19 m，转够圈数开门。
    private static void AddGears(Transform parent)
    {
        GameObject host = new GameObject("GearPair");
        host.transform.SetParent(parent, false);
        host.transform.position = new Vector3(0f, 4.5f, 4.65f);

        Transform driver = MakeGear(parent, "DriverGear", new Vector3(-9.5f, 4.5f, 3.0f));
        Transform driven = MakeGear(parent, "DrivenGear", new Vector3(9.5f, 4.5f, 6.3f));

        GearPair pair = host.AddComponent<GearPair>();
        pair.driverGear = driver;
        pair.drivenGear = driven;
        pair.driverRadius = 1.65f;
        pair.drivenRadius = 1.65f;
        pair.localAxis = Vector3.up;          // 圆柱本地 Y = 盘面法线
        pair.toothGap = 0.15f;
        pair.driverSpeed = 60f;
        pair.requiredTurns = 0.25f;           // 实验室里转 1/4 圈就算开门，省时间

        // 拉杆（3D 里按 E）
        GameObject lever = Box(parent, "GearLever", new Vector3(-7f, 0.8f, 1.0f),
            new Vector3(0.28f, 1.6f, 0.28f), new Color(0.55f, 0.48f, 0.34f), LayerShared);
        Interactable3D li = lever.AddComponent<Interactable3D>();
        li.activeIn = DimensionMode.Present3D;
        li.prompt = "按 E 拉下拉杆";
        li.oneShot = true;
        if (li.onInteract == null) li.onInteract = new UnityEvent();
        UnityEventTools.AddPersistentListener(li.onInteract, pair.ToggleDriver);

        // 齿轮驱动的门
        GameObject gate = Box(parent, "GearGate", new Vector3(-7f, 1.5f, 7.5f),
            new Vector3(4f, 3f, 0.4f), new Color(0.40f, 0.34f, 0.28f), LayerShared);
        GateController gc = gate.AddComponent<GateController>();
        gc.requiredPlates = new PressurePlate[0];
        gc.stayOpen = true;
        gc.gateVisual = gate.transform;
        gc.openOffset = new Vector3(0f, 3.2f, 0f);
        gc.blocker = gate.GetComponent<Collider>();
        if (pair.onCompleted == null) pair.onCompleted = new UnityEvent();
        UnityEventTools.AddPersistentListener(pair.onCompleted, gc.ForceOpen);
    }

    private static Transform MakeGear(Transform parent, string name, Vector3 pos)
    {
        // 圆柱默认轴是本地 Y；绕 Z 转 90° 后轴朝世界 X，盘面正对 2D 相机
        GameObject gear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gear.name = name;
        gear.transform.SetParent(parent, false);
        gear.transform.position = pos;
        gear.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        gear.transform.localScale = new Vector3(3.3f, 0.125f, 3.3f);   // 半径 1.65，厚 0.25
        gear.layer = LayerShared;
        gear.GetComponent<Renderer>().sharedMaterial = GetMaterial(new Color(0.62f, 0.55f, 0.35f), false);
        return gear.transform;
    }

    // ---------------- 5. 十年树木 ----------------
    // 策划案 §5.1：2D 玻璃隔断墙挡路 → 推花洒浇树苗 → 玻璃碎、3D 长出枯木桥。
    private static void AddTree(Transform parent)
    {
        GameObject host = new GameObject("GrowthTree");
        host.transform.SetParent(parent, false);
        host.transform.position = new Vector3(-7f, 0f, 18f);

        GameObject sapling = Box(host.transform, "Sapling2D", new Vector3(-7f, 0.55f, 15.8f),
            new Vector3(0.45f, 1.1f, 0.45f), new Color(0.35f, 0.75f, 0.4f), Layer2D);
        Object.DestroyImmediate(sapling.GetComponent<Collider>());

        // 2D 玻璃隔断墙：横跨整条走廊，浇水前挡住 Z = 21
        GameObject glass = Box(parent, "GlassWall2D", new Vector3(0f, 3f, 21f),
            new Vector3(19f, 6f, 0.35f), new Color(0.55f, 0.78f, 0.9f), Layer2D);

        // 3D 参天枯木桥（Present3D_Only，浇水后激活）
        GameObject bridge = Box(host.transform, "Bridge3D", new Vector3(-7f, 0.2f, 21f),
            new Vector3(3.2f, 0.8f, 7f), new Color(0.42f, 0.33f, 0.24f), Layer3D);

        GrowthTree tree = host.AddComponent<GrowthTree>();
        tree.waterIn = DimensionMode.Past2D;
        tree.sapling2D = sapling;
        tree.glassWall2D = glass;
        tree.glassCollider = glass.GetComponent<Collider>();
        tree.bridge3D = bridge;
        tree.bridgeCollider = bridge.GetComponent<Collider>();
        tree.growDuration = 1f;

        // 铜花洒：2D 里按 E 推进去
        GameObject can = Box(parent, "WaterSprinkler", new Vector3(-7f, 0.45f, 15.0f),
            new Vector3(0.55f, 0.9f, 0.55f), new Color(0.72f, 0.55f, 0.30f), Layer2D);
        can.GetComponent<BoxCollider>().isTrigger = true;

        GameObject spout = new GameObject("Spout");
        spout.transform.SetParent(can.transform, false);
        spout.transform.localPosition = new Vector3(0f, 0.6f, 0.35f);

        LineRenderer stream = MakeLine(can.transform, "WaterStream", new Color(0.55f, 0.8f, 1f), 0.06f);
        stream.enabled = false;

        WaterSprinkler sp = can.AddComponent<WaterSprinkler>();
        sp.pushIn = DimensionMode.Past2D;
        sp.tree = tree;
        sp.targetZ = 15.8f;
        sp.slideSpeed = 2.5f;
        sp.spout = spout.transform;
        sp.stream = stream;

        Interactable3D it = can.AddComponent<Interactable3D>();
        it.activeIn = DimensionMode.Past2D;
        it.prompt = "按 E 推花洒";
        it.oneShot = true;
        if (it.onInteract == null) it.onInteract = new UnityEvent();
        UnityEventTools.AddPersistentListener(it.onInteract, sp.Push);
    }

    // ---------------- 6. 断链配重 ----------------
    private static void AddChainRig(Transform parent, Vector3 pos)
    {
        PressurePlate plate = MakePlate(parent, "ChainPlate", pos + new Vector3(0f, 0.25f, 0f));

        // 挂在 2m 高：玩家抬刀够得着，砍断后落 1.6m 压到踏板
        GameObject weight = Box(parent, "ChainWeight", pos + new Vector3(0f, 2f, 0f),
            new Vector3(0.8f, 0.8f, 0.8f), new Color(0.30f, 0.30f, 0.34f), LayerShared);

        Rigidbody rb = weight.AddComponent<Rigidbody>();
        rb.useGravity = true;

        PressureWeight pw = weight.AddComponent<PressureWeight>();
        pw.weight = 1f;
        pw.dimension = DimensionMode.Both;

        GameObject chain = Box(parent, "ChainVisual", pos + new Vector3(0f, 3.2f, 0f),
            new Vector3(0.08f, 1.8f, 0.08f), new Color(0.25f, 0.25f, 0.28f), Layer3D);

        ChainWeight cw = weight.AddComponent<ChainWeight>();
        cw.cuttableIn = DimensionMode.Past2D;
        cw.hitsToCut = 1;
        cw.body = rb;
        cw.chainVisual = chain.transform;
        cw.cutImpulse = 0.2f;
        if (plate == null) Debug.LogWarning("[MechanismLab] ChainPlate 创建失败");
    }

    // ---------------- 7. 聚光棱镜 ----------------
    private static void AddPrism(Transform parent, Vector3 pos, Vector3 receiverPos)
    {
        GameObject host = new GameObject("Prism");
        host.transform.SetParent(parent, false);
        host.transform.position = pos;

        GameObject beamOrigin = new GameObject("BeamOrigin");
        beamOrigin.transform.SetParent(host.transform, false);
        beamOrigin.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        beamOrigin.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);   // 朝 -X

        LineRenderer line = MakeLine(host.transform, "BeamLine", new Color(1f, 0.85f, 0.4f), 0.08f);

        PrismBeam prism = host.AddComponent<PrismBeam>();
        prism.activeIn = DimensionMode.Present3D;
        prism.pivot = host.transform;
        prism.beamOrigin = beamOrigin.transform;
        prism.beamLine = line;
        prism.range = 12f;
        prism.minAngle = -25f;
        prism.maxAngle = 25f;
        prism.hitMask = LayerMask.GetMask("Present3D_Only");

        GameObject receiver = Box(parent, "LightReceiver", receiverPos,
            new Vector3(0.8f, 0.8f, 0.8f), new Color(0.35f, 0.42f, 0.5f), Layer3D);
        LightReceiver lr = receiver.AddComponent<LightReceiver>();
        lr.crystalRenderer = receiver.GetComponent<Renderer>();
    }

    // ---------------- 通用 ----------------
    private static int LayerShared { get { return LayerMask.NameToLayer("Shared_Both"); } }
    private static int Layer3D { get { return LayerMask.NameToLayer("Present3D_Only"); } }
    private static int Layer2D { get { return LayerMask.NameToLayer("Past2D_Only"); } }

    private static LineRenderer MakeLine(Transform parent, string name, Color color, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material mat = new Material(shader);
        mat.SetColor("_Color", color);
        line.material = mat;
        line.startColor = color;
        line.endColor = color;
        return line;
    }

    private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int layer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (layer >= 0) go.layer = layer;
        go.GetComponent<Renderer>().sharedMaterial = GetMaterial(color, false);
        return go;
    }

    private static Material GetMaterial(Color color, bool emissive)
    {
        string key = ColorUtility.ToHtmlStringRGBA(color) + (emissive ? "_e" : "_d");
        Material cached;
        if (Cache.TryGetValue(key, out cached) && cached != null) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.5f);
        }
        Cache[key] = material;
        return material;
    }
}
#endif
