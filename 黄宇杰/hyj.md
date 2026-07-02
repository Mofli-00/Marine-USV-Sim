# 赛艇游戏实时小地图迭代日志

这份文档记录了小地图从最初的“贪吃蛇线框”一步步迭代为“大厂级实景圆角地图”的全过程。中间经历了多次方案推翻、底层逻辑重构以及打包环境下的 Bug 排查。

---

## 第一阶段：外框重塑

**目标**：把原本用线框画出来的死地图，替换成能显示高空航拍画面的实景圆形地图。

<img width="1024" height="639" alt="image" src="https://github.com/user-attachments/assets/e752c11c-3d91-45a2-a0e1-79cbc57c26b2" />


* **初始状态**：挂载了 RenderTexture 的画布被挤压成了一条弯曲的细线。原因是父物体 `Map` 的 Mask（遮罩）使用的是原版赛道贴图 `RaceMap`。
* **❌ 踩坑方案 1：使用 `slicedDot`**
* *做法*：将遮罩模具替换为自带的 `slicedDot` 资产。
* *结果*：由于该资产是做 UI 九宫格用的圆角方块，导致地图变成了圆角正方形，不符合正圆的需求。


* **❌ 踩坑方案 2：使用 `WhiteCircle`**
* *做法*：在项目里翻出了 `WhiteCircle` 资产并替换。
* *结果*：这是一个空心圆环，导致实景地图只能在细细的一圈边缘显示，中间全被掏空漏出了底色。


* **✅ 最终方案：自建纯实心圆**
* *做法*：直接在 Unity 引擎内右键 `Create -> 2D -> Sprites -> Circle`，自己生成了一个绝对完美的纯实心白圆。将其作为 `Map` 的遮罩源图片，并取消勾选 `Show Mask Graphic`（隐藏白底），完美裁切出圆形实景。


---

## 第二阶段：跨场景部署与相机逻辑重构

**目标**：将做好的小地图打包成预制体，拖入 `level_Pyramid` 等正式关卡中，并让高空相机精准跟随玩家。

* **初始状态**：把相机拖入正式 Level 后，小地图变成了纯红色或纯蓝色的实心圆饼，看不到地形。
* **❌ 踩坑方案 1：错误的名称追踪**
* *分析*：以为是代码找不到船了，看到 Level 场景里有个叫 `Player 1` 的节点，就在代码里加上了 `GameObject.Find("Player 1")`。
* *结果*：地图依然是纯色圆饼，而且完全失去了移动追踪的效果（误以为 `Player 1` 是个静态文件夹）。


* **❌ 踩坑方案 2：致命的高度硬编码**
* *分析*：旧版 `MinimapFollow.cs` 脚本里有一句 `public float cameraHeight = 150f;`。
* *结果*：无论在面板里把相机抬多高，一运行就会被代码强行拽回 150 的高度。而正式关卡的连绵大山远超 150，导致相机直接一头扎进山体内部，拍出来的全是穿模的纯色贴图。


* **✅ 最终方案：自适应高度 + 备用主相机雷达**
* *修改 1*：删除了固定的 150 高度，改为在 `Start()` 里读取面板设置的初始高度（`currentHeight = transform.position.y`），彻底解决穿模。
* *修改 2*：在确认 `Player 1` 确实包含实体船组件（`BoatHull`）后，保留了对 `Player 1` 的追踪。
* *修改 3*：加入了容错逻辑，如果找不到船实体，强制跟随主相机（`Camera.main`）移动，确保视野永不丢失。

---

## 第三阶段：UI 渲染层级与透明度陷阱排查

**目标**：解决在 Prefab 同步和场景修改过程中导致的小地图频繁隐身或变色问题。

* **问题 1：透明度导致遮罩失效**
* *表现*：场景里有两个嵌套的 `Map` 节点，小地图完全不可见。
* *解决*：发现其中一个 `Map` 的 `Image` 组件 `Color` 属性 Alpha（透明度）被拉到了 0（颜色面板右下角有黑色缺口）。将其 A 值拉满至 255，恢复了裁切能力。


* **问题 2：信号线断连（纯蓝底色）**
* *表现*：地图变成了纯蓝色的圆圈，上面有代表船的圆点，但没有实景。
* *解决*：发现 `Realtime_Map_Render` 子节点的 `Raw Image` 组件里，`Texture` 槽位变成了 `None`。重新将 `Realtime_Minimap_Texture` 拖入连接。


* **问题 3：深色滤镜导致黑屏**
* *表现*：Texture 接上了，但画面极暗或全黑。
* *解决*：发现 `Raw Image` 自身的颜色被设成了深灰色（RGB: 56, 56, 56）。将其改为纯白色（RGB: 255, 255, 255, Alpha: 255），去除了染色滤镜，画面恢复正常。


* *(注：期间还遇到了 Mask 组件弹黄底警告，经排查确认是 Unity 编辑器在取消勾选 `Show Mask Graphic` 时常犯的显示 Bug，直接无视即可。)*

---

## 第四阶段：Build 独立包崩溃攻坚战

**目标**：解决在 Editor 里运行完美，但打包成 EXE 游戏后小地图失效、降级回线框的问题。

* **初始状态**：打包运行后，不但小地图变回了线框，画面实景也完全丢失。
* **❌ 踩坑方案 1：只清理场景中的引用**
* *分析*：查看 Console 报错日志，发现是 `Material 'MainMenuPanel' with Shader 'BoatAttack/UI/Halftone Fade' doesn't have a texture property '_MainTex'` 导致了整个 UI Canvas 渲染链断裂。
* *做法*：尝试在 Hierarchy 里搜索使用该材质的面板，把 Material 槽位清空为 `None`。
* *结果*：重新打包依然报错，说明该材质藏在其他未打开的 Scene 或更深的 Prefab 里。


* **✅ 最终方案：**
* *做法*：可能是版本等其它原因，已将情况汇报组长
* *成果*：通过网盘分享的文件：941372a1c16b955ceaff72d7f0f88a24_raw.mp4等3个文件
链接: https://pan.baidu.com/s/1NyRmDDDKbYaH5IhCbUYj5g?pwd=6bqg 提取码: 6bqg


# 4.10

## 1. 今日完成工作 

### 🚢 船只资产集成 
完成了以下 5 类船只模型的初步导入与适配：
* **Scout Boat (`boat`)**: 基础巡逻艇。
* **Fishing Boat (`fishing_boat`)**: 渔船模型。
* **Norrtelje Boat (`norrtelje-lowpoly`)**: 低功耗/低多边形船只，用于远景渲染。
* **Trawler (`trawler`)**: 拖网渔船。
* **Wrecked Motorboat (`wreck-of-a-white-motorboat`)**: 废弃电机船，用于障碍物避障场景。

### 🌴 环境与植被资产 
为了完善港口/岸线仿真，引入了以下植被模型：
* **Palm Trees (`free-game-ready-palm`)**: 热带岸线棕榈树。
* **High-poly & Realistic Trees**: 高精度及写实风格树木，用于近景视觉验证。

### 🛠️ 技术适配方案
1. **LFS 架构部署**：针对上述所有 FBX 模型和高分辨率纹理贴图，建立了 Git LFS 追踪机制，确保仓库不会因二进制资产过大而卡顿。
2. **URP 材质转换**：统一将材质球从内置管线迁移至 **团结引擎 URP (Lit)**，修正了贴图丢失导致的紫色报错。
3. **坐标系与比例**：执行了统一的缩放校准（1:1 比例），确保所有船只在海洋插件中的物理反馈一致。

---

## 2. 待办
- [ ] **物理适配**：为新引入的船只（尤其是 Trawler 和 Wreck）构建组合碰撞体（Colliders）。
- [ ] **资产预制化**：将所有模型封装为标准 Prefab，并统一存放至各个子文件夹的 `/Prefabs` 目录下。

---

# 4.4 
## 1. 完成内容
我已完成 **Fishing Boat** 外部资产的基础导入与 URP 适配工作：
* **材质修复**：解决了导入时的“紫色报错”，已提取并重新关联了 PBR 贴图（船身、甲板、玻璃）。
* **比例校准**：模型缩放已调整为 $1:1$ 真实比例，可直接配合传感器仿真使用。
* **预制体化**：已封装为标准 Prefab，清理了原始模型中冗余的层级。

## 2. 资产位置
* **模型/贴图根目录**: `Assets/External_Assets/fishing_boat/`
* **可以直接使用的预制体**: `Assets/External_Assets/fishing_boat/Prefabs/Fishing_Boat_Entity.prefab`

## 3. 如何调用
1. 在 Project 窗口搜索 `Fishing_Boat_Entity`。
2. 直接将其**拖入场景（Hierarchy）**。
3. **注意**：目前仅完成了视觉表现适配，**暂未添加 Rigidbody 和 Collider**（物理碰撞待下阶段完成），手动移动测试即可。

## 4. 待办项
- [ ] 添加船体碰撞体（Box Collider 组合）。
- [ ] 挂载水动力浮力脚本（需配合 Ocean 插件）。
- [ ] 预留雷达/摄像头安装点位。
# 4.3 
在unity assetstore和Quixel中寻找项目中需要的船体 树木 花草等

由于技术原因 目前还没有做好适配工作 争取在下周解决该问题 并找到些更好的模型

<img width="450" height="228" alt="image" src="https://github.com/user-attachments/assets/250a6bdf-5e29-471c-b40e-7eeb929e8e21" />
<img width="465" height="246" alt="image" src="https://github.com/user-attachments/assets/7f50a418-de14-45e8-a8f6-c33c0d3ab65e" />
