from __future__ import annotations

import json
import logging
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any, Dict


APP_NAME = "yuexinmiao-pet"
PROJECT_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_GIF_DIR = PROJECT_ROOT / "assets" / "classified_gifs"
DEFAULT_MAP_PATH = PROJECT_ROOT / "config" / "mood_category_map.json"
ADMINISTRATIVE_DATA_PATH = PROJECT_ROOT / "config" / "china_administrative_divisions.json"
CONFIG_DIR = Path.home() / ".config" / APP_NAME
CONFIG_PATH = CONFIG_DIR / "config.json"
DATA_DIR = Path.home() / ".local" / "share" / APP_NAME
LOG_DIR = DATA_DIR / "logs"
LOG_PATH = LOG_DIR / "app.log"


@dataclass
class AppConfig:
    x: int = 120
    y: int = 120
    scale_percent: int = 100
    opacity_percent: int = 100
    always_on_top: bool = True
    mood: str = "neutral"
    gif_directory: str = str(DEFAULT_GIF_DIR)
    interval_seconds: int = 10
    weather_enabled: bool = False
    province_code: str = ""
    province_name: str = ""
    city_code: str = ""
    city_name: str = ""
    county_code: str = ""
    county_name: str = ""
    weather_refresh_minutes: int = 30
    weather_cache: Dict[str, Any] | None = None
    amap_weather_api_key: str = ""

    def normalize(self) -> "AppConfig":
        self.scale_percent = max(50, min(200, int(self.scale_percent or 100)))
        self.opacity_percent = max(30, min(100, int(self.opacity_percent or 100)))
        self.interval_seconds = max(2, min(120, int(self.interval_seconds or 10)))
        self.weather_refresh_minutes = max(10, min(120, int(self.weather_refresh_minutes or 30)))
        if self.weather_cache is None:
            self.weather_cache = {}
        if not self.gif_directory:
            self.gif_directory = str(DEFAULT_GIF_DIR)
        if not self.mood:
            self.mood = "neutral"
        return self


def setup_logging() -> None:
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    logging.basicConfig(
        filename=str(LOG_PATH),
        level=logging.INFO,
        format="%(asctime)s [%(levelname)s] %(message)s",
    )


def load_config() -> AppConfig:
    CONFIG_DIR.mkdir(parents=True, exist_ok=True)
    if not CONFIG_PATH.exists():
        config = AppConfig().normalize()
        save_config(config)
        return config

    try:
        with CONFIG_PATH.open("r", encoding="utf-8") as f:
            raw: Dict[str, Any] = json.load(f)
        config = AppConfig(**{k: v for k, v in raw.items() if k in AppConfig.__annotations__})
        return config.normalize()
    except Exception as exc:
        logging.exception("读取配置失败，使用默认配置：%s", exc)
        return AppConfig().normalize()


def save_config(config: AppConfig) -> None:
    CONFIG_DIR.mkdir(parents=True, exist_ok=True)
    config.normalize()
    with CONFIG_PATH.open("w", encoding="utf-8") as f:
        json.dump(asdict(config), f, ensure_ascii=False, indent=2)


def load_mood_category_map() -> Dict[str, str]:
    default = {
        "neutral": "01_普通",
        "happy": "02_开心",
        "love": "03_喜欢",
        "shy": "04_害羞",
        "angry": "05_生气",
        "sad": "06_难过",
        "tired": "07_累了",
        "sleepy": "08_困了",
        "lazy": "09_想摸鱼",
        "hungry": "10_饿了",
        "excited": "11_兴奋",
        "thinking": "12_思考",
        "collapse": "13_崩溃",
    }
    try:
        with DEFAULT_MAP_PATH.open("r", encoding="utf-8") as f:
            loaded = json.load(f)
        default.update({str(k): str(v) for k, v in loaded.items()})
    except Exception as exc:
        logging.exception("读取心情映射失败，使用默认映射：%s", exc)
    return default
