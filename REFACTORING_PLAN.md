# PigResizeF 现代化重构计划

## 版本信息
- 当前版本: v1.5 (WinForms)
- 目标版本: v2.0 (Avalonia + MVVM)
- 重构分支: feature/modern-ui-redesign

## 发现的问题

### 1. 架构问题
- Form1.cs 文件过大(1593行),违反单一职责原则
- UI、业务逻辑、数据处理混在一起
- 缺少分层架构和依赖注入
- 硬编码过多,魔法数字和字符串遍布

### 2. UI/UX 问题  
- WinForms 界面老旧,不够现代化
- 颜色和布局硬编码在代码中
- 缺少响应式设计
- 没有深色模式支持
- 缺少拖拽预览和实时反馈
- 进度显示不够直观

### 3. 功能缺失
- 没有批量操作进度条
- 缺少处理历史记录
- 没有设置持久化
- 缺少快捷键支持
- 没有导出/导入配置功能

### 4. 代码质量
- 没有单元测试
- 异常处理不完善
- 日志系统简单
- 缺少配置文件管理
- 更新检查硬编码 GitHub API

### 5. 性能问题
- 大文件处理时UI会卡顿
- 没有任务队列管理
- 内存占用未优化

## 重构目标

### 1. 技术栈 (方案4: WinForms + ReaLTaiizor UI)
- ✅ WinForms (保持，零迁移成本)
- ✅ .NET 8 (保持)
- ✅ **ReaLTaiizor 3.8.0.8** (现代化 UI 控件库，MIT 协议)
- ✅ Material Theme (Google Material Design 风格)
- ✅ MVP 架构 (适配 WinForms)
- ✅ 依赖注入 (Microsoft.Extensions.DependencyInjection)
- ✅ 配置管理 (Microsoft.Extensions.Configuration)
- ✅ 日志系统 (Serilog)
- ✅ SixLabors.ImageSharp (保持)

### 2. 项目结构
```
PigResizeF/
├── Core/                      # 核心业务逻辑
│   ├── Models/               # 数据模型
│   │   ├── ConvertTask.cs
│   │   ├── CropTask.cs
│   │   ├── JdDetailTask.cs
│   │   └── ProcessResult.cs
│   ├── Services/             # 业务服务
│   │   ├── IImageProcessor.cs
│   │   ├── ImageProcessor.cs
│   │   ├── IUpdateChecker.cs
│   │   ├── UpdateChecker.cs
│   │   ├── IConfigService.cs
│   │   └── ConfigService.cs
│   ├── Enums/               # 枚举
│   │   ├── ConvertMode.cs
│   │   ├── CropMode.cs
│   │   └── LogLevel.cs
│   └── Common/              # 通用工具
│       ├── Constants.cs
│       └── Extensions.cs
├── UI/                       # UI 层
│   ├── Forms/               # 窗体
│   │   └── MainForm.cs
│   ├── Controls/            # 自定义控件
│   │   ├── ModernButton.cs
│   │   ├── ModernCard.cs
│   │   ├── ModernListView.cs
│   │   ├── ProgressCard.cs
│   │   └── DropZone.cs
│   ├── Themes/              # 主题系统
│   │   ├── ThemeManager.cs
│   │   ├── LightTheme.cs
│   │   └── DarkTheme.cs
│   └── Presenters/          # MVP Presenter
│       ├── MainPresenter.cs
│       ├── ConvertPresenter.cs
│       ├── CropPresenter.cs
│       └── JdDetailPresenter.cs
├── Resources/               # 资源文件
│   ├── Icons/
│   └── Fonts/
├── Program.cs              # 主程序
└── appsettings.json        # 配置文件
```

### 3. 现代化 UI 设计 (WinForms 自绘实现)

#### 设计原则 (基于 design_sense)
- **深色主题**: 
  - 背景层级: #0F131C (最深) → #161D2B → #1E2636 (浅层)
  - 卡片表面: #1E2636 带微妙阴影
  - 避免纯黑和纯白
  
- **色彩系统**:
  - 主色(Primary): #38BDF8 (天蓝色 - 科技感)
  - 成功(Success): #6EE7B7 (翡翠绿)
  - 警告(Warning): #FCD34D (琥珀色)
  - 危险(Danger): #F87171 (珊瑚红)
  - 中性灰阶: #64748B → #94A3B8 → #CBD5E1
  
- **排版系统**:
  - 主标题: Microsoft YaHei UI, 24px, Bold
  - 次标题: Microsoft YaHei UI, 16px, SemiBold  
  - 正文: Microsoft YaHei UI, 13px, Regular
  - 辅助文字: Microsoft YaHei UI, 11px, Regular
  
- **间距节奏**: 4px 基准单位 (8, 12, 16, 24, 32, 48)
- **圆角系统**: 
  - 小圆角(按钮/输入框): 8px
  - 中圆角(卡片): 16px
  - 大圆角(对话框): 24px
  - 胶囊(Tag/Badge): 999px
  
- **阴影系统**:
  - 浮起层: 0 2px 8px rgba(0,0,0,0.15)
  - 悬停层: 0 4px 16px rgba(0,0,0,0.25)
  - 对话框: 0 8px 32px rgba(0,0,0,0.35)

#### 关键 UI 改进 (自绘实现)

1. **主窗体布局**
   - 左侧导航栏 (240px): 深色背景 + 图标按钮
   - 右侧内容区: 浅色背景 + 卡片布局
   - 无边框窗体 + 自定义标题栏
   - 支持亮色/深色主题切换

2. **ModernButton (自绘按钮)**
   - 圆角矩形 + 渐变色
   - 悬停动画 (200ms 缓动)
   - 点击波纹效果
   - 加载状态旋转图标
   - 支持图标 + 文字组合

3. **ModernCard (卡片容器)**
   - 圆角边框 + 阴影
   - 统一内边距 (16px)
   - 可折叠标题栏
   - 悬停提升效果

4. **DropZone (拖拽区域)**
   - 虚线边框 (拖拽时高亮)
   - 拖入时显示预览缩略图
   - 支持多文件预览网格
   - 拖拽进度指示

5. **ProgressCard (进度卡片)**
   - 环形进度条 (GDI+ 绘制)
   - 百分比数字居中
   - 剩余时间估算
   - 速度显示 (张/秒)

6. **ModernListView (现代列表)**
   - 卡片式列表项
   - 多选支持 + 全选
   - 虚拟化滚动 (大数据)
   - 拖拽排序

7. **Toast 通知**
   - 右下角弹出
   - 自动消失 (3秒)
   - 成功/警告/错误样式
   - 可堆叠显示

8. **动画系统**
   - 淡入淡出 (Opacity)
   - 滑动 (TranslateTransform)
   - 缩放 (ScaleTransform)
   - 使用 Timer 实现平滑动画

### 4. 新增功能

1. **批处理管理器**
   - 任务队列可视化
   - 暂停/继续/取消单个任务
   - 批量操作进度汇总

2. **历史记录**
   - 保存最近处理记录
   - 快速重复上次操作
   - 导出处理报告

3. **预设管理**
   - 保存常用配置
   - 导入/导出预设
   - 快速切换预设

4. **快捷键支持**
   - Ctrl+O: 打开文件
   - Ctrl+S: 开始处理
   - Ctrl+,: 打开设置
   - F1: 帮助文档

5. **智能优化**
   - 自动检测最佳裁切模式
   - 批量文件智能分组
   - 输出质量预估

## 实施步骤

### Phase 1: 基础架构搭建 (第1周)
- [x] 创建重构分支
- [ ] 创建项目目录结构
- [ ] 实现依赖注入容器
- [ ] 配置日志系统 (Serilog)
- [ ] 实现配置管理 (appsettings.json)
- [ ] 定义核心数据模型

### Phase 2: 核心服务重构 (第2周)
- [ ] 抽取图片处理服务 (ImageProcessor)
- [ ] 实现裁切算法服务 (CropService)
- [ ] 迁移京东详情页处理 (JdDetailService)
- [ ] 实现更新检查服务 (UpdateChecker)
- [ ] 添加单元测试 (xUnit)

### Phase 3: UI主题系统 (第3周)
- [ ] 实现 ThemeManager (亮色/深色主题)
- [ ] 创建自绘基类 (ModernControlBase)
- [ ] 实现 ModernButton
- [ ] 实现 ModernCard
- [ ] 实现 DropZone
- [ ] 实现 ProgressCard
- [ ] 实现 ModernListView
- [ ] 实现 Toast 通知系统
- [ ] 实现动画引擎 (AnimationHelper)

### Phase 4: 主窗体重构 (第4周)
- [ ] 设计无边框主窗体
- [ ] 实现侧边导航栏
- [ ] 实现图片转换视图 (ConvertView)
- [ ] 实现图片裁切视图 (CropView)
- [ ] 实现京东详情页视图 (JdDetailView)
- [ ] 集成 MVP Presenter 层

### Phase 5: 功能增强 (第5周)
- [ ] 批处理任务队列管理器
- [ ] 历史记录系统
- [ ] 预设配置管理
- [ ] 快捷键支持
- [ ] 设置面板 (主题/语言/行为)
- [ ] 拖拽预览优化

### Phase 6: 优化与测试 (第6周)
- [ ] 性能优化 (异步处理/并发控制)
- [ ] 内存优化 (图片缓存/资源释放)
- [ ] 集成测试
- [ ] 用户体验测试
- [ ] 文档完善
- [ ] 准备发布

## 兼容性保证

1. **功能完全兼容**: 保留所有 v1.5 功能
2. **数据兼容**: 支持导入 v1.5 设置
3. **平滑升级**: 提供迁移向导

## 预期成果

1. **代码质量**: 测试覆盖率 > 80%
2. **性能**: 大文件处理速度提升 30%
3. **用户体验**: 现代化界面,操作更直观
4. **可维护性**: 模块化架构,易于扩展
5. **跨平台**: Windows/macOS/Linux 统一体验

## 版本发布计划

- **v2.0-beta1**: 基础功能完成 (4周)
- **v2.0-beta2**: 新功能测试 (2周)
- **v2.0-rc1**: 候选版本 (2周)
- **v2.0**: 正式发布 (预计2个月后)
