# 月薪喵桌宠

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)](#windows-7--10--11-兼容性)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple.svg)](#开发运行)

「月薪喵桌宠」是一个基于 C# / WPF / .NET Framework 4.8 的 Windows 桌宠应用，目标兼容 Windows 7 SP1、Windows 10、Windows 11。

它会扫描中文命名的 GIF，根据天气、时间段、上下班、心情和鼠标互动自动切换月薪喵动画。

## 主要功能

- 透明、无边框、可拖动、默认置顶的桌宠窗口
- GIF 动画播放，支持中文路径和中文文件名
- 默认加载内置 `PetAssets/classified_gifs` 的月薪喵分类 GIF，原始 `PetAssets/Gifs` 作为兜底
- 支持自定义 GIF 目录，并可一键切回内置月薪喵
- 自动扫描 GIF 并生成 `PetAssets/assets.generated.json`
- 支持 `assets.json` 人工标签和 `assets.tags.override.json` 覆盖标签
- 单击互动、双击打开心情窗口、右键快捷菜单、拖动保存位置
- 托盘菜单：显示/隐藏、今日心情、设置、重新扫描 GIF、退出
- 设置窗口：省市选择、天气经纬度、缩放 Slider、透明度 Slider、开机启动、置顶、时间段、调试面板
- 心情窗口：心情、有效期、保存、取消；保存后立即切换 GIF
- 配置保存到 `%AppData%\YueXinMiaoPet\config.json`
- 日志保存到 `%AppData%\YueXinMiaoPet\logs\app.log`

## 下载与发布

稳定版安装包建议从 GitHub Releases 下载：

- Releases 页面：`https://github.com/QingYe-05/YueXinMiaoPet/releases`
- 推荐安装包名称：`YueXinMiaoPet_Setup.exe`

源码仓库不会提交 `installer/output/` 下的安装包，也不会提交 `.NET Framework 4.8` 离线安装包。安装包和压缩包请作为 Release 附件发布。

## 如何放入 186 个 GIF

推荐的内置分类 GIF 目录是：

```text
src/YueXinMiaoPet/PetAssets/classified_gifs/
```

本项目当前已经包含 13 个分类目录、共 186 个 GIF。后续替换或增加资源时：

1. 优先把 `.gif` 放入 `src/YueXinMiaoPet/PetAssets/classified_gifs/` 的对应分类目录
2. 如果暂时不想分类，也可以放入 `src/YueXinMiaoPet/PetAssets/Gifs/` 作为原始内置资源
3. 保持中文文件名即可，不需要改英文名
4. 不要修改安装目录里的用户配置；用户配置在 AppData
5. 启动程序，或在托盘/设置窗口点击“重新扫描 GIF”

分类目录示例：

```text
01_普通
02_开心
03_喜欢
04_害羞
05_生气
06_难过
07_累了
08_困了
09_想摸鱼
10_饿了
11_兴奋
12_思考
13_崩溃
```

安装后内置分类 GIF 会在安装目录的 `PetAssets\classified_gifs` 下，程序默认使用它们。

## 内置 GIF / 自定义 GIF

设置窗口里有“GIF 资源来源”：

- 使用内置月薪喵分类 GIF：扫描安装目录下的 `PetAssets\classified_gifs`
- 使用原始内置 GIF：扫描安装目录下的 `PetAssets\Gifs`
- 使用自定义 GIF 目录：扫描用户选择的目录
- 一键切回内置月薪喵分类 GIF：恢复分类内置资源

如果自定义目录不存在，或目录里没有可用 GIF，程序会回退到内置分类资源，避免桌宠没有动画。

## 心情与分类强绑定

心情窗口现在是卡片式 UI，选择心情时会显示对应分类说明和 GIF 预览。保存后：

- 写入 `%AppData%\YueXinMiaoPet\config.json`
- 立即更新当前 `MoodTag`
- 立即重新选择并播放对应分类 GIF
- 托盘菜单和设置调试面板同步刷新

默认映射由 `PetAssets/mood_category_map.json` 固定维护：

- `neutral / 普通` → `01_普通`
- `happy / 开心` → `02_开心`
- `love / 喜欢` → `03_喜欢`
- `shy / 害羞` → `04_害羞`
- `angry / 生气` → `05_生气`
- `sad / 难过` → `06_难过`
- `tired / 累了` → `07_累了`
- `sleepy / 困了` → `08_困了`
- `lazy / 想摸鱼` → `09_想摸鱼`
- `hungry / 饿了` → `10_饿了`
- `excited / 兴奋` → `11_兴奋`
- `thinking / 思考` → `12_思考`
- `collapse / 崩溃` → `13_崩溃`

单击桌宠时会严格在当前心情对应分类里随机选择 GIF；只有该分类为空时才回退到 `neutral / 01_普通`，最后才回退到全部 enabled GIF。这样例如 `angry` 状态点击桌宠只会播放 `05_生气`。

## 如何修改标签

优先级从高到低：

1. `PetAssets/assets.json` 中手工写好的 `tags`
2. `PetAssets/assets.tags.override.json`
3. 根据中文文件名自动推断
4. 未命中时默认为 `idle`

`assets.tags.override.json` 示例：

```json
{
  "上班困困.gif": ["work_start", "sleepy", "tired"],
  "下班开心.gif": ["work_end", "happy", "excited"]
}
```

如果要完整人工管理资源，可以把 `assets.generated.json` 的条目复制到 `assets.json`，再修改 `tags`、`weight`、`enabled`。

## 自动标签规则

文件名会先标准化：去空格、去常见符号、全角半角统一、转小写、保留中文。然后按“精确短语 → 常见词 → 单字兜底”推断多个标签。

示例：

- `上班困困.gif` → `work_start`, `sleepy`, `tired`
- `下班开心.gif` → `work_end`, `happy`, `excited`
- `下雨天发呆.gif` → `rain`, `idle` 或 `lazy`
- `晚安睡觉.gif` → `night`, `sleep`, `sleepy`
- `摸头开心.gif` → `touch`, `happy`
- `生气炸毛.gif` → `angry`
- `饿了干饭.gif` → `hungry`
- `下雪发抖.gif` → `snow`, `cold`

## 开发运行

推荐环境：

- Windows 10/11 开发机
- Visual Studio 2019/2022 或 Microsoft Build Tools
- .NET Framework 4.8 Developer Pack / Targeting Pack
- Inno Setup 6

构建：

```powershell
msbuild YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

运行：

```powershell
src/YueXinMiaoPet/bin/Release/YueXinMiaoPet.exe
```

心情点击分类自测：

```powershell
src/YueXinMiaoPet/bin/Release/YueXinMiaoPet.exe --mood-click-test
```

## 生成安装包

```powershell
cd E:\Tool\codex\YueXinMiaoPet
msbuild YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"

cd E:\Tool\codex\YueXinMiaoPet\installer
& "D:\Setting\InnoSetup\Inno Setup 6\ISCC.exe" YueXinMiaoPet.iss
```

输出：

```text
E:\Tool\codex\YueXinMiaoPet\installer\output\YueXinMiaoPet_Setup.exe
```

## Windows 7 / 10 / 11 兼容性

- 使用 WPF + .NET Framework 4.8，不依赖 Electron、Tauri、WinUI 3、WebView2 或 .NET 6+
- Windows 7 必须是 SP1，并安装 .NET Framework 4.8
- 安装包内置检测 .NET 4.8 Release Key，最低值为 `528040`
- 缺少 .NET 4.8 时，会调用 `installer/redist/NDP48-x86-x64-AllOS-ENU.exe`
- 用户配置、日志、天气缓存都写入 AppData，不写入安装目录
- 断网、GIF 缺失、配置损坏、`assets.json` 格式错误都会安全回退

## 调试面板

打开“设置 → 资源调试面板”，可以看到：

- 当前 WeatherTag、TimeTag、MoodTag、ActionTag
- 当前播放 GIF 文件名
- 当前播放 GIF 分类
- 当前 MoodTag 对应分类、点击候选分类和候选数量
- GIF 来源模式和目录
- GIF 总数量、enabled 数量
- 13 类扫描计数
- 当前标签来源
- 当前候选 GIF 前 5 名及分数
- 当前省市、经纬度、缩放、透明度

## 参与贡献

欢迎提交 Issue 或 Pull Request。贡献前请阅读：

- [CONTRIBUTING.md](CONTRIBUTING.md)
- [CHANGELOG.md](CHANGELOG.md)
- [SECURITY.md](SECURITY.md)

建议贡献方向：

- GIF 分类优化
- 天气表现优化
- Win7 / Win10 / Win11 兼容性测试
- 桌宠交互优化
- 安装脚本和文档改进

## 许可证

本项目代码使用 [MIT License](LICENSE)。

请确保提交的 GIF、图片、图标等素材拥有合法使用和分发权限。若未来对素材采用单独授权，请在对应目录补充说明文件。
