# v1.0.0 手动发布步骤

当前本机未安装 GitHub CLI `gh`，也没有可用的 `GITHUB_TOKEN` / `GH_TOKEN` 环境变量。如果无法通过 GitHub 连接器或其他自动化方式创建 Release，请按以下步骤手动发布。

## Release 页面

打开：

```text
https://github.com/QingYe-05/YueXinMiaoPet/releases/new
```

## Release 信息

- Tag：`v1.0.0`
- Title：`YueXinMiaoPet v1.0.0`
- Notes：复制 `RELEASE_NOTES.md` 内容

## 上传附件

上传：

```text
E:\Tool\codex\YueXinMiaoPet\installer\output\YueXinMiaoPet_Setup.exe
```

上传后附件名应为：

```text
YueXinMiaoPet_Setup.exe
```

## 校验

发布后确认：

- Release 地址存在：`https://github.com/QingYe-05/YueXinMiaoPet/releases/tag/v1.0.0`
- 附件中有 `YueXinMiaoPet_Setup.exe`
- 安装包没有提交到 Git 仓库
- `.NET Framework 4.8` 离线安装包没有提交到 Git 仓库
