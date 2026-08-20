# 月薪喵桌宠 Linux 实验版

这是「月薪喵桌宠」的 Linux 独立实验版，使用 Python 3 + PyQt6 实现，不依赖 Windows WPF、.NET 或注册表。

Windows 版仍然是主版本；Linux 版放在 `linux/yuexinmiao-pet-linux`，不会影响 Windows 安装包构建。

## 功能

- 透明无边框桌宠窗口
- 置顶显示
- 鼠标左键拖动并保存窗口位置
- 使用 `QMovie` 播放 GIF
- 支持中文 GIF 路径和中文文件名
- 支持 13 类心情目录
- 当前心情 GIF 顺序轮播，不随机
- 右键菜单：今日心情、设置、重新扫描 GIF、重置位置、退出
- 系统托盘菜单：显示月薪喵、今日心情、设置、重新扫描 GIF、重置位置、退出
- 简单设置：缩放比例、透明度、置顶、GIF 目录、轮播间隔
- 可选天气挂件（默认关闭），固定显示在 GIF 正上方
- 全国省 / 市 / 县区三级联动和地区搜索
- 天气气泡每 4 秒轮换“天气 温度”和“风向 风力”，并支持雨量细分

## 兼容性说明

目标桌面环境：

- Ubuntu / Debian / Linux Mint / Fedora
- KDE / GNOME / XFCE

说明：

- X11 下透明窗口、置顶和托盘通常更稳定。
- Wayland 下透明窗口、置顶、托盘可能受桌面环境限制；如果表现异常，建议切换到 X11 会话测试。
- 如果系统托盘不可用，程序仍会运行，只是不会显示托盘图标。

## 安装依赖

```bash
cd linux/yuexinmiao-pet-linux
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

或者执行：

```bash
chmod +x install.sh
./install.sh
```

## 同步 Windows 版 GIF 资源

Linux 版默认从：

```text
assets/classified_gifs
```

读取 13 类 GIF。可以从 Windows 项目资源复制：

```bash
python3 sync_assets_from_windows_project.py
```

脚本来源：

```text
src/YueXinMiaoPet/PetAssets/classified_gifs
```

复制到：

```text
linux/yuexinmiao-pet-linux/assets/classified_gifs
```

脚本只复制 GIF，不移动、不删除、不重命名原始资源。

## 运行

```bash
./run.sh
```

或：

```bash
python3 src/main.py
```

## 配置与日志

配置文件：

```text
~/.config/yuexinmiao-pet/config.json
```

日志文件：

```text
~/.local/share/yuexinmiao-pet/logs/app.log
```

## 13 类心情映射

```text
neutral  -> 01_普通
happy    -> 02_开心
love     -> 03_喜欢
shy      -> 04_害羞
angry    -> 05_生气
sad      -> 06_难过
tired    -> 07_累了
sleepy   -> 08_困了
lazy     -> 09_想摸鱼
hungry   -> 10_饿了
excited  -> 11_兴奋
thinking -> 12_思考
collapse -> 13_崩溃
```

## 注意

- Linux 版是独立桌宠实现，不依赖 Windows 专有能力。
- Linux 版不读取 Windows 注册表。
- Linux 版不需要管理员权限或 root 权限。
- Linux 版不会上传任何数据。

## 天气说明

在设置中勾选“显示天气挂件”，依次选择省、市、县区并保存。中国地区天气统一使用行政区代码查询高德地图天气实况，不再请求其他天气数据源。高德接口超时、Key 不可用、数据缺失或实况超过 3 小时时，仅回退到当前行政区缓存。完整地区路径只在设置窗口中显示，桌面气泡保持简洁：

```text
小雨 26℃
东北风 3级
```

两种文本约每 4 秒轮换。支持小雨、中雨、大雨、暴雨、雷阵雨；两个数据源均失败时使用当前行政区缓存，关闭天气后气泡与刷新定时器一并停止。

高德 Web 服务 Key 可通过配置字段 `amap_weather_api_key`、环境变量 `YUEXINMIAO_AMAP_KEY` 或 `config/amap.key` 提供。Key 文件不会提交到源码仓库，日志也不会输出 Key。
