# 月薪喵桌宠安装包说明

本目录用于通过 Inno Setup 生成单文件安装包：

```text
output/YueXinMiaoPet_Setup.exe
```

## 构建步骤

先在项目根目录构建 Release：

```powershell
cd /d E:\Tool\codex\YueXinMiaoPet
msbuild YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

再执行 Inno Setup：

```powershell
cd /d E:\Tool\codex\YueXinMiaoPet\installer
& "D:\Setting\InnoSetup\Inno Setup 6\ISCC.exe" YueXinMiaoPet.iss
```

## .NET Framework 4.8 离线包

安装脚本会使用：

```text
installer/redist/NDP48-x86-x64-AllOS-ENU.exe
```

请保持文件名完全一致。脚本会检测 .NET Framework 4.8 Release Key 是否大于等于 `528040`；缺失时会提示用户并调用该离线安装包。

## 安装内容

安装目录会包含：

- `YueXinMiaoPet.exe`
- `PetAssets/`：内置月薪喵分类 GIF、原始 GIF 和资源索引
- `Assets/Icons/`：exe、窗口、托盘、快捷方式、安装包图标
- `Data/china_cities.json`：省市经纬度数据

用户配置和日志不会写入安装目录，而是写入：

```text
%AppData%\YueXinMiaoPet
```

## Windows 7 SP1 说明

Windows 7 必须是 SP1，并安装 .NET Framework 4.8。若目标机器缺少 TLS 1.2 或系统补丁，天气接口可能失败；程序会回退缓存天气或 `unknown`，不会崩溃。
