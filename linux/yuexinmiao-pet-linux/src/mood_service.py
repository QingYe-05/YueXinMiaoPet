from __future__ import annotations

from dataclasses import dataclass
from typing import List


@dataclass(frozen=True)
class MoodOption:
    key: str
    name: str


MOODS: List[MoodOption] = [
    MoodOption("neutral", "普通"),
    MoodOption("happy", "开心"),
    MoodOption("love", "喜欢"),
    MoodOption("shy", "害羞"),
    MoodOption("angry", "生气"),
    MoodOption("sad", "难过"),
    MoodOption("tired", "累了"),
    MoodOption("sleepy", "困了"),
    MoodOption("lazy", "想摸鱼"),
    MoodOption("hungry", "饿了"),
    MoodOption("excited", "兴奋"),
    MoodOption("thinking", "思考"),
    MoodOption("collapse", "崩溃"),
]


def normalize_mood(mood: str) -> str:
    keys = {item.key for item in MOODS}
    return mood if mood in keys else "neutral"


def mood_name(mood: str) -> str:
    normalized = normalize_mood(mood)
    for item in MOODS:
        if item.key == normalized:
            return item.name
    return "普通"
