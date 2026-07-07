# 构建与发布指南

## 环境要求

开发构建：

- Visual Studio 2019/2022 或 Microsoft Build Tools
- .NET Framework 4.8 Developer Pack / Targeting Pack
- Inno Setup 6

运行环境：

- Windows 7 SP1 / Windows 10 / Windows 11
- .NET Framework 4.8 Runtime

## Release 构建

在项目根目录执行：

```powershell
cd /d E:\Tool\codex\YueXinMiaoPet
msbuild YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

如果系统找不到 `msbuild`，可以使用本机 Visual Studio 的实际路径，例如：

```powershell
& "D:\Tool\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

输出：

```text
src/YueXinMiaoPet/bin/Release/YueXinMiaoPet.exe
```

Release 输出目录必须包含：

- `YueXinMiaoPet.exe`
- `PetAssets/classified_gifs/**/*.gif`
- `PetAssets/Gifs/*.gif`
- `PetAssets/assets.json`
- `PetAssets/assets.generated.json`
- `PetAssets/assets.tags.override.json`
- `PetAssets/mood_category_map.json`
- `Assets/Icons/app.ico`
- `Assets/Icons/tray.ico`
- `Assets/Icons/app.png`
- `Data/china_cities.json`

## Smoke test

```powershell
src/YueXinMiaoPet/bin/Release/YueXinMiaoPet.exe --smoke-test
```

心情点击分类自测：

```powershell
src/YueXinMiaoPet/bin/Release/YueXinMiaoPet.exe --mood-click-test
```

## Codex 状态脚本测试

开发环境中可以直接运行：

```powershell
.\tools\codex-status.ps1 -Status coding -Title "Codex 正在写代码" -Message "正在测试月薪喵状态气泡" -Progress 45
```

脚本会写入：

```text
%AppData%\YueXinMiaoPet\codex_status.json
```

月薪喵设置里启用“Codex 状态显示”后，应能在 GIF 上方看到 Codex 状态气泡。

## 生成安装包

Inno Setup 路径固定为：

```text
D:\Setting\InnoSetup\Inno Setup 6\ISCC.exe
```

执行：

```powershell
cd /d E:\Tool\codex\YueXinMiaoPet\installer
& "D:\Setting\InnoSetup\Inno Setup 6\ISCC.exe" YueXinMiaoPet.iss
```

输出：

```text
E:\Tool\codex\YueXinMiaoPet\installer\output\YueXinMiaoPet_Setup.exe
```

安装包应包含：

- 主程序 exe
- 必要 DLL / WPF 编译资源
- `PetAssets`
- `Assets`
- `Data`
- `tools/codex-status.ps1`
- `tools/codex-status-example.bat`
- `.NET Framework 4.8` 离线安装包（安装时复制到临时目录）

## .NET Framework 4.8 检测

安装脚本通过注册表检测：

```text
HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\Release >= 528040
```

离线安装包路径必须是：

```text
installer/redist/NDP48-x86-x64-AllOS-ENU.exe
```

如果目标机器缺少 .NET Framework 4.8，安装程序会提示并调用该离线安装包。该运行时安装器本身可能触发管理员权限或重启提示。

## v2.1.0 验收重点

- Release 构建成功。
- Inno Setup 打包成功。
- 设置窗口包含“Codex 状态”区域。
- 默认 `CodexStatusEnabled=false`。
- 点击“测试状态”后，启用 Codex 状态显示并写入 `codex_status.json`。
- 状态气泡显示在天气气泡和月薪喵 GIF 上方，不遮挡 GIF。
- 托盘菜单包含“Codex 状态”子菜单。
- `tools/codex-status.ps1` 可以写入中文 JSON。
- Codex 状态默认不影响当前心情 GIF 轮播。
- JSON 损坏、文件缺失、未知状态都不会导致应用崩溃。

## 不要提交的文件

- `installer/output/`
- `installer/redist/*.exe`
- `bin/`
- `obj/`
- `.vs/`
- `config.json`
- `logs/`
- `*.log`
- `*.tmp`
- `*.bak`
- `YueXinMiaoPet_Setup.exe`
- `YueXinMiaoPet_Setup.zip`
