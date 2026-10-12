# 📦 提交 GitHub Actions 配置

## ✅ 已创建的文件

```
.github/
├── workflows/
│   ├── build.yml          # 完整构建发布流程
│   └── quick-build.yml    # 快速编译检查
└── GITHUB_ACTIONS_GUIDE.md # 使用文档
```

---

## 🚀 提交命令

请在终端执行以下命令来提交和推送 GitHub Actions 配置：

```bash
# 1. 查看新增的文件
git status

# 2. 暂存 GitHub Actions 配置
git add .github/

# 3. 提交
git commit -m "ci: 添加 GitHub Actions 自动编译和发布工作流

功能:
- 完整构建发布流程 (build.yml)
  - 自动编译 Windows x64 版本
  - 生成两个版本: 自包含版 + 框架依赖版
  - 推送标签时自动创建 GitHub Release
  - 上传构建产物
  - 代码质量检查

- 快速编译检查 (quick-build.yml)
  - 每次推送快速验证编译
  - PR 自动检查

- 详细使用文档 (GITHUB_ACTIONS_GUIDE.md)
  - 使用场景说明
  - 版本发布流程
  - 构建产物说明
  - 最佳实践

触发条件:
- push: main, feature/modern-ui-redesign 分支
- tag: v* 标签推送
- pull_request: 所有 PR
- workflow_dispatch: 手动触发

构建产物:
- PigResizeF-vX.X.X-win-x64-standalone.zip (自包含 ~100MB)
- PigResizeF-vX.X.X-win-x64-framework.zip (框架依赖 ~5MB)"

# 4. 推送到 GitHub
git push
```

---

## 📋 提交后的效果

### 1. 自动触发编译 ✅
推送后，GitHub Actions 会自动：
- ✅ 运行 `quick-build.yml` - 快速编译检查
- ✅ 运行 `build.yml` - 完整构建（因为在 feature/modern-ui-redesign 分支）

### 2. 查看构建状态
访问: https://github.com/byttao/PigResizeF/actions

你会看到两个工作流正在运行：
- 🔵 Quick Build
- 🔵 Build and Release

### 3. 下载构建产物
构建完成后，在 Actions 页面可以下载编译好的程序：
- `PigResizeF-win-x64-standalone.zip` (自包含版本)
- `PigResizeF-win-x64-framework.zip` (框架依赖版本)

---

## 🏷️ 发布新版本

当你准备发布 v2.0.0 时：

```bash
# 1. 确保代码已提交
git status

# 2. 创建标签
git tag -a v2.0.0 -m "PigResizeF v2.0.0 正式版

新特性:
- 🎨 全新 Material Design UI (ReaLTaiizor)
- 🏗️ MVP 架构重构
- 🌓 亮色/深色主题切换
- 📦 依赖注入框架
- ⚙️ 配置管理系统
- 📝 Serilog 日志系统
- 🔄 自动更新检查

改进:
- 代码分层清晰，易于维护
- 现代化界面，用户体验提升
- 完整的文档体系

修复:
- 解决了圆角控件锯齿问题
"

# 3. 推送标签
git push origin v2.0.0
```

推送标签后，GitHub Actions 会：
- ✅ 自动编译两个版本
- ✅ 创建 GitHub Release
- ✅ 上传安装包
- ✅ 生成变更日志

Release 页面: https://github.com/byttao/PigResizeF/releases

---

## 📊 工作流说明

### build.yml (完整构建)
```yaml
触发条件:
  - push: main, feature/modern-ui-redesign
  - tag: v*
  - pull_request: main
  - 手动触发

编译内容:
  1. 恢复依赖
  2. 编译项目
  3. 运行测试
  4. 发布自包含版本 (win-x64)
  5. 发布框架依赖版本 (win-x64)
  6. 创建压缩包
  7. 上传构建产物
  8. 创建 Release (标签推送时)

产物保留: 30 天
```

### quick-build.yml (快速检查)
```yaml
触发条件:
  - push: 所有分支
  - pull_request: 所有分支

编译内容:
  1. 恢复依赖
  2. 快速编译
  3. 显示结果

时长: ~2-3 分钟
```

---

## 🎯 使用场景

### 场景 1: 日常开发
```bash
git add .
git commit -m "feat: 添加功能"
git push
```
→ 自动运行快速编译检查

### 场景 2: 测试完整构建
访问: https://github.com/byttao/PigResizeF/actions/workflows/build.yml  
点击 "Run workflow" 按钮手动触发

### 场景 3: 发布版本
```bash
git tag v2.0.0
git push origin v2.0.0
```
→ 自动创建 Release + 上传安装包

---

## 🔍 查看详细文档

完整的使用说明请查看: [.github/GITHUB_ACTIONS_GUIDE.md](.github/GITHUB_ACTIONS_GUIDE.md)

包含:
- 详细使用场景
- 版本发布流程
- 构建产物说明
- 高级配置
- 最佳实践
- 常见问题

---

## ✅ 配置完成

现在你的项目已经配置了完整的 CI/CD 流程！

**下一步**: 执行上面的提交命令，然后查看 GitHub Actions 的运行情况。

---

_生成时间: 2026-10-12_
