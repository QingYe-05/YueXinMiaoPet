# 中国大陆三级行政区数据说明

## 当前数据

- 运行时文件：`china_administrative_divisions.json`
- 生成脚本：`tools/generate_china_regions.py`
- 主数据源：`kk-418/cn-division` 的 2026 行政区数据。该项目声明数据来自民政部行政区划查询接口。
- 交叉校验源：`modood/Administrative-divisions-of-China` 的国家统计局 2023 年行政区划衍生数据。

生成后的树包含 `code`、`name`、`level`、`children`。界面按省级、地级、县级三级展示；直辖市保留“直辖市 → 同名城市 → 区县”的一致操作结构。重庆来源数据中的两个地级占位分组合并为一个“重庆市”节点，因此界面地级节点数比原始来源统计少 1，县级节点不丢失。

东莞、中山、儋州、嘉峪关等不设常规县级行政区的特殊结构，保留数据源提供的第三层辖区代码，保证三级选择仍可用。

## 校验

执行本地结构校验：

```powershell
python tools\validate_china_regions.py
```

执行在线交叉校验：

```powershell
python tools\validate_china_regions.py --online
```

在线校验会输出当前数据和 2023 参考数据的代码差异及样本。行政区会随时间调整，差异用于人工复核，不把全国节点总数硬编码为固定通过条件。

## 天气定位

天气查询按“县区代码 → 城市代码 → 省级代码”逐级回退。行政区代码先解析为中心点并按代码缓存，再由 Open-Meteo 查询天气；天气结果同样按行政区代码隔离缓存，避免同名县区或切换城市后错误复用旧天气。

数据源链接：

- https://github.com/kk-418/cn-division
- https://github.com/modood/Administrative-divisions-of-China
- https://www.stats.gov.cn/hd/lyzx/zxgk/202312/t20231206_1945208.html
