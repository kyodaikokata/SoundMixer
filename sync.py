#!/usr/bin/env python3
"""Sync plugin source from WorkInProgress into this git repo.

Prefer the release pipeline:
  WorkInProgress/<Plugin>/scripts/publish-release.ps1
which calls KKT-Catalog/scripts/sync-source-repo.ps1 after catalog publish.

Manual usage (from repo root):
  python sync.py
  python sync.py --src E:/work/DalamudProject/WorkInProgress/SoundMixer
"""
from __future__ import annotations

import argparse
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

DEFAULT_SRC = Path(r"E:\work\DalamudProject\WorkInProgress\SoundMixer")
SKIP_NAMES = {"SoundSetter.csproj", "SoundSetter.json", ".sync-complete"}
SKIP_DIRS = {"bin", "obj", "dist", ".vs", ".idea"}


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
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--src", type=Path, default=DEFAULT_SRC)
    parser.add_argument("--dst", type=Path, default=Path(__file__).resolve().parent)
    args = parser.parse_args()

    src: Path = args.src
    dst: Path = args.dst

    copied = 0
    copied += copy_tree(src / "SoundMixer", dst / "SoundMixer")
    copied += copy_tree(src / "images", dst / "images")
    if (src / "scripts").exists():
        copied += copy_tree(src / "scripts", dst / "scripts")

    for name in (
        "SoundMixer.sln",
        "README.md",
        "KNOWN_ISSUES.md",
        "DESIGN.md",
        "LICENSE",
        ".gitignore",
        ".gitattributes",
        "pluginmaster.cn.json",
        "pluginmaster.global.json",
    ):
        src_file = src / name
        if src_file.exists():
            shutil.copy2(src_file, dst / name)
            copied += 1

    marker = {
        "syncedAt": datetime.now(timezone.utc).isoformat(),
        "source": str(src),
        "filesCopied": copied,
        "projectCsprojExists": (dst / "SoundMixer" / "SoundMixer.csproj").exists(),
    }
    (dst / ".sync-complete").write_text(json.dumps(marker, indent=2), encoding="utf-8")

    required = [
        dst / "SoundMixer" / "SoundMixer.csproj",
        dst / "SoundMixer" / "OfficialBlacklistSync.cs",
        dst / "images" / "SoundMixer" / "icon.png",
    ]
    print(f"Sync complete: {copied} files")
    for path in required:
        print(f"  {'OK' if path.exists() else 'MISSING'}: {path}")


if __name__ == "__main__":
    main()
