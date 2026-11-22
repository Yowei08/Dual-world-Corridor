// HallOneBuilder.cs —— 第一展厅「尘封迎宾厅与生态廊」白膜
// 完全按策划案 §5.1 的坐标表摆放（Pos / Scale 都来自文档）：
//   1. 红毯过坑   Z 8~14 深渊，2D 红毯桥
//   2. 十年树木   Z 18~24，2D 玻璃隔断 + 花洒 + 树苗 → 3D 枯木桥
//   3. 双墙齿轮   Z 24~36 大厅，左右墙相隔 19 m，切 2D 咬合开门
//   4. 错位断桥   Z 36~48，左右断桥 + 2D 连贯桥 + X 轴吸附
// 菜单：Tools → Chrono Mosaic → 6. 生成第一展厅白膜

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class HallOneBuilder
{
    public const string RootName = "Hall01";
    public const string ScenePath = "Assets/Scenes/Hall01.unity";

    private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

    private static Color FloorColor = new Color(0.28f, 0.31f, 0.35f);
    private static Color WallColor = new Color(0.20f, 0.23f, 0.27f);
    private static Color CarpetColor = new Color(0.55f, 0.18f, 0.18f);
    private static Color WoodColor = new Color(0.42f, 0.33f, 0.24f);
    private static Color BrassColor = new Color(0.62f, 0.55f, 0.35f);

    [MenuItem("Tools/Chrono Mosaic/6. 生成第一展厅白膜", false, 6)]
    public static void BuildHall()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = ChronoMosaicSceneBuilder.BuildSystems(RootName);
        Transform hall = root.transform;

        BuildShell(hall);
        BuildPitSection(hall);        // 1. 红毯过坑
        BuildTreeSection(hall);       // 2. 十年树木
        BuildGearHall(hall);          // 3. 双墙齿轮
        BuildBridgeSection(hall);     // 4. 错位断桥

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log("[Hall01] 第一展厅白膜已生成：入口 → 深渊 → 树木 → 齿轮大厅 → 断桥。");
    }

    public static void BuildAndSave()
    {
        BuildHall();
        ChronoMosaicSceneBuilder.SaveSceneAs(ScenePath);
    }

    // ---------------- 外壳 ----------------
    private static void BuildShell(Transform hall)
    {
        // 左墙：2D 侧视的“剖面墙”，两个维度都看得见
        Box(hall, "Wall_Left", new Vector3(-9.8f, 5f, 24f), new Vector3(0.4f, 10f, 48f), WallColor, LayerShared);
        // 右墙：2D 必须剔除
        Box(hall, "Wall_Right_3D", new Vector3(9.8f, 5f, 24f), new Vector3(0.4f, 10f, 48f),
            new Color(0.16f, 0.18f, 0.21f), LayerWall3D);
        // 尽头
        Box(hall, "Wall_Back", new Vector3(0f, 5f, 48.2f), new Vector3(20f, 10f, 0.4f), WallColor, LayerShared);
        // 屋顶：只在 3D 有（2D 是剖面图，不能有顶）
        Box(hall, "Ceiling_3D", new Vector3(0f, 10.15f, 24f), new Vector3(20f, 0.3f, 48f),
            new Color(0.17f, 0.19f, 0.22f), Layer3D);
        // 2D 远景背景板：给正交侧视一个底，避免看到空背景
        Box(hall, "Backdrop_2D", new Vector3(-12f, 5f, 24f), new Vector3(0.2f, 12f, 50f),
            new Color(0.55f, 0.46f, 0.36f), Layer2D);
    }

    // ---------------- 1. 红毯过坑 ----------------
    private static void BuildPitSection(Transform hall)
    {
        // 入口地板 Z ∈ [0, 8]
        Box(hall, "Floor_Entrance", new Vector3(0f, -0.5f, 4f), new Vector3(20f, 1f, 8f), FloorColor, LayerShared);
        // Z ∈ [8, 14] 是深渊，3D 里不放地板
        // 中间平台 Z ∈ [14, 18]
        Box(hall, "Floor_Mid", new Vector3(0f, -0.5f, 16f), new Vector3(20f, 1f, 4f), FloorColor, LayerShared);

        // 2D 红毯桥：补上深渊
        GameObject carpet = Box(hall, "Carpet_Pit_2D", new Vector3(0f, -0.22f, 11f),
            new Vector3(14f, 0.4f, 6.4f), CarpetColor, Layer2D);
        DimensionCollider dc = carpet.AddComponent<DimensionCollider>();
        dc.activeIn = DimensionMode.Past2D;
        dc.colliders = new Collider[] { carpet.GetComponent<Collider>() };
    }

    // ---------------- 2. 十年树木 ----------------
    private static void BuildTreeSection(Transform hall)
    {
        // Z ∈ [18, 24] 3D 无地板（要在 3D 靠枯木桥过）
        // 2D 红毯
        GameObject carpet = Box(hall, "Carpet_Tree_2D", new Vector3(0f, -0.22f, 21f),
            new Vector3(14f, 0.4f, 6.4f), CarpetColor, Layer2D);
        DimensionCollider dc = carpet.AddComponent<DimensionCollider>();
        dc.activeIn = DimensionMode.Past2D;
        dc.colliders = new Collider[] { carpet.GetComponent<Collider>() };

        // 2D 玻璃隔断墙（Z = 21，横跨走廊）
        GameObject glass = Box(hall, "GlassWall_2D", new Vector3(0f, 3f, 21f),
            new Vector3(18f, 6f, 0.4f), new Color(0.55f, 0.78f, 0.9f), Layer2D);

        // 3D 参天枯木桥（浇水后激活）
        GameObject bridge = Box(hall, "DeadTreeBridge_3D", new Vector3(0f, -0.2f, 21f),
            new Vector3(3.2f, 0.8f, 7f), WoodColor, Layer3D);

        // 2D 树苗
        GameObject sapling = Box(hall, "Sapling_2D", new Vector3(0f, 0.55f, 17.5f),
            new Vector3(0.45f, 1.1f, 0.45f), new Color(0.35f, 0.75f, 0.4f), Layer2D);
        Object.DestroyImmediate(sapling.GetComponent<Collider>());

        GameObject host = new GameObject("GrowthTree");
        host.transform.SetParent(hall, false);
        host.transform.position = new Vector3(0f, 0f, 21f);

        GrowthTree tree = host.AddComponent<GrowthTree>();
        tree.waterIn = DimensionMode.Past2D;
        tree.sapling2D = sapling;
        tree.glassWall2D = glass;
        tree.glassCollider = glass.GetComponent<Collider>();
        tree.bridge3D = bridge;
        tree.bridgeCollider = bridge.GetComponent<Collider>();
        tree.growDuration = 1f;

        // 2D 铜花洒（Z = 15.5 → 推到树苗 Z = 17.5）
        GameObject can = Box(hall, "WaterSprinkler", new Vector3(0f, 0.45f, 15.5f),
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
        sp.targetZ = 17.5f;
        sp.slideSpeed = 2f;
        sp.spout = spout.transform;
        sp.stream = stream;

        Interactable3D it = can.AddComponent<Interactable3D>();
        it.activeIn = DimensionMode.Past2D;
        it.prompt = "按 E 推花洒";
        it.oneShot = true;
        if (it.onInteract == null) it.onInteract = new UnityEvent();
        UnityEventTools.AddPersistentListener(it.onInteract, sp.Push);
    }

    // ---------------- 3. 双墙齿轮 ----------------
    private static void BuildGearHall(Transform hall)
    {
        // 大厅地板 Z ∈ [24, 36]
        Box(hall, "Floor_Hall", new Vector3(0f, -0.5f, 30f), new Vector3(20f, 1f, 12f), FloorColor, LayerShared);

        // 左右墙齿轮：相隔 19 m，投影差 3.3 m = r1 + r2
        Transform driver = MakeGear(hall, "Gear_Left_Driver", new Vector3(-9.5f, 4.5f, 30.0f));
        Transform driven = MakeGear(hall, "Gear_Right_Driven", new Vector3(9.5f, 4.5f, 33.3f));

        GameObject host = new GameObject("GearPair");
        host.transform.SetParent(hall, false);
        host.transform.position = new Vector3(0f, 4.5f, 31.65f);

        GearPair pair = host.AddComponent<GearPair>();
        pair.driverGear = driver;
        pair.drivenGear = driven;
        pair.driverRadius = 1.65f;
        pair.drivenRadius = 1.65f;
        pair.localAxis = Vector3.up;
        pair.toothGap = 0.15f;
        pair.driverSpeed = 60f;
        pair.requiredTurns = 1f;

        // 地面拉杆 Z = 27
        // 策划案：地面拉杆 Pos(0, 0, 27) —— y=0 是"底座落在第 0 层"，所以立方体中心在 0.8
        GameObject lever = Box(hall, "GearLever", new Vector3(0f, 0.8f, 27f),
            new Vector3(0.3f, 1.6f, 0.3f), new Color(0.55f, 0.48f, 0.34f), LayerShared);
        Interactable3D li = lever.AddComponent<Interactable3D>();
        li.activeIn = DimensionMode.Present3D;
        li.prompt = "按 E 拉下拉杆";
        li.oneShot = true;
        if (li.onInteract == null) li.onInteract = new UnityEvent();
        UnityEventTools.AddPersistentListener(li.onInteract, pair.ToggleDriver);

        // 出口大门 Z = 36：6 m 门洞，两侧砌墙
        GameObject door = Box(hall, "ExitDoor", new Vector3(0f, 3f, 36f),
            new Vector3(6f, 6f, 0.6f), new Color(0.40f, 0.34f, 0.28f), LayerShared);

        GateController gc = door.AddComponent<GateController>();
        gc.requiredPlates = new PressurePlate[0];
        gc.stayOpen = true;
        gc.gateVisual = door.transform;
        gc.openOffset = new Vector3(0f, 6.2f, 0f);
        gc.blocker = door.GetComponent<Collider>();
        if (pair.onCompleted == null) pair.onCompleted = new UnityEvent();
        UnityEventTools.AddPersistentListener(pair.onCompleted, gc.ForceOpen);

        // 门框墙：门洞两侧补满，防止绕过门
        Box(hall, "DoorFrame_L", new Vector3(-7.5f, 3f, 36f), new Vector3(5f, 6f, 0.6f), WallColor, LayerShared);
        Box(hall, "DoorFrame_R", new Vector3(7.5f, 3f, 36f), new Vector3(5f, 6f, 0.6f), WallColor, LayerShared);
        Box(hall, "DoorFrame_L2", new Vector3(-4f, 3f, 36f), new Vector3(2f, 6f, 0.6f), WallColor, LayerShared);
        Box(hall, "DoorFrame_R2", new Vector3(4f, 3f, 36f), new Vector3(2f, 6f, 0.6f), WallColor, LayerShared);

        // 两只巡卫的占位（白膜阶段不做 AI）
        Box(hall, "Patrol_Placeholder_1", new Vector3(-3f, 1.2f, 28f), new Vector3(0.9f, 1.2f, 0.9f),
            new Color(0.30f, 0.28f, 0.32f), Layer3D);
        Box(hall, "Patrol_Placeholder_2", new Vector3(3f, 1.2f, 30f), new Vector3(0.9f, 1.2f, 0.9f),
            new Color(0.30f, 0.28f, 0.32f), Layer3D);
    }

    // ---------------- 4. 错位断桥 ----------------
    private static void BuildBridgeSection(Transform hall)
    {
        // 门外过渡平台：把 6 m 门洞接到左断桥
        Box(hall, "Platform_DoorLink", new Vector3(0f, -0.5f, 37.2f), new Vector3(9f, 1f, 2.4f), FloorColor, LayerShared);

        // 3D 左断桥 Z ∈ [36, 42]，X = -5
        Box(hall, "Bridge_Left_3D", new Vector3(-5f, -0.3f, 39f), new Vector3(5f, 0.6f, 6f), FloorColor, LayerShared);
        // 3D 右断桥 Z ∈ [41, 47]，X = +5
        Box(hall, "Bridge_Right_3D", new Vector3(5f, -0.3f, 44f), new Vector3(5f, 0.6f, 6f), FloorColor, LayerShared);

        // 2D 连贯桥：横跨整段，X = 0
        GameObject carpet = Box(hall, "Carpet_Bridge_2D", new Vector3(0f, -0.3f, 41.5f),
            new Vector3(14f, 0.6f, 10.5f), CarpetColor, Layer2D);
        DimensionCollider dc = carpet.AddComponent<DimensionCollider>();
        dc.activeIn = DimensionMode.Past2D;
        dc.colliders = new Collider[] { carpet.GetComponent<Collider>() };

        // 碎片底座 + 电梯（白膜只放占位）
        Box(hall, "Elevator_Base", new Vector3(5f, 0.2f, 46.5f), new Vector3(3f, 0.4f, 3f),
            new Color(0.32f, 0.30f, 0.26f), LayerShared);
        Box(hall, "Shard_Pedestal", new Vector3(5f, 0.9f, 46.5f), new Vector3(0.8f, 0.6f, 0.8f),
            new Color(0.35f, 0.32f, 0.28f), LayerShared);
        Box(hall, "Shard_Fragment_1", new Vector3(5f, 1.6f, 46.5f), new Vector3(0.4f, 0.4f, 0.4f),
            new Color(1f, 0.8f, 0.35f), Layer3D);
    }

    // ---------------- 工具 ----------------
    private static int LayerShared { get { return LayerMask.NameToLayer("Shared_Both"); } }
    private static int Layer3D { get { return LayerMask.NameToLayer("Present3D_Only"); } }
    private static int Layer2D { get { return LayerMask.NameToLayer("Past2D_Only"); } }
    private static int LayerWall3D { get { return LayerMask.NameToLayer("RightWall_3D"); } }

    private static Transform MakeGear(Transform parent, string name, Vector3 pos)
    {
        GameObject gear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gear.name = name;
        gear.transform.SetParent(parent, false);
        gear.transform.position = pos;
        gear.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        gear.transform.localScale = new Vector3(3.3f, 0.125f, 3.3f);   // 半径 1.65
        gear.layer = LayerShared;
        gear.GetComponent<Renderer>().sharedMaterial = GetMaterial(BrassColor, false);
        return gear.transform;
    }

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
