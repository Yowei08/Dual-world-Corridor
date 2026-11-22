// ChronoMosaicSceneBuilder.cs
// 放在 Editor/ 目录下。菜单 Tools → Chrono Mosaic → 一键生成相机测试场景。
// 它会：自动补齐 4 个维度 Layer → 搭一个带深渊/断桥/右墙的白盒 → 把相机、光圈、转场全部接线。
// 生成完直接按 Play 就能试。

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChronoMosaicSceneBuilder
{
    private const string RootName = "ChronoMosaicTest";
    private static readonly string[] LayerNames = { "Present3D_Only", "RightWall_3D", "Past2D_Only", "Shared_Both" };
    private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

    [MenuItem("Tools/Chrono Mosaic/1. 一键生成相机测试场景", false, 1)]
    public static void Build()
    {
        EnsureLayers();

        CleanupScene();

        GameObject root = new GameObject(RootName);

        int layer3D = LayerMask.NameToLayer("Present3D_Only");
        int layerWall = LayerMask.NameToLayer("RightWall_3D");
        int layer2D = LayerMask.NameToLayer("Past2D_Only");
        int layerShared = LayerMask.NameToLayer("Shared_Both");

        SetupLighting();
        BuildRoom(root.transform, layer3D, layerWall, layer2D, layerShared);
        BuildActors(root.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log("[ChronoMosaic] 场景已生成。按 Play：WASD 移动、鼠标转视角、Q 切维度。左上角有调试面板。");
    }

    /// <summary>
    /// 只生成"系统"（玩家 / 双相机 / 光圈 / 遮罩 / 转场 / HUD），不搭任何房间。
    /// 关卡生成器（第一展厅等）用它复用同一套底层，保证手感和相机测试场景完全一致。
    /// </summary>
    public static GameObject BuildSystems(string rootName)
    {
        EnsureLayers();
        CleanupScene();

        GameObject root = new GameObject(rootName);
        SetupLighting();
        BuildActors(root.transform);
        return root;
    }

    /// <summary>把当前场景存成指定路径（供自动化测试调用）。</summary>
    public static void SaveSceneAs(string path)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            SceneManager.GetActiveScene(), path);
        AssetDatabase.Refresh();
    }

    private static void BuildActors(Transform root)
    {
        GameObject player = BuildPlayer(root);
        ThirdPersonCameraRig rig = BuildCamera3D(player.transform);
        SideViewCamera2D side = BuildCamera2D(player.transform);
        SafetyFootRing ring = BuildFootRing(player.transform);
        ScreenWipe wipe = BuildWipe(root);

        BuildSystem(root, player, rig, side, ring, wipe);

        GameObject hud = new GameObject("DebugHUD");
        hud.transform.SetParent(root, false);
        hud.AddComponent<DimensionDebugHUD>();
    }

    [MenuItem("Tools/Chrono Mosaic/2. 只检查维度 Layer", false, 2)]
    public static void CheckLayers()
    {
        EnsureLayers();
        EditorUtility.DisplayDialog("Chrono Mosaic",
            "4 个维度 Layer 已就绪（缺失的会补到第 8~31 号空槽位）。", "好");
    }

    // ---------------- Layer ----------------

    private static void EnsureLayers()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        SerializedObject so = new SerializedObject(assets[0]);
        SerializedProperty layers = so.FindProperty("layers");
        if (layers == null) return;

        foreach (string name in LayerNames) EnsureLayer(layers, so, name);
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureLayer(SerializedProperty layers, SerializedObject so, string name)
    {
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == name) return;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty element = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = name;
                so.ApplyModifiedProperties();
                return;
            }
        }
        Debug.LogWarning("[ChronoMosaic] 没有空槽位可以创建 Layer: " + name);
    }

    // ---------------- 场景内容 ----------------

    private static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.26f, 0.28f, 0.32f);

        if (RenderSettings.sun != null) return;

        GameObject go = new GameObject("Sun");
        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(0.86f, 0.91f, 1f);
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        RenderSettings.sun = light;
    }

    private static void BuildRoom(Transform parent, int layer3D, int layerWall, int layer2D, int layerShared)
    {
        Color floorColor = new Color(0.28f, 0.31f, 0.35f);
        Color wallColor = new Color(0.20f, 0.23f, 0.27f);
        Color carpetColor = new Color(0.55f, 0.18f, 0.18f);

        // z 0~8 入口地板
        Box(parent, "Floor_A", new Vector3(0f, -0.5f, 4f), new Vector3(20f, 1f, 8f), floorColor, layerShared);
        // z 8~14 深渊（不放地板）
        // z 14~24 平台
        Box(parent, "Floor_B", new Vector3(0f, -0.5f, 19f), new Vector3(20f, 1f, 10f), floorColor, layerShared);

        // 2D 红毯桥：只在 2D 出现，正好补上深渊；碰撞体也必须跟着维度开关
        GameObject carpet = Box(parent, "Carpet_2D", new Vector3(0f, -0.22f, 11f),
            new Vector3(14f, 0.4f, 6.2f), carpetColor, layer2D);
        DimensionCollider carpetCollider = carpet.AddComponent<DimensionCollider>();
        carpetCollider.activeIn = DimensionMode.Past2D;
        carpetCollider.colliders = new Collider[] { carpet.GetComponent<Collider>() };

        // 左墙（两侧都能看到，作为 2D 的“剖面”）
        Box(parent, "LeftWall", new Vector3(-9.8f, 5f, 12f), new Vector3(0.4f, 10f, 24f), wallColor, layerShared);
        // 右墙：2D 必须被剔除，否则挡镜头
        Box(parent, "RightWall_3D", new Vector3(9.8f, 5f, 12f), new Vector3(0.4f, 10f, 24f),
            new Color(0.16f, 0.18f, 0.21f), layerWall);
        // 尽头
        Box(parent, "BackWall", new Vector3(0f, 5f, 24.2f), new Vector3(20f, 10f, 0.4f), wallColor, layerShared);

        // 相机碰撞测试柱（贴着走一圈，镜头应该自动拉近）
        Box(parent, "Pillar_CameraTest", new Vector3(3.5f, 2f, 5f), new Vector3(1f, 4f, 1f), floorColor, layerShared);
        // 2D 侧视的读图参照物
        Box(parent, "Pillar_Left_1", new Vector3(-6f, 2f, 6f), new Vector3(1f, 4f, 1f), floorColor, layerShared);
        Box(parent, "Pillar_Left_2", new Vector3(-6f, 2f, 18f), new Vector3(1f, 4f, 1f), floorColor, layerShared);
        // 只在 3D 出现的东西，用来验证剔除（碰撞体同样要按维度开关）
        GameObject debris = Box(parent, "Debris_3D", new Vector3(4f, 0.3f, 20f), new Vector3(2f, 0.6f, 2f),
            new Color(0.35f, 0.3f, 0.26f), layer3D);
        DimensionCollider debrisCollider = debris.AddComponent<DimensionCollider>();
        debrisCollider.activeIn = DimensionMode.Present3D;
        debrisCollider.colliders = new Collider[] { debris.GetComponent<Collider>() };
    }

    private static GameObject BuildPlayer(Transform parent)
    {
        // 玩家两个视角都要能看到，必须放在 Shared_Both，否则 2D 相机里角色会消失
        int layerShared = LayerMask.NameToLayer("Shared_Both");

        GameObject player = new GameObject("Player");
        player.transform.SetParent(parent, false);
        player.transform.position = new Vector3(0f, 0.02f, 2f);
        player.layer = layerShared;
        player.tag = "Player";

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.slopeLimit = 50f;
        cc.stepOffset = 0.35f;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
        visual.layer = layerShared;
        SetColor(visual.GetComponent<Renderer>(), new Color(0.86f, 0.76f, 0.55f));

        player.AddComponent<TestPlayerMotor>();
        player.AddComponent<PlayerInteractor>();

        SideViewFacing facing = player.AddComponent<SideViewFacing>();
        facing.visual = visual.transform;

        return player;
    }

    private static ThirdPersonCameraRig BuildCamera3D(Transform target)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        cam.transform.position = new Vector3(0f, 3f, -1.5f);
        cam.cullingMask = LayerMask.GetMask("Present3D_Only", "RightWall_3D", "Shared_Both");

        ThirdPersonCameraRig rig = cam.GetComponent<ThirdPersonCameraRig>();
        if (rig == null) rig = cam.gameObject.AddComponent<ThirdPersonCameraRig>();
        rig.target = target;
        rig.distance = 3.6f;
        rig.collisionMask = LayerMask.GetMask("Present3D_Only", "RightWall_3D", "Shared_Both");

        TestPlayerMotor motor = target.GetComponent<TestPlayerMotor>();
        if (motor != null) motor.camera3D = cam.transform;

        return rig;
    }

    private static SideViewCamera2D BuildCamera2D(Transform target)
    {
        GameObject go = new GameObject("Camera2D");

        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.13f, 0.10f, 0.09f);
        cam.depth = -1f;
        cam.cullingMask = LayerMask.GetMask("Past2D_Only", "Shared_Both");

        go.transform.position = new Vector3(35f, 6f, 8f);
        go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        TryAddUrpCameraData(go);

        SideViewCamera2D side = go.AddComponent<SideViewCamera2D>();
        side.target = target;
        side.cameraX = 35f;
        side.orthoSize = 6.5f;

        return side;
    }

    private static SafetyFootRing BuildFootRing(Transform player)
    {
        int layerShared = LayerMask.NameToLayer("Shared_Both");
        GameObject holder = new GameObject("FootRing");
        holder.layer = layerShared;
        holder.transform.SetParent(player, false);
        holder.transform.localPosition = new Vector3(0f, 0.03f, 0f);

        // 3D 视角看到的一片（水平）
        GameObject flat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flat.name = "Ring_3D";
        Object.DestroyImmediate(flat.GetComponent<Collider>());
        flat.transform.SetParent(holder.transform, false);
        flat.transform.localScale = new Vector3(0.95f, 0.04f, 0.95f);
        flat.layer = layerShared;
        SetEmissive(flat.GetComponent<Renderer>(), new Color(0.22f, 0.78f, 1f));

        // 2D 视角看到的一片（竖直，正对 +X 相机）
        GameObject side = GameObject.CreatePrimitive(PrimitiveType.Cube);
        side.name = "Ring_2D";
        Object.DestroyImmediate(side.GetComponent<Collider>());
        side.transform.SetParent(holder.transform, false);
        side.transform.localScale = new Vector3(0.04f, 0.95f, 0.95f);
        side.layer = layerShared;
        SetEmissive(side.GetComponent<Renderer>(), new Color(0.22f, 0.78f, 1f));

        SafetyFootRing ring = holder.AddComponent<SafetyFootRing>();
        ring.probe = player;
        ring.halfWidth = 10f;
        // 只认"两个维度都实心"的地面：否则站在 2D 红毯上会误判成有 3D 落点
        ring.groundMask = LayerMask.GetMask("Shared_Both");
        // 但"站得住"要放宽：2D 站在红毯上、3D 站在枯木桥上，都必须能按 Q
        ring.standMask = LayerMask.GetMask("Shared_Both", "Past2D_Only", "Present3D_Only");
        ring.obstructionMask = LayerMask.GetMask("Shared_Both", "Present3D_Only", "RightWall_3D", "Past2D_Only");
        ring.ringRenderer = flat.GetComponent<Renderer>();
        ring.additionalRingRenderers = new Renderer[] { side.GetComponent<Renderer>() };

        return ring;
    }

    private static ScreenWipe BuildWipe(Transform parent)
    {
        GameObject canvasGO = new GameObject("WipeCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(parent, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        GameObject imageGO = new GameObject("Wipe",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        imageGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rt = imageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = imageGO.GetComponent<Image>();
        image.color = new Color(0.96f, 0.93f, 0.85f, 0f);
        image.raycastTarget = false;

        ScreenWipe wipe = imageGO.AddComponent<ScreenWipe>();
        wipe.group = imageGO.GetComponent<CanvasGroup>();
        wipe.overlay = image;

        return wipe;
    }

    private static void BuildSystem(Transform parent, GameObject player, ThirdPersonCameraRig rig,
        SideViewCamera2D side, SafetyFootRing ring, ScreenWipe wipe)
    {
        GameObject sys = new GameObject("DimensionSystem");
        sys.transform.SetParent(parent, false);
        DimensionTransitionController ctrl = sys.AddComponent<DimensionTransitionController>();

        ctrl.player = player.transform;
        ctrl.playerCharacter = player.GetComponent<CharacterController>();
        ctrl.camera3D = rig;
        ctrl.camera2D = side;
        ctrl.footRing = ring;
        ctrl.wipe = wipe;

        TestPlayerMotor motor = player.GetComponent<TestPlayerMotor>();
        if (motor != null) ctrl.freezeDuringTransition = new Behaviour[] { motor };
    }

    // ---------------- 工具 ----------------

    private static void CleanupScene()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        // 清掉上一次运行可能残留的根级对象，避免重复
        DestroyRootObject("Hall01");
        DestroyRootObject("DimensionSystem");
        DestroyRootObject("DebugHUD");
        DestroyRootObject("Camera2D");
    }

    private static void DestroyRootObject(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null && go.transform.parent == null) Object.DestroyImmediate(go);
    }

    private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int layer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.layer = layer;
        SetColor(go.GetComponent<Renderer>(), color);
        return go;
    }

    private static void SetColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        renderer.sharedMaterial = GetMaterial(color, false);
    }

    private static void SetEmissive(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        renderer.sharedMaterial = GetMaterial(color, true);
    }

    private static Material GetMaterial(Color color, bool emissive)
    {
        string key = ColorUtility.ToHtmlStringRGBA(color) + (emissive ? "_e" : "_d");
        Material cached;
        if (MaterialCache.TryGetValue(key, out cached) && cached != null) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = emissive ? "CM_Emissive" : "CM_Surface";
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.6f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        MaterialCache[key] = material;
        return material;
    }

    private static void TryAddUrpCameraData(GameObject go)
    {
        System.Type type = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (type != null && go.GetComponent(type) == null) go.AddComponent(type);
    }
}
#endif
