# PigResizeF 后续工作计划

## 📌 当前状态

### ✅ 已完成
- 创建了完整的服务层架构（Core/Services, Core/Models, Core/Common）
- 配置了 GitHub Actions CI/CD
- 版本升级到 1.6.0
- 代码已推送到 GitHub 分支：`feature/ui-improvements-v2`

### ⚠️ 待解决的问题
1. **界面空白问题** - MainForm 的 TabPage 中没有控件
2. **编译问题** - 在 macOS 上无法编译 Windows 项目（需要在 Windows 或 GitHub Actions 上编译）
3. **功能迁移** - 原 Form1 的功能还未迁移到新架构

## 🎯 推荐方案：渐进式改进

### 阶段 1：快速修复（优先级：高）

#### 1.1 回到原始 Form1
```bash
# 从 main 分支复制 Form1.cs 和 Form1.Designer.cs
git checkout main -- PigResizeF/Form1.cs
git checkout main -- PigResizeF/Form1.Designer.cs
git checkout main -- PigResizeF/Form1.resx
```

#### 1.2 集成服务层到 Form1
修改 Form1.cs，使用新的服务：
- 将图片处理逻辑替换为 `ImageProcessor` 服务调用
- 将更新检查逻辑替换为 `UpdateChecker` 服务调用
- 保持 UI 完全不变

#### 1.3 测试和发布
- 在 GitHub Actions 上自动编译
- 测试所有功能
- 发布 v1.6.0

### 阶段 2：UI 优化（优先级：中）

#### 2.1 提取可复用组件
- 创建 `FileListControl` - 文件列表管理
- 创建 `ProgressControl` - 进度显示
- 创建 `LogControl` - 日志显示
- 创建 `SettingsPanel` - 设置面板

#### 2.2 优化视觉效果
- 统一配色方案
- 添加图标和提示
- 改进布局
- 添加动画效果

#### 2.3 改进交互
- 添加拖拽预览
- 实时参数调整
- 快捷键支持
- 右键菜单

### 阶段 3：功能增强（优先级：低）

#### 3.1 批处理管理
- 任务队列可视化
- 暂停/继续/取消
- 批量操作汇总
- 多线程处理

#### 3.2 历史记录
- 保存处理记录
- 快速重复操作
- 导出处理报告
- 统计分析

#### 3.3 预设管理
- 保存常用配置
- 导入/导出预设
- 预设分类
- 快速切换

#### 3.4 高级功能
- 智能裁切建议
- 批量文件分组
- 水印添加
- 图片拼接

## 📝 下一步具体操作

### Option A: 修复当前分支（如果你想保留新架构）

1. **恢复原始 Form1**
```bash
git checkout main -- PigResizeF/Form1.cs PigResizeF/Form1.Designer.cs PigResizeF/Form1.resx
```

2. **删除空白的 MainForm**
```bash
git rm PigResizeF/MainForm.cs PigResizeF/MainForm.Designer.cs PigResizeF/MainForm.resx
```

3. **修改 Program.cs，使用 Form1**
```csharp
Application.Run(new Form1());
```

4. **重构 Form1，集成服务层**
   - 在 Form1 构造函数中初始化服务
   - 替换业务逻辑为服务调用
   - 保持 UI 不变

### Option B: 创建新分支，从 main 开始（推荐）

1. **创建新分支**
```bash
git checkout main
git checkout -b feature/integrate-services
```

2. **只添加服务层文件**
```bash
git checkout feature/ui-improvements-v2 -- PigResizeF/Core/
git checkout feature/ui-improvements-v2 -- .github/workflows/build.yml
git checkout feature/ui-improvements-v2 -- PigResizeF/PigResizeF.csproj
```

3. **修改 Form1，集成服务**
   - 逐步替换业务逻辑
   - 保持界面不变
   - 增量提交

4. **测试和发布**

## 🔧 Form1 集成服务示例

### 修改前（原始代码）
```csharp
private async Task ProcessImageAsync(string inputFilePath)
{
    try
    {
        using (var image = await Image.LoadAsync<Rgba32>(inputFilePath))
        {
            // 直接在 Form1 中处理图片
            var resizedImage = ResizeImageWithPadding(image, targetSize);
            await resizedImage.SaveAsync(outputFilePath, new JpegEncoder { Quality = 90 });
        }
    }
    catch (Exception ex)
    {
        LogMessage($"处理失败: {ex.Message}", LogLevel.Error);
    }
}
```

### 修改后（使用服务）
```csharp
private readonly IImageProcessor _imageProcessor;

public Form1()
{
    InitializeComponent();
    
    // 初始化服务
    _imageProcessor = new ImageProcessor();
}

private async Task ProcessImageAsync(string inputFilePath)
{
    var options = new ProcessOptions
    {
        Mode = ConversionMode.ToJpg,
        TargetWidth = targetSize.Width,
        TargetHeight = targetSize.Height,
        JpegQuality = 90,
        OutputDirectory = Path.GetDirectoryName(inputFilePath)!
    };

    var progress = new Progress<string>(msg => LogMessage(msg, LogLevel.Processing));
    var result = await _imageProcessor.ConvertImageAsync(inputFilePath, options, progress);

    if (result.Success)
    {
        LogMessage($"处理完成: {result.OutputPath}", LogLevel.Success);
    }
    else
    {
        LogMessage($"处理失败: {result.ErrorMessage}", LogLevel.Error);
    }
}
```

## 📊 预期成果

### v1.6.0 功能清单
- ✅ 保留所有原有功能
- ✅ 代码结构优化
- ✅ 可测试性提升
- ✅ 性能优化
- ✅ 更好的错误处理
- ✅ 自动更新检查
- ✅ CI/CD 自动构建

### v1.7.0 规划（UI 优化）
- 现代化界面
- 进度条和提示
- 拖拽预览
- 快捷键支持
- 深色模式

### v2.0.0 规划（功能增强）
- 批处理管理
- 历史记录
- 预设管理
- 智能建议
- 插件系统

## 🤝 需要你的决定

请告诉我你希望：

1. **选择哪个方案？**
   - [ ] Option A: 修复当前分支（feature/ui-improvements-v2）
   - [ ] Option B: 创建新分支从 main 开始（推荐）

2. **优先级排序？**
   - [ ] 先让程序能运行（修复界面空白）
   - [ ] 先完善服务层和测试
   - [ ] 先发布一个可用版本

3. **时间计划？**
   - [ ] 快速修复（1-2天）
   - [ ] 完整重构（1-2周）
   - [ ] 渐进改进（持续迭代）

**我建议：采用 Option B + 快速修复 + 渐进改进**

这样可以：
- ✅ 快速交付可用的 v1.6.0
- ✅ 保持代码质量提升
- ✅ 降低风险
- ✅ 用户持续受益

你觉得如何？
