🔵 第二部分：【核心攻坚】实时小地图系统全生命周期迭代
背景：原版项目的小地图通过静态赛道贴图结合 UI 组件绘制，不仅视觉效果简陋（形似“贪吃蛇”），且无法反映水面动态、光影及实时障碍物。
目标：彻底重构底层，基于正交摄像机与 RenderTexture 技术，打造一个“正圆形、自适应追踪、大厂级实景呈现”的高级小地图系统。

🔄 阶段一：UI 遮罩（Mask）底层的视觉重塑与踩坑
核心痛点：初始将 RenderTexture 挂载到画布后，发现高空航拍画面被严重挤压、裁切成了一条弯曲的细线。经排查，是因为父物体 Map 节点上的 Mask 组件，依旧在使用原版赛道贴图 RaceMap 作为裁切模具。

为了实现完美的“实心正圆形”裁切，我们经历了三次迭代：

❌ 踩坑 1：误用 UI 切片资产 (slicedDot)

操作：将遮罩的 Source Image 替换为 Unity 内置的 slicedDot 资产。

结果：地图变成了“圆角正方形”。

底层原因：该资产带有 sliced 属性，是专为 UI 按钮制作的九宫格图像，其四个角的弧度被锁定，中心会被拉伸，不符合正圆的数学定义。

❌ 踩坑 2：误用中空资产 (WhiteCircle)

操作：在项目纹理库中检索并替换为 WhiteCircle。

结果：实景画面“灵异”消失，仅在边缘留下一圈极细的像素，中间完全漏出底下的 UI 背景色。

底层原因：该贴图的 Alpha 通道实际上是一个“甜甜圈”（空心圆环）。Unity 的 Mask 严格遵循 Alpha 值裁切，中间透明的区域自然把实景也“掏空”了。

✅ 终极方案：引擎自建基础图形与渲染剥离

重构思路：放弃寻找外部贴图，避免任何未知的图像压缩或通道干扰。

操作步骤：直接在引擎 Project 窗口右键 Create -> 2D -> Sprites -> Circle，生成由引擎底层算法渲染的绝对实心白圆 (SolidCircle)。

细节拉满：将其挂载给 Map 节点作为终极遮罩，并关键性地取消勾选 Show Mask Graphic。这一步让白色的底图在渲染管线中隐形，但完美保留了其 100% 不透明的 Alpha 裁切功能，最终获得了边缘极其平滑的圆形实景。

🎥 阶段二：跨场景部署与摄像机追踪逻辑 (MinimapFollow.cs) 重构
核心痛点：将配置好的航拍摄像机打包为 Prefab 拖入正式关卡（如 level_Pyramid）后，小地图变成了纯红或纯蓝的单色圆饼，且完全失去了跟随玩家的追踪能力。

❌ 踩坑 1：致命的高度硬编码 (Hardcoding)

现象：地图呈现纯色无纹理状态。

底层原因：原脚本中存在 public float cameraHeight = 150f;。在正式关卡中，金字塔或海岛地形的高度远超 150 单元。游戏一运行，摄像机就被这行代码强行拽回 150 的高度，直接一头扎进了山体或水底模型内部。由于摄像机处于模型内部，只能拍到背面剔除后的纯色背景。

❌ 踩坑 2：目标检索误判与层级陷阱

现象：为了让相机认识新场景的玩家，在代码里写死了 GameObject.Find("Player 1")，结果相机依然纹丝不动。

复盘过程：起初误以为 Player 1 只是一个存放静态 Checkpoint 的空文件夹，导致追踪失效。后续展开 Hierarchy 层级仔细排查，发现 Player 1 内部确实挂载了真实的 BoatHull（船体实体）、尾迹粒子和音频组件。追踪失效的真正元凶依然是被高度硬编码卡死导致的视觉静止错觉。

✅ 终极代码重构（三大升级）

废除高度锁死，实现自适应：在 Start() 函数中引入 currentHeight = transform.position.y;。允许开发者在 Inspector 面板中手动将相机拉高（如 Y=250）以越过最高地形，脚本会自动继承该安全高度，彻底告别穿模。

完善实体检索引擎：重写 FindBoat() 函数，兼容 Player 1 等多级节点命名，精准锁定动态船只的 Transform。

引入终极备用雷达（降级策略）：在 LateUpdate() 中加入容错逻辑。一旦 targetBoat 为空（船只销毁或未生成），强制让小地图相机追踪 Camera.main（主摄像机）的 X/Z 水平坐标。因为主相机永远锁定玩家，这保证了小地图的视野永不丢失。

👻 阶段三：UI 渲染层级与“透明度陷阱”深度排查
在整理层级结构（删除多余的嵌套 Map 节点）后，遇到了三次极其典型的 Unity UI 隐身/变色事件，全部通过深挖 Inspector 属性解决：

1. Alpha 归零陷阱导致遮罩瘫痪

现象：补上 Mask 组件后，小地图完全隐身。

排查与修复：点开 Map 节点的 Image.Color 属性面板，发现底部的 Alpha（透明度）滑块被拉到了 0（面板显示黑色缺口）。因为 Mask 组件的裁切范围完全由 Image 的 Alpha 通道决定，Alpha 为 0 等同于 100% 裁掉子物体。手动将 Alpha 拉回 255 后，裁切功能瞬间恢复。

2. “信号线”物理断开（透底现象）

现象：地图变成了蓝色的底饼，虽然有代表船只的 UI 坐标点，但毫无实景画面。

排查与修复：检查子节点 Realtime_Map_Render，发现 Raw Image 组件的 Texture 槽位变成了 None（呈现灰白透明格子）。这是因为 Prefab 更新时丢失了外部引用。重新将 Project 中的 Realtime_Minimap_Texture 拖入槽位，打通了相机到显示屏的视频流。

3. 乘法滤镜导致画面全黑

现象：画面虽然连上了，但像蒙了一层黑纱，极暗。

排查与修复：Raw Image 自身的 Color 属性被设成了深灰色（RGB: 56, 56, 56）。在 Unity 的底层渲染中，UI 颜色与 Texture 颜色是乘法叠加关系。将其十六进制颜色码改为 #FFFFFF（纯白，R:255, G:255, B:255）后，成功去除了“黑纱滤镜”，完美还原了底层的真实色彩。

(额外收获：期间 Mask 组件弹出了 Masking disabled due to Graphic component being disabled 的黄色警告。经查阅底层机制，确认这是 Unity Editor 在 Show Mask Graphic 取消勾选时的一个视觉假报警，底层逻辑依然在正常运行，坚决予以无视。)

💥 阶段四：Build 独立包恶性崩溃攻坚战
终极危机：在 Editor 编辑器中所有功能均已完美运行。但执行 File -> Build 打包成独立游戏后，进入场景发现小地图全部退化回了原始的弯曲粗糙线框，实景画面彻底死亡。

🔍 深度排查与堆栈分析：

调出 Development Console，发现运行期间抛出了满屏的红色异常，核心渲染流水线断裂在 UnityEngine.UI.Image.Rebuild() 和 CanvasUpdateRegistry.PerformUpdate()。

顺着调用栈往上游追溯，锁定了致命报错源头：
Material 'MainMenuPanel' with Shader 'BoatAttack/UI/Halftone Fade' doesn't have a texture property '_MainTex'

底层原理剖析：原项目的主菜单面板使用了自定义的 Halftone Fade 着色器。该 Shader 的属性声明不规范，在 Editor 环境下被引擎宽容通过，但在严格的 Build 编译环境下，丢失了 UI 系统强依赖的 _MainTex 纹理属性。一旦 Canvas 尝试渲染该材质，便引发抛出异常，直接导致整个 UI Canvas 渲染循环 (Update Loop) 崩塌。小地图的渲染指令被中断，系统被迫 fallback 降级到最原始的线框图。

❌ 尝试修复 1：场景级引用清理

尝试使用 Find References In Scene 找到挂载报错材质的面板，将其 Material 设为 None。但再次打包依旧报错，证明该材质深藏在未激活的 Prefab 或主菜单场景中。

* **✅ 最终方案：**
* *做法*：可能是版本等其它原因，已将情况汇报组长
* *成果*：通过网盘分享的文件：941372a1c16b955ceaff72d7f0f88a24_raw.mp4等3个文件
链接: https://pan.baidu.com/s/1NyRmDDDKbYaH5IhCbUYj5g?pwd=6bqg 提取码: 6bqg

<img width="1024" height="639" alt="image" src="https://github.com/user-attachments/assets/e752c11c-3d91-45a2-a0e1-79cbc57c26b2" />
<img width="2314" height="1367" alt="image" src="https://github.com/user-attachments/assets/e3ed16fb-984a-4d59-a140-f42a3bdc5d7e" />


🟢 第一部分：环境搭建与船只资产集成 (4.3 - 4.10)
为了让仿真环境更加贴近真实水域，我负责了大量外部资产的筛选、导入与底层管线适配。

资产寻优与初步筛选 (4.3)：
初期在 Unity Asset Store 和 Quixel Megascans 材质库中进行了大量比对。考虑到项目运行的性能要求，需要平衡模型的精度与面数，最终筛选出了一批适合仿真环境的船只、海岸植被和港口物件。

单体模型适配打样 (4.4)：
以 Fishing Boat（渔船）为试点，跑通了从外部导入到项目标准预制体的完整工作流：

解决渲染异常：刚导入时模型由于残留旧版 Standard Shader 变成了“紫块”。我手动提取了模型的 PBR 贴图（漫反射、法线、金属度等），重新创建了 URP/Lit 材质球并逐一映射，恢复了船体、木甲板和玻璃的真实质感。

物理比例校准：将模型的 Transform Scale 严格限制在 1:1 的真实世界物理比例。这是为了确保后续接入 Ocean 海洋系统时，水动力学和浮力计算不会出现物理反馈失真的问题。

规模化集成与版本控制优化 (4.10)：

扩充模型库：批量导入了基础巡逻艇 (boat)、低多边形远景船 (norrtelje-lowpoly)、拖网渔船 (trawler) 以及用于避障测试的废弃电机船 (wreck-of-a-white-motorboat)。同时加入了热带棕榈树等植被点缀岸线。

Git LFS 部署：考虑到大批量高精度 .fbx 和 4K 贴图会撑爆仓库导致组员拉取卡顿，我主动在 .gitattributes 中配置了 Git LFS (Large File Storage) 大文件追踪，规范了团队的资产提交流程。

<img width="450" height="228" alt="image" src="https://github.com/user-attachments/assets/250a6bdf-5e29-471c-b40e-7eeb929e8e21" />
<img width="465" height="246" alt="image" src="https://github.com/user-attachments/assets/7f50a418-de14-45e8-a8f6-c33c0d3ab65e" />
