<p align="center">
  <img src="docs/img/logol.png" alt="Workspace Manager Logo" width="120" />
</p>

<h1 align="center">Workspace Manager</h1>

<p align="center">
  一个给 Windows 桌面“收场”的小工具。
  <br />
  当你的桌面开始像临时仓库，它负责把秩序请回来。
</p>

<p align="center">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square" />
  <img alt="Windows 10/11" src="https://img.shields.io/badge/Windows-10%20%2F%2011-0078D6?style=flat-square" />
  <img alt="WPF" src="https://img.shields.io/badge/UI-WPF-0C7D9D?style=flat-square" />
  <img alt="Status MVP" src="https://img.shields.io/badge/Status-MVP-F97316?style=flat-square" />
  <img alt="License MIT" src="https://img.shields.io/badge/License-MIT-16A34A?style=flat-square" />
</p>

## 这项目是干什么的

`Workspace Manager` 是一个面向 Windows 的桌面管理工具，核心目标很朴素：

- 让“隐藏桌面图标”这件事不用再点到系统菜单深处
- 让被分辨率、投屏、误操作打乱的桌面布局还能找回来
- 让录屏、演示、开会共享前的“桌面整理仪式”从几分钟缩成几个按钮
- 让壁纸切换这件小事，也能顺手变得更舒服一点

它不是桌面壳替代品，不打算接管整个 Windows。

它更像一个常驻托盘的小管家。

平时安静待着，需要时出来帮你把桌面从“创意发酵现场”拉回“还能见人”的状态。

## 现在已经能做什么

### 托盘和桌面状态

- 常驻系统托盘
- 最小化到托盘
- 关闭窗口时隐藏到托盘
- 双击托盘图标恢复主窗口
- 一键显示或隐藏桌面图标
- 一键显示或隐藏任务栏
- 支持全局快捷键快速拉回主窗口

### 布局和模式

- 保存当前桌面图标布局
- 生成布局预览图，方便识别
- 从列表恢复指定布局
- 内置默认模式、工作模式、演示模式
- 支持创建、编辑、删除自定义模式
- 支持设置默认模式，并在启动时自动应用

### 壁纸能力

- 一键手动切换壁纸
- 支持启动时自动换一张
- 支持按分钟定时轮换
- 支持内置在线图源
- 支持手动添加远程图片地址
- 支持把本地图片作为图源
- 支持把本地图片文件夹作为图源

### 启动和设置

- 开机自启
- 启动后最小化到托盘
- 最小化时隐藏到托盘
- 关闭窗口时隐藏到托盘
- 自定义桌面图标切换快捷键
- 自定义显示主窗口快捷键

## 适合谁

- 经常录屏、演示、共享桌面的用户
- 桌面图标容易被分辨率变化打乱的人
- 想快速切换“工作桌面”和“演示桌面”的人
- 不想每次都去系统菜单里翻入口的人
- 有点在意桌面整洁，但又不想被桌面管理工具教育做人的人

## 快速开始

### 运行环境

- Windows 10 或 Windows 11
- `.NET 8 Desktop Runtime` 或 `.NET 8 SDK`

### 从源码运行

```powershell
dotnet run --project src/App/App.csproj
```

### 编译到 `.\artifacts`

推荐直接构建应用项目，并输出到仓库根目录下的 `artifacts`：

```powershell
dotnet build src/App/App.csproj -c Release -p:BuildProjectReferences=false -o .\artifacts
```

构建完成后可直接运行：

```powershell
.\artifacts\App.exe
```

## 一个很短的使用说明

### 开会前

打开应用，切到演示模式，隐藏桌面图标和任务栏。

你的桌面会比你本人先进入工作状态。

### 图标布局炸了之后

去“布局”页选择之前保存的快照，恢复布局。

如果这次是显示器、分辨率、缩放一起上强度，它也至少能帮你把大局拉回来，不用从零手摆。

### 想让壁纸别总是一张

在壁纸设置里添加在线地址、本地图片，或者直接选一个本地文件夹。

然后开启启动切换或定时轮换，让桌面背景自己找点存在感。

## 数据保存在哪里

应用数据默认保存在：

`%LOCALAPPDATA%\WorkspaceManager`

常见内容包括：

- `settings.json`：应用设置
- `modes.json`：模式配置
- `layouts\`：桌面布局快照
- `layout-previews\`：布局预览图
- `wallpapers\`：本地缓存后的壁纸文件

如果你打算重装系统、迁移机器，备份这里通常就够了。

## 当前已知限制

- 桌面布局恢复依赖 Windows Explorer 的桌面 ListView 行为
- 在多显示器、分辨率切换、系统缩放变化较大时，恢复结果可能存在偏差
- 如果桌面对应文件已经不存在，恢复结果不会做到像时光倒流一样完整
- 桌面整理规则、撤销整理、日志导出等能力仍在继续补
- 当前仍以手工验证为主，自动化测试还在逐步补齐

## 项目结构

- `src/App`：WPF 启动、窗口和资源
- `src/UI`：ViewModel、托盘宿主、界面装配
- `src/Application`：应用服务和用例编排
- `src/Domain`：领域模型
- `src/Infrastructure`：JSON 配置和本地存储
- `src/Interop`：Win32 / Shell / 热键 / 壁纸等平台交互
- `docs`：PRD、技术设计、计划和发布说明

## 文档

- [PRD v1](docs/PRD_v1.md)
- [Technical Design v1](docs/Technical_Design_v1.md)
- [Development Plan v1](docs/Development_Plan_v1.md)
- [Release Notes v0.2.0](docs/Release_Notes_v0.2.0.md)
- [Release Notes v0.1.0](docs/Release_Notes_v0.1.0.md)

## Roadmap

- 继续提升布局保存与恢复的稳定性
- 完善托盘入口和高频操作流
- 补齐桌面整理规则 MVP
- 补日志、回滚和更稳的异常提示
- 逐步补自动化测试和工程化细节

## 参与项目

如果你在用这个项目时遇到问题，欢迎提 Issue。

如果你想直接动手，也欢迎提 PR。能附上下面这些信息会很有帮助：

- Windows 版本
- 单屏还是多屏
- 缩放比例
- 问题出现前做了什么操作
- 如果和布局恢复有关，最好说明分辨率是否发生过变化

## License

本项目使用 [MIT License](LICENSE)。

你可以自由使用、修改、分发，甚至拿去商用；只要保留原许可和版权声明即可。
