#!/usr/bin/env python3
"""Sync plugin source from WorkInProgress into this git repo."""
from __future__ import annotations

import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

SRC = Path(r"E:\work\DalamudProject\WorkInProgress\SoundMixer")
DST = Path(__file__).resolve().parent

SKIP_NAMES = {"SoundSetter.csproj", "SoundSetter.json"}
SKIP_DIRS = {"bin", "obj"}


def copy_tree(src: Path, dst: Path) -> int:
    count = 0
    if not src.exists():
        raise FileNotFoundError(f"Source not found: {src}")
    dst.mkdir(parents=True, exist_ok=True)
    for item in src.rglob("*"):
        rel = item.relative_to(src)
        if any(part in SKIP_DIRS for part in rel.parts):
            continue
        if item.is_file() and item.name in SKIP_NAMES:
            continue
        target = dst / rel
        if item.is_dir():
            target.mkdir(parents=True, exist_ok=True)
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(item, target)
        count += 1
    return count


def main() -> None:
    copied = 0
    copied += copy_tree(SRC / "SoundMixer", DST / "SoundMixer")
    copied += copy_tree(SRC / "images", DST / "images")

    for name in ("SoundMixer.sln", "pluginmaster.cn.json", "pluginmaster.global.json"):
        src_file = SRC / name
        if src_file.exists():
            shutil.copy2(src_file, DST / name)
            copied += 1

    marker = {
        "syncedAt": datetime.now(timezone.utc).isoformat(),
        "source": str(SRC),
        "filesCopied": copied,
        "projectCsprojExists": (DST / "SoundMixer" / "SoundMixer.csproj").exists(),
    }
    (DST / ".sync-complete").write_text(
        json.dumps(marker, indent=2), encoding="utf-8"
    )

    required = [
        DST / "SoundMixer" / "Filter.cs",
        DST / "SoundMixer" / "PluginUI.cs",
        DST / "SoundMixer" / "Configuration.cs",
        DST / "images" / "SoundMixer" / "icon.png",
    ]
    print(f"Sync complete: {copied} files")
    for path in required:
        print(f"  {'OK' if path.exists() else 'MISSING'}: {path}")


if __name__ == "__main__":
    main()
