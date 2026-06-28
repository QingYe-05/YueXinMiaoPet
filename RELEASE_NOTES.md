# YueXinMiaoPet v1.0.0

「月薪喵桌宠」v1.0.0 是第一个公开发布版本，提供可安装的 Windows 桌宠应用。

## 版本亮点

- C# / WPF / .NET Framework 4.8，兼容 Windows 7 SP1 / Windows 10 / Windows 11。
- 透明、无边框、可拖动、可置顶的桌宠窗口。
- 内置 13 类月薪喵 GIF，共 186 个动画。
- 支持中文 GIF 文件名扫描和自动标签索引。
- 心情与分类强绑定：点击桌宠时只在当前心情分类内切换 GIF。
- 心情窗口、设置窗口、托盘菜单、天气刷新、省市选择、开机启动。
- Inno Setup 安装包，支持 .NET Framework 4.8 检测和离线安装包接入。

## 安装要求

- Windows 7 SP1、Windows 10 或 Windows 11
- .NET Framework 4.8 Runtime

如果目标机器缺少 .NET Framework 4.8，安装程序会提示并尝试运行内置离线安装器。

## 下载文件

请下载 Release 附件：

```text
YueXinMiaoPet_Setup.exe
```

## 已知限制

- Windows 7 SP1 仍需真实环境补充安装验证。
- 天气触发的 GIF 表现规则还可以继续精细化。
- 自定义 GIF 目录只读取本地 `.gif`，不处理损坏 GIF 或其他格式。
- 安装包较大，因为包含 .NET Framework 4.8 离线安装包和内置 GIF 资源。

## 校验建议

安装后建议验证：

1. 桌宠窗口出现并能播放 GIF。
2. 托盘图标、桌面快捷方式、开始菜单快捷方式正常。
3. 设置窗口可打开，调试面板显示 13 类 GIF 计数。
4. 心情切换后立即播放对应分类 GIF。
5. 单击桌宠不会跳出当前心情分类。
6. 日志生成在 `%AppData%\YueXinMiaoPet\logs\app.log`。
