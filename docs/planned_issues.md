# Planned Issues

本文件保留 v1.0.0 前后需要跟踪的公开 Issue 内容，便于在 GitHub CLI 不可用时手动创建。当前会话如果 GitHub 连接器可用，会优先直接创建 Issue。

## [Enhancement] 优化 GIF 分类和标签推断规则

GitHub: https://github.com/QingYe-05/YueXinMiaoPet/issues/6

- 梳理 13 类目录下的 GIF 命名规律
- 增强中文短语优先匹配规则
- 增加更多文件名示例测试
- 检查 assets.generated.json 与分类目录一致性
- 在 README 中补充新增 GIF 推荐命名规则

## [Enhancement] 增强天气表现和天气提示气泡

GitHub: https://github.com/QingYe-05/YueXinMiaoPet/issues/7

- 优化 sunny / rain / thunder / snow / hot / cold 的候选 GIF 规则
- 天气变化时展示更自然的短提示气泡
- 调试面板显示天气候选 GIF 和分数
- 处理断网、unknown、城市未设置等情况

## [Compatibility] 补充 Windows 7 SP1 / Windows 10 / Windows 11 兼容性测试

GitHub: https://github.com/QingYe-05/YueXinMiaoPet/issues/8

- Windows 7 SP1 + .NET Framework 4.8 安装测试
- Windows 10 安装和运行测试
- Windows 11 安装和运行测试
- 高 DPI 缩放测试
- 多显示器窗口位置保存测试
- 安装包卸载测试
- 缺少 .NET Framework 4.8 时的提示测试

## [Enhancement] 增加更多桌宠交互动作和反馈

GitHub: https://github.com/QingYe-05/YueXinMiaoPet/issues/9

- 增加长时间无互动后的状态反馈
- 增加连续点击后的特殊反应
- 增加下班时间、午休时间等时间段彩蛋
- 增加可开关提示气泡
- 调试面板显示最近一次互动来源

## [Release] v1.0.0 发布检查清单

GitHub: https://github.com/QingYe-05/YueXinMiaoPet/issues/10

- Release 构建成功
- Inno Setup 打包成功
- 安装包不提交到源码仓库
- 安装包上传到 GitHub Releases 附件
- README 中下载说明可用
- CHANGELOG 中记录 v1.0.0
- 安装后能加载 13 类 classified_gifs
- 安装后桌面快捷方式、开始菜单和托盘图标正常
- 卸载流程正常
