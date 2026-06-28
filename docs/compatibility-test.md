# 兼容性测试记录

## 测试日期

2026-06-28

## 测试系统

本机实测：

- 系统：Windows 10 Home China
- WindowsVersion：2009
- OS build / HAL：10.0.19041.6456
- 架构：64 位
- 内存：约 16 GB

待补充：

- Windows 7 SP1：待测试，需要 .NET Framework 4.8 Runtime
- Windows 11：待测试

## 构建命令

```powershell
msbuild YueXinMiaoPet.sln /p:Configuration=Release /p:Platform="Any CPU"
```

本机 `msbuild` 不在 PATH 时，使用了 Visual Studio / Build Tools 的完整路径：

```powershell
D:\Tool\VS_Code\devtools\vs2019\MSBuild\Current\Bin\MSBuild.exe
```

结果：

- Release 构建成功
- 0 warning
- 0 error

## 打包命令

```powershell
cd E:\Tool\codex\YueXinMiaoPet\installer
"D:\Setting\InnoSetup\Inno Setup 6\ISCC.exe" YueXinMiaoPet.iss
```

结果：

- Inno Setup 打包成功
- 输出文件存在：

```text
E:\Tool\codex\YueXinMiaoPet\installer\output\YueXinMiaoPet_Setup.exe
```

安装包信息：

- 文件大小：143,200,923 bytes
- SHA256：`38AEFB7A25EFC304DA379C0CC76A46C6571E7A9106F26B8F6C22185D010ACBEE`

## 安装测试

测试方式：

- 使用 `YueXinMiaoPet_Setup.exe` 静默安装到临时目录：

```text
E:\Tool\codex\YueXinMiaoPet\.install-test-app
```

验证结果：

- 主程序 `YueXinMiaoPet.exe` 存在
- 卸载器 `unins000.exe` 存在
- `PetAssets\classified_gifs` 存在
- 安装后内置 GIF 总数：186
- 安装后 13 类目录均存在

说明：

- Inno Setup 静默安装在当前 PowerShell 环境下未提供标准 `$LASTEXITCODE`，因此以文件存在、安装后 smoke test 和卸载结果作为判断依据。

## 启动测试

执行：

```powershell
E:\Tool\codex\YueXinMiaoPet\src\YueXinMiaoPet\bin\Release\YueXinMiaoPet.exe --ui-smoke-test
```

结果：

- 桌宠进程可启动
- 设置窗口 `月薪喵设置` 可打开
- 心情窗口 `今日心情` 可打开
- UI smoke test 自动退出成功

## GIF 加载测试

执行：

```powershell
src\YueXinMiaoPet\bin\Release\YueXinMiaoPet.exe --smoke-test
```

结果：

- 扫描目录：`PetAssets\classified_gifs`
- 分类数：13
- GIF 数量：186
- 规则引擎可选出 GIF

13 类计数：

| 分类 | 数量 |
| --- | ---: |
| 01_普通 | 55 |
| 02_开心 | 8 |
| 03_喜欢 | 12 |
| 04_害羞 | 5 |
| 05_生气 | 14 |
| 06_难过 | 6 |
| 07_累了 | 17 |
| 08_困了 | 9 |
| 09_想摸鱼 | 13 |
| 10_饿了 | 16 |
| 11_兴奋 | 13 |
| 12_思考 | 13 |
| 13_崩溃 | 5 |

## 心情切换测试

执行：

```powershell
src\YueXinMiaoPet\bin\Release\YueXinMiaoPet.exe --mood-click-test
```

结果：

- `angry` 连续点击 10 次均命中 `05_生气`
- `happy` 连续点击 10 次均命中 `02_开心`
- `hungry` 连续点击 10 次均命中 `10_饿了`
- `sleepy` 连续点击 10 次均命中 `08_困了`
- `neutral` 连续点击 10 次均命中 `01_普通`
- 心情过期后自动恢复 `neutral`

## 点击心情分类内切换测试

结果：

- 通过 `--mood-click-test`
- 日志中可看到每次点击的 `MoodTag`、候选分类、候选数量、选中 GIF 和选中分类
- 未发现点击后跳到非当前心情分类的问题

## 天气刷新测试

结果：

- Open-Meteo 天气刷新成功
- 本机测试期间天气标签映射为 `hot`
- 日志示例：`天气更新成功：hot 34.4℃`
- 断网场景未在本轮实测，代码预期会回退缓存或 `unknown`

## 设置窗口测试

结果：

- `月薪喵设置` 窗口可打开
- 调试面板可显示当前状态、13 类扫描计数、GIF 数量和候选信息
- 缩放、透明度、资源来源、省市选择等 UI 保持可访问

## 托盘菜单测试

本轮未逐项点击托盘菜单。已通过 UI smoke test 验证应用可启动并打开设置 / 心情窗口；托盘菜单仍需在 Win10 / Win11 手工补充完整点击验证：

- 显示 / 隐藏
- 今日心情
- 设置
- 重新扫描 GIF
- 退出

## 卸载测试

结果：

- 测试安装目录下卸载器 `unins000.exe` 存在
- 执行静默卸载后，临时安装目录已清理
- 未测试正式开始菜单快捷方式和桌面快捷方式删除情况，需在完整交互式安装场景补充验证

## 已知问题 / 待补充

- Windows 7 SP1 真实环境待测试，需要先确认 .NET Framework 4.8 Runtime 安装流程。
- Windows 11 真实环境待测试。
- 高 DPI 缩放、多显示器窗口位置保存需要在对应硬件环境补充测试。
- 托盘菜单完整交互需要手工补充验证。
- 缺少 .NET Framework 4.8 时的安装提示需要在干净虚拟机中补充验证。
