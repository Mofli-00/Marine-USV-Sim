4.4 
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

## 4. 待办项 (Todo)
- [ ] 添加船体碰撞体（Box Collider 组合）。
- [ ] 挂载水动力浮力脚本（需配合 Ocean 插件）。
- [ ] 预留雷达/摄像头安装点位。

4.3 在unity assetstore和Quixel中寻找项目中需要的船体 树木 花草等

    由于技术原因 目前还没有做好适配工作 争取在下周解决该问题 并找到些更好的模型

<img width="450" height="228" alt="image" src="https://github.com/user-attachments/assets/250a6bdf-5e29-471c-b40e-7eeb929e8e21" />
<img width="465" height="246" alt="image" src="https://github.com/user-attachments/assets/7f50a418-de14-45e8-a8f6-c33c0d3ab65e" />
