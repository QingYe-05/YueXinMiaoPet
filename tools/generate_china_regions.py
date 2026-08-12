#!/usr/bin/env python3
"""生成月薪喵桌宠使用的中国大陆省、市、县三级行政区树。"""

import json
import pathlib
import urllib.request


SOURCE_URL = "https://raw.githubusercontent.com/kk-418/cn-division/main/dist/code/pca.json"
PROJECT_ROOT = pathlib.Path(__file__).resolve().parents[1]
OUTPUT_PATH = PROJECT_ROOT / "src" / "YueXinMiaoPet" / "Data" / "china_administrative_divisions.json"


def fetch_json(url):
    request = urllib.request.Request(url, headers={"User-Agent": "YueXinMiaoPet-region-generator/1.0"})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def normalize_code(value, level):
    text = str(value)
    if level == "province":
        return text[:2].ljust(6, "0")
    if level == "city":
        return text[:4].ljust(6, "0")
    # 绝大多数县级单位使用六位代码。东莞、中山、儋州、嘉峪关等没有标准县级层的城市，
    # 数据源会以下辖街镇的十二位代码补齐第三级 UI，此处必须保留完整代码以避免冲突。
    return text if len(text) > 6 else text[:6].ljust(6, "0")


def convert_county(item):
    return {
        "code": normalize_code(item["c"], "county"),
        "name": item["n"],
        "level": "county",
        "children": [],
    }


def convert_city(item):
    return {
        "code": normalize_code(item["c"], "city"),
        "name": item["n"],
        "level": "city",
        "children": [convert_county(child) for child in item.get("ch", [])],
    }


def convert_province(item):
    cities = [convert_city(child) for child in item.get("ch", [])]

    # 重庆源数据把市辖区和县拆成两组。桌宠 UI 统一为“重庆市 → 重庆市 → 区县”。
    if item.get("n") == "重庆市":
        counties = []
        for city in cities:
            counties.extend(city["children"])
        cities = [{"code": "500100", "name": "重庆市", "level": "city", "children": counties}]

    return {
        "code": normalize_code(item["c"], "province"),
        "name": item["n"],
        "level": "province",
        "children": cities,
    }


def main():
    source = fetch_json(SOURCE_URL)
    divisions = [convert_province(item) for item in source]
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT_PATH.open("w", encoding="utf-8", newline="\n") as stream:
        json.dump(divisions, stream, ensure_ascii=False, indent=2)
        stream.write("\n")

    city_count = sum(len(province["children"]) for province in divisions)
    county_count = sum(len(city["children"]) for province in divisions for city in province["children"])
    print("Generated:", OUTPUT_PATH)
    print("Province count:", len(divisions))
    print("City count:", city_count)
    print("County count:", county_count)


if __name__ == "__main__":
    main()
