from __future__ import annotations

import shutil
from pathlib import Path


PROJECT_DIR = Path(__file__).resolve().parent
REPO_ROOT = PROJECT_DIR.parents[1]
SOURCE = REPO_ROOT / "src" / "YueXinMiaoPet" / "PetAssets" / "classified_gifs"
DEST = PROJECT_DIR / "assets" / "classified_gifs"


def main() -> int:
    if not SOURCE.exists():
        print(f"Windows 版 classified_gifs 不存在：{SOURCE}")
        return 1

    DEST.mkdir(parents=True, exist_ok=True)
    count = 0
    for file_path in SOURCE.rglob("*.gif"):
        relative = file_path.relative_to(SOURCE)
        target = DEST / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(file_path, target)
        count += 1

    print(f"已复制 {count} 个 GIF")
    print(f"来源：{SOURCE}")
    print(f"目标：{DEST}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
