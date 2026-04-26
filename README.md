# ClickAuto

一个简单的 Windows 自动点击工具。

- 启动后默认为暂停状态。
- 开启后每隔 1 秒点击一次鼠标左键。
- 按 `Alt+W` 可以暂停或继续，并在屏幕右下角显示短暂悬浮提示。
- 窗口里的按钮也可以暂停或继续。
- 程序启动时会请求管理员权限，用来提高全屏游戏或管理员权限程序中的热键/点击兼容性。

## 运行

```powershell
dotnet run --project .\ClickAuto\ClickAuto.csproj
```

## 发布 exe

```powershell
dotnet publish .\ClickAuto\ClickAuto.csproj -c Release -r win-x64 --self-contained false
```

发布后的程序在：

```text
ClickAuto\bin\Release\net8.0-windows\win-x64\publish\ClickAuto.exe
```

## 全屏游戏说明

本工具使用 Windows 的全局热键、低级键盘钩子和 `SendInput` 模拟鼠标点击。普通窗口、无边框全屏和多数管理员权限程序通常可以工作；但部分独占全屏、Raw Input、DirectInput 或带反作弊的游戏会拦截模拟输入，这种情况下系统层点击器可能仍然无法生效。
