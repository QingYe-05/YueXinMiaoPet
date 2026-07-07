# 月薪喵桌宠

「月薪喵桌宠」是一个基于 C# / WPF / .NET Framework 4.8 的 Windows 桌宠应用，目标兼容 Windows 7 SP1、Windows 10、Windows 11。

它会扫描中文命名的 GIF，并按当前心情分类顺序轮播月薪喵动画；天气和 Codex 工作状态都作为可选小气泡显示在 GIF 上方，默认不干扰 GIF 轮播。

## 主要功能

- 透明、无边框、可拖动、可置顶的桌宠窗口。
- 支持中文路径和中文 GIF 文件名。
- 默认加载内置 `PetAssets/classified_gifs` 的 13 类月薪喵 GIF。
- 支持自定义 GIF 目录，并可一键切回内置月薪喵。
- 当前心情下的 GIF 默认按固定顺序循环播放，不再随机抢回普通 GIF。
- 支持当前心情自定义轮播、全局自定义轮播。
- 支持心情窗口、设置窗口、托盘菜单、资源调试面板。
- 可选天气挂件：默认关闭，只显示天气状况和温度。
- 可选 Codex 工作状态挂件：默认关闭，通过本地 JSON 文件桥接。
- 配置保存到 `%AppData%\YueXinMiaoPet\config.json`。
- 日志保存到 `%AppData%\YueXinMiaoPet\logs\app.log`。

## 运行环境

- Windows 7 SP1 / Windows 10 / Windows 11
- .NET Framework 4.8 Runtime

Windows 7 SP1 需要安装 .NET Framework 4.8。安装包会检测 .NET 4.8 Release Key；如果缺失，会尝试调用随安装包携带的离线安装器。

## GIF 资源

内置分分类 GIF 目录：

```text
src/YueXinMiaoPet/PetAssets/classified_gifs/
```

13 类目录映射：

- `neutral` -> `01_普通`
- `happy` -> `02_开心`
- `love` -> `03_喜欢`
- `shy` -> `04_害羞`
- `angry` -> `05_生气`
- `sad` -> `06_难过`
- `tired` -> `07_累了`
- `sleepy` -> `08_困了`
- `lazy` -> `09_想摸鱼`
- `hungry` -> `10_饿了`
- `excited` -> `11_兴奋`
- `thinking` -> `12_思考`
- `collapse` -> `13_崩溃`

重新扫描 GIF：

1. 右键托盘图标。
2. 点击“重新扫描 GIF”。
3. 或打开设置窗口点击“重新扫描 GIF”。

## 天气功能

天气功能默认关闭：

- `WeatherEnabled=false`
- `WeatherAffectsGif=false`

启用后，天气气泡显示在月薪喵 GIF 正上方，只显示天气状况和温度，例如：

```text
晴 28℃
多云 21℃
```

天气服务使用 Open-Meteo。断网、接口失败、天气缓存缺失都不会导致应用崩溃。

## Codex 工作状态显示

v2.1.0 新增完全本地的 Codex 工作状态桥接方案。月薪喵不会读取 Codex 私有 API、不会硬编码 OpenAI API Key、不会读取浏览器 Cookie，也不会上传本地文件。它只监听一个本地 JSON 文件，并把文件里的状态显示成桌宠上方的小气泡。

默认配置：

- `CodexStatusEnabled=false`：默认不启用 Codex 状态显示。
- `CodexStatusBubbleEnabled=true`：启用后默认显示状态气泡。
- `CodexStatusAffectsGif=false`：默认只显示文字，不影响当前心情 GIF 轮播。
- `CodexStatusRefreshIntervalSeconds=2`：FileSystemWatcher 之外，每 2 秒轮询兜底一次。

状态文件默认路径：

```text
%AppData%\YueXinMiaoPet\codex_status.json
```

状态 JSON 示例：

```json
{
  "enabled": true,
  "status": "coding",
  "title": "Codex 正在写代码",
  "message": "正在修改月薪喵桌宠项目",
  "task": "v2.1.0",
  "progress": 45,
  "updatedAt": "2026-07-07T19:30:00+08:00",
  "source": "codex"
}
```

支持的状态：

- `idle`：空闲
- `planning`：正在分析任务
- `reading`：正在阅读项目
- `coding`：正在写代码
- `building`：正在构建
- `testing`：正在测试
- `reviewing`：正在检查
- `waiting`：等待用户确认
- `done`：已完成
- `error`：出错了

未知状态不会导致崩溃，会显示为“未知状态”。如果 `updatedAt` 距离当前时间超过 10 分钟且状态不是 `idle`，气泡会提示“Codex：状态可能已过期”。

设置入口：

1. 打开“设置”。
2. 找到“Codex 状态”区域。
3. 勾选“启用 Codex 状态显示”。
4. 可选择是否显示气泡、是否让 Codex 状态影响 GIF。
5. 可自定义状态文件路径、打开状态文件目录、创建默认状态文件，或点击“测试状态”验证气泡显示。

托盘菜单入口：

- “Codex 状态 / 启用 Codex 状态显示”
- “Codex 状态 / 显示 Codex 状态”
- “Codex 状态 / 打开 Codex 状态文件”
- “Codex 状态 / 重置 Codex 状态为空闲”

辅助脚本：

```powershell
.\tools\codex-status.ps1 -Status planning -Title "Codex 正在分析" -Message "正在阅读项目结构" -Progress 10
.\tools\codex-status.ps1 -Status coding -Title "Codex 正在写代码" -Message "正在修改播放逻辑" -Progress 40
.\tools\codex-status.ps1 -Status building -Title "Codex 正在构建" -Message "正在执行 msbuild" -Progress 70
.\tools\codex-status.ps1 -Status done -Title "Codex 已完成" -Message "任务完成，可以查看结果" -Progress 100
```

安装后脚本位于：

```text
{安装目录}\tools\codex-status.ps1
```

## 桌宠不显示怎么办

如果右下角托盘有月薪喵图标，但桌面上看不到 GIF：

1. 右键托盘图标，点击“显示月薪喵”。
2. 点击“重置位置到屏幕中央”。
3. 备份或删除配置文件后重启：

```text
%AppData%\YueXinMiaoPet\config.json
```

4. 使用重置窗口参数：

```powershell
YueXinMiaoPet.exe --reset-window
```

5. Win7 / 老电脑 / 透明窗口异常时使用安全模式：

```powershell
YueXinMiaoPet.exe --safe-mode
```

日志路径：

```text
%AppData%\YueXinMiaoPet\logs\app.log
```

## 构建

推荐环境：

- Visual Studio 2019/2022 或 Microsoft Build Tools
- .NET Framework 4.8 Developer Pack / Targeting Pack
- Inno Setup 6

构建：

```powershell
cd /d E:\Tool\codex\YueXinMiaoPet
msbuild YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

如果 `msbuild` 不在 PATH 中，可使用本机实际路径，例如：

```powershell
& "D:\Tool\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

打包：

```powershell
cd /d E:\Tool\codex\YueXinMiaoPet\installer
& "D:\Setting\InnoSetup\Inno Setup 6\ISCC.exe" YueXinMiaoPet.iss
```

安装包输出：

```text
E:\Tool\codex\YueXinMiaoPet\installer\output\YueXinMiaoPet_Setup.exe
```

## 兼容性说明

- 使用 WPF + .NET Framework 4.8，不依赖 Electron、Tauri、WinUI、WebView2 或 .NET 6+。
- Windows 7 SP1 默认启用 WPF 软件渲染，降低老显卡黑屏、透明窗口异常或驱动不稳定风险。
- 安装包检测 .NET 4.8 Release Key，最低值 `528040`。
- 用户配置、日志、天气缓存、Codex 状态文件都写入 AppData，不写入安装目录。

## License

MIT License，见 [LICENSE](LICENSE)。
