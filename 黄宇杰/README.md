# 🚀 Git LFS 资产管理使用指南

本项目中的模型（FBX、OBJ）、贴图（PNG、JPG）等大文件通过 **Git LFS (Large File Storage)** 进行管理。

## 1. 首次使用环境配置
在使用 Git 拉取代码前，请确保本地已安装并初始化 LFS 组件（全库仅需执行一次）：

```bash
# 安装 LFS 扩展
git lfs install
```

## 2. 获取完整资产文件
如果你在 `git pull` 后发现 Unity 里的模型显示异常，或者文件大小只有 1KB（那是 LFS 指针文件），请执行以下命令强制下载原始二进制资源：

```bash
# 强制拉取并更新所有大文件资源
git lfs pull
```

## 3. 提交新资产 (fbx/png/jpg等)
当你向 `Assets` 引入新的大文件时，请遵循以下流程：

1. **确认后缀是否已被追踪**：
   目前已自动追踪 `*.fbx`, `*.obj`, `*.png`, `*.jpg`, `*.tga`, `*.unitypackage`。(比较常用的格式我均已经导入）如果你引入了新格式（如 `.exr`），请先运行：
   ```bash
   git lfs track "*.exr"
   ```

2. **正常提交并推送**：
   ```bash
   git add .
   git commit -m "feat: 增加新资产"
   git push origin <你的分支名>
   ```

   最快的方法：查看你当前在哪个分支
   在终端输入以下命令：

   ```Bash
   git branch
   ```
   你会看到一列分支名，前面带 * 号且显示为绿色的那个，就是你当前所在的分支。（或者你可以填写HEAD代表当前分支）

   假设输出是 * feature-assets，那么你就写：git push origin feature-assets。


   注：后续若有有关资源配置的问题 也会在此文档中说明