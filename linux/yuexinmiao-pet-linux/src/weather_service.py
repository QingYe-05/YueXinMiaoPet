from __future__ import annotations

import json
import logging
import math
import os
import threading
import urllib.parse
import urllib.request
from dataclasses import asdict, dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path
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
    source: str = "none"
    updated_at: str = ""


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
            try:
                info = self._load_amap(config, code)
            except Exception as amap_error:
                logging.warning("高德地图天气不可用，切换 Open-Meteo：%s", amap_error)
                info = self._load_open_meteo(code)
            config.weather_cache[code] = asdict(info)
            logging.info("天气更新：source=%s updated=%s code=%s text=%s temperature=%s wind=%s %s", info.source, info.updated_at, code, info.weather_text, info.temperature, info.wind_direction, info.wind_level)
            self.updated.emit(info, True)
        except Exception:
            logging.exception("天气更新失败：code=%s", code)
            cached = (config.weather_cache or {}).get(code)
            info = WeatherInfo(**cached) if isinstance(cached, dict) else WeatherInfo()
            self.updated.emit(info, False)
        finally:
            self._busy = False

    def _load_amap(self, config: AppConfig, code: str) -> WeatherInfo:
        api_key = self._resolve_amap_key(config)
        if not api_key:
            raise ValueError("未配置高德地图 Web 服务 API Key")
        query = urllib.parse.urlencode({"city": code, "extensions": "base", "output": "JSON", "key": api_key})
        payload: Dict[str, Any] = json.loads(self._get_text("https://restapi.amap.com/v3/weather/weatherInfo?" + query))
        if str(payload.get("status") or "") != "1" or str(payload.get("infocode") or "") != "10000":
            raise ValueError(f"高德天气接口返回失败：{payload.get('infocode') or '无响应'} / {payload.get('info') or ''}")
        lives = payload.get("lives") or []
        if not lives:
            raise ValueError("高德天气接口未返回实况数据")
        current = next((item for item in lives if str(item.get("adcode") or "") == code), lives[0])
        weather = self.normalize_amap_weather(str(current.get("weather") or ""))
        temperature = self._number(current.get("temperature"))
        if not weather or temperature is None:
            raise ValueError("高德天气实况缺少天气状况或温度")
        updated = self._parse_amap_time(str(current.get("reporttime") or ""))
        if updated and datetime.now(timezone.utc) - updated > timedelta(hours=3):
            raise ValueError(f"高德天气实况已超过 3 小时：{updated.isoformat()}")
        return WeatherInfo(
            weather_text=weather,
            temperature=temperature,
            wind_direction=self._normalize_amap_wind_direction(current.get("winddirection")),
            wind_level=self._normalize_amap_wind_power(current.get("windpower")),
            wind_speed=None,
            weather_code=self.map_amap_weather_code(weather),
            administrative_code=code,
            source="amap",
            updated_at=(updated or datetime.now(timezone.utc)).isoformat(),
        )

    def _load_open_meteo(self, code: str) -> WeatherInfo:
        lat, lon = self._resolve_location(code)
        query = urllib.parse.urlencode({"latitude": lat, "longitude": lon, "current_weather": "true", "timezone": "auto"})
        payload = json.loads(self._get_text("https://api.open-meteo.com/v1/forecast?" + query))
        current: Dict[str, Any] = payload.get("current_weather") or {}
        speed = self._number(current.get("windspeed"))
        weather_code = int(current.get("weathercode", -1))
        return WeatherInfo(
            weather_text=self.map_weather_text(weather_code),
            temperature=self._number(current.get("temperature")),
            wind_direction=self.map_wind_direction(self._number(current.get("winddirection"))),
            wind_level=self.map_wind_level(speed),
            wind_speed=speed,
            weather_code=weather_code,
            administrative_code=code,
            source="open-meteo",
            updated_at=datetime.now(timezone.utc).isoformat(),
        )

    def _get_text(self, url: str) -> str:
        request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 YueXinMiaoPet/2.1"})
        with urllib.request.urlopen(request, timeout=12) as response:
            return response.read().decode("utf-8")

    @staticmethod
    def _clean_text(value: Any) -> str:
        text = str(value or "").strip()
        return "" if text in ("暂无实况", "null", "undefined") else text

    def _resolve_amap_key(self, config: AppConfig) -> str:
        if config.amap_weather_api_key.strip():
            return config.amap_weather_api_key.strip()
        environment_key = os.environ.get("YUEXINMIAO_AMAP_KEY", "").strip()
        if environment_key:
            return environment_key
        key_path = Path(__file__).resolve().parents[1] / "config" / "amap.key"
        try:
            return key_path.read_text(encoding="utf-8").strip() if key_path.exists() else ""
        except OSError as exc:
            logging.warning("读取高德天气 Key 文件失败：%s", exc)
            return ""

    @staticmethod
    def _parse_amap_time(value: str) -> Optional[datetime]:
        try:
            china_tz = timezone(timedelta(hours=8))
            return datetime.strptime(value.strip(), "%Y-%m-%d %H:%M:%S").replace(tzinfo=china_tz).astimezone(timezone.utc)
        except (TypeError, ValueError):
            return None

    @staticmethod
    def normalize_amap_weather(value: str) -> str:
        text = (value or "").strip()
        if "雷阵雨" in text or "雷雨" in text: return "雷阵雨"
        if "暴雨" in text: return "暴雨"
        if "大雨" in text: return "大雨"
        if "中雨" in text: return "中雨"
        if "小雨" in text or "阵雨" in text or "毛毛雨" in text or "细雨" in text or text == "雨": return "小雨"
        # 其余高德天气现象（如晴间多云、浓雾、中度霾）保留原文显示。
        return text

    @staticmethod
    def map_amap_weather_code(text: str) -> int:
        if "晴" in text or "少云" in text: return 0
        if "多云" in text: return 2
        if "阴" in text: return 3
        if "雾" in text or "霾" in text: return 45
        if text == "雷阵雨": return 95
        if text == "暴雨": return 82
        if text == "大雨": return 65
        if text == "中雨": return 63
        if text == "小雨": return 61
        if "雪" in text: return 71
        return -1

    @classmethod
    def _normalize_amap_wind_direction(cls, value: Any) -> str:
        text = cls._clean_text(value)
        if not text or text in ("无风向", "旋转不定"):
            return text
        return text if text.endswith("风") else text + "风"

    @classmethod
    def _normalize_amap_wind_power(cls, value: Any) -> str:
        text = cls._clean_text(value)
        return text if not text or text.endswith("级") else text + "级"

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
