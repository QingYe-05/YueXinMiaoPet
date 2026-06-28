# Changelog

所有重要变更都会记录在这里。

## [v1.0.0] - 2026-06-28

### Added

- 基于 C# / WPF / .NET Framework 4.8 的 Windows 桌宠应用。
- 兼容目标：Windows 7 SP1、Windows 10、Windows 11。
- 透明、无边框、可拖动、默认置顶的桌宠窗口。
- 内置 13 类月薪喵 GIF 资源，共 186 个 GIF。
- 支持中文路径、中文 GIF 文件名扫描和 `assets.generated.json` 生成。
- 心情分类强绑定：单击桌宠时只在当前心情对应分类内切换 GIF。
- 心情窗口支持有效期：30 分钟、1 小时、2 小时、今天有效、一直有效。
- 天气服务：Open-Meteo、省市选择、经纬度高级选项、断网安全回退。
- 设置窗口：GIF 来源、缩放、透明度、置顶、开机启动、上下班时间、调试面板。
- 托盘菜单：显示 / 隐藏、今日心情、设置、重新扫描 GIF、退出。
- Inno Setup 安装脚本，包含 .NET Framework 4.8 Runtime 检测与离线安装包接入。
- 应用图标、托盘图标、快捷方式图标和安装包图标。

### Changed

- 默认优先加载 `PetAssets/classified_gifs`，原始 `PetAssets/Gifs` 作为兜底。
- 资源扫描只读取 13 个一级分类目录下的 `.gif` 文件，忽略 CSV、JPG 等辅助文件。
- 调试面板增加当前播放分类、点击候选分类、候选数量和 13 类扫描计数。

### Known limitations

- Windows 7 SP1 需要 .NET Framework 4.8，且仍需在真实 Win7 环境补充安装测试。
- 自定义 GIF 目录目前只扫描本地 GIF 文件，不会自动修复损坏 GIF。
- 天气表现规则仍有优化空间，后续可增强不同天气下的动画候选池。
- GitHub Release 附件需要通过 GitHub Releases 分发，不应提交到源码仓库。
