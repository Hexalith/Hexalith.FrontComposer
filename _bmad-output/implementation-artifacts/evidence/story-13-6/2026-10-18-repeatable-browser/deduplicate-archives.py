"""Compact identical members with ordinary tar hard links; preserve every path/byte.

Logical results/HTML sizes remain unchanged. Replay hashes every restored file.
This construction recipe uses the retained raw roots and never changes them.
"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

packet = Path(__file__).resolve().parent
mapping_path = packet / "attachment-path-map.json"
mapping = json.loads(mapping_path.read_text())
records = []
digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
for lane in mapping["lanes"]:
    archive = packet / lane["archive"]
    if digest(archive) != lane["archiveSha256"]:
        raise ValueError(f"Archive mismatch: {archive}")
    old_hash = lane["archiveSha256"]
    with tempfile.TemporaryDirectory(prefix="story-13-6-tar-dedup-") as temporary:
        staging = Path(temporary) / "tree"
        staging.mkdir()
        seen = {}
        for path, expected in lane["files"].items():
            target = staging / path
            target.parent.mkdir(parents=True, exist_ok=True)
            if expected["sha256"] in seen:
                os.link(seen[expected["sha256"]], target)
            else:
                source = Path(lane["path"]) / path
                if digest(source) != expected["sha256"]:
                    raise ValueError(f"Raw file mismatch: {source}")
                shutil.copy2(source, target)
                seen[expected["sha256"]] = target
        compact = Path(temporary) / "compact.tar.gz"
        subprocess.run(["tar", "-czf", str(compact), "-C", str(staging), "."], check=True)
        lane["archiveSha256"] = digest(compact)
        records.append({"lane": lane["key"], "oldArchiveSha256": old_hash,
                        "archiveSha256": lane["archiveSha256"],
                        "logicalFiles": len(lane["files"]), "uniqueContents": len(seen),
                        "logicalBytes": sum(item["bytes"] for item in lane["files"].values()),
                        "archiveBytes": compact.stat().st_size})
        shutil.copyfile(compact, archive)
    mapping_path.write_text(json.dumps(mapping, indent=2) + "\n")
    print(json.dumps(records[-1]), flush=True)
(packet / "archive-deduplication.json").write_text(json.dumps(records, indent=2) + "\n")
