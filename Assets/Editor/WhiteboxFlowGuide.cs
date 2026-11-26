using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class WhiteboxFlowGuide : EditorWindow
{
    private const string SceneGuid = "85237745e6be6d0429ebb82d1e06ece1";
    internal static string ScenePath => AssetDatabase.GUIDToAssetPath(SceneGuid);

    private static readonly string[] Acts = { "1 迎宾", "2 工坊", "3 档案", "4 终局" };

    [SerializeField] private int act;
    [SerializeField] private int siteIndex;
    private Point selectedPoint;
    private GameObject selectedTarget;
    private readonly Dictionary<Point, GameObject> targets = new();
    private Scene cachedScene;
    private Vector2 scroll;

    [MenuItem("Tools/白模机关导航")]
    private static void Open()
    {
        var window = GetWindow<WhiteboxFlowGuide>("白模机关导航");
        window.minSize = new Vector2(440, 390);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += LabelSelectedPoint;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= LabelSelectedPoint;
        SceneView.RepaintAll();
    }

    private void OnHierarchyChange()
    {
        targets.Clear();
        Repaint();
    }

    private void OnProjectChange()
    {
        OnHierarchyChange();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("时痕拼图 · 流程 V2", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("X 推进　Y 高度　Z 左右（2D 舞台位于 Z ≈ 200）", EditorStyles.wordWrappedMiniLabel);
        var path = ScenePath;
        var scene = string.IsNullOrEmpty(path) ? default : SceneManager.GetSceneByPath(path);
        var loaded = scene.IsValid() && scene.isLoaded;
        if (string.IsNullOrEmpty(path))
        {
            EditorGUILayout.HelpBox("当前项目找不到流程 V2 场景。请确认已同步包含该场景及其 .meta 的分支。", MessageType.Warning);
        }
        else if (!loaded)
        {
            EditorGUILayout.HelpBox("打开流程 V2 关卡，才能定位场景里的对象。", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("打开流程 V2 关卡") && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    loaded = scene.isLoaded;
                }
            }
        }

        var selection = Selection.activeGameObject;
        using (new EditorGUI.DisabledScope(!loaded || !selection || selection.scene != scene))
        {
            if (GUILayout.Button("查选中的方块"))
                FindSelection(selection, scene);
        }

        var nextAct = GUILayout.Toolbar(act, Acts);
        if (nextAct != act)
        {
            act = nextAct;
            SelectSite(0);
        }

        var sites = Array.FindAll(Sites, site => site.Act == act);
        var choices = Array.ConvertAll(sites, site => $"{site.Code}  {site.Title}");
        siteIndex = Mathf.Clamp(siteIndex, 0, sites.Length - 1);
        var nextSite = EditorGUILayout.Popup("机关 / 路段", siteIndex, choices);
        if (nextSite != siteIndex)
            SelectSite(nextSite);

        var current = sites[siteIndex];
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField(DescribeSite(current, scene), EditorStyles.miniBoldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("程序接入", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(current.Task, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("联调验收", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(current.Test, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        foreach (var point in current.Points)
        {
            var target = GetTarget(point, scene);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{point.World}  {(target ? target.name : point.Name)}", EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(!target))
                    {
                        if (GUILayout.Button("定位", GUILayout.Width(58)))
                            Focus(point, scene);
                    }
                }
                EditorGUILayout.LabelField(point.Role, EditorStyles.wordWrappedLabel);
                if (target)
                {
                    EditorGUILayout.LabelField($"当前坐标 {FormatPosition(target.transform.position)} m", EditorStyles.miniLabel);
                    if (selectedPoint == point)
                        EditorGUILayout.SelectableLabel(HierarchyPath(target.transform), EditorStyles.wordWrappedMiniLabel, GUILayout.Height(35));
                }
                else if (loaded)
                {
                    EditorGUILayout.LabelField("当前场景中未找到对应对象", EditorStyles.miniLabel);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    internal GameObject GetTarget(Point point, Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        if (cachedScene != scene)
        {
            cachedScene = scene;
            targets.Clear();
        }

        if (!targets.TryGetValue(point, out var target))
        {
            target = Resolve(point, scene);
            targets.Add(point, target);
        }
        return target;
    }

    private string DescribeSite(Site site, Scene scene)
    {
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        foreach (var point in site.Points)
        {
            var target = GetTarget(point, scene);
            if (!target)
                continue;
            var x = target.transform.position.x;
            min = Mathf.Min(min, x);
            max = Mathf.Max(max, x);
        }
        return float.IsPositiveInfinity(min)
            ? site.Kind
            : FormattableString.Invariant($"点位 X {min:0.##} ～ {max:0.##} m　{site.Kind}");
    }

    private static string HierarchyPath(Transform part)
    {
        var path = part.name;
        for (var parent = part.parent; parent; parent = parent.parent)
            path = parent.name + "/" + path;
        return path;
    }

    private void SelectSite(int index)
    {
        siteIndex = index;
        selectedPoint = null;
        selectedTarget = null;
        scroll = Vector2.zero;
        SceneView.RepaintAll();
    }

    private void FindSelection(GameObject selection, Scene scene)
    {
        var site = FindSite(selection, out var point);
        if (site == null)
        {
            ShowNotification(new GUIContent("此物体无机关条目。请看 Hierarchy 中的所属分组。"));
            return;
        }

        act = site.Act;
        var sameAct = Array.FindAll(Sites, item => item.Act == act);
        SelectSite(Array.IndexOf(sameAct, site));
        Focus(point, scene);
    }

    internal static Site FindSite(GameObject selection, out Point point)
    {
        for (var part = selection.transform; part; part = part.parent)
        {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(part.gameObject).ToString();
            foreach (var site in Sites)
            {
                point = Array.Find(site.Points, entry => entry.Id == id);
                if (point != null)
                    return site;
            }
        }

        point = null;
        return null;
    }

    private void Focus(Point point, Scene scene)
    {
        var target = GetTarget(point, scene);
        if (!target)
        {
            ShowNotification(new GUIContent("找不到此对象，请核对当前白模版本。"));
            return;
        }

        selectedPoint = point;
        selectedTarget = target;
        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView)
        {
            sceneView.FrameSelected();
            sceneView.Repaint();
        }
    }

    private void LabelSelectedPoint(SceneView sceneView)
    {
        if (!selectedTarget || selectedTarget != Selection.activeGameObject)
            return;

        var position = selectedTarget.transform.position;
        var scale = HandleUtility.GetHandleSize(position);
        var label = new GUIContent($"{selectedPoint.World} · {selectedPoint.Role}\n{selectedTarget.name}  {FormatPosition(position)} m");
        using (new Handles.DrawingScope(new Color(1f, 0.76f, 0.24f)))
        {
            Handles.DrawWireDisc(position, sceneView.camera.transform.forward, scale * 0.14f);
            Handles.DrawLine(position, position + Vector3.up * scale * 0.45f);
        }
        var style = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(8, 8, 5, 5),
        };
        Handles.Label(position + Vector3.up * scale * 0.45f, label, style);
    }

    private static string FormatPosition(Vector3 point)
    {
        return string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##}, {2:0.##})", point.x, point.y, point.z);
    }

    internal static GameObject Resolve(Point point, Scene scene)
    {
        if (GlobalObjectId.TryParse(point.Id, out var id) &&
            GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is GameObject target &&
            target && target.scene == scene)
            return target;

        var segments = point.Path.Split('/');
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == segments[0])
                return FindChildAtPath(root.transform, segments, 1, point.Position);
        }
        return null;
    }

    private static GameObject FindChildAtPath(Transform parent, string[] segments, int level, Vector3 position)
    {
        var index = level;
        while (index < segments.Length)
        {
            Transform match = null;
            foreach (Transform child in parent)
            {
                if (child.name != segments[index])
                    continue;

                if (match)
                {
                    // The three cover objects share a hierarchy path. Use their surveyed positions as the tie-breaker.
                    foreach (Transform candidate in parent)
                    {
                        if (candidate.name == child.name && (candidate.position - position).sqrMagnitude < 0.001f)
                            return FindChildAtPath(candidate, segments, index + 1, position);
                    }
                    return null;
                }
                match = child;
            }
            if (!match)
                return null;
            parent = match;
            index++;
        }
        return parent.gameObject;
    }

    internal sealed class Point
    {
        internal readonly string World;
        internal readonly string Role;
        internal readonly string Path;
        internal readonly string Id;
        internal readonly Vector3 Position;

        internal string Name => Path.Substring(Path.LastIndexOf('/') + 1);

        internal Point(string world, string role, string path, string id, Vector3 position)
        {
            World = world;
            Role = role;
            Path = path;
            Id = id;
            Position = position;
        }
    }

    internal sealed class Site
    {
        internal readonly string Code;
        internal readonly int Act;
        internal readonly string Title;
        internal readonly string Kind;
        internal readonly string Task;
        internal readonly string Test;
        internal readonly Point[] Points;

        internal Site(string code, int act, string title, string kind, string task, string test, params Point[] points)
        {
            Code = code;
            Act = act;
            Title = title;
            Kind = kind;
            Task = task;
            Test = test;
            Points = points;
        }
    }

    internal static readonly Site[] Sites =
    {
            new Site("1.01", 0, "起点、跳跃与切换教学", "主线", "从迎宾入口出发，在 2D 缺口前找安全落点切到 3D。按 Q 站稳切换，保持 X、Y 并读取安全落点引用。正式转场总长 1.0 s，在 0.35–0.40 s 切换世界。", "在入口稳定地面往返切换。跳跃中或无有效落点时拒绝切换，并给出 SFX-03 提示。",
                new Point("3D", "两世界起点标记", "3DWorld/Maps/Act_01_Welcome_000_228/Storys/01_Reception/Start_Act01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1817410839-0", new Vector3(-30.5000000f, 0.9200000f, 0.0000000f)),
                new Point("2D", "两世界起点标记", "2DWorld/Maps/Act_01_Welcome_000_228/Storys/01_Reception/Start_Act01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1404595455-0", new Vector3(-30.5000000f, 0.9200000f, 200.0000000f)),
                new Point("3D", "入口通行平台", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/01_Reception/Reception_Entry", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1093903978-0", new Vector3(-28.7500000f, -0.5750000f, 0.0000000f)),
                new Point("2D", "入口通行平台", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/01_Reception/Reception_Entry", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-171761415-0", new Vector3(-25.7700000f, -0.5750000f, 200.0000000f)),
                new Point("3D", "切换教学标记", "3DWorld/Maps/Act_01_Welcome_000_228/Storys/02_Carpet_Atrium/Time_Switch_Tutorial", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1828095738-0", new Vector3(-6.5000000f, 0.0000000f, -4.0000000f)),
                new Point("2D", "切换教学标记", "2DWorld/Maps/Act_01_Welcome_000_228/Storys/02_Carpet_Atrium/Time_Switch_Tutorial", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1093489282-0", new Vector3(-6.5000000f, 0.0000000f, 200.5800000f))),
            new Site("1.02", 0, "红毯断口", "主线", "3D 是断口。让玩家在桥前切到 2D，通过红毯，再在桥后切回 3D。保留这段的世界差异。", "分别从两侧测试切换。红毯上仅在对侧有安全落点时允许切换。",
                new Point("3D", "断口前稳定地面", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/02_Carpet_Atrium/Carpet_Approach", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1555959031-0", new Vector3(-7.5000000f, -0.5750000f, 0.0000000f)),
                new Point("2D", "断口前稳定地面", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/02_Carpet_Atrium/Carpet_Approach", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1876701473-0", new Vector3(-7.5000000f, -0.5750000f, 200.0000000f)),
                new Point("2D", "过去世界可走红毯", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/02_Carpet_Atrium/Carpet_Bridge_Past", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1701035064-0", new Vector3(0.3000000f, -0.5750000f, 200.0000000f)),
                new Point("3D", "断口后落点", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/02_Carpet_Atrium/Carpet_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-600313615-0", new Vector3(9.3000000f, -0.5750000f, 0.0000000f)),
                new Point("2D", "断口后落点", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/02_Carpet_Atrium/Carpet_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1010575693-0", new Vector3(9.3000000f, -0.5750000f, 200.0000000f))),
            new Site("1.03", 0, "浇水、树桥与玻璃隔断", "主线", "在 2D 取得花洒并给树苗浇水，写入共享树桥状态。2D 玻璃挡路；玩家回 3D 走长成的树桥。白模树桥现在可踩，程序接入时按状态开放。成长提示 SFX-16。", "分别测试未浇水、浇水完成、切回 3D、抵达对岸。初始状态下树桥通行应受控。",
                new Point("2D", "花洒占位，接拾取", "2DWorld/Maps/Act_01_Welcome_000_228/Machines/03_Ecology_Court/Watering_Can", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1367391881-0", new Vector3(20.3000000f, 0.0000000f, 200.5800000f)),
                new Point("2D", "树苗占位，接浇水交互", "2DWorld/Maps/Act_01_Welcome_000_228/Machines/03_Ecology_Court/Sprout_And_Stump", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2044693037-0", new Vector3(21.9000000f, 0.0000000f, 200.5800000f)),
                new Point("2D", "2D 阻挡墙，有碰撞", "2DWorld/Maps/Act_01_Welcome_000_228/Machines/03_Ecology_Court/Past_Glass_Barrier", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1017921337-0", new Vector3(27.7500000f, 2.6000000f, 200.0000000f)),
                new Point("3D", "3D 树桥实体，接可通行状态", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/03_Ecology_Court/Living_Tree_Ramp_Reality", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2145074633-0", new Vector3(29.5000000f, 0.4250000f, 0.0000000f)),
                new Point("3D", "对岸落点", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/03_Ecology_Court/Tree_Bridge_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1146809026-0", new Vector3(40.0000000f, 1.4250000f, 0.0000000f)),
                new Point("2D", "对岸落点", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/03_Ecology_Court/Tree_Bridge_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-525155795-0", new Vector3(40.0000000f, 1.4250000f, 200.0000000f))),
            new Site("1.04", 0, "守卫与中庭视野", "战斗、可选探索", "巡卫方块确定战斗站位，用正式敌人预制体替换。掩体是实际障碍。观测点只承载交互与叙事。", "测试 3D 巡卫在平台活动、掩体能阻挡；跳过观测交互仍可到中央广场。",
                new Point("3D", "普通巡卫 A 的生成位置", "3DWorld/Maps/Act_01_Welcome_000_228/Machines/04_Guard_Court/Sentinel_A", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1119385837-0", new Vector3(64.5000000f, 4.0000000f, -7.3000000f)),
                new Point("3D", "普通巡卫 B 的生成位置", "3DWorld/Maps/Act_01_Welcome_000_228/Machines/04_Guard_Court/Sentinel_B", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-631924875-0", new Vector3(69.5000000f, 4.0000000f, 7.0000000f)),
                new Point("3D", "左掩体", "3DWorld/Maps/Act_01_Welcome_000_228/Decorations/04_Guard_Court/Cover_Block_Left", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1817447571-0", new Vector3(66.5000000f, 5.0000000f, 11.0000000f)),
                new Point("3D", "右掩体", "3DWorld/Maps/Act_01_Welcome_000_228/Decorations/04_Guard_Court/Cover_Block_Right", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-697135824-0", new Vector3(70.5000000f, 5.0000000f, -11.0000000f)),
                new Point("3D", "可选观测点", "3DWorld/Maps/Act_01_Welcome_000_228/Storys/04A_Vista_And_Manuscript/Manuscript_Vista", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1737508171-0", new Vector3(98.5000000f, 4.0000000f, -27.0000000f))),
            new Site("1.05", 0, "双墙齿轮与大门", "主线、可选探索", "把中央拉杆、两套齿轮表现和出口门接到共享状态。2D 确认投影咬合后放行。白模门框对象名保留 Double_Lock，流程 V2 以齿轮联动放行为准，两翼祭坛和手稿供可选探索。齿轮到位 SFX-14，开门 SFX-18。", "跳过两侧支路，验证齿轮对位后仍可通关，两世界门状态一致。",
                new Point("3D", "中央拉杆输入", "3DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Central_Lever", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1741098920-0", new Vector3(117.0000000f, 4.0000000f, -4.8000000f)),
                new Point("2D", "中央拉杆输入", "2DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Central_Lever", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1058978303-0", new Vector3(117.0000000f, 4.0000000f, 200.5800000f)),
                new Point("3D", "左齿轮占位，接转轴", "3DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Gear_Left", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-722290066-0", new Vector3(118.7000000f, 8.5000000f, -22.0000000f)),
                new Point("2D", "左齿轮占位，接转轴", "2DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Gear_Left", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1636642109-0", new Vector3(118.7000000f, 8.5000000f, 202.1000000f)),
                new Point("3D", "右齿轮占位，接转轴", "3DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Gear_Right", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-537940952-0", new Vector3(124.6000000f, 8.5000000f, 22.0000000f)),
                new Point("2D", "右齿轮占位，接转轴", "2DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Gear_Right", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1422224983-0", new Vector3(124.6000000f, 8.5000000f, 202.1000000f)),
                new Point("3D", "右翼祭坛，支路展陈或可选交互", "3DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Right_Energy_Altar", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1058532176-0", new Vector3(131.5000000f, 4.0000000f, 25.6000000f)),
                new Point("2D", "右翼祭坛，支路展陈或可选交互", "2DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Right_Energy_Altar", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-782858799-0", new Vector3(131.5000000f, 4.0000000f, 200.5800000f)),
                new Point("3D", "左翼手稿观景台", "3DWorld/Maps/Act_01_Welcome_000_228/Storys/05_Grand_Transept/Left_Manuscript_Lookout", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1672774642-0", new Vector3(129.5000000f, 4.0000000f, -25.3000000f)),
                new Point("2D", "左翼手稿观景台", "2DWorld/Maps/Act_01_Welcome_000_228/Storys/05_Grand_Transept/Left_Manuscript_Lookout", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1667036412-0", new Vector3(129.5000000f, 4.0000000f, 200.5800000f)),
                new Point("3D", "3D 门框，作为门预制体定位参照", "3DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Double_Lock_Gate_Lintel", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1187557030-0", new Vector3(139.5000000f, 13.2500000f, 0.0000000f)),
                new Point("2D", "2D 门框，作为门预制体定位参照", "2DWorld/Maps/Act_01_Welcome_000_228/Machines/05_Grand_Transept/Double_Lock_Gate_Frame", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-630560591-0", new Vector3(139.5000000f, 8.5000000f, 200.7800000f))),
            new Site("1.06", 0, "错位桥、碎片 1 与温室跳台", "主线", "断桥有 3D 加宽石台与 2D 横版高台。2D 起跳线标出助跑起跳位置，高台按二段跳验收。碎片 1 在桥后，接入时需确认玩家在高台前已有二段跳能力。", "在 2D 测试贴墙停下、单跳不够高、二段跳能落到高台。高台有 3D 对应引用，过桥后检查碎片不会重复拾取。",
                new Point("3D", "3D 左桥段", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/06_Split_Skybridge/Split_Left_Reality", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1999477762-0", new Vector3(149.9500000f, 3.4250000f, -4.5000000f)),
                new Point("3D", "3D 右桥段", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/06_Split_Skybridge/Split_Right_Reality", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1166827695-0", new Vector3(190.1249800f, 3.4250000f, 5.5000000f)),
                new Point("2D", "2D 高台前的助跑桥面", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/06_Split_Skybridge/Split_Straight_Past", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-447827755-0", new Vector3(149.9500000f, 3.4250000f, 200.0000000f)),
                new Point("2D", "2D 二段跳起跳线", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/06_Split_Skybridge/Jump_Takeoff_Past", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2100071000-0", new Vector3(153.1500000f, 4.0400000f, 199.8900000f)),
                new Point("2D", "2D 二段跳高台，带侧面挡板", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/06_Split_Skybridge/Split_mid_cross_2_Past", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2100000016-0", new Vector3(158.7500000f, 5.2188500f, 200.0000000f)),
                new Point("3D", "3D 对齐的二段跳高台", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/06_Split_Skybridge/Split_mid_cross_2", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-25132303-0", new Vector3(158.7500000f, 5.2188500f, -5.0000000f)),
                new Point("3D", "桥后安全落点", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/06_Split_Skybridge/Split_Far_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1856182296-0", new Vector3(195.0000000f, 3.4250000f, 0.0000000f)),
                new Point("2D", "桥后安全落点", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/06_Split_Skybridge/Split_Far_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1749875862-0", new Vector3(195.0000000f, 3.4250000f, 200.0000000f)),
                new Point("3D", "碎片 1 拾取占位", "3DWorld/Maps/Act_01_Welcome_000_228/Storys/06_Split_Skybridge/Fragment_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-617978579-0", new Vector3(195.0000000f, 4.0000000f, 4.0000000f)),
                new Point("2D", "碎片 1 拾取占位", "2DWorld/Maps/Act_01_Welcome_000_228/Storys/06_Split_Skybridge/Fragment_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-744801513-0", new Vector3(195.0000000f, 4.0000000f, 200.5800000f)),
                new Point("3D", "温室第一跳台", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/07_Conservatory_Terraces/Conservatory_Tier_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1086738656-0", new Vector3(199.5000000f, 3.4250000f, 0.0000000f)),
                new Point("2D", "温室第一跳台", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/07_Conservatory_Terraces/Conservatory_Tier_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1184509512-0", new Vector3(199.5000000f, 3.4250000f, 200.0000000f)),
                new Point("3D", "温室第二跳台", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/07_Conservatory_Terraces/Conservatory_Tier_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-981637522-0", new Vector3(211.2500000f, 5.3400000f, 0.0000000f)),
                new Point("2D", "温室第二跳台", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/07_Conservatory_Terraces/Conservatory_Tier_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-996660933-0", new Vector3(211.2500000f, 4.4250000f, 200.0000000f)),
                new Point("3D", "温室第三跳台", "3DWorld/Maps/Act_01_Welcome_000_228/Grounds/07_Conservatory_Terraces/Conservatory_Tier_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2114689312-0", new Vector3(223.7500000f, 5.4250000f, 0.0000000f)),
                new Point("2D", "温室第三跳台", "2DWorld/Maps/Act_01_Welcome_000_228/Grounds/07_Conservatory_Terraces/Conservatory_Tier_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1113145714-0", new Vector3(223.7500000f, 5.4250000f, 200.0000000f))),
            new Site("2.01", 1, "配重砍链与放行", "主线", "2D 观察或对位 → 走 3D 检修坡 → 在 3D 站位攻击锁链 → 落下配重压板并开门。链条功能、落体和撞板由程序连接。砍链与配重撞板使用 SFX-15。", "2D 观察台即使攻击也不能断链。只有 3D 链条接受有效攻击；配重到板后开门，回程坡可返回。",
                new Point("2D", "2D 对位平台", "2DWorld/Maps/Act_02_Workshop_228_540/Grounds/01_Counterweight_Hall/Weight_2D_Alignment_Perch", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1022423739-0", new Vector3(246.7500000f, 11.4800000f, 200.0000000f)),
                new Point("3D", "3D 去检修坡", "3DWorld/Maps/Act_02_Workshop_228_540/Grounds/01_Counterweight_Hall/Weight_3D_Service_Ramp", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-982971209-0", new Vector3(239.5000000f, 6.0000000f, 10.2500000f)),
                new Point("3D", "3D 攻击站位", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Workshop_3D_Cut_Stance", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2074692548-0", new Vector3(245.1500100f, 12.0000000f, 8.2500000f)),
                new Point("3D", "唯一砍链接收点", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Workshop_Cuttable_Chain_3D", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-900404923-0", new Vector3(245.8000000f, 14.3100000f, 7.0000000f)),
                new Point("3D", "落体对位参照", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Counterweight_Impact_Plate_Projection", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1845578145-0", new Vector3(245.8000000f, 6.1000000f, -7.0000000f)),
                new Point("3D", "配重外观占位", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Counterweight", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-323822433-0", new Vector3(245.8000000f, 11.4500000f, 7.0000000f)),
                new Point("2D", "配重外观占位", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Counterweight", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1393907472-0", new Vector3(245.8000000f, 11.4500000f, 202.1000060f)),
                new Point("3D", "压板占位", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Weight_Pressure_Plate", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-980018748-0", new Vector3(245.8000000f, 6.0000000f, -7.0000000f)),
                new Point("2D", "压板占位", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Weight_Pressure_Plate", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2034050844-0", new Vector3(245.8000000f, 6.0000000f, 200.5800018f)),
                new Point("3D", "3D 门框", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Counterweight_Gate_Lintel", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-589144952-0", new Vector3(252.0000000f, 13.2500000f, 0.0000000f)),
                new Point("2D", "2D 门框", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/01_Counterweight_Hall/Counterweight_Gate_Frame", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-291463225-0", new Vector3(252.0000000f, 9.5000000f, 200.7800000f))),
            new Site("2.02", 1, "回响中庭与盾巡卫", "过渡、可选探索", "观测台、手稿和展台提供可选交互。钟盘是巨型场景装饰。盾巡卫在 3D 正面格挡，随切换在 2D 停止追踪并锁定朝向，允许玩家绕后回 3D 攻击。", "测试正面弹刀、2D 冻结、回 3D 绕背击破；确认观测交互结束能回到主路。",
                new Point("3D", "黄铜声学观测台", "3DWorld/Maps/Act_02_Workshop_228_540/Storys/02_Clock_Echo_Court/Acoustic_Vista_Scope", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-910281964-0", new Vector3(272.9700000f, 7.0000000f, 15.5100000f)),
                new Point("2D", "黄铜声学观测台", "2DWorld/Maps/Act_02_Workshop_228_540/Storys/02_Clock_Echo_Court/Acoustic_Vista_Scope", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1125461369-0", new Vector3(273.0000000f, 7.0000000f, 200.5800000f)),
                new Point("3D", "手稿收集占位", "3DWorld/Maps/Act_02_Workshop_228_540/Storys/02_Clock_Echo_Court/Manuscript_02_Pedestal", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1028674905-0", new Vector3(288.0000000f, 7.0000000f, -19.0000000f)),
                new Point("2D", "手稿收集占位", "2DWorld/Maps/Act_02_Workshop_228_540/Storys/02_Clock_Echo_Court/Manuscript_02_Pedestal", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1295767738-0", new Vector3(288.0000000f, 7.0000000f, 200.5800000f)),
                new Point("3D", "左钟面，仅场景装饰", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/02_Clock_Echo_Court/Clock_Dial_Left", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1679436331-0", new Vector3(278.0000000f, 14.0000000f, -19.0000000f)),
                new Point("2D", "左钟面，仅场景装饰", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/02_Clock_Echo_Court/Clock_Dial_Left", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1974700792-0", new Vector3(278.0000000f, 14.0000000f, 202.1000000f)),
                new Point("3D", "右钟面，仅场景装饰", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/02_Clock_Echo_Court/Clock_Dial_Right", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-22980568-0", new Vector3(284.0000000f, 14.0000000f, 19.0000000f)),
                new Point("2D", "右钟面，仅场景装饰", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/02_Clock_Echo_Court/Clock_Dial_Right", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1692501160-0", new Vector3(284.0000000f, 14.0000000f, 202.1000000f)),
                new Point("3D", "钟表展品台", "3DWorld/Maps/Act_02_Workshop_228_540/Storys/02A_Assembly_Gallery/Clockwork_Exhibit", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1378394468-0", new Vector3(321.0000000f, 7.0000000f, 28.5000000f)),
                new Point("3D", "盾巡卫与盾牌占位", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/02A_Assembly_Gallery/Shield_Sentinel_Staging", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1543819854-0", new Vector3(328.0000000f, 7.0000000f, -5.8000000f)),
                new Point("2D", "盾巡卫的 2D 对位标记", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/02A_Assembly_Gallery/Shield_Sentinel_2D_Projection", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-404960886-0", new Vector3(328.0000000f, 7.0000000f, 200.6000061f))),
            new Site("2.03", 1, "聚光棱镜与碎片 2", "主线", "手轮驱动滑轨棱镜，接收水晶判定光束对齐。完成后开放碎片 2，拾取后解锁 2D 右键生成 15 s 的过去残影。接收成功用 SFX-17，收集用 SFX-20。", "错误角度时水晶保持未激活；正确位置时反馈清晰，切世界不会重置完成状态。",
                new Point("3D", "棱镜和滑轨的目标区域", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/03_Prism_Gallery/Prism_Rail", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-334234335-0", new Vector3(352.0000000f, 7.0000000f, -8.0000000f)),
                new Point("2D", "棱镜和滑轨的目标区域", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/03_Prism_Gallery/Prism_Rail", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1569575337-0", new Vector3(352.0000000f, 7.0000000f, 200.5800000f)),
                new Point("3D", "供玩家操控的手轮位置", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/03_Prism_Gallery/Prism_Wheel", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-914309844-0", new Vector3(356.0000000f, 7.0000000f, -4.5000000f)),
                new Point("2D", "供玩家操控的手轮位置", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/03_Prism_Gallery/Prism_Wheel", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1051412810-0", new Vector3(356.0000000f, 7.0000000f, 200.5800000f)),
                new Point("3D", "接收水晶位置", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/03_Prism_Gallery/Receiver_Crystal", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1140339556-0", new Vector3(362.0000000f, 7.0000000f, 8.5000000f)),
                new Point("2D", "接收水晶位置", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/03_Prism_Gallery/Receiver_Crystal", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1466504955-0", new Vector3(362.0000000f, 7.0000000f, 200.5800000f)),
                new Point("3D", "碎片 2 拾取占位", "3DWorld/Maps/Act_02_Workshop_228_540/Storys/03_Prism_Gallery/Fragment_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-721961475-0", new Vector3(379.7800000f, 9.0000000f, 0.3000000f)),
                new Point("2D", "碎片 2 拾取占位", "2DWorld/Maps/Act_02_Workshop_228_540/Storys/03_Prism_Gallery/Fragment_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-638945894-0", new Vector3(379.0000000f, 9.0000000f, 200.5800000f))),
            new Site("2.04", 1, "云海跳台与右翼绞盘", "主线、可选探索", "保留跳台之间的跳跃距离，三座台作为白模通行基准。右翼绞盘和晶柱是可选支路。", "从中段进入右翼再返回，验证也能跳过整个支路继续到残影门。",
                new Point("3D", "第一跳台", "3DWorld/Maps/Act_02_Workshop_228_540/Grounds/04_Leap_Terraces/Leap_Tier_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1490128613-0", new Vector3(386.0000000f, 8.4250000f, 0.0000000f)),
                new Point("2D", "第一跳台", "2DWorld/Maps/Act_02_Workshop_228_540/Grounds/04_Leap_Terraces/Leap_Tier_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-535446539-0", new Vector3(386.0000000f, 8.4250000f, 200.0000000f)),
                new Point("3D", "第二跳台", "3DWorld/Maps/Act_02_Workshop_228_540/Grounds/04_Leap_Terraces/Leap_Tier_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-320957443-0", new Vector3(400.7500000f, 10.2200000f, 0.0000000f)),
                new Point("2D", "第二跳台", "2DWorld/Maps/Act_02_Workshop_228_540/Grounds/04_Leap_Terraces/Leap_Tier_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-434545311-0", new Vector3(400.7500000f, 10.2200000f, 200.0000000f)),
                new Point("3D", "第三跳台", "3DWorld/Maps/Act_02_Workshop_228_540/Grounds/04_Leap_Terraces/Leap_Tier_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-648086068-0", new Vector3(418.2500000f, 9.3250000f, 0.0000000f)),
                new Point("2D", "第三跳台", "2DWorld/Maps/Act_02_Workshop_228_540/Grounds/04_Leap_Terraces/Leap_Tier_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1361587505-0", new Vector3(418.2500000f, 9.3250000f, 200.0000000f)),
                new Point("3D", "可选侧翼绞盘占位", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/04_Leap_Terraces/SubLock_Winch", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1966959813-0", new Vector3(403.0000000f, 9.4500000f, 15.5000000f)),
                new Point("2D", "可选侧翼绞盘占位", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/04_Leap_Terraces/SubLock_Winch", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-93030078-0", new Vector3(403.0000000f, 12.1600000f, 200.5800000f)),
                new Point("3D", "右翼可选手稿", "3DWorld/Maps/Act_02_Workshop_228_540/Storys/04_Leap_Terraces/Manuscript_Winch_Wing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1293364931-0", new Vector3(405.0000000f, 9.4500000f, 17.4000000f)),
                new Point("2D", "右翼可选手稿", "2DWorld/Maps/Act_02_Workshop_228_540/Storys/04_Leap_Terraces/Manuscript_Winch_Wing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1427015516-0", new Vector3(405.0000000f, 12.1200000f, 200.5800000f))),
            new Site("2.05", 1, "A→B 残影踏板与门", "主线", "在 2D 的 A 按右键留下 15 s 残影，再回 3D 踩 B。主门只检查 A、B；3D 的 C 只用于可选捷径。", "验证 A/B 时间窗和状态同步；单独踩 C 不开主门。跳过 C 与侧翼绞盘仍可通关。",
                new Point("2D", "A：2D 残影踏板", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/05_Echo_Plate_Tiers/A_Past_Clone_Plate", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-264752134-0", new Vector3(436.0000000f, 10.0000000f, 200.5800000f)),
                new Point("3D", "B：3D 端踏板", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/05_Echo_Plate_Tiers/B_Reality_Exit_Plate", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1970231835-0", new Vector3(449.0000000f, 10.5000000f, 3.8000000f)),
                new Point("3D", "C：3D 可选捷径踏板", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/05_Echo_Plate_Tiers/C_Optional_Shortcut_Plate", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-386983302-0", new Vector3(461.0000000f, 11.0000000f, -16.5000000f)),
                new Point("3D", "3D 残影门框", "3DWorld/Maps/Act_02_Workshop_228_540/Machines/05_Echo_Plate_Tiers/Echo_Double_Lock_Lintel", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-829589157-0", new Vector3(465.0000000f, 17.2500000f, 0.0000000f)),
                new Point("2D", "2D 残影门框", "2DWorld/Maps/Act_02_Workshop_228_540/Machines/05_Echo_Plate_Tiers/Echo_Double_Lock_Frame", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1983478545-0", new Vector3(465.0000000f, 14.0000000f, 200.7800000f))),
            new Site("2.06", 1, "观景天阶、展品与悬索吊桥", "主线、可选探索", "两处展台接可选今昔对比；到断崖前切 2D 走完好的吊桥，到岸后再切回。保留 3D 断崖。", "展台播放结束回到原位。桥上按 Q 时，缺少 3D 安全落点应拒绝切换。",
                new Point("3D", "第一处展台", "3DWorld/Maps/Act_02_Workshop_228_540/Storys/06_Cliff_Exhibit_Trail/Cliff_Exhibit_Past_And_Present_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1140255102-0", new Vector3(479.0000000f, 12.0000000f, -8.0000000f)),
                new Point("2D", "第一处展台", "2DWorld/Maps/Act_02_Workshop_228_540/Storys/06_Cliff_Exhibit_Trail/Cliff_Exhibit_Past_And_Present_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2146601181-0", new Vector3(479.0000000f, 12.0000000f, 200.5800000f)),
                new Point("3D", "第二处展台", "3DWorld/Maps/Act_02_Workshop_228_540/Storys/06_Cliff_Exhibit_Trail/Cliff_Exhibit_Past_And_Present_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-598414277-0", new Vector3(496.0000000f, 13.0000000f, 8.0000000f)),
                new Point("2D", "第二处展台", "2DWorld/Maps/Act_02_Workshop_228_540/Storys/06_Cliff_Exhibit_Trail/Cliff_Exhibit_Past_And_Present_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1353659105-0", new Vector3(496.0000000f, 13.0000000f, 200.5800000f)),
                new Point("3D", "可选手稿位置", "3DWorld/Maps/Act_02_Workshop_228_540/Storys/06_Cliff_Exhibit_Trail/Cliff_Manuscript", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-293878464-0", new Vector3(499.0000000f, 13.0000000f, 6.5000000f)),
                new Point("2D", "可选手稿位置", "2DWorld/Maps/Act_02_Workshop_228_540/Storys/06_Cliff_Exhibit_Trail/Cliff_Manuscript", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1007787970-0", new Vector3(499.0000000f, 13.0000000f, 200.5800000f)),
                new Point("3D", "吊桥前", "3DWorld/Maps/Act_02_Workshop_228_540/Grounds/07_Suspension_Bridge/Suspension_Approach", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-74735884-0", new Vector3(516.0000000f, 13.4250000f, 0.0000000f)),
                new Point("2D", "吊桥前", "2DWorld/Maps/Act_02_Workshop_228_540/Grounds/07_Suspension_Bridge/Suspension_Approach", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1181152761-0", new Vector3(516.0000000f, 13.4250000f, 200.0000000f)),
                new Point("2D", "过去世界完整吊桥", "2DWorld/Maps/Act_02_Workshop_228_540/Grounds/07_Suspension_Bridge/Suspension_Bridge_Past", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2119533200-0", new Vector3(524.0000000f, 13.9250000f, 200.0000000f)),
                new Point("3D", "吊桥后", "3DWorld/Maps/Act_02_Workshop_228_540/Grounds/07_Suspension_Bridge/Suspension_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-28438200-0", new Vector3(534.0000000f, 14.4250000f, 0.0000000f)),
                new Point("2D", "吊桥后", "2DWorld/Maps/Act_02_Workshop_228_540/Grounds/07_Suspension_Bridge/Suspension_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-505918840-0", new Vector3(534.0000000f, 14.4250000f, 200.0000000f))),
            new Site("3.01", 2, "激光长廊与三根掩体", "主线", "接机械眼扫描、命中伤害与重置。3D 掩体与 2D 投影要按各自的遮挡规则接。三个站位对应三根掩体。", "逐根测试掩体后方安全，离开掩体会被扫描，并检查切换后的遮挡状态。",
                new Point("3D", "3D 机械眼占位", "3DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Laser_Eye", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1304527182-0", new Vector3(556.0000000f, 23.0000000f, 14.5000000f)),
                new Point("2D", "2D 机械眼占位", "2DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Laser_Eye", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-212753417-0", new Vector3(556.0000000f, 23.0000000f, 202.1000000f)),
                new Point("3D", "3D 掩体 1", "3DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Laser_Cover_Left_Column", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1306575679-0", new Vector3(546.0000000f, 18.9000000f, -7.4000000f)),
                new Point("3D", "3D 掩体 2", "3DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Laser_Cover_Left_Column", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-50128793-0", new Vector3(553.0000000f, 18.9000000f, -7.4000000f)),
                new Point("3D", "3D 掩体 3", "3DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Laser_Cover_Left_Column", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1416237554-0", new Vector3(560.0000000f, 18.9000000f, -7.4000000f)),
                new Point("2D", "2D 投影掩体 1", "2DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Laser_Cover_Projected", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1777429740-0", new Vector3(546.0000000f, 18.1500000f, 206.0000000f)),
                new Point("2D", "2D 投影掩体 2", "2DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Laser_Cover_Projected", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1403407853-0", new Vector3(553.0000000f, 18.1500000f, 206.0000000f)),
                new Point("2D", "2D 投影掩体 3", "2DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Laser_Cover_Projected", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1020292632-0", new Vector3(560.0000000f, 18.1500000f, 206.0000000f))),
            new Site("3.02", 2, "圣殿树桥与档案展陈", "主线、可选探索", "2D 浇水后切回 3D 走树坡，抵达通路检查点后记录第一重封印。档案展陈可跳过。", "验证浇水前后树坡通行变化，过桥后的第一重封印在切换后持续有效。",
                new Point("2D", "2D 花洒与树苗组合占位", "2DWorld/Maps/Act_03_Archives_540_662/Machines/01_Pillar_Shadow_Hall/Watering_Can_And_Sprout", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-158291164-0", new Vector3(560.0000000f, 15.0000000f, 200.5800000f)),
                new Point("3D", "3D 树桥通路", "3DWorld/Maps/Act_03_Archives_540_662/Grounds/01_Pillar_Shadow_Hall/Sanctum_Tree_Ramp_Reality", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1453418870-0", new Vector3(572.0000000f, 15.0000000f, 0.0000000f)),
                new Point("2D", "2D 对岸落点", "2DWorld/Maps/Act_03_Archives_540_662/Grounds/01_Pillar_Shadow_Hall/Sanctum_Tree_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1581957481-0", new Vector3(578.0000000f, 18.4250000f, 200.0000000f)),
                new Point("3D", "可选档案展台", "3DWorld/Maps/Act_03_Archives_540_662/Storys/01A_Archive_Reading_Annex/Designers_Archive_Exhibit", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-260136314-0", new Vector3(604.0000000f, 19.0000000f, -28.0000000f)),
                new Point("3D", "封印一：树桥检查位置", "3DWorld/Maps/Act_03_Archives_540_662/Machines/02_Astral_Echo_Court/Seal_1_Tree_Route_Check", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-871339934-0", new Vector3(626.0000000f, 19.0000000f, -4.5000000f))),
            new Site("3.03", 2, "三重封印与档案区出口", "主线", "封印一记树桥通过；封印二接拉杆、棱镜与齿轮联动；封印三接 2D 残影踏板。三项满足后开门，观测与手稿仍为可选。", "逐项关闭验证不会提前放行。补上第二重联动的活动构件时，以检查点为逻辑位置；装饰星环可作外观参考。",
                new Point("3D", "封印一检查点", "3DWorld/Maps/Act_03_Archives_540_662/Machines/02_Astral_Echo_Court/Seal_1_Tree_Route_Check", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-871339934-0", new Vector3(626.0000000f, 19.0000000f, -4.5000000f)),
                new Point("3D", "封印二输入组件占位", "3DWorld/Maps/Act_03_Archives_540_662/Machines/02_Astral_Echo_Court/Sealing_Lever_And_Prism", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-105506242-0", new Vector3(630.0000000f, 19.0000000f, -5.0000000f)),
                new Point("2D", "封印二输入组件占位", "2DWorld/Maps/Act_03_Archives_540_662/Machines/02_Astral_Echo_Court/Sealing_Lever_And_Prism", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-378687755-0", new Vector3(630.0000000f, 19.0000000f, 200.5800000f)),
                new Point("3D", "封印二目标位置", "3DWorld/Maps/Act_03_Archives_540_662/Machines/02_Astral_Echo_Court/Seal_2_Prism_And_Gears", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1697021130-0", new Vector3(638.0000000f, 19.0000000f, -6.4000000f)),
                new Point("2D", "封印三：残影踏板", "2DWorld/Maps/Act_03_Archives_540_662/Machines/02_Astral_Echo_Court/Seal_3_Past_Clone_Plate", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2014863027-0", new Vector3(653.0000000f, 19.1200000f, 200.3500061f)),
                new Point("3D", "可选观测台", "3DWorld/Maps/Act_03_Archives_540_662/Storys/02_Astral_Echo_Court/Astral_Vista_Scope", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-292218449-0", new Vector3(646.0000000f, 19.0000000f, 24.0000000f)),
                new Point("2D", "可选观测台", "2DWorld/Maps/Act_03_Archives_540_662/Storys/02_Astral_Echo_Court/Astral_Vista_Scope", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1526721085-0", new Vector3(646.0000000f, 19.0000000f, 200.5800000f)),
                new Point("3D", "可选手稿收集", "3DWorld/Maps/Act_03_Archives_540_662/Storys/02_Astral_Echo_Court/Echo_Manuscript", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-736803888-0", new Vector3(647.0000000f, 19.0000000f, -27.0000000f)),
                new Point("2D", "可选手稿收集", "2DWorld/Maps/Act_03_Archives_540_662/Storys/02_Astral_Echo_Court/Echo_Manuscript", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-934751082-0", new Vector3(647.0000000f, 19.0000000f, 200.5800000f)),
                new Point("3D", "3D 三封印门框", "3DWorld/Maps/Act_03_Archives_540_662/Machines/02_Astral_Echo_Court/Seal_Gate_Lintel", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-820742506-0", new Vector3(659.0000000f, 27.2500000f, 0.0000000f)),
                new Point("2D", "2D 三封印门框", "2DWorld/Maps/Act_03_Archives_540_662/Machines/02_Astral_Echo_Court/Seal_Gate_Frame", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1862564233-0", new Vector3(659.0000000f, 23.0000000f, 200.7800000f))),
            new Site("4.01", 3, "P1 对位大钟与 3D 砍链", "Boss 主线", "拉杆经双墙齿轮联动天钟导轨，将大钟对位到巨像上方。2D 对位观察后回 3D 检修台，在断链点左键攻击，落钟破甲完成 P1。断链用 SFX-15，撞击和冲击波用 SFX-19。", "在 2D 尝试攻击、在 3D 未对位时尝试攻击，再测试正确流程。检查砍中后不会重复触发落钟。",
                new Point("3D", "大钟导轨拉杆", "3DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/Bell_Lever", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-710672575-0", new Vector3(711.0000000f, 19.0000000f, -5.0000000f)),
                new Point("2D", "大钟导轨拉杆", "2DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/Bell_Lever", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-862771407-0", new Vector3(711.0000000f, 19.0000000f, 200.5800000f)),
                new Point("3D", "大钟初始外观占位", "3DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/Giant_Bell", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1729273495-0", new Vector3(712.0000000f, 32.2000000f, 14.3000000f)),
                new Point("2D", "大钟初始外观占位", "2DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/Giant_Bell", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-55944421-0", new Vector3(712.0000000f, 32.2000000f, 202.1000060f)),
                new Point("3D", "3D 导轨", "3DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/Bell_High_Rail", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1801122201-0", new Vector3(724.5000000f, 43.5000000f, 14.3000000f)),
                new Point("2D", "2D 导轨轮廓", "2DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/Bell_Rail_Past", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-244160755-0", new Vector3(724.5000000f, 43.5000000f, 202.3999940f)),
                new Point("2D", "2D 观察／对位台", "2DWorld/Maps/Act_04_Finale_662_900/Grounds/03_Bell_Sanctum_P1/Bell_2D_Alignment_Perch", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1345577624-0", new Vector3(739.2500000f, 36.6800000f, 200.0000000f)),
                new Point("3D", "3D 检修平台", "3DWorld/Maps/Act_04_Finale_662_900/Grounds/03_Bell_Sanctum_P1/Bell_3D_Stage_Maintenance", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1127126451-0", new Vector3(721.0000000f, 19.0000000f, 10.6500000f)),
                new Point("3D", "3D 砍链站位", "3DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/P1_Bell_3D_Cut_Stance", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2072283719-0", new Vector3(738.3500000f, 37.2000000f, 13.0500000f)),
                new Point("3D", "P1 唯一可砍锁链", "3DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/P1_Bell_Cuttable_Chain_3D", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1413303087-0", new Vector3(739.0000000f, 39.0000000f, 14.3000000f)),
                new Point("3D", "落钟对位目标", "3DWorld/Maps/Act_04_Finale_662_900/Machines/03_Bell_Sanctum_P1/P1_Bell_Rail_Docking_Position", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-871435708-0", new Vector3(739.0000000f, 32.2000000f, 14.3000000f)),
                new Point("3D", "3D 回程坡", "3DWorld/Maps/Act_04_Finale_662_900/Grounds/04_Colossus_Cliff_P2/Bell_3D_Return_Ramp", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2049682706-0", new Vector3(759.0000000f, 37.2000000f, 10.6500000f)),
                new Point("3D", "共享 Boss 的两套模型占位", "3DWorld/Maps/Act_04_Finale_662_900/Machines/04_Colossus_Cliff_P2/Boss_Arrogance_Colossus", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-211263060-0", new Vector3(739.0000000f, 12.0000000f, 14.3000000f)),
                new Point("2D", "共享 Boss 的两套模型占位", "2DWorld/Maps/Act_04_Finale_662_900/Machines/04_Colossus_Cliff_P2/Boss_Arrogance_Colossus", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-603432655-0", new Vector3(739.0000000f, 12.0000000f, 202.1000060f))),
            new Site("4.02", 3, "P2 残影快门与棱镜破盾", "Boss 主线", "在 3D 调棱镜，切到 2D 的正式快门踏板留下残影，光束命中盾接收点后回 3D 近战，开启通往 P3 的路径。旧的 Bell_Shutter_Plate是服务台旁占位；使用下方列出的正式 P2 残影快门踏板。", "踏板或棱镜任一未达成时不能破盾；破盾后再接受 3D 挥砍。光束目标显式绑定到接收点。",
                new Point("2D", "2D 侧掩体", "2DWorld/Maps/Act_04_Finale_662_900/Storys/04_Colossus_Cliff_P2/P2_Cover_Anchor/P2_Side_Cover", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1622681087-0", new Vector3(750.0000000f, 23.0000000f, 205.3000030f)),
                new Point("2D", "正式 P2 残影快门踏板", "2DWorld/Maps/Act_04_Finale_662_900/Machines/04_Colossus_Cliff_P2/P2_Shutter_Past_Clone_Plate", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2043086006-0", new Vector3(757.0000000f, 20.1200000f, 200.3999939f)),
                new Point("3D", "3D 棱镜操作点", "3DWorld/Maps/Act_04_Finale_662_900/Machines/04_Colossus_Cliff_P2/Cliff_Prism", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1040244350-0", new Vector3(764.0000000f, 20.0000000f, -5.0000000f)),
                new Point("3D", "Boss 盾上的光束目标", "3DWorld/Maps/Act_04_Finale_662_900/Machines/04_Colossus_Cliff_P2/P2_Shield_Receiver", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-222340735-0", new Vector3(739.0000000f, 21.3500000f, 14.3000000f)),
                new Point("3D", "临崖测试区位置", "3DWorld/Maps/Act_04_Finale_662_900/Machines/04_Colossus_Cliff_P2/Cliff_Cover_Test", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1774866665-0", new Vector3(769.0000000f, 20.0000000f, -2.1000000f)),
                new Point("2D", "临崖测试区位置", "2DWorld/Maps/Act_04_Finale_662_900/Machines/04_Colossus_Cliff_P2/Cliff_Cover_Test", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1763467922-0", new Vector3(769.0000000f, 20.0000000f, 200.5800000f))),
            new Site("4.03", 3, "碎裂柱廊与星台飞跃", "主线、可选探索", "四座星台用于二段跳，右翼绞盘与第三段录音按当前流程放为可选互动。主线继续前往阳光桥。", "跳过侧翼也能抵达两世界共有的桥前安全落点。检查四座平台的可达性。",
                new Point("3D", "星台 1", "3DWorld/Maps/Act_04_Finale_662_900/Grounds/05_Star_Leap_Colonnade/Star_Leap_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1903285818-0", new Vector3(781.0000000f, 19.4250000f, 0.0000000f)),
                new Point("2D", "星台 1", "2DWorld/Maps/Act_04_Finale_662_900/Grounds/05_Star_Leap_Colonnade/Star_Leap_01", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-938525763-0", new Vector3(781.0000000f, 19.4250000f, 200.0000000f)),
                new Point("3D", "星台 2", "3DWorld/Maps/Act_04_Finale_662_900/Grounds/05_Star_Leap_Colonnade/Star_Leap_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-564414028-0", new Vector3(793.7500000f, 21.7700000f, 0.0000000f)),
                new Point("2D", "星台 2", "2DWorld/Maps/Act_04_Finale_662_900/Grounds/05_Star_Leap_Colonnade/Star_Leap_02", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1776279428-0", new Vector3(793.7500000f, 21.7700000f, 200.0000000f)),
                new Point("3D", "星台 3", "3DWorld/Maps/Act_04_Finale_662_900/Grounds/05_Star_Leap_Colonnade/Star_Leap_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1477579871-0", new Vector3(805.7500000f, 20.1250000f, 0.0000000f)),
                new Point("2D", "星台 3", "2DWorld/Maps/Act_04_Finale_662_900/Grounds/05_Star_Leap_Colonnade/Star_Leap_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-52154135-0", new Vector3(805.7500000f, 20.1250000f, 200.0000000f)),
                new Point("3D", "星台 4", "3DWorld/Maps/Act_04_Finale_662_900/Grounds/05_Star_Leap_Colonnade/Star_Leap_04", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1669656855-0", new Vector3(821.7500000f, 20.4250000f, 0.0000000f)),
                new Point("2D", "星台 4", "2DWorld/Maps/Act_04_Finale_662_900/Grounds/05_Star_Leap_Colonnade/Star_Leap_04", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1290346387-0", new Vector3(821.7500000f, 20.4250000f, 200.0000000f)),
                new Point("3D", "右翼可选绞盘", "3DWorld/Maps/Act_04_Finale_662_900/Machines/05_Star_Leap_Colonnade/Dome_SubLock_Winch", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-288558453-0", new Vector3(805.5000000f, 20.7000000f, 15.0000000f)),
                new Point("2D", "右翼可选绞盘", "2DWorld/Maps/Act_04_Finale_662_900/Machines/05_Star_Leap_Colonnade/Dome_SubLock_Winch", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1050666926-0", new Vector3(805.5000000f, 20.7000000f, 200.5800000f)),
                new Point("3D", "可选第三段录音触发位置", "3DWorld/Maps/Act_04_Finale_662_900/Storys/05_Star_Leap_Colonnade/Recording_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1751400596-0", new Vector3(823.0000000f, 21.0000000f, -4.0000000f)),
                new Point("2D", "可选第三段录音触发位置", "2DWorld/Maps/Act_04_Finale_662_900/Storys/05_Star_Leap_Colonnade/Recording_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2046164290-0", new Vector3(823.0000000f, 21.0000000f, 200.5800000f))),
            new Site("4.04", 3, "P3 阳光桥、对齐落点与最终击破", "Boss 主线", "走 2D 阳光桥取得碎片 3，靠明确标记的安全位置切回 3D，再攻击最终核心。猩红光束为 3D 表现／危害位置，光门由击破状态放行。", "从 Past 切换位置到 Reality 落点验证 X、Y 对齐，以两处列出的实际点位为准。碎片可共享拾取，在 2D 无法触发最终击破。",
                new Point("3D", "3D 猩红光束占位", "3DWorld/Maps/Act_04_Finale_662_900/Machines/06_Astral_Dome_P3/P3_Red_Beam_Preview", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1961693853-0", new Vector3(845.0000000f, 22.1250000f, 0.0000000f)),
                new Point("2D", "2D 可走阳光桥", "2DWorld/Maps/Act_04_Finale_662_900/Grounds/06_Astral_Dome_P3/Sunroad_Past", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1367907811-0", new Vector3(845.0000000f, 20.9250000f, 200.0000000f)),
                new Point("3D", "碎片 3 占位", "3DWorld/Maps/Act_04_Finale_662_900/Storys/06_Astral_Dome_P3/Fragment_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2063170414-0", new Vector3(852.7500000f, 22.0000000f, -3.6000000f)),
                new Point("2D", "碎片 3 占位", "2DWorld/Maps/Act_04_Finale_662_900/Storys/06_Astral_Dome_P3/Fragment_03", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-593223377-0", new Vector3(852.7500000f, 22.0000000f, 200.5800018f)),
                new Point("2D", "切换前的 2D 安全站位", "2DWorld/Maps/Act_04_Finale_662_900/Grounds/06_Astral_Dome_P3/P3_Aligned_Past_Switch_Position", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-280236296-0", new Vector3(853.3000000f, 22.0000000f, 200.0000000f)),
                new Point("3D", "切换后的 3D 安全落点", "3DWorld/Maps/Act_04_Finale_662_900/Grounds/06_Astral_Dome_P3/P3_Aligned_Reality_Safe_Landing", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-404061816-0", new Vector3(853.3000000f, 22.0000000f, 0.0000000f)),
                new Point("3D", "最终核心攻击接收位置", "3DWorld/Maps/Act_04_Finale_662_900/Machines/06_Astral_Dome_P3/P3_Final_Strike_Core_3D", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-253293753-0", new Vector3(854.4500000f, 23.2500000f, 0.9500000f)),
                new Point("3D", "3D 出口框", "3DWorld/Maps/Act_04_Finale_662_900/Machines/06_Astral_Dome_P3/Starlight_Exit_Arch_Lintel", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1271437980-0", new Vector3(860.5000000f, 31.2500000f, 0.0000000f)),
                new Point("2D", "2D 出口框", "2DWorld/Maps/Act_04_Finale_662_900/Machines/06_Astral_Dome_P3/Starlight_Exit_Arch_Frame", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-179641328-0", new Vector3(860.5000000f, 26.5000000f, 200.7800000f))),
            new Site("4.05", 3, "星坛控制台、插销与双结局", "主线、可选探索", "经登神坡道到 Y26 星坛。三枚碎片合一后在 3D 拔插销，两世界读同一状态。3D 按 E 重启控制台触发复苏；进入 2D 画框门触发另一结局。展品做可选对比。", "验证插销未开时两边不能提前出门；开启后分别从 3D 与 2D 验收各自的结局触发。",
                new Point("3D", "展台 A", "3DWorld/Maps/Act_04_Finale_662_900/Storys/07_Ascension_And_Altar/Museum_Exhibit_A", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-813807049-0", new Vector3(873.0000000f, 24.0000000f, -7.5000000f)),
                new Point("2D", "展台 A", "2DWorld/Maps/Act_04_Finale_662_900/Storys/07_Ascension_And_Altar/Museum_Exhibit_A", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1614031308-0", new Vector3(873.0000000f, 24.0000000f, 200.5800000f)),
                new Point("3D", "展台 B", "3DWorld/Maps/Act_04_Finale_662_900/Storys/07_Ascension_And_Altar/Museum_Exhibit_B", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2015803251-0", new Vector3(877.0000000f, 24.0000000f, 7.5000000f)),
                new Point("2D", "展台 B", "2DWorld/Maps/Act_04_Finale_662_900/Storys/07_Ascension_And_Altar/Museum_Exhibit_B", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1936175543-0", new Vector3(877.0000000f, 24.0000000f, 200.5800000f)),
                new Point("3D", "终端外观占位", "3DWorld/Maps/Act_04_Finale_662_900/Machines/07_Ascension_And_Altar/Final_Console", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1422990479-0", new Vector3(894.0000000f, 26.0000000f, 3.6000000f)),
                new Point("2D", "终端外观占位", "2DWorld/Maps/Act_04_Finale_662_900/Machines/07_Ascension_And_Altar/Final_Console", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2140619531-0", new Vector3(894.0000000f, 26.0000000f, 200.5800000f)),
                new Point("3D", "现实侧复苏触发点", "3DWorld/Maps/Act_04_Finale_662_900/Machines/07_Ascension_And_Altar/Ending_3D_Console_Revival", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-2128672385-0", new Vector3(894.0000000f, 26.0000000f, 3.6000000f)),
                new Point("3D", "独立插销交互点", "3DWorld/Maps/Act_04_Finale_662_900/Machines/07_Ascension_And_Altar/Ending_Bolt_3D_Interact", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1204263214-0", new Vector3(897.0500000f, 27.3500000f, -1.4000000f)),
                new Point("3D", "3D 最终门框", "3DWorld/Maps/Act_04_Finale_662_900/Machines/07_Ascension_And_Altar/Final_Museum_Gate_Lintel", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-328808495-0", new Vector3(898.0000000f, 36.2500000f, 0.0000000f)),
                new Point("2D", "2D 最终门框", "2DWorld/Maps/Act_04_Finale_662_900/Machines/07_Ascension_And_Altar/Final_Museum_Gate_Frame", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-584164811-0", new Vector3(898.0000000f, 31.0000000f, 200.7800000f)),
                new Point("2D", "2D 穿门触发位置", "2DWorld/Maps/Act_04_Finale_662_900/Machines/07_Ascension_And_Altar/Ending_2D_Through_Portal", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1233729364-0", new Vector3(899.2000000f, 26.0000000f, 200.0000000f)),
                new Point("3D", "两种路线终点标记", "3DWorld/Maps/Act_04_Finale_662_900/Storys/07_Ascension_And_Altar/Finish", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-880121736-0", new Vector3(899.5000000f, 26.0000000f, 0.0000000f)),
                new Point("2D", "两种路线终点标记", "2DWorld/Maps/Act_04_Finale_662_900/Storys/07_Ascension_And_Altar/Finish", "GlobalObjectId_V1-2-85237745e6be6d0429ebb82d1e06ece1-1285414722-0", new Vector3(899.5000000f, 26.0000000f, 200.0000000f)))
    };
}
