"""Retain every focused browser attempt without changing historical packet bytes."""
import hashlib
import json
from pathlib import Path
import subprocess

packet = Path(__file__).resolve().parent
captures = []
for label in ["red", "green", "red-final", "green-final"]:
    root = Path("/tmp/story-13-6-repeatable-review-native-" + label)
    archive = packet / ("native-" + label + ".tar.gz")
    if archive.exists():
        raise ValueError(f"Refusing replacement: {archive}")
    files = {path.relative_to(root).as_posix(): {
        "bytes": path.stat().st_size,
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    } for path in sorted(root.rglob("*")) if path.is_file()}
    report = json.loads((root / "results/playwright.json").read_bytes())
    paths = {}
    def visit(suite):
        for spec in suite.get("specs", []):
            for test in spec.get("tests", []):
                for attempt in test.get("results", []):
                    for attachment in attempt.get("attachments", []):
                        if "path" in attachment:
                            path = Path(attachment["path"])
                            relative = path.relative_to(root).as_posix()
                            paths[str(path)] = {"path": relative, **files[relative]}
        for child in suite.get("suites", []):
            visit(child)
    visit(report)
    subprocess.run(["tar", "-czf", str(archive), "-C", str(root), "."], check=True)
    captures.append({"label": label, "originalRoot": str(root), "archive": archive.name,
        "archiveSha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
        "files": files, "attachmentPaths": paths, "stats": report["stats"]})
(packet / "focused-browser-captures.json").write_text(json.dumps({"version": 1, "captures": captures}, indent=2) + "\n")
print(json.dumps([{key: value for key, value in capture.items() if key not in ["files", "attachmentPaths"]} for capture in captures], indent=2))
