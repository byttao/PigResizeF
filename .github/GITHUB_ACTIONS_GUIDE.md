# GitHub Actions CI/CD 配置说明

## 📋 概述

本项目配置了两个 GitHub Actions 工作流：

### 1. 🚀 完整构建发布流程 (`build.yml`)
**触发条件**:
- 推送到 `main` 或 `feature/modern-ui-redesign` 分支
- 创建以 `v` 开头的标签 (如 `v2.0.0`)
- 创建 Pull Request 到 `main` 分支
- 手动触发

**功能**:
- ✅ 在 Windows 环境编译
- ✅ 运行测试
- ✅ 发布两个版本:
  - **自包含版本** (包含 .NET 运行时，~100MB)
  - **框架依赖版本** (需要预装 .NET 8，~5MB)
- ✅ 上传构建产物
- ✅ 创建 GitHub Release (当推送标签时)
- ✅ 代码质量检查

### 2. ⚡ 快速编译检查 (`quick-build.yml`)
**触发条件**:
- 推送到任何分支
- 创建 Pull Request

**功能**:
- ✅ 快速编译检查
- ✅ 验证代码可编译

---

## 🎯 使用场景

### 场景 1: 日常开发推送
```bash
git add .
git commit -m "feat: 添加新功能"
git push
```
- ✅ 触发 `quick-build.yml` - 快速检查编译
- ✅ 触发 `build.yml` - 完整构建（如果在 main 或 feature 分支）

### 场景 2: 发布新版本
```bash
# 1. 更新版本号
# 编辑 PigResizeF/PigResizeF.csproj
# <Version>2.0.0</Version>

# 2. 提交并打标签
git add .
git commit -m "release: v2.0.0"
git tag -a v2.0.0 -m "Release version 2.0.0

新功能:
- 现代化 UI (ReaLTaiizor Material Design)
- MVP 架构重构
- 主题切换支持
- 依赖注入
- 配置管理
"

# 3. 推送标签
git push origin v2.0.0
```
- ✅ 自动编译两个版本
- ✅ 自动创建 GitHub Release
- ✅ 自动上传安装包

### 场景 3: 创建 Pull Request
在 GitHub 网页创建 PR:
- ✅ 自动运行所有检查
- ✅ PR 页面显示编译状态
- ✅ 可下载构建产物测试

---

## 📦 构建产物说明

### 自包含版本 (`standalone`)
```
PigResizeF-v2.0.0-win-x64-standalone.zip
├── PigResizeF.exe          # 主程序
├── *.dll                   # .NET 运行时库
└── icons8-picture-100.ico  # 图标
```
**特点**:
- ✅ 无需安装 .NET
- ✅ 解压即用
- ❌ 体积较大 (~100MB)

### 框架依赖版本 (`framework`)
```
PigResizeF-v2.0.0-win-x64-framework.zip
├── PigResizeF.exe          # 主程序
├── *.dll                   # 依赖库
└── icons8-picture-100.ico  # 图标
```
**特点**:
- ✅ 体积小 (~5MB)
- ❌ 需要预装 .NET 8 Runtime
- 💡 下载地址: https://dotnet.microsoft.com/download/dotnet/8.0

---

## 🔧 查看构建状态

### 1. 在 GitHub 仓库页面
```
https://github.com/byttao/PigResizeF/actions
```

### 2. 添加状态徽章到 README
在 `README.md` 中添加：

```markdown
[![Build Status](https://github.com/byttao/PigResizeF/workflows/Build%20and%20Release/badge.svg)](https://github.com/byttao/PigResizeF/actions)
[![Quick Build](https://github.com/byttao/PigResizeF/workflows/Quick%20Build/badge.svg)](https://github.com/byttao/PigResizeF/actions)
```

### 3. 查看具体构建
- 点击 Actions 标签
- 选择对应的 workflow run
- 查看日志和下载产物

---

## 📥 下载构建产物

### 方法 1: GitHub Actions 页面
1. 进入 Actions 页面
2. 选择最新的成功构建
3. 滚动到底部 "Artifacts" 部分
4. 下载对应版本

### 方法 2: GitHub Releases (标签构建)
1. 进入 Releases 页面
2. 下载对应版本的 zip 文件

---

## 🛠️ 高级配置

### 自定义编译参数
编辑 `.github/workflows/build.yml`:

```yaml
# 修改输出目录
env:
  OUTPUT_DIR: dist

# 添加编译参数
- name: 发布
  run: |
    dotnet publish `
      --configuration Release `
      -p:DebugType=None `
      -p:DebugSymbols=false
```

### 添加其他平台支持
```yaml
# 添加 Linux 支持
- name: 发布 Linux-x64
  run: |
    dotnet publish ${{ env.PROJECT_PATH }} `
      --runtime linux-x64 `
      --self-contained true `
      --output ${{ env.OUTPUT_DIR }}/linux-x64
```

### 配置通知
添加 Slack/Discord/Email 通知:

```yaml
- name: 通知构建完成
  if: success()
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    text: '✅ PigResizeF 构建成功！'
  env:
    SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
```

---

## 🔒 安全配置

### Secrets 配置
如需配置额外的 secrets（如代码签名证书）:

1. 进入 GitHub 仓库 Settings
2. 选择 Secrets and variables > Actions
3. 添加 secrets:
   - `CODE_SIGN_CERT`: 代码签名证书
   - `CODE_SIGN_PASSWORD`: 证书密码
   - `SLACK_WEBHOOK`: Slack 通知地址

### 使用 Secrets
```yaml
- name: 代码签名
  env:
    CERT_PASSWORD: ${{ secrets.CODE_SIGN_PASSWORD }}
  run: |
    # 签名命令
```

---

## 📊 工作流程图

```
┌──────────────┐
│  代码推送    │
└──────┬───────┘
       │
       ├─────────────────────────────────┐
       │                                 │
       ▼                                 ▼
┌──────────────┐                ┌──────────────┐
│ Quick Build  │                │    Build     │
│  快速检查    │                │   完整构建   │
└──────┬───────┘                └──────┬───────┘
       │                               │
       ▼                               ▼
   编译检查                    ┌─────────────┐
                               │  构建产物   │
                               └──────┬──────┘
                                      │
                              ┌───────┴────────┐
                              │                │
                              ▼                ▼
                       ┌────────────┐   ┌────────────┐
                       │ Artifacts  │   │  Release   │
                       │  (临时)    │   │ (标签构建) │
                       └────────────┘   └────────────┘
```

---

## 🎯 最佳实践

### 1. 分支策略
```
main (稳定版本)
  ↑
feature/xxx (功能开发)
  ↑
hotfix/xxx (紧急修复)
```

### 2. 版本号规范
```
v主版本.次版本.修订号[-预发布标识]

示例:
- v2.0.0         (正式版)
- v2.0.1         (修复版)
- v2.1.0-beta1   (测试版)
- v2.1.0-rc1     (候选版)
```

### 3. 提交信息规范
```
feat: 新功能
fix: 修复 bug
docs: 文档更新
style: 代码格式
refactor: 重构
test: 测试
chore: 构建/工具
```

---

## 📝 常见问题

### Q: 编译失败怎么办？
A: 
1. 查看 Actions 日志定位错误
2. 本地运行 `dotnet build` 验证
3. 检查依赖包是否正确

### Q: 如何跳过 CI？
A: 在 commit 信息中添加 `[skip ci]`:
```bash
git commit -m "docs: 更新文档 [skip ci]"
```

### Q: 如何手动触发构建？
A:
1. 进入 Actions 页面
2. 选择 "Build and Release" workflow
3. 点击 "Run workflow" 按钮

### Q: 构建时间太长？
A: 
- Quick Build: ~2-3 分钟
- Full Build: ~5-10 分钟
- 可以通过缓存加速：添加 `actions/cache@v4`

---

## 🔗 相关链接

- [GitHub Actions 文档](https://docs.github.com/cn/actions)
- [.NET CLI 发布选项](https://docs.microsoft.com/dotnet/core/tools/dotnet-publish)
- [工作流语法](https://docs.github.com/cn/actions/using-workflows/workflow-syntax-for-github-actions)

---

**最后更新**: 2026-10-12  
**适用版本**: PigResizeF v2.0+
