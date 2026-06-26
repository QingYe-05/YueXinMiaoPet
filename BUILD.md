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

如果系统找不到 `msbuild`，可以使用：

```powershell
& "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
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

smoke test 会扫描 GIF、生成/更新资源索引，并验证规则引擎能选出 GIF。新用户默认应扫描 `PetAssets/classified_gifs`，日志中应显示 `资源数：186，分类数：13`。

心情点击分类自测：

```powershell
src/YueXinMiaoPet/bin/Release/YueXinMiaoPet.exe --mood-click-test
```

该自测会验证 `angry / happy / hungry / sleepy / neutral` 各点击 10 次均只命中当前心情对应分类，并验证心情过期后恢复 `neutral`。

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

## .NET Framework 4.8 检测

安装脚本通过注册表检测：

```text
HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\Release >= 528040
```

离线安装包路径必须是：

```text
installer/redist/NDP48-x86-x64-AllOS-ENU.exe
```

如果目标机器缺少 .NET Framework 4.8，安装程序会提示并调用这个离线安装包。这个运行时安装器本身可能触发管理员权限或重启提示。

## 验收建议

1. Release 构建成功
2. `YueXinMiaoPet.exe --smoke-test` 成功
3. 直接启动 exe 能看到桌宠和托盘图标
4. 设置窗口能打开，缩放/透明度 Slider 可实时预览
5. 心情窗口保存后立即切换 GIF
6. 单击桌宠时不会跳出当前 MoodTag 对应分类
7. 自定义 GIF 目录为空时能回退内置分类资源
8. `assets.generated.json` 中中文文件名正常
9. Inno Setup 打包成功
10. 安装包包含主程序、PetAssets、Assets、Data、.NET 4.8 redist
11. 安装和卸载正常
