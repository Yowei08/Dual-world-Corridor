# 协作接口契约（给队友看的）

> 你交出去的不是"一堆脚本"，而是：**一个 Prefab + 一页参数 + 一个谁都能复现的测试**。
> 别人拿到这三个，不需要问你任何问题就能用、能调、能改。

---

## 1. 三层分工：谁能动什么

| 角色 | 可以改 | 不要改 |
|---|---|---|
| 关卡/策划 | 场景里 Prefab 实例的 `[Header("规则")]` 字段、Layer、命名 | 脚本、`[Header("引用")]` 以外的代码 |
| 美术 | Prefab 里的模型/材质、`[Header("表现")]` 下的字段 | 逻辑字段、碰撞体尺寸（除非同步给程序） |
| 程序 | 全部 | —— |

所以每个机关脚本都按 **规则 / 引用 / 表现 / 事件** 四段分组写 `[Header]`。
这不是洁癖：分组就是权限边界，策划打开 Inspector 一眼知道哪几个格子是他能动的。

---

## 2. 程序之间的接口

### 2.1 只读查询（谁都别自己维护"现在是不是 2D"）

`@csharp
DimensionRules.Is2D        // 当前是否 2D
DimensionRules.Busy        // 是否正在转场（0.65s 内）
DimensionRules.ActiveIn(DimensionMode.Past2D)   // 某个机关此刻是否生效
`@

反例：自己 `private bool is2D` 再在 `Update()` 里猜。转场那 0.65 秒里两边状态是矛盾的，
一旦有人自己记状态，机关就会在转场中途闪一下。

### 2.2 事件（让特效/音频/UI 挂上去，不用改机关代码）

`@csharp
DimensionTransitionController.OnTransitionBegin   // (bool to2D) 转场开始
DimensionTransitionController.OnDimensionChanged  // (bool to2D) 已换维度（遮罩最不透明那一帧）
DimensionTransitionController.OnSwitchDenied      // 落点不安全，被拒绝
`@

订阅规矩：

- 在 `OnEnable()` 里 `+=`，在 `OnDisable()` 里 `-=`。**不反订阅 = 场景切换后空引用**。
- 回调里不要 Instantiate 大东西，那 0.65 秒是性能最敏感的窗口。

### 2.3 两个"动词"接口

| 接口 | 谁实现 | 谁调用 | 语义 |
|---|---|---|---|
| `ISwordHittable.OnSwordHit(damage, point)` | 可被砍的东西（配重、敌人、藤蔓） | 玩家的攻击判定 | "我被这一刀打到了" |
| `IJumpPadReceiver.Launch(heightMeters)` | 玩家控制器 | 弹簧板等 | "以这个高度把我弹起来" |

这两个是**解耦点**：机关不知道玩家是谁，玩家不知道机关是什么。
你的攻击系统只要 `OverlapSphere` 之后找 `GetComponentInParent<ISwordHittable>()` 调一下，所有可击打物自动生效。

### 2.4 组件契约（约定俗成的"暗号"）

| 组件 | 作用 | 谁需要它 |
|---|---|---|
| `PressureWeight` | "我有多少重量，且此刻算不算数" | 任何要压踏板的东西（玩家、箱子、配重） |
| `DimensionCollider` | 这个碰撞体只在某个维度存在 | 2D 红毯桥、3D 箱子 |
| `Interactable3D` | 可 E 交互，带 `DimensionMode` 门控 | 拉杆、浇水、开门 |

**踏板只认 `PressureWeight`，不认标签、不认名字。** 想让它压得住，就给它挂这个组件。

---

## 3. 场景契约（给关卡和美术的硬规矩）

### 3.1 Layer 表（4 个，名字不能改）

| Layer | 放什么 | 2D 相机 | 3D 相机 |
|---|---|---|---|
| `Present3D_Only` | 只在 3D 存在的东西 | 不渲染 | 渲染 |
| `RightWall_3D` | 右侧墙/遮挡，2D 必须剔除 | 不渲染 | 渲染 |
| `Past2D_Only` | 只在 2D 存在的东西（红毯桥） | 渲染 | 不渲染 |
| `Shared_Both` | 两个维度都有：地板、玩家、主墙 | 渲染 | 渲染 |

三条必须记住：

1. **承重地面一律 `Shared_Both`**。落点探测只认这一层，放错层 = 站在地上却切不了维度。
2. **Layer 只管渲染，不管物理**。只切 Layer 的话，玩家在 2D 里还会撞到 3D 的箱子。
   物理要靠 `DimensionCollider` 开关碰撞体，两个都要做。
3. 玩家和光圈必须 `Shared_Both`，否则切过去人影消失。

### 3.2 命名

`@
Mech_<关卡号>_<机关名>      例：Mech_01_ChainWeight
Lab_<机关名>                实验室里的
`@

### 3.3 每个机关要拖的字段

机关脚本的 `[Header("引用")]` 下都是必填项，空着就是"没接线"。上线前用
**Tools → Chrono Mosaic → 2. 只检查维度 Layer** 和下面的测试各过一遍。

### 3.4 两条铁律（会直接做废关卡的）

1. **2D 里 X 是锁死的。** 只能在"按下 Q 那一刻"站的那条 X 上活动。
   → 同一条 2D 走廊上的机关，X 必须一致，否则玩家永远够不着。
2. **2D 相机在 +X 看向 -X。** 任何 X > 玩家位置的东西都会挡在镜头和角色之间。
   → X > 0 只放被剔除的墙或专用前景剪影；带碰撞的物件必须 X ≤ 0，或者有 `Past2D_Only` 的替身。

---

## 4. 写新机关时的骨架

照抄这个结构，队友就不用读你的实现：

`@csharp
public class MyMechanism : MonoBehaviour
{
    // 1. 规则：策划调
    [Header("规则")]
    public DimensionMode activeIn = DimensionMode.Present3D;
    public float someThreshold = 1f;

    // 2. 引用：必须接线，空引用要在 Awake 里明确报错
    [Header("引用")]
    public Transform movingPart;
    public Renderer indicator;

    // 3. 表现：美术调
    [Header("表现")]
    public Color idleColor = Color.gray;
    public float moveSpeed = 2f;

    // 4. 事件：给别人挂特效/音频
    [Header("事件")]
    public UnityEvent onTriggered;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        if (movingPart == null) Debug.LogError("[MyMechanism] movingPart 没接", this);
    }

    private void Update()
    {
        // 维度切换时，非本维度的状态必须能自己收回去
        if (!DimensionRules.ActiveIn(activeIn) && IsActive) { /* 复位 */ }
    }
}
`@

三条硬性要求：

- **维度门控**：不在本维度时必须能完整复位（踏板抬不起来、齿轮不转、光束灭）。
- **空引用不崩**：所有 `public` 引用在 `Awake()` 里判空并 `Debug.LogError(..., this)`，点一下日志就能定位到物体。
- **对外只暴露 `public` 方法 + `UnityEvent`**：别人不用读你的代码也能挂钩子。

---

## 5. 交付检查表（合并前过一遍）

- [ ] 脚本挂在 Prefab 上，不是散在场景层级里
- [ ] `[Header]` 分四段，逻辑字段和表现字段分开
- [ ] 所有引用字段在 `Awake()` 里判空
- [ ] 非本维度时能复位（切过去切回来不残留）
- [ ] 走 `DimensionRules` 查询，没有自己维护 is2D
- [ ] 攻击/弹射类走 `ISwordHittable` / `IJumpPadReceiver`
- [ ] 有一个 UnityEvent，能挂音效/特效
- [ ] 加进 `MechanismLab` 并且 **Tools → Chrono Mosaic → 5** 跑过

最后一条是关键：**没有进实验室的机关，等于没做完。**

---

## 6. 工程协作细节（不写进代码但会咬人的）

| 事 | 规矩 |
|---|---|
| `.meta` 文件 | **必须一起提交**。丢一个 meta，所有人打开就变成两个不同资源 |
| 共享文件 | `TagManager.asset`、`ProjectSettings.asset`、`manifest.json` —— 只有这三个，一次改完立刻提交，别两个人同时改 |
| 场景冲突 | `.unity` 是文本但几乎无法合并。按功能拆场景：`CameraTest` / `MechanismLab` / `Level_01`，用 Additive 加载 |
| 机关改版 | 改 Prefab，不要改场景里的实例。场景里只放实例，选中 Prefab 改一次全场生效 |
| 提交前 | 跑一次 **Tools → Chrono Mosaic → 5. 机关自动测试**（约 40 秒），红了别提交 |

---

## 7. 给非程序员的最小 API

策划/美术不写代码也能用的三个东西：

1. **Inspector 字段**：改了立刻在 Play 里看效果，不用重编译。
2. **UnityEvent**：`onTriggered` / `onOpened` / `onCut` 之类，在 Inspector 里点 `+` 挂 AudioSource.Play、ParticleSystem.Play、Animator.SetTrigger。
3. **public 方法**：需要按钮或别的脚本触发时，直接拖方法进去，方法名就是文档：
   `Open()` / `Water()` / `Spawn()` / `Swing()` / `SetAngle(float)` / `ForceRelease()` / `ToggleDriver()`
---

## 8. 机关判定要来自几何，不要来自开关

这条是这次返工换来的，建议全组统一。

**反例（错误）**：齿轮写一个 `bool canMesh = is2D;`。
能跑，但策划一改坐标（比如把两轮相隔从 19 m 改成 22 m）就全废了，
而且新人读完代码也不知道"为什么 2D 能咬合"。

**正例（现在实现）**：啮合 = 两轮外切。

`@csharp
间隙误差 = |轮心距 - (r1 + r2)|
轮心距 = 2D ? 投影(Y,Z)距离 : 真实三维距离
咬合 = 主动轮在转 && 间隙误差 <= 齿隙
`@

于是"3D 隔 19 m 咬不上、2D 投影相切能咬上"是**算出来的**，不是写死的。
实测：3D 间隙误差 16.0（不咬），2D 投影距 3.30 = r1+r2（间隙误差 0.000，咬合）。
策划把齿轮挪到任何地方，行为都自动正确。

同样的思路用在十年树木上：

- **判定**：花洒沿 Z 滑到树苗位置（距离判定），不是"按 E 就浇水"。
- **因果**：水在 2D 浇下去，桥在 3D 长出来（`Present3D_Only`），
  而且生长动画只在玩家**切回 3D 的那一帧**开始播放——玩家才看得见自己做了什么。
- **连带**：浇水同时让 2D 玻璃墙碎掉（碰撞体立刻关、网格延迟 0.15 s 隐藏），
  因为"树长大了撑破玻璃"是一个动作，不该做成两个开关。

写新机关时问自己一句：**这个机关的"通/不通"是算出来的，还是我打了个 bool？**
如果是 bool，八成会在改关卡时出问题。
