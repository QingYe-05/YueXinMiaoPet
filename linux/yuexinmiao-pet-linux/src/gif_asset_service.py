from __future__ import annotations

import logging
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List


@dataclass(frozen=True)
class GifAsset:
    path: Path
    mood: str
    category: str

    @property
    def name(self) -> str:
        return self.path.name


class GifAssetService:
    def __init__(self, mood_category_map: Dict[str, str]) -> None:
        self.mood_category_map = mood_category_map
        self.assets_by_mood: Dict[str, List[GifAsset]] = {}
        self.all_assets: List[GifAsset] = []
        self.gif_directory = Path()

    def scan(self, gif_directory: str) -> None:
        root = Path(gif_directory).expanduser()
        self.gif_directory = root
        self.assets_by_mood = {mood: [] for mood in self.mood_category_map}
        self.all_assets = []

        if not root.exists():
            logging.warning("GIF 目录不存在：%s", root)
            return

        category_to_mood = {category: mood for mood, category in self.mood_category_map.items()}
        for file_path in self._iter_gifs(root):
            category = self._infer_category(root, file_path)
            mood = category_to_mood.get(category, "neutral")
            asset = GifAsset(path=file_path, mood=mood, category=category)
            self.assets_by_mood.setdefault(mood, []).append(asset)
            self.all_assets.append(asset)

        for items in self.assets_by_mood.values():
            items.sort(key=lambda item: str(item.path).lower())
        self.all_assets.sort(key=lambda item: str(item.path).lower())
        logging.info("Linux 版 GIF 扫描完成：root=%s total=%s", root, len(self.all_assets))

    def get_assets_for_mood(self, mood: str) -> List[GifAsset]:
        return list(self.assets_by_mood.get(mood, []))

    def _iter_gifs(self, root: Path) -> Iterable[Path]:
        try:
            yield from sorted(root.rglob("*.gif"), key=lambda path: str(path).lower())
        except Exception as exc:
            logging.exception("扫描 GIF 目录失败：%s", exc)

    def _infer_category(self, root: Path, file_path: Path) -> str:
        try:
            relative = file_path.relative_to(root)
            if len(relative.parts) >= 2:
                return relative.parts[0]
        except ValueError:
            pass
        return "01_普通"
