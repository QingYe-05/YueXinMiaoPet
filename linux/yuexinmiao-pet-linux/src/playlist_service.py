from __future__ import annotations

from typing import Dict, List, Optional

from gif_asset_service import GifAsset, GifAssetService
from mood_service import normalize_mood


class PlaylistService:
    def __init__(self, asset_service: GifAssetService) -> None:
        self.asset_service = asset_service
        self.index_by_mood: Dict[str, int] = {}

    def reset(self, mood: Optional[str] = None) -> None:
        if mood:
            self.index_by_mood[normalize_mood(mood)] = 0
        else:
            self.index_by_mood.clear()

    def current_playlist(self, mood: str) -> List[GifAsset]:
        normalized = normalize_mood(mood)
        playlist = self.asset_service.get_assets_for_mood(normalized)
        if playlist:
            return playlist
        neutral = self.asset_service.get_assets_for_mood("neutral")
        if neutral:
            return neutral
        return list(self.asset_service.all_assets)

    def next_gif(self, mood: str) -> Optional[GifAsset]:
        normalized = normalize_mood(mood)
        playlist = self.current_playlist(normalized)
        if not playlist:
            return None
        index = self.index_by_mood.get(normalized, 0)
        if index >= len(playlist):
            index = 0
        asset = playlist[index]
        self.index_by_mood[normalized] = (index + 1) % len(playlist)
        return asset
