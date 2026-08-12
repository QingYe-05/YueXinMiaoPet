from __future__ import annotations

import json
import logging
import math
import threading
import urllib.parse
import urllib.request
from dataclasses import asdict, dataclass
from typing import Any, Dict, Optional

from PyQt6.QtCore import QObject, pyqtSignal

from app_config import AppConfig


@dataclass
class WeatherInfo:
    weather_text: str = "未知"
    temperature: Optional[float] = None
    wind_direction: str = ""
    wind_level: str = ""
    wind_speed: Optional[float] = None
    weather_code: int = -1
    administrative_code: str = ""


class WeatherService(QObject):
    updated = pyqtSignal(object, bool)

    def __init__(self) -> None:
        super().__init__()
        self._busy = False

    def refresh(self, config: AppConfig) -> None:
        if self._busy or not config.weather_enabled:
            return
        code = config.county_code or config.city_code or config.province_code
        if not code:
            self.updated.emit(WeatherInfo(), False)
            return
        self._busy = True
        threading.Thread(target=self._load, args=(config, code), daemon=True).start()

    def _load(self, config: AppConfig, code: str) -> None:
        try:
            lat, lon = self._resolve_location(code)
            query = urllib.parse.urlencode({"latitude": lat, "longitude": lon, "current_weather": "true", "timezone": "auto"})
            with urllib.request.urlopen("https://api.open-meteo.com/v1/forecast?" + query, timeout=12) as response:
                payload = json.loads(response.read().decode("utf-8"))
            current: Dict[str, Any] = payload.get("current_weather") or {}
            speed = self._number(current.get("windspeed"))
            direction = self._number(current.get("winddirection"))
            temperature = self._number(current.get("temperature"))
            weather_code = int(current.get("weathercode", -1))
            info = WeatherInfo(
                weather_text=self.map_weather_text(weather_code),
                temperature=temperature,
                wind_direction=self.map_wind_direction(direction),
                wind_level=self.map_wind_level(speed),
                wind_speed=speed,
                weather_code=weather_code,
                administrative_code=code,
            )
            config.weather_cache[code] = asdict(info)
            logging.info("天气更新：code=%s text=%s temperature=%s wind=%s %s", code, info.weather_text, info.temperature, info.wind_direction, info.wind_level)
            self.updated.emit(info, True)
        except Exception:
            logging.exception("天气更新失败：code=%s", code)
            cached = (config.weather_cache or {}).get(code)
            info = WeatherInfo(**cached) if isinstance(cached, dict) else WeatherInfo()
            self.updated.emit(info, False)
        finally:
            self._busy = False

    def _resolve_location(self, code: str) -> tuple[float, float]:
        url = "https://uapis.cn/api/v1/misc/district?" + urllib.parse.urlencode({"adcode": code, "limit": 20})
        with urllib.request.urlopen(url, timeout=12) as response:
            payload = json.loads(response.read().decode("utf-8"))
        for item in payload.get("results") or []:
            if str(item.get("adcode", "")) == code and item.get("center"):
                center = item["center"]
                return float(center["lat"]), float(center["lng"])
        raise ValueError("未找到行政区天气坐标")

    @staticmethod
    def _number(value: Any) -> Optional[float]:
        try:
            number = float(value)
            return number if math.isfinite(number) else None
        except (TypeError, ValueError):
            return None

    @staticmethod
    def map_weather_text(code: int) -> str:
        if code in (0, 1): return "晴"
        if code == 2: return "多云"
        if code == 3: return "阴"
        if code in (45, 48): return "雾"
        if 95 <= code <= 99: return "雷阵雨"
        if code == 82: return "暴雨"
        if code in (55, 57, 65, 67): return "大雨"
        if code in (53, 63, 81): return "中雨"
        if code in (51, 56, 61, 66, 80): return "小雨"
        if 71 <= code <= 77 or code in (85, 86): return "雪"
        return "未知"

    @staticmethod
    def map_wind_direction(degrees: Optional[float]) -> str:
        if degrees is None: return ""
        directions = ("北风", "东北风", "东风", "东南风", "南风", "西南风", "西风", "西北风")
        return directions[int(((degrees % 360) + 22.5) // 45) % 8]

    @staticmethod
    def map_wind_level(speed: Optional[float]) -> str:
        if speed is None or speed < 0: return ""
        bounds = (1, 6, 12, 20, 29, 39, 50, 62, 75, 89, 103, 118)
        for level, bound in enumerate(bounds):
            if speed < bound: return f"{level}级"
        return "12级"
