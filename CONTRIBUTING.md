# Contributing

感谢你愿意一起改进「月薪喵桌宠」。这个项目优先保持轻量、稳定和 Windows 7 SP1 / Windows 10 / Windows 11 兼容。

## 提交 Issue

提交 Bug 时请尽量包含：

- 问题描述
- 复现步骤
- 期望表现和实际表现
- Windows 版本、是否安装 .NET Framework 4.8
- 相关日志：`%AppData%\YueXinMiaoPet\logs\app.log`
- 如有必要，附截图或 GIF 文件名

提交功能建议时请说明：

- 使用场景
- 预期效果
- 是否影响现有心情分类、天气规则或安装包

## 提交 PR

1. 从 `main` 创建功能分支。
2. 只提交和当前任务相关的文件。
3. 保持 C# / WPF / .NET Framework 4.8 技术路线，不引入 Electron、Tauri、.NET 6+、WinUI。
4. 修改前后都尽量保留中文路径和中文文件名兼容。
5. PR 描述中说明改动内容、原因和验证命令。

## 本地开发环境

- Windows 10/11 开发机
- Visual Studio 2019/2022 或 Microsoft Build Tools
- .NET Framework 4.8 Developer Pack / Targeting Pack
- Inno Setup 6

## 构建命令

```powershell
msbuild YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

如果本机 `msbuild` 不在 PATH，可使用 Visual Studio / Build Tools 自带的 `MSBuild.exe` 完整路径。

## 测试要求

基础 smoke test：

```powershell
src/YueXinMiaoPet/bin/Release/YueXinMiaoPet.exe --smoke-test
```

心情分类逻辑测试：

```powershell
src/YueXinMiaoPet/bin/Release/YueXinMiaoPet.exe --mood-click-test
```

修改心情映射、`GifPicker`、资源扫描或 GIF 分类时，必须至少运行 `--mood-click-test`。

## 不要提交的文件

请不要提交：

- `bin/`
- `obj/`
- `.vs/`
- `installer/output/`
- `installer/redist/*.exe`
- `%AppData%` 下的 `config.json` 或日志
- 本地临时文件、打包输出、安装包

安装包应上传到 GitHub Releases，而不是提交进 Git 仓库。

## GIF 资源贡献注意事项

- 内置分类目录：`src/YueXinMiaoPet/PetAssets/classified_gifs`
- 分类目录保持 13 类：
  - `01_普通`
  - `02_开心`
  - `03_喜欢`
  - `04_害羞`
  - `05_生气`
  - `06_难过`
  - `07_累了`
  - `08_困了`
  - `09_想摸鱼`
  - `10_饿了`
  - `11_兴奋`
  - `12_思考`
  - `13_崩溃`
- 不要重命名、压缩或转换已有 GIF，除非 PR 明确说明原因。
- 新增 GIF 建议使用能体现状态的中文文件名，方便自动标签推断。
