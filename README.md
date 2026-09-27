# Snap 截图工具

轻量 Windows 截图工具，支持区域截图、矩形/椭圆/箭头标注、文字、马赛克、贴图和截图历史。

## 使用

运行安装包 `Snap-Setup-1.0.1-win-x64.exe`，安装完成后打开桌面快捷方式。
安装包自带运行环境，安装到当前用户目录，无需管理员权限。

- 按 **Alt + Q** 开始截图。
- 画好矩形或椭圆后，直接点击拖动；选中后可修改颜色、线宽，按 Delete 删除。
- 按住 Alt 可在已有矩形或椭圆内部继续绘制。
- 在系统托盘右键 Snap 图标可打开截图台或退出。
- 可在 Windows“设置 → 应用”中卸载，个人截图和设置会保留。

目前提供 Windows 10/11 x64 安装包，尚未进行数字签名。

## 目录

```text
src/                       应用源码
  Snap.App/                WPF 界面、截图和系统集成
  Snap.Core/               几何、快捷键、设置等独立逻辑
tests/
  Snap.Core.Tests/         自动单元测试
  Snap.App.SmokeTests/     WPF 事件和布局冒烟验证
packaging/Snap.Setup/      安装界面、安装和卸载逻辑
scripts/build.ps1          一键测试、编译、打包
docs/CHANGELOG.md          版本说明
artifacts/                本地产物，Git 忽略
  staging/                可重新生成的中间文件
  release/                唯一对外分发目录：安装包和 SHA-256 校验文件
```

## 开发与打包

需要 Windows、.NET 10 SDK 和 PowerShell 7。版本号统一维护在 `Directory.Build.props`。

```powershell
dotnet build Snap.slnx
pwsh -File scripts/build.ps1
```

在有桌面会话的 Windows 上，可额外运行 WPF 冒烟检查：

```powershell
pwsh -File scripts/build.ps1 -SmokeTest
```

SDK 不在 PATH 时，通过 `-DotNet '完整路径/dotnet.exe'` 指定。
脚本会清理旧的 `artifacts/staging` 和 `artifacts/release` 后重新生成，不需要手动复制文件。
`bin`、`obj`、运行环境、ZIP 和 EXE 均不提交进源码仓库；安装包适合上传 GitHub Releases。

WPF 冒烟检查会读取当前桌面到内存以构造截图窗口，不会保存或上传桌面图像。
完整的物理多屏及不同 DPI 组合仍需人工验收。
