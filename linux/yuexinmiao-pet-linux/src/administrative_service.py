from __future__ import annotations

import json
import logging
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Optional

from app_config import ADMINISTRATIVE_DATA_PATH


@dataclass(frozen=True)
class Region:
    code: str
    name: str
    level: str
    children: tuple["Region", ...] = ()


@dataclass(frozen=True)
class RegionPath:
    province: Region
    city: Region
    county: Region

    @property
    def display(self) -> str:
        return f"{self.province.name} / {self.city.name} / {self.county.name}"


class AdministrativeService:
    """一次加载全国三级行政区数据，并在内存中完成联动和搜索。"""

    def __init__(self, path: Path = ADMINISTRATIVE_DATA_PATH) -> None:
        self.path = path
        self.provinces: List[Region] = []
        self.by_code: Dict[str, Region] = {}
        self.paths: List[RegionPath] = []
        self._load()

    def _parse(self, raw: dict) -> Region:
        children = tuple(self._parse(item) for item in raw.get("children", []))
        region = Region(str(raw.get("code", "")), str(raw.get("name", "")), str(raw.get("level", "")), children)
        if region.code:
            self.by_code[region.code] = region
        return region

    def _load(self) -> None:
        try:
            with self.path.open("r", encoding="utf-8") as handle:
                raw = json.load(handle)
            self.provinces = [self._parse(item) for item in raw]
            for province in self.provinces:
                for city in province.children:
                    for county in city.children:
                        self.paths.append(RegionPath(province, city, county))
            logging.info("行政区数据加载成功：province=%s city=%s county=%s", len(self.provinces), sum(len(p.children) for p in self.provinces), len(self.paths))
        except Exception:
            logging.exception("行政区数据加载失败：%s", self.path)
            self.provinces = []

    def find(self, code: str) -> Optional[Region]:
        return self.by_code.get(code or "")

    def cities(self, province_code: str) -> List[Region]:
        region = self.find(province_code)
        return list(region.children) if region else []

    def counties(self, city_code: str) -> List[Region]:
        region = self.find(city_code)
        return list(region.children) if region else []

    def search(self, keyword: str, limit: int = 50) -> List[RegionPath]:
        keyword = (keyword or "").strip().lower()
        if not keyword:
            return []
        return [path for path in self.paths if keyword in path.display.lower()][:limit]
