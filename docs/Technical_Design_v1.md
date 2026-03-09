# Workspace Manager 技术设计 v1

## 1. 目标

### 1.1 技术目标
- 基于 Windows 平台构建稳定、轻量、可扩展的桌面管理工具
- 优先保障托盘、桌面控制、布局恢复、文件整理的可靠性

### 1.2 设计原则
- 先稳定，再扩展
- 先本地化，再考虑同步
- 先服务拆分，再增加复杂 UI
- 尽量把 Windows 交互收敛到独立的 Interop 层

## 2. 技术选型

### 2.1 推荐技术栈
- 语言：`C#`
- 运行时：`.NET 8`
- UI：`WPF`
- 日志：`Serilog`
- 配置存储：`JSON`
- 可选本地数据库：`SQLite`（P1/P2 之后按需引入）

### 2.2 选型理由
- `WPF + C#` 对 Windows 原生集成最成熟
- 调用 Win32、Shell、托盘、热键、自启都更直接
- 更适合做长期常驻型系统工具
- 便于后续模块化扩展和维护

### 2.3 暂不采用
- `Electron`：体积与资源占用不适合轻量工具
- `Python + PySide`：适合原型，不适合作为长期主线
- `Tauri`：UI 灵活，但深度 Windows 集成成本更高

## 3. 总体架构

采用分层 + 模块化架构。

### 3.1 分层结构
- `UI`：界面、托盘菜单、用户交互
- `Application`：用例编排、模式切换、规则执行入口
- `Domain`：模式、规则、布局、操作记录等领域模型
- `Infrastructure`：配置、日志、文件系统、持久化
- `Interop`：Win32 / Shell API 调用封装

### 3.2 模块清单
- `TrayService`
- `HotkeyService`
- `DesktopIconService`
- `TaskbarService`
- `DesktopLayoutService`
- `ModeService`
- `RuleEngineService`
- `FileOrganizerService`
- `FileWatchService`
- `SettingsService`
- `StartupService`
- `OperationLogService`

## 4. 建议目录结构

```text
src/
  App/
  UI/
  Application/
  Domain/
  Infrastructure/
  Interop/
tests/
  Domain.Tests/
  Application.Tests/
docs/
```

### 4.1 各层职责
- `App`：应用启动、依赖注入、宿主配置
- `UI`：WPF 窗口、页面、ViewModel、托盘入口
- `Application`：用例服务与流程编排
- `Domain`：实体、值对象、规则模型、状态模型
- `Infrastructure`：JSON 配置、日志、文件访问、时间与系统抽象
- `Interop`：桌面窗口句柄、热键、Explorer 交互、P/Invoke 封装

## 5. 核心领域模型

## 5.1 桌面模式 `DesktopMode`

字段建议：
- `Id`
- `Name`
- `Description`
- `DesktopIconsVisible`
- `TaskbarVisible`
- `LayoutId`
- `EnableAutoOrganize`
- `EnabledRuleIds`
- `HideSensitiveItems`
- `CreatedAt`
- `UpdatedAt`

## 5.2 布局快照 `DesktopLayoutSnapshot`

字段建议：
- `Id`
- `Name`
- `PreviewImageFileName`
- `ResolutionWidth`
- `ResolutionHeight`
- `CreatedAt`
- `Items`

子项 `DesktopLayoutItem`：
- `Path`
- `DisplayName`
- `PositionX`
- `PositionY`
- `ItemType`

## 5.3 整理规则 `OrganizeRule`

字段建议：
- `Id`
- `Name`
- `Enabled`
- `Priority`
- `MatchType`
- `Extensions`
- `Keywords`
- `TargetPath`
- `DelaySeconds`
- `StopProcessingAfterMatch`

## 5.4 操作日志 `OperationRecord`

字段建议：
- `Id`
- `OperationType`
- `StartedAt`
- `CompletedAt`
- `Status`
- `Message`
- `AffectedItems`
- `CanUndo`

## 6. 关键服务设计

## 6.1 `DesktopIconService`

职责：
- 获取桌面图标当前显示状态
- 切换图标显示/隐藏
- 在异常场景下尝试重新附着 Explorer 相关句柄

接口建议：
- `bool IsVisible()`
- `Task SetVisibleAsync(bool visible)`
- `Task ToggleAsync()`

## 6.2 `TaskbarService`

职责：
- 获取任务栏当前显示状态
- 切换主任务栏与副任务栏显示/隐藏

接口建议：
- `bool IsVisible()`
- `Task SetVisibleAsync(bool visible)`
- `Task ToggleAsync()`
## 6.3 `DesktopLayoutService`

职责：
- 读取当前桌面图标布局
- 保存布局快照
- 恢复指定布局
- 处理布局与当前桌面项不一致的问题

接口建议：
- `Task<DesktopLayoutSnapshot> CaptureAsync()`
- `Task SaveAsync(DesktopLayoutSnapshot snapshot)`
- `Task RestoreAsync(string layoutId)`
- `Task<IReadOnlyList<LayoutConflict>> ValidateAsync(string layoutId)`

## 6.4 `ModeService`

职责：
- 加载模式配置
- 切换模式
- 恢复模式绑定的布局快照
- 创建并持久化自定义模式
- 执行模式附带动作
- 恢复退出前状态

接口建议：
- `Task<IReadOnlyList<DesktopMode>> GetModesAsync()`
- `Task SwitchAsync(string modeId)`
- `DesktopMode CreateCustomMode(...)`
- `DesktopMode UpdateCustomMode(...)`
- `void DeleteCustomMode(string modeId)`
- `Task RevertLastAsync()`

当前 MVP 中，自定义模式通过独立模式编辑弹窗维护，主界面只保留紧凑入口与操作按钮。

## 6.5 `RuleEngineService`

职责：
- 解析规则
- 对文件进行匹配
- 决定移动目标和优先级

接口建议：
- `RuleMatchResult Match(FileContext context)`
- `IReadOnlyList<OrganizeAction> BuildActions(IEnumerable<FileContext> files)`

## 6.6 `FileOrganizerService`

职责：
- 执行手动或自动整理动作
- 生成操作记录
- 提供撤销能力

接口建议：
- `Task<OperationRecord> OrganizeDesktopAsync(OrganizeRequest request)`
- `Task UndoAsync(string operationId)`

## 6.7 `FileWatchService`

职责：
- 监听桌面目录变化
- 做事件去抖与合并
- 避免重复处理和循环触发

接口建议：
- `Task StartAsync()`
- `Task StopAsync()`

## 6.8 `TrayService`

职责：
- 托盘图标初始化
- 菜单项绑定命令
- 托盘状态反馈

## 6.9 `HotkeyService`

职责：
- 注册全局快捷键
- 解析并规范化用户输入的快捷键字符串
- 冲突检测
- 更新失败时回滚到上一个可用快捷键
- 分发热键命令

## 7. 数据存储设计

首版本采用 JSON 文件持久化。

### 7.1 推荐路径结构

```text
data/
  appsettings.json
  modes.json
  rules.json
  layouts/
    default.json
    work.json
  layout-previews/
    default.png
    work.png
  operations/
    2026-03-09-001.json
logs/
  app-.log
```

### 7.2 文件职责
- `appsettings.json`：基础设置、自启动、托盘、默认模式、快捷键
- `modes.json`：预设模式定义、布局绑定、图标/任务栏显隐配置
- `rules.json`：整理规则定义
- `layouts/*.json`：布局快照
- `layout-previews/*.png`：布局保存时生成的桌面缩略图
- `operations/*.json`：可撤销操作记录
- `logs/*.log`：运行与异常日志

## 8. 配置对象设计

## 8.1 `AppSettings`
- `LaunchAtStartup`
- `StartMinimizedToTray`
- `MinimizeToTrayOnMinimize`
- `CloseToTrayOnClose`
- `DefaultModeId`
- `RememberLastMode`
- `AutoOrganizeEnabled`
- `DesktopToggleHotkey`
- `ShowMainWindowHotkey`
- `LogLevel`

## 8.2 `HotkeyDefinition`
- `Modifiers`
- `Key`
- `Enabled`

当前 MVP 使用两组全局快捷键：
- `DesktopToggleHotkey`：切换桌面图标显示/隐藏
- `ShowMainWindowHotkey`：从托盘或隐藏任务栏状态下恢复主窗口

## 9. Windows 集成设计

## 9.1 桌面图标控制
- 通过 Explorer/桌面相关窗口句柄定位桌面图标视图
- 通过消息机制切换可见性
- 需封装窗口查找、消息发送、异常重试逻辑

## 9.2 布局读写
- 读取桌面图标列表与位置
- 将位置持久化为布局快照
- 保存布局时短暂隐藏主窗口并截取桌面缩略图，用于列表预览
- 恢复时按项目匹配并设置坐标

## 9.3 任务栏控制
- 通过 `Shell_TrayWnd` 与 `Shell_SecondaryTrayWnd` 控制任务栏显隐
- 主窗口与托盘菜单共用同一服务，保持状态一致
## 9.4 全局热键
- 基于 Win32 热键注册机制
- 应用需维护热键生命周期和冲突处理

## 9.5 开机自启
- 优先通过当前用户启动项或注册表实现
- 需避免重复注册与脏数据残留

## 10. 主要流程

## 10.1 切换演示模式流程
1. 读取当前模式状态
2. 记录恢复快照
3. 隐藏桌面图标或切换到演示布局
4. 根据模式决定是否暂停自动整理或隐藏敏感项
5. 写入操作记录

## 10.2 一键整理桌面流程
1. 扫描桌面文件
2. 规则引擎匹配
3. 生成整理动作计划
4. 执行移动
5. 写入操作记录
6. 提供撤销入口

## 10.3 恢复布局流程
1. 读取布局快照
2. 校验文件是否存在
3. 处理分辨率兼容
4. 逐项恢复位置
5. 记录失败项

## 11. 异常与恢复策略

### 11.1 文件整理相关
- 默认只移动，不删除
- 每次整理记录来源路径与目标路径
- 撤销失败时保留详细错误信息

### 11.2 Explorer 异常
- 检测 Explorer 句柄失效
- 尝试重新附着桌面对象
- 必要时提示用户重试

### 11.3 配置损坏
- 读取失败时回退到默认配置
- 备份最近一次有效配置

## 12. 日志与可观测性

### 12.1 日志分类
- 应用生命周期日志
- 桌面控制日志
- 布局快照日志
- 文件整理日志
- 异常日志

### 12.2 推荐日志字段
- `Timestamp`
- `Level`
- `OperationId`
- `Module`
- `Message`
- `Exception`

## 13. 测试策略

### 13.1 单元测试
- 规则匹配逻辑
- 模式切换编排逻辑
- 配置序列化与反序列化
- 撤销记录生成逻辑

### 13.2 集成测试
- 文件整理服务与本地目录交互
- 配置读写
- 规则优先级执行

### 13.3 手工验证
- 托盘初始化
- 热键注册与冲突
- 桌面图标显示/隐藏
- 布局保存/恢复
- Explorer 重启后的恢复能力

## 14. 开发阶段建议

## 14.1 阶段一：可行性验证
- 建立 WPF 托盘程序骨架
- 验证桌面图标显示/隐藏
- 验证布局捕获与恢复最小闭环

## 14.2 阶段二：MVP 实现
- 引入设置页
- 完成模式系统
- 完成手动整理与规则配置
- 完成本地配置持久化

## 14.3 阶段三：稳定性增强
- 自动整理
- 撤销能力
- Explorer 恢复处理
- 日志和异常恢复

## 15. 当前建议结论

- 首发技术主线采用 `C# + .NET 8 + WPF`
- 先把 Windows 集成能力封装在 `Interop` 层
- 首版本只使用 `JSON` 进行本地存储
- 优先做出稳定的托盘、布局、模式、整理四大核心模块
