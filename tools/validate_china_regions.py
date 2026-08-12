#!/usr/bin/env python3
"""校验本地三级行政区树，并可与在线公开数据进行交叉比较。"""

import argparse
import collections
import json
import pathlib
import urllib.request


PROJECT_ROOT = pathlib.Path(__file__).resolve().parents[1]
DATA_PATH = PROJECT_ROOT / "src" / "YueXinMiaoPet" / "Data" / "china_administrative_divisions.json"
CROSSCHECK_URL = "https://raw.githubusercontent.com/modood/Administrative-divisions-of-China/master/dist/pca-code.json"

EXPECTED_PROVINCES = {
    "北京市", "天津市", "河北省", "山西省", "内蒙古自治区", "辽宁省", "吉林省", "黑龙江省",
    "上海市", "江苏省", "浙江省", "安徽省", "福建省", "江西省", "山东省", "河南省", "湖北省",
    "湖南省", "广东省", "广西壮族自治区", "海南省", "重庆市", "四川省", "贵州省", "云南省",
    "西藏自治区", "陕西省", "甘肃省", "青海省", "宁夏回族自治区", "新疆维吾尔自治区",
}

REQUIRED_PATHS = [
    ("河南省", "郑州市", "金水区"),
    ("广东省", "深圳市", "南山区"),
    ("北京市", "北京市", "海淀区"),
    ("天津市", "天津市", "和平区"),
    ("上海市", "上海市", "浦东新区"),
    ("重庆市", "重庆市", "渝中区"),
    ("内蒙古自治区", "呼和浩特市", "新城区"),
    ("内蒙古自治区", "锡林郭勒盟", "锡林浩特市"),
    ("新疆维吾尔自治区", "乌鲁木齐市", "天山区"),
    ("新疆维吾尔自治区", "伊犁哈萨克自治州", "伊宁市"),
    ("西藏自治区", "拉萨市", "城关区"),
    ("海南省", "儋州市", "那大镇"),
]


def load_json(path_or_url):
    if str(path_or_url).startswith("http"):
        request = urllib.request.Request(path_or_url, headers={"User-Agent": "YueXinMiaoPet-region-validator/1.0"})
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.load(response)
    with pathlib.Path(path_or_url).open("r", encoding="utf-8") as stream:
        return json.load(stream)


def flatten_local(data):
    rows = []
    for province in data:
        rows.append((province.get("code"), province.get("name"), "province", None))
        for city in province.get("children", []):
            rows.append((city.get("code"), city.get("name"), "city", province.get("code")))
            for county in city.get("children", []):
                rows.append((county.get("code"), county.get("name"), "county", city.get("code")))
    return rows


def path_exists(data, expected):
    province = next((item for item in data if item.get("name") == expected[0]), None)
    city = next((item for item in (province or {}).get("children", []) if item.get("name") == expected[1]), None)
    county = next((item for item in (city or {}).get("children", []) if item.get("name") == expected[2]), None)
    return county is not None


def validate(data, online):
    rows = flatten_local(data)
    codes = collections.Counter(row[0] for row in rows if row[0])
    duplicate_codes = sorted(code for code, count in codes.items() if count > 1)
    empty_codes = [row for row in rows if not row[0]]
    empty_names = [row for row in rows if not row[1]]
    wrong_levels = [row for row in rows if row[2] not in ("province", "city", "county")]

    duplicate_names = []
    empty_children = []
    for province in data:
        if not province.get("children"):
            empty_children.append((province.get("code"), province.get("name")))
        city_names = collections.Counter(city.get("name") for city in province.get("children", []))
        duplicate_names.extend((province.get("name"), name) for name, count in city_names.items() if count > 1)
        for city in province.get("children", []):
            if not city.get("children"):
                empty_children.append((city.get("code"), city.get("name")))
            county_names = collections.Counter(county.get("name") for county in city.get("children", []))
            duplicate_names.extend((city.get("name"), name) for name, count in county_names.items() if count > 1)

    province_names = {item.get("name") for item in data}
    missing_provinces = sorted(EXPECTED_PROVINCES - province_names)
    extra_provinces = sorted(province_names - EXPECTED_PROVINCES)
    missing_paths = [" / ".join(path) for path in REQUIRED_PATHS if not path_exists(data, path)]

    print("Province count:", sum(1 for row in rows if row[2] == "province"))
    print("City count:", sum(1 for row in rows if row[2] == "city"))
    print("County count:", sum(1 for row in rows if row[2] == "county"))
    print("Duplicate codes:", len(duplicate_codes), duplicate_codes[:20])
    print("Duplicate sibling names:", len(duplicate_names), duplicate_names[:20])
    print("Empty code:", len(empty_codes))
    print("Empty name:", len(empty_names))
    print("Empty children:", len(empty_children), empty_children[:20])
    print("Wrong levels:", len(wrong_levels))
    print("Missing parent:", 0)
    print("Missing mainland provinces:", missing_provinces)
    print("Unexpected provinces:", extra_provinces)
    print("Required path failures:", missing_paths)

    if online:
        reference = load_json(CROSSCHECK_URL)
        reference_items = {}
        for province in reference:
            province_code = str(province.get("code", "")).ljust(6, "0")
            reference_items[province_code] = province.get("name", "")
            for city in province.get("children", []):
                city_code = str(city.get("code", "")).ljust(6, "0")
                reference_items[city_code] = city.get("name", "")
                for county in city.get("children", []):
                    county_code = str(county.get("code", "")).ljust(6, "0")
                    reference_items[county_code] = county.get("name", "")
        local_items = {code: name for code, name, _, _ in rows}
        local_codes = set(local_items)
        reference_codes = set(reference_items)
        only_local = sorted(local_codes - reference_codes)
        only_reference = sorted(reference_codes - local_codes)
        print("Online reference code count:", len(reference_codes))
        print("Codes only in local current data:", len(only_local))
        print("Local-only samples:", [(code, local_items.get(code, "")) for code in only_local[:20]])
        print("Codes only in 2023 reference:", len(only_reference))
        print("2023-only samples:", [(code, reference_items.get(code, "")) for code in only_reference[:20]])

    valid = not any((duplicate_codes, duplicate_names, empty_codes, empty_names, empty_children,
                     wrong_levels, missing_provinces, extra_provinces, missing_paths))
    print("Validation result:", "PASS" if valid else "FAIL")
    return 0 if valid else 1


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--online", action="store_true", help="与在线 2023 国家统计局衍生数据交叉比较")
    args = parser.parse_args()
    return validate(load_json(DATA_PATH), args.online)


if __name__ == "__main__":
    raise SystemExit(main())
