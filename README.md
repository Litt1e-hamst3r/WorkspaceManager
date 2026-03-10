# Workspace Manager

一个面向 Windows 的桌面管理工具，当前以 `.NET 8 + WPF` 实现，目标是把常用的桌面控制能力收敛到一个稳定、轻量、可持续演进的桌面应用中。

当前项目重点围绕以下几个方向推进：

- 托盘常驻与窗口收纳
- 桌面图标显示 / 隐藏
- 任务栏显示 / 隐藏
- 桌面布局保存 / 恢复
- 模式切换
- 启动行为与全局快捷键配置

---

## 1. 当前状态

项目已经具备可运行的 MVP 主体能力，现阶段不是纯原型，已经可以完成一批日常桌面管理操作。

### 已完成

- 托盘常驻
- 最小化到托盘
- 关闭窗口不退出
- 开机自启
- 启动最小化到托盘
- 桌面图标显示 / 隐藏
- 任务栏显示 / 隐藏
- 桌面布局保存 / 恢复
- 布局预览图与大图查看
- 自定义全局快捷键
- 主窗口恢复快捷键
- 按键捕获式快捷键录入
- 模式系统
  - 预设模式：默认 / 工作 / 演示
  - 自定义模式：新建 / 编辑 / 删除
  - 模式与布局绑定
  - 启动默认模式
- 第一轮架构收敛
  - `App` 负责启动与组合
  - `UI` 负责 ViewModel 与界面辅助服务
  - `Application` 负责业务编排
  - `Domain` 负责领域模型
  - `Infrastructure` 负责 JSON 持久化
  - `Interop` 负责 Windows 交互

### 正在推进

- 继续瘦身 `MainWindow.xaml.cs`
- 把窗口中的流程协调逻辑逐步外移
- 为后续“桌面整理 / 规则 / 日志 / 撤销”做结构准备

### 尚未完成

- 一键整理桌面
- 规则管理
- 自动整理
- 整理撤销
- 操作日志查看 / 导出
- 完整托盘菜单
- 更系统的自动化测试

---

## 2. 功能概览

### 2.1 托盘与窗口行为

- 程序可常驻托盘
- 支持最小化隐藏到托盘
- 支持关闭窗口时保留后台运行
- 支持通过快捷键重新拉起主窗口

### 2.2 桌面状态控制

- 一键显示 / 隐藏桌面图标
- 一键显示 / 隐藏任务栏
- 主窗口可实时刷新当前状态

### 2.3 桌面布局管理

- 保存当前桌面图标布局
- 生成布局缩略图
- 从列表中预览布局
- 恢复指定布局
- 删除布局

### 2.4 模式系统

- 预设模式：默认模式、工作模式、演示模式
- 自定义模式支持：
  - 名称
  - 描述
  - 桌面图标显隐
  - 任务栏显隐
  - 绑定布局
- 可设置启动默认模式

### 2.5 设置能力

- 开机自启
- 启动时最小化到托盘
- 最小化时隐藏到托盘
- 关闭时隐藏到托盘
- 桌面图标切换快捷键
- 显示主窗口快捷键

---

## 3. 技术栈

- 语言：`C#`
- 运行时：`.NET 8`
- UI：`WPF`
- 持久化：`JSON`
- Windows 集成：`Win32 / Registry / Global Hotkey / Explorer ListView`

---

## 4. 当前目录结构

当前仍使用单一可执行项目 `src/App/App.csproj`，但已按源码目录做分层收敛。

```text
docs/
  PRD_v1.md
  Technical_Design_v1.md
  Development_Plan_v1.md

src/
  App/
    App.csproj
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    ModeEditorWindow.xaml
    ModeEditorWindow.xaml.cs

  Application/
    Layouts/
    Modes/

  Domain/
    Layouts/
    Modes/

  Infrastructure/
    Configuration/
    Layouts/
    Modes/

  Interop/
    Desktop/
    Hotkeys/
    Layouts/
    Startup/

  UI/
    Services/
    Shell/
    ViewModels/
```

### 分层说明

- `src/App/`
  - 启动入口
  - WPF 窗口
  - 组合根
- `src/Application/`
  - 业务编排服务
  - 例如模式切换、布局保存恢复流程
- `src/Domain/`
  - 纯领域模型
  - 例如 `DesktopMode`、`DesktopLayoutSnapshot`
- `src/Infrastructure/`
  - JSON 配置和快照存储
- `src/Interop/`
  - Win32 / Explorer / 热键 / 自启动 等平台相关实现
- `src/UI/`
  - ViewModel、托盘宿主、窗口数据装配服务

---

## 5. 本地运行

### 5.1 环境要求

- Windows 10 / 11
- 已安装 `.NET SDK 8`
- 具备正常的桌面 Explorer 环境

### 5.2 构建

```powershell
dotnet build src\App\App.csproj
```

如果想输出到固定目录：

```powershell
dotnet build src\App\App.csproj -m:1 -v minimal /p:OutputPath=D:\code\Workspace_Manager\artifacts\app-build\
```

### 5.3 运行

```powershell
dotnet run --project src\App\App.csproj
```

或直接运行生成的 `App.exe`。

---

## 6. 配置与数据

当前使用本地 JSON 存储，默认落在当前用户本地应用数据目录下：

- 设置：`settings.json`
- 模式：`modes.json`
- 布局：`layouts/*.json`
- 布局预览：`layout-previews/*.png`

设计文档中的目标路径可参考：

- `docs/Technical_Design_v1.md`

---

## 7. 关键文档

- 产品需求：`docs/PRD_v1.md`
- 技术设计：`docs/Technical_Design_v1.md`
- 开发拆解：`docs/Development_Plan_v1.md`

---

## 8. 后续路线

建议后续按以下顺序继续推进：

1. 继续拆分 `MainWindow.xaml.cs`
2. 完善托盘菜单
3. 实现手动整理桌面 MVP
4. 接入基础规则配置
5. 加入操作日志与撤销机制
6. 补测试工程与核心逻辑测试

---

## 9. 当前开发约束

项目默认约束如下：

- 优先 `C# + .NET 8 + WPF`
- Windows 交互优先收敛到 `Interop`
- 持久化优先 JSON
- 优先做可验证、可演进的 MVP
- 优先修根因，不做表面补丁

详细协作与架构约束见仓库根目录 `AGENTS.md`。

---

## 10. 注意事项

- 任务栏与桌面图标控制依赖 Windows Explorer 行为，不同系统环境可能存在差异
- 布局恢复本质上依赖桌面 ListView 读写，属于平台敏感能力
- 如果构建输出目录中的 `App.exe` 正在运行，重新构建到同一路径时可能因文件占用失败
- 当前阶段以手工验证为主，自动化测试仍待补齐

---

## 11. License

当前仓库未单独声明开源许可证。如需公开发布，建议补充明确的 License 文件。
