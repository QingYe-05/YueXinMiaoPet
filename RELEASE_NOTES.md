# YueXinMiaoPet v2.0.1 发布说明

## 下载

请下载 Release 附件：

- `YueXinMiaoPet_Setup.exe`
- 或维护者额外压缩上传的 `YueXinMiaoPet_Setup.zip`

## 本版本重点

v2.0.1 是兼容性和稳定性热修复版本，重点修复部分用户安装后右下角托盘图标存在、但桌面上看不到月薪喵 GIF 的问题，并改善 Windows 7 / 老电脑环境下的稳定性。

本版本不改变 v2.0.0 的核心播放逻辑：仍然保留天气与城市功能、心情功能、13 类 GIF 分类结构、当前心情顺序轮播和用户自定义 GIF 轮播。

## 修复内容

- 修复部分 Windows 11 环境下托盘图标存在但桌宠窗口不可见时缺少自动恢复的问题。
- 修复窗口坐标异常、多显示器变化或配置损坏导致桌宠显示到屏幕外的问题。
- 托盘菜单新增“显示月薪喵”，可强制恢复窗口显示、激活和置顶状态。
- 托盘菜单新增“重置位置到屏幕中央”，可把桌宠移动回主屏幕中央并保存位置。
- 增强 GIF 加载失败 fallback：资源为空时显示诊断提示，单张 GIF 解码失败时记录日志并尝试下一张。
- 优化拖动开始时的卡顿：拖动期间不切换 GIF、不频繁写配置，拖动结束后延迟保存窗口位置。
- 增强启动诊断日志，方便排查用户环境问题。

## 兼容性改进

- Windows 7 环境默认启用 WPF 软件渲染，降低老显卡、透明窗口和驱动异常风险。
- 新增 `--safe-mode`：强制软件渲染、禁用天气刷新、使用内置 GIF、重置窗口到屏幕中央。
- 新增 `--reset-window`：只重置窗口位置到主屏幕中央。
- 新增 `--force-software-render`：强制启用 WPF 软件渲染。
- 安装包资源检查确认包含主程序、PetAssets、图标资源、城市数据和 .NET Framework 4.8 离线安装包。

## 用户排查建议

如果右下角托盘有月薪喵图标，但桌面上看不到 GIF：

1. 右键托盘图标，点击“显示月薪喵”。
2. 如果仍不可见，右键托盘图标，点击“重置位置到屏幕中央”。
3. 仍异常时，可以备份或删除配置文件后重启：

```text
%AppData%\YueXinMiaoPet\config.json
```

4. 使用重置窗口参数启动：

```powershell
YueXinMiaoPet.exe --reset-window
```

5. 老电脑、Windows 7 或透明窗口/GIF 显示异常时，使用安全模式启动：

```powershell
YueXinMiaoPet.exe --safe-mode
```

6. 如需反馈问题，请提供日志：

```text
%AppData%\YueXinMiaoPet\logs\app.log
```

Windows 7 老电脑建议确认：

- 已安装 Windows 7 SP1。
- 已安装 .NET Framework 4.8。
- 显卡驱动尽量更新到可用的稳定版本。
- 优先使用 `--safe-mode` 启动排查。

## 安装要求

- Windows 7 SP1 / Windows 10 / Windows 11
- .NET Framework 4.8 Runtime

如果目标机器缺少 .NET Framework 4.8，安装程序会提示并尝试运行内置离线安装器。

## 已知说明

- 安装包不提交到源码仓库。
- GitHub Release 中的 Source code zip/tar.gz 是 GitHub 自动生成的源码包，不是普通用户安装包。
- 普通用户下载 `YueXinMiaoPet_Setup.exe` 或维护者额外上传的 `YueXinMiaoPet_Setup.zip` 即可。
- 安装包较大，因为包含 .NET Framework 4.8 离线安装包和内置 GIF 资源。
