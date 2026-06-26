# 贡献指南

感谢你关注「月薪喵桌宠」。本项目欢迎问题反馈、功能建议、文档改进和代码贡献。

## 贡献方式

- 提交 Issue，反馈 Bug 或提出功能建议。
- 改进 README、BUILD、CHANGELOG 等文档。
- 优化 GIF 分类、心情映射和天气规则。
- 修复 Windows 兼容性问题。
- 提交 Pull Request 改进代码。

## 本地开发环境

推荐环境：

- Windows 10/11 开发机
- Visual Studio 2019/2022 或 Microsoft Build Tools
- .NET Framework 4.8 Developer Pack / Targeting Pack
- Inno Setup 6

构建命令：

```powershell
msbuild YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

## 提交 Issue 前

请尽量提供：

- 操作系统版本。
- 程序版本号。
- 复现步骤。
- 期望表现和实际表现。
- 相关日志片段。
- 如果和 GIF 有关，请说明 GIF 所在分类和文件名。

## Pull Request 要求

提交 PR 前请确认：

- 代码能正常构建。
- 不提交 `bin/`、`obj/`、`installer/output/`、本地配置、日志或 .NET 离线安装包。
- 不重命名、压缩或删除现有 GIF 资源，除非 PR 明确说明原因。
- 涉及心情逻辑时，请测试单击桌宠是否仍只在当前心情分类内切换 GIF。
- 涉及安装脚本时，请说明是否测试过 Release 构建和 Inno Setup 打包。

## GIF 资源贡献

新增 GIF 时建议：

1. 放入 `src/YueXinMiaoPet/PetAssets/classified_gifs/` 的对应分类。
2. 保持文件名语义清晰。
3. 不要提交来源不明或无授权的素材。
4. 如需新增分类，请同步更新 `mood_category_map.json`、README 和调试逻辑。

## 分支和提交信息

建议分支命名：

- `fix/mood-click-switching`
- `feat/weather-bubble`
- `docs/update-readme`
- `chore/installer-script`

建议提交信息：

- `Fix mood click GIF switching`
- `Improve weather reaction rules`
- `Update classified GIF documentation`

请保持友好、清晰、尊重。反馈问题时尽量给出复现步骤，讨论设计时聚焦问题本身。
