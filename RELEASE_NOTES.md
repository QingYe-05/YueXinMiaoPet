# YueXinMiaoPet v1.0.0 发布说明

## 下载

请在 GitHub Releases 中下载：

- `YueXinMiaoPet_Setup.exe`

## v1.0.0 亮点

- Windows 桌宠应用，基于 C# / WPF / .NET Framework 4.8。
- 支持 Windows 7 SP1、Windows 10、Windows 11。
- 内置 13 类月薪喵 GIF 分类资源，共 186 张 GIF。
- 支持心情卡片选择和心情有效期。
- 单击桌宠时严格在当前心情分类内切换 GIF。
- 支持天气、省市选择、10 分钟自动刷新和天气提示气泡。
- 支持缩放比例、透明度、置顶、托盘菜单和自定义 GIF 目录。
- 支持 Inno Setup 安装包和 .NET Framework 4.8 Runtime 检测。

## 安装要求

- Windows 7 SP1 / Windows 10 / Windows 11
- .NET Framework 4.8

如果系统缺少 .NET Framework 4.8，安装包会尝试提示或调用离线安装包，具体取决于打包配置。

## 已知限制

- 天气服务需要网络。
- Windows 7 需要 SP1 和 .NET Framework 4.8。
- 自定义 GIF 目录中的文件名和分类越清晰，展示效果越稳定。

## 校验建议

下载后建议先在测试环境检查：

1. 安装是否成功。
2. 桌宠是否能启动。
3. 托盘图标是否正常。
4. 设置心情后 GIF 是否跟随分类切换。
5. 卸载是否正常。
