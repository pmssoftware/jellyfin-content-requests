#!/usr/bin/env python3
"""Add release artifacts to the Jellyfin plugin repository manifest."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import pathlib
import re
import sys


ARTIFACT_PATTERN = re.compile(
    r"^content-requests_(?P<server>10\.11|12\.0)_(?P<version>\d+\.\d+\.\d+\.\d+)\.zip$"
)
TARGET_ABIS = {
    "10.11": "10.11.11.0",
    "12.0": "12.0.0.0",
}


def md5(path: pathlib.Path) -> str:
    digest = hashlib.md5(usedforsecurity=False)
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=pathlib.Path, default=pathlib.Path("manifest.json"))
    parser.add_argument("--repository", required=True, help="GitHub owner/repository")
    parser.add_argument("--tag", required=True)
    parser.add_argument("--artifacts", type=pathlib.Path, required=True)
    parser.add_argument("--changelog", default="See the linked GitHub release for changes.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    version = args.tag.removeprefix("v")
    if not re.fullmatch(r"\d+\.\d+\.\d+\.\d+", version):
        print("Release tags must look like v1.2.3.4", file=sys.stderr)
        return 2

    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    if not isinstance(manifest, list) or len(manifest) != 1:
        raise ValueError("manifest.json must contain exactly one plugin entry")

    plugin = manifest[0]
    plugin["owner"] = args.repository.split("/", 1)[0]
    existing = plugin.setdefault("versions", [])
    timestamp = dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    additions = []

    for artifact in sorted(args.artifacts.glob("content-requests_*.zip")):
        match = ARTIFACT_PATTERN.match(artifact.name)
        if not match or match.group("version") != version:
            continue

        server = match.group("server")
        target_abi = TARGET_ABIS[server]
        source_url = f"https://github.com/{args.repository}/releases/download/{args.tag}/{artifact.name}"
        additions.append(
            {
                "version": version,
                "changelog": args.changelog,
                "targetAbi": target_abi,
                "sourceUrl": source_url,
                "checksum": md5(artifact),
                "timestamp": timestamp,
            }
        )

    if len(additions) != len(TARGET_ABIS):
        found = ", ".join(item["targetAbi"] for item in additions) or "none"
        raise FileNotFoundError(f"Expected one artifact per server line; found: {found}")

    replaced = {(item["version"], item["targetAbi"]) for item in additions}
    plugin["versions"] = additions + [
        item for item in existing if (item.get("version"), item.get("targetAbi")) not in replaced
    ]
    args.manifest.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
