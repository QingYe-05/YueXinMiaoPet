# 月薪喵桌宠 v2.2.0 发布说明

## 本版本重点

v2.2.0 是月薪喵桌宠的 Windows + Linux 双平台更新版本。

本次更新为 Windows 版带来更精确的天气地区选择和更简洁的桌面天气气泡，同时正式提供独立的 Linux 实验版。Linux 版使用 Python 3 + PyQt6 实现，不依赖 Windows WPF 或 .NET。

## 下载

请根据操作系统下载对应的 Release 附件：

### Windows

- `YueXinMiaoPet_Setup.exe`

适用于：

- Windows 7 SP1
- Windows 10
- Windows 11

Windows 版需要 .NET Framework 4.8。安装程序会检测运行环境；缺少 .NET Framework 4.8 时会给出提示，并可调用随安装包提供的离线安装程序。

### Linux

- `yuexinmiao-pet-linux.tar.gz`

Linux 版目前为实验版，目标支持 Ubuntu、Debian、Linux Mint、Fedora，以及 KDE、GNOME、XFCE 等常见桌面环境。

> GitHub 自动生成的 `Source code (zip)` 和 `Source code (tar.gz)` 是源码快照，不是可直接安装的桌宠程序。普通用户请下载上面列出的系统附件。

## Windows 版更新

### 全国省、市、县区三级天气地区选择

- 天气地区设置升级为“省/直辖市 → 市/州/盟 → 县/区”三级联动。
- 支持全国大陆 31 个省级、341 个市级、2933 个县区级行政区节点。
- 兼容直辖市、自治州、地区、盟、旗、自治旗、林区和县级市等行政结构。
- 新增中文地区搜索，可按省、市、县区名称快速查找。
- 天气查询优先使用县区行政代码，降低同名县区导致的定位错误。
- 旧版省市配置会尝试自动迁移；无法匹配时会提示用户重新选择。
- 不再要求用户手动填写经纬度。

### 天气气泡优化

- 中国地区天气采用“高德地图天气实况优先、Open-Meteo 备用”的双数据源方案，天气现象、温度、风向和风力优先使用高德实况。
- 高德接口超时、Key 不可用、数据缺失或实况超过 3 小时时自动切换 Open-Meteo；两个数据源均失败时才使用当前行政区缓存。
- 直接使用省、市、县区选择结果中的行政区代码查询高德天气，设置调试面板可查看实际数据源与更新时间。
- 天气气泡继续位于月薪喵 GIF 正上方，不遮挡表情。
- 第一屏仅显示天气和温度，例如：`小雨 26℃`。
- 第二屏仅显示风向和风力，例如：`东北风 3级`。
- 两屏约每 4 秒轮换，避免把信息挤成一行。
- 桌面气泡不显示冗长的省市县名称；完整地区路径保留在设置窗口中。
- 支持小雨、中雨、大雨、暴雨、雷阵雨细分。
- 支持风向、风力级别和风速兜底显示。
- 天气缓存按行政区绑定，切换地区后不会继续显示其他城市的缓存。
- 天气功能默认关闭；关闭后不显示气泡，也不会主动刷新天气。

### 交互与稳定性

- 修复 Win11 高 DPI 缩放下拖动坐标与鼠标位置不一致、桌宠可能提前跑出屏幕的问题。
- 修复透明窗口区域偶发无法稳定按住桌宠的问题，并增强拖动期间的鼠标捕获。
- 修复不同缩放比例下“重置位置到屏幕中央”偏向右下的问题。
- 桌宠本体右键菜单使用“隐藏桌宠”，隐藏后程序和托盘继续运行。
- 托盘菜单可重新显示月薪喵，并可将桌宠重置到屏幕中央。
- 保留窗口位置异常、多显示器屏幕外自动修复。
- 保留 GIF 加载失败 fallback、拖动卡顿优化和启动诊断日志。
- 保留 Windows 7 软件渲染兼容方案，以及 `--safe-mode`、`--reset-window`、`--force-software-render` 启动参数。

## Linux 实验版

Linux 版是一个位于 `linux/yuexinmiao-pet-linux` 的独立桌宠实现。

### 已实现功能

- 透明、无边框、可置顶的桌宠窗口。
- 使用 QMovie 播放动态 GIF，支持中文路径和中文文件名。
- 内置 13 类心情分类及 186 个动态 GIF。
- 当前心情分类下的 GIF 按固定顺序循环播放，不随机。
- 支持鼠标拖动并保存窗口位置。
- 支持心情切换、重新扫描 GIF、重置位置和简单设置。
- 桌宠右键菜单与系统托盘菜单。
- 支持缩放比例、透明度、置顶、GIF 目录和轮播间隔设置。
- 支持全国省、市、县区三级天气地区选择和中文搜索。
- 支持与 Windows 版一致的天气气泡、雨量细分、风向及风力轮换显示。
- 配置和日志写入 Linux 用户目录，不需要 root 权限。

### Linux 安装与运行

解压后进入目录：

```bash
tar -xzf yuexinmiao-pet-linux.tar.gz
cd yuexinmiao-pet-linux
```

创建虚拟环境并安装依赖：

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

启动桌宠：

```bash
chmod +x run.sh
./run.sh
```

也可以直接运行：

```bash
python3 src/main.py
```

Linux 配置文件：

```text
~/.config/yuexinmiao-pet/config.json
```

Linux 日志文件：

```text
~/.local/share/yuexinmiao-pet/logs/app.log
```

### Linux 桌面环境说明

- X11 下透明窗口、置顶和托盘通常更稳定。
- Wayland 对置顶、透明窗口和系统托盘的限制因桌面环境而异。
- 如果托盘不可用，桌宠仍可运行，可通过桌宠右键菜单操作。
- Linux 版目前是实验版，欢迎反馈不同发行版和桌面环境下的兼容情况。

## 两个平台共同保留的核心体验

- 13 类月薪喵心情分类。
- 当前心情 GIF 顺序轮播。
- 中文 GIF 文件名和中文路径支持。
- 透明无边框桌宠、拖动与窗口位置保存。
- 天气挂件不会打断当前心情或用户自定义 GIF 轮播。
- 天气接口或资源加载失败时安全回退，不让主程序崩溃。

## Windows 用户排查

如果右下角托盘图标存在，但桌面上看不到月薪喵：

1. 右键托盘图标，点击“显示月薪喵”。
2. 点击“重置位置到屏幕中央”。
3. 使用以下参数启动：

```powershell
YueXinMiaoPet.exe --reset-window
```

老电脑或透明窗口显示异常时可尝试：

```powershell
YueXinMiaoPet.exe --safe-mode
```

Windows 配置文件：

```text
%AppData%\YueXinMiaoPet\config.json
```

Windows 日志文件：

```text
%AppData%\YueXinMiaoPet\logs\app.log
```

## 验证情况

- Windows Release 构建通过：0 个警告，0 个错误。
- Windows 目标框架：.NET Framework 4.8。
- 行政区数据校验通过：31 个省级、341 个市级、2933 个县区级节点。
- 行政区重复代码、空代码、错误层级和必测路径检查通过。
- Linux Python 语法检查通过。
- Linux PyQt6 离屏启动测试通过。
- Linux 发布包确认包含 186 个 GIF、应用图标、天气服务和三级行政区数据。

## 隐私说明

- 月薪喵桌宠不会上传用户文件。
- 不读取浏览器 Cookie、API Key 或其他认证信息。
- 用户配置、日志和天气缓存仅保存在本机用户目录。

## 反馈问题

反馈问题时，请尽量提供：

- 操作系统及版本。
- Windows 或 Linux 版本。
- 桌面环境（Linux 用户）。
- 问题截图或录屏。
- 对应平台的 `app.log`。
- 能稳定复现问题的操作步骤。
