# 图片批量裁切工具（Tauri 版本）开发说明

基于 **Tauri 2 + React 19 + TypeScript + Rust**，从原 .NET WinForms 项目迁移而来。功能介绍与使用说明见仓库根目录的 [README](../README.md)。

## 🚀 技术栈

- **前端**：React 19、TypeScript、Vite 8、[ui-kit](https://github.com/byttao/ui-kit)（基于 Ant Design 6）、Tailwind CSS
- **后端**：Rust
  - `image`：解码、EXIF 方向、JPG 编码
  - `fast_image_resize`：Lanczos3 缩放（SIMD + 多线程）
  - `rayon`：批处理并发
  - `reqwest`：启动时查询 GitHub 最新版本
- **Tauri 插件**：`tauri-plugin-dialog`（文件选择）、`tauri-plugin-fs`、`tauri-plugin-opener`（打开 Release 页面）

## 🛠️ 开发环境

1. Node.js 20.19+ 或 22.12+（Vite 8 要求）
2. Rust 1.90+（`rustup` 稳定版）
3. 系统依赖
   - Windows：WebView2（Windows 10/11 一般已自带）
   - macOS：Xcode Command Line Tools

```bash
npm install
npm run tauri dev
```

`tauri dev` 默认用 debug 编译，`image`、`fast_image_resize` 等依赖没有优化，出图会比正式版慢几十倍。需要看真实速度时用 release 模式启动：

```bash
npm run tauri -- dev --release
```

### 检查

```bash
npx tsc -b                # 前端类型检查
npm run lint              # oxlint
cd src-tauri
cargo test                # Rust 单元测试
cargo clippy --all-targets
```

## 📦 构建与发布

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

正式发布由 GitHub Actions 完成：推送 `v*` 标签后，[release.yml](../.github/workflows/release.yml) 会编译 Windows 安装包、Windows 便携版和 macOS 通用版，并上传到同名 Release 草稿。工作流不会填写 Release 标题和说明，需要在草稿里手动补上，再正式发布。

升级版本号时需要同时修改：`package.json`（及 `package-lock.json`）、`src-tauri/tauri.conf.json`、`src-tauri/Cargo.toml`（及 `Cargo.lock`）、根目录 README 的版本号与更新日志。侧栏显示的版本号读取 `package.json`，更新检测比较的是 `Cargo.toml` 的版本号。

macOS 版本只做 ad-hoc 签名，未经 Apple 公证。发布包内附带 `scripts/macos/安装.command`，用于复制到“应用程序”并移除隔离属性。

## 🔧 项目结构

```
tauri-app/
├── src/
│   ├── App.tsx                    # 侧栏导航、页签切换、启动时检查更新
│   ├── components/
│   │   ├── ImageConvertTab.tsx    # 图片转换
│   │   ├── ImageCropTab.tsx       # 图片裁切
│   │   ├── JdDetailsTab.tsx       # 京东详情页
│   │   └── JdPreview.tsx          # 详情页拼接预览（虚拟滚动、缩略图、缩放）
│   ├── lib/images.ts              # 支持的图片扩展名
│   ├── types/                     # 与 Rust 端对应的类型
│   └── ui-kit/                    # 界面组件库（从 byttao/ui-kit 拷入，见下文）
│       ├── components/app/        # AppShell、DropZone、FileList、LogViewer、ProgressPanel、主题切换
│       └── tauri/                 # useBatchProgress、useFileDrop
├── src-tauri/
│   ├── src/main.rs                # Tauri 命令：图片处理、预览缩略图、更新检测
│   ├── Cargo.toml
│   └── tauri.conf.json
└── scripts/                       # macOS 打包与安装脚本
```

## 🎨 ui-kit

`src/ui-kit/` 是独立仓库 [byttao/ui-kit](https://github.com/byttao/ui-kit) 的源码拷贝（当前 1.2.0），通过 `@/ui-kit` 引用。不要直接在这里改：先在 ui-kit 仓库修改、升版本号、写 CHANGELOG 并推送，再同步过来：

```bash
rsync -a --delete --exclude demo --exclude .git --exclude .DS_Store ../ui-kit/ src/ui-kit/
```

界面写法（反馈用 `App.useApp()`、危险操作样式等）见 [src/ui-kit/AI_GUIDE.md](src/ui-kit/AI_GUIDE.md)。

## 🖼️ 图片处理

- **读图**：`load_oriented_image` 解码后按 EXIF 方向转正，并统一转为 RGB；带透明通道的图片按 alpha 混合到白底，与输出 JPG 一致。
- **缩放**：`resize_lanczos` 统一用 `fast_image_resize` 的 Lanczos3，支持直接在源图上指定裁切区域（四角乱切不用先复制出子图）。
- **保存**：`save_jpeg` 用 `BufWriter` 缓冲写入。JPEG 编码器按小块写出，不加缓冲时每次写入都是一次系统调用，会慢几十倍。保存时先写同目录的临时文件再改名，并发任务输出到同一路径时不会互相写坏。
- **并发**：`run_batch` 在最多 4 线程的专用线程池里处理图片转换和图片裁切，结果按输入顺序返回。限制并发数是为了控制多张大图同时解码时的内存峰值。京东详情页需要按顺序拼接，仍逐张处理。

## 📊 进度事件

`batch_resize`、`batch_crop`、`jd_export` 都是异步命令，实际处理放在阻塞线程池中执行，不占用主线程。处理过程中分别推送 `resize-progress`、`crop-progress`、`jd-progress` 事件，载荷为：

```ts
{ current: number; total: number; file_name: string; percent: number; stage: string }
```

- `percent`：所有图片完成度之和占总数的比例。每张图片内部按阶段（读取、缩放、输出等）推进，多张并发时也只增不减。
- `current`：已开始处理的张数，只增不减。前端 `useBatchProgress` 在它变化时回调一次，用来记“正在处理”日志。
- `file_name`、`stage`：最近一次上报的文件和阶段。

## 🧩 京东详情页预览

[JdPreview.tsx](src/components/JdPreview.tsx) 以 790 像素输出宽度为 100% 基准：

- 尺寸：`read_image_sizes` 只解析文件头并按 EXIF 方向换算，用来预先算出每张图的位置。
- 缩略图：`jd_preview_thumbnail` 生成 1024 像素宽的 JPG，存放在应用缓存目录的 `jd-preview/` 中，按“路径 + 修改时间 + 文件大小”区分；生成超过 7 天的缩略图会在启动时清理。
- 只渲染可见区域上下 800 像素内的图片，同时最多生成 2 张缩略图，滚出视野且尚未开始的请求会被丢弃。
- 尺寸和缩略图缓存放在模块级，调整顺序、缩放、关闭后重新预览都直接复用。

## 🔔 更新检测

- 前端 `App.tsx` 在启动时调用 `check_update`。
- Rust 端请求 `https://api.github.com/repos/byttao/PigResizeF/releases/latest`，超时 5 秒；把 `tag_name` 去掉 `v` 后按 2～4 段版本号与 `CARGO_PKG_VERSION` 比较。网络失败、超时、数据异常都静默返回空，不弹任何提示。
- 有更高版本时，前端用 `modal.confirm` 询问是否打开发布页；点“打开”调用 `open_release_page`，只放行 `https://github.com/byttao/PigResizeF/` 开头的链接。
- 未登录的 GitHub API 每个 IP 每小时限 60 次，超出时这次检测会静默跳过。
- 本地测试弹窗：临时把 `Cargo.toml` 的版本号改低于最新 Release，启动一次，测完改回。
