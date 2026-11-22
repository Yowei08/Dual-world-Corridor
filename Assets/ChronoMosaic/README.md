# 《时痕拼图》相机 + 机关实现包

配套《时痕拼图_游戏设计策划案.docx》的 2.1 / 2.2 / 2.3 与七大机关。
引擎：Unity URP（按策划案口径）。全部脚本无第三方依赖，不挑 Input System 版本。

> **只想先看效果？** 看 [QUICKSTART.md](QUICKSTART.md)，10 分钟白盒跑起来。
> 菜单 Tools → Chrono Mosaic → 一键生成相机测试场景，按 Play 即可。
>
> 已在 Unity 6000.0/6000.3 实测：相机 30/30、七个机关 36/36 检查通过，过程中修掉 4 个真 bug。
> 见 [VERIFICATION.md](VERIFICATION.md)。
>
> 机关单独有一间实验室：菜单 **Tools → Chrono Mosaic → 4. 生成机关实验室**，
> 七个机关各摆一个，可以按 Q 切维度逐个试。
>
> 整关白膜：菜单 **6. 生成第一展厅白膜** —— 按策划案 §5.1 坐标搭的 48 m 第一展厅，
> 红毯过坑 → 十年树木 → 双墙齿轮 → 错位断桥，一条路走完（自动测试 30 项）。

---

## 一、文件清单

`text
chrono-mosaic/
├─ Camera/
│  ├─ JamInput.cs                      输入抽象（新旧 Input System 都能跑）
│  ├─ ThirdPersonCameraRig.cs          3D 第三人称轨道相机（挂主相机）
│  ├─ SideViewCamera2D.cs              2D 正交侧视相机（+X 看向 -X）
│  ├─ SafetyFootRing.cs                脚下安全光圈 + X 轴落点探测
│  ├─ DimensionTransitionController.cs 0.65 s 转场总控
│  ├─ ScreenWipe.cs                    全屏水彩遮罩（UI Image）
│  └─ SideViewFacing.cs                2D 里角色侧面朝向与翻面
└─ Mechanisms/
   ├─ DimensionMode.cs                 维度枚举 + 判定
   ├─ PressureWeight.cs                重量标记
   ├─ PressurePlate.cs                 压力踏板
   ├─ GateController.cs                门 / 闸 / 升降梯
   ├─ ChainWeight.cs                   悬吊配重（可砍断）
   ├─ GearPair.cs                      双墙齿轮
   ├─ GrowthTree.cs                    十年树木
   ├─ PrismBeam.cs / LightReceiver.cs  聚光棱镜与接收水晶
   ├─ PastEcho.cs / TimedDestroy.cs    过去残影
   ├─ LaserSweep.cs                    扫描激光
   ├─ PatrolEnemy.cs                   静滞巡卫（含正面盾变体）
   ├─ Interactable3D.cs / PlayerInteractor.cs  E 键交互
   ├─ SwordContracts.cs                近战接口
   └─ LevelGizmos.cs                   白盒辅助线
`

---

## 二、接入八步（按顺序，别跳）

### 1. 建 Layer
Unity → Layers 里新建 4 个，名字必须一字不差：

| Layer | 放什么 | 3D 相机 | 2D 相机 |
|---|---|---|---|
| `Present3D_Only` | 碎石、体积光、3D 枯木桥、激光红线、巡卫 | 显示 | 剔除 |
| `RightWall_3D` | +X 侧外墙与大型遮挡柱 | 显示 | 剔除 |
| `Past2D_Only` | 红毯桥、玻璃隔断、树苗、2D 背景板、木梯 | 剔除 | 显示 |
| `Shared_Both` | 地板、双墙齿轮、断桥、棱镜、大门 | 显示 | 显示 |

`DimensionTransitionController` 的 `autoBuildCullingMasks` 打开后会自动按这四个名字组 Mask；
名字写错会得到 0，脚本会退回相机原 Mask（画面会穿帮，但不会崩）。

### 2. 相机
- 主相机挂 `ThirdPersonCameraRig`，再加一个空物体 `Camera2D` 挂 `SideViewCamera2D`（默认隐藏）。
- 2D 相机 Transform 手动设 `(35, 6, 0)`、旋转 `(0, -90, 0)`，Projection 设为 Orthographic。
- 两个相机都不要挂 AudioListener（放在玩家身上，否则切换会静音）。

### 3. 玩家
- 根节点：移动脚本 + `PlayerInteractor` + `PastEcho` + `SideViewFacing`。
- 模型子物体拖给 `SideViewFacing.visual`。
- 有 Rigidbody 就拖给 `DimensionTransitionController.playerBody`；用 CharacterController 就拖 `playerCharacter`。

### 4. 光圈
- 玩家脚下放一个圆环 Mesh，挂 `SafetyFootRing`。
- `probe` = 玩家根节点，`halfWidth` = 房间半宽（第一展厅 10），`groundMask` 勾 `Shared_Both` + `Past2D_Only`。

### 5. 转场
- 空物体 `DimensionSystem` 挂 `DimensionTransitionController`。
- 把两个相机、光圈、`player`、`playerBody` 拖进去。
- `freezeDuringTransition` 里放：移动脚本、攻击脚本、相机跟随脚本（如果有）。

### 6. 遮罩
- Canvas（Screen Space - Overlay）下建全屏 Image，挂 `ScreenWipe`，颜色米白。
- 把 Image 拖给 `wipe.overlay`；有 `_Progress` 材质再拖 `wipeMaterial`。

### 7. 机关
按第五节表格接线。

### 8. 白盒辅助
- 空物体挂 `LevelGizmos`，Scene 视图里就能看到 X=0 投影平面和跳跃高度。

---

## 三、相机参数建议

| 参数 | 建议值 | 理由 |
|---|---|---|
| `distance`（3D） | 3.4 ~ 3.8 | 房间只有 20 m 宽，4.5 以上在走廊必贴墙 |
| `pivotOffset.y` | 1.45 | 胸口高度，角色占屏约 1/4 |
| `shoulderOffset` | 0.35 | 右肩偏移，正前方不被角色挡住 |
| `pitchLimits` | -25 ~ 65 | 低于 -25 会看到地板接缝 |
| `collisionRadius` | 0.25 | 再大在门框处会乱抖 |
| `autoAlignDelay` | 1.8 s | 无鼠标输入后自动回正，减少手忙脚乱 |
| `orthoSize`（2D） | 6.0 ~ 6.5 | 画面高 12 ~ 13 m，玩家落在下 1/3 |
| `verticalRange` | 3.0 ~ 8.5 | 防止拍到没做的天花板 |
| `lookAhead`（2D） | 2.0 ~ 2.5 | 侧视只能看到前方，前瞻必须给 |

**两条硬经验**：
1. 3D 相机只在 LateUpdate 里动，且在转场峰值帧 `SnapBehind()` 硬切，绝不 lerp——透视到正交数学上不连续，插值只会得到诡异的缩放。
2. 2D 相机永远不旋转。侧视图一旦有轻微俯仰，玩家对"能不能跳过去"的判断会全错。

---

## 四、0.65 s 时间轴对照

| 时间 | 策划案演出 | 代码里发生什么 |
|---|---|---|
| 0.00 | 举刃光晕、水彩从边缘收拢 | `SetFrozen(true)`，`wipe.Play(0.65)` |
| 0.14 ~ 0.36 | 遮罩覆盖全屏 | `_DimBlend` 0 → 1（材质换外观） |
| **0.25** | **峰值帧** | `ApplySwap()`：换相机、换 CullingMask、做 X 轴吸附 |
| 0.55 | 遮罩开始消散 | `SetFrozen(false)` 交还控制 |
| 0.65 | 光圈刷新、徽章翻完 | `busy = false` |

改时间轴只改 `swapAt` / `blendStart` / `blendEnd` / `releaseAt` 四个字段，不要改协程结构。

---

## 五、七大机关接线表

| 机关 | 用到的脚本 | 挂载 | 关键设置 |
|---|---|---|---|
| 红毯过坑 | 无（纯平台） | 2D 红毯桥归 `Past2D_Only` | 光圈自动判禁切 |
| 十年树木 | `GrowthTree` | 树苗/桥的父物体 | `waterIn=Past2D`，拖 sapling2D / bridge3D / bridgeCollider |
| 双墙齿轮 | `GearPair` + `GateController` | 齿轮父物体 / 大门 | `alignTolerance=0.45`，onMeshed 接 GateController.ForceOpen |
| 错位断桥 | 无（平台 + 光圈） | 左右桥 `Shared_Both` | 2D 连通碰撞体归 `Past2D_Only` |
| 跨墙砍链 | `ChainWeight` + `PressurePlate` | 配重 / 踏板 | 配重挂 `PressureWeight` |
| 聚光棱镜 | `PrismBeam` + `LightReceiver` | 棱镜 / 水晶 | `hitMask` 只勾水晶 + 墙，别勾玩家 |
| 过去残影 | `PastEcho` | 玩家 | `lifetime=15`，残影预制体挂 PressureWeight + Collider |
| 异面掩体 | `LaserSweep` | 激光眼 | `blockerMask` 勾大理石柱，柱子自然挡光 |
| Boss 红光桥 | 双层物件 | 3D 伤害盒 Present3D_Only / 2D 斜坡 Past2D_Only | 同一 Z 位置两层，切维度自动互换 |

### 交互（E）
拉杆、手轮、控制台都挂 `Interactable3D`，onInteract 里接对应方法：
- 齿轮拉杆 → `GearPair.ToggleDriver`
- 棱镜手轮 → `PrismBeam.RotateByStep`（传 1 或 -1）
- 花洒浇水 → `GrowthTree.Water`

---

## 六、三处必须先拍板的设计冲突

1. **"仅 3D 可挥砍" vs "2D 检修廊砍铁链"**
   键位表写鼠标左键只在 3D 生效，但第二展厅第 1 关和 Boss P1 都要求"切 2D 爬梯砍链"。
   现在 `ChainWeight.cuttableIn` 默认 `Both` 兜住了，但必须二选一：
   - 允许 2D 挥砍 → 删掉键位表那行限制，攻击脚本去掉维度判断；
   - 只许 3D 挥砍 → 这两个谜题改成"在 3D 砍断，靠 2D 把配重对到踏板正上方"。

2. **游玩时长 20 分钟 vs 40 分钟**
   1.1 表格写 20 分钟，核心规格写 40 分钟，6.1 验收又按 20 分钟白盒。
   20 天 5 人建议按 20 分钟锁死，第四展厅预算从 5 分钟压到 3 分钟。

3. **2D 相机的 X=+35 与"房间只有 ±10"**
   正交相机在 X=35，意味着 **X>0 的一切都会画在玩家前面**。
   右断桥（X=+5）、右墙齿轮（X=+9.5）在 2D 里都会挡住玩家。
   规则：2D 里 X>0 只允许放"会被剔除的墙"和"专门的 2D 前景剪影层"，
   所有参与碰撞的物件必须落在 X ≤ 0，或给它单独做一个 Past2D_Only 的视觉替身。

---

## 七、白盒尺寸速查（按策划案数值）

| 项 | 数值 | 用途 |
|---|---|---|
| 玩家身高 / 胶囊 | 1.8 m / (0.8, 0.9, 0.8) | 所有门洞 ≥ 2.2 m |
| 移速 | 5.5 m/s | 4 m 平台约 0.7 s 走完 |
| 单段跳 | 1.6 m | 台差 ≤ 1.4 m 可直接走上去 |
| 二段跳总高 | 3.0 m | 需要二段的地方给 2.6 ~ 2.8 m |
| 网格吸附 | 0.5 m | 全部模块坐标对齐 0.5 的倍数 |
| 走廊净宽 | ≥ 3 m | 再窄 3D 相机必穿墙 |
| 交互物高度 | 0.8 ~ 1.4 m | 站着就能按 E |

---

## 八、调试清单（出问题先看这）

| 现象 | 原因 | 处理 |
|---|---|---|
| 切 2D 后画面被墙挡住 | 右墙没进 RightWall_3D，或 Mask 名字拼错 | 看相机 Culling Mask 是否只勾了 2D 两组 |
| 切 2D 后角色瞬移 | TeleportTo 在遮罩透明时执行 | 确认 swapAt 落在 ScreenWipe 全遮区间 |
| 光圈一直红 | groundMask 没勾地板层，或 probe 不在脚底 | Scene 视图看 Gizmo 连线 |
| 切回 3D 落到深渊 | halfWidth 小于房间半宽 | 第一展厅必须 ≥ 10 |
| 转场中按 Q 卡住 | busy 没复位 | 协程末尾必须 busy = false |
| 2D 里角色原地转圈 | SideViewFacing 和移动脚本同时改朝向 | 2D 时冻结根节点旋转 |
| 相机穿过地板 | pitchLimits 下限太低 | -25 已经够用 |
| 机关在错误维度触发 | 没判 DimensionRules.ActiveIn | 所有机关第一行都该有这个判断 |

---

## 九、和已有 Shader 包的衔接

上一轮给的 jam-shader-kit（Jam/DimensionSurface + 全局 _DimBlend）可以直接接：
DimensionTransitionController 每帧写 Shader.SetGlobalFloat("_DimBlend", …)，
你不需要改 shader 一行，材质会自动从 PBR 感过渡到色阶纸片。