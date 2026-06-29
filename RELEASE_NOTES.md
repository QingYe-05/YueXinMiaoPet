# YueXinMiaoPet v2.0.0 发布说明

「月薪喵桌宠」v2.0.0 重点优化了播放逻辑、天气展示和用户自定义轮播体验。这个版本仍保持 C# / WPF / .NET Framework 4.8 技术路线，目标兼容 Windows 7 SP1、Windows 10、Windows 11。

## 下载文件

请下载 Release 附件：

```text
YueXinMiaoPet_Setup.exe
```

## 主要更新

- 天气功能改为可选弱干扰模块，默认关闭。
- 新增 `WeatherEnabled`：控制是否显示 GIF 正上方天气气泡。
- 新增 `WeatherAffectsGif`：控制天气是否允许参与 GIF 候选补充，默认不影响 GIF。
- 天气气泡显示在表情包正上方，不遮挡月薪喵 GIF。
- 天气气泡内容精简为天气状况和温度，例如 `晴 28℃`。
- 默认播放逻辑取消随机 / 权重 / 偏好 GIF，改为当前心情分类下所有 enabled GIF 按顺序轮播。
- 单击桌宠会推进当前播放列表下一张，不再跳回普通 GIF。
- 新增 GIF 轮播设置窗口，支持从“当前心情 GIF”或“全部 GIF”中多选。
- 支持当前心情自定义轮播和全局自定义轮播。

## 播放优先级

1. 当前心情自定义轮播。
2. 全局自定义轮播。
3. 当前心情分类默认顺序轮播。
4. `neutral / 01_普通` 兜底轮播。
5. 全部 enabled GIF 兜底轮播。

## 配置与调试

- 配置继续保存到 `%AppData%\YueXinMiaoPet\config.json`。
- 新增配置字段包括 `WeatherEnabled`、`WeatherAffectsGif`、`UseGlobalCustomPlaylist`、`GlobalCustomPlaylist`、`MoodCustomPlaylists`。
- 设置窗口调试面板可查看当前播放模式、播放列表来源、播放列表数量、播放索引、当前 GIF、天气开关和天气气泡文本。
- 旧版本配置缺失新增字段时会自动补默认值，不需要手动迁移。

## 安装要求

- Windows 7 SP1、Windows 10 或 Windows 11。
- .NET Framework 4.8 Runtime。

如果目标机器缺少 .NET Framework 4.8，安装程序会提示并尝试运行内置离线安装器。

## 安装包说明

- 本地安装包路径：`E:\Tool\codex\YueXinMiaoPet\installer\output\YueXinMiaoPet_Setup.exe`。
- 安装包包含主程序、内置月薪喵 GIF、图标资源、城市数据和 .NET Framework 4.8 离线安装包。
- 安装后用户配置和日志写入 AppData，不写入安装目录。
- 日志路径：`%AppData%\YueXinMiaoPet\logs\app.log`。

## 已知说明

- Windows 7 SP1 建议先安装系统更新和 .NET Framework 4.8。
- 自定义 GIF 目录只读取本地 `.gif` 文件，不会转换或修复损坏 GIF。
- 安装包较大，因为包含 .NET Framework 4.8 离线安装包和内置 GIF 资源。
- GitHub Release 附件由维护者手动上传，安装包不提交到源码仓库。
