# 图片批量裁切工具（Tauri 版本）开发说明

基于 **Tauri 2 + React 19 + TypeScript + Rust**，从原 .NET WinForms 项目迁移而来。使用说明见仓库根目录的 [README](../README.md)。

## 技术栈

- 前端：React 19、TypeScript、Vite、Tailwind CSS
- 后端：Rust，`image` crate 负责解码、缩放、裁切与 JPG 编码
- 插件：`tauri-plugin-dialog`（文件选择）、`tauri-plugin-fs`、`tauri-plugin-opener`

## 开发环境

1. Node.js 20.19+ 或 22.12+（Vite 8 要求）
2. Rust 稳定版工具链（`rustup`）
3. 系统依赖
   - Windows：WebView2（Windows 10/11 一般已自带）
   - macOS：Xcode Command Line Tools

```bash
npm install
npm run tauri dev
```

## 构建

```bash
# 当前平台
npm run tauri build

# macOS 通用版（Intel + Apple 芯片），并打包为发布用 zip
rustup target add aarch64-apple-darwin x86_64-apple-darwin
npm run tauri build -- --target universal-apple-darwin --bundles app
scripts/package-macos.sh \
  "src-tauri/target/universal-apple-darwin/release/bundle/macos/图片批量裁切工具.app" \
  dist-release/PigResize_macos_universal.zip
```

正式发布由 GitHub Actions 完成：推送 `v*` 标签后，[release.yml](../.github/workflows/release.yml) 会编译 Windows 安装包、Windows 便携版和 macOS 通用版，并上传到同名 Release 草稿。

macOS 版本只做 ad-hoc 签名，未经 Apple 公证。发布包内附带 `scripts/macos/安装.command`，用于复制到“应用程序”并移除隔离属性。

## 项目结构

```
tauri-app/
├── src/
│   ├── App.tsx                    # 侧边栏与页面切换
│   ├── components/
│   │   ├── ImageConvertTab.tsx    # 图片转换
│   │   ├── ImageCropTab.tsx       # 图片裁切
│   │   ├── JdDetailsTab.tsx       # 京东详情页裁切
│   │   ├── ProgressPanel.tsx      # 实时进度条
│   │   ├── DropZone.tsx / LogViewer.tsx
│   │   └── ui/                    # 基础组件
│   ├── hooks/                     # useBatchProgress、useFileDrop、useLogger
│   └── types/                     # 与 Rust 端对应的类型
├── src-tauri/
│   ├── src/main.rs                # Tauri 命令与图片处理
│   └── tauri.conf.json
└── scripts/                       # macOS 打包与安装脚本
```

## 进度事件

`batch_resize`、`batch_crop`、`jd_export` 都是异步命令，实际处理放在阻塞线程池中执行，不占用主线程。处理过程中分别推送 `resize-progress`、`crop-progress`、`jd-progress` 事件，载荷为：

```ts
{ current: number; total: number; file_name: string; percent: number; stage: string }
```

`percent` 综合了文件序号和单张图片内部的阶段（读取、缩放、输出等），前端通过 `useBatchProgress` 订阅。
