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
