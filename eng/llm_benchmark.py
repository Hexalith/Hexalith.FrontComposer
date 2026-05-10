#!/usr/bin/env python3
"""Offline orchestration helpers for the FrontComposer v1 LLM benchmark."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import pathlib
import sys
from typing import Any


PROMPT_SET = pathlib.Path("docs/skills/frontcomposer/benchmark-prompts/v1/prompt-set.json")
PROMPT_COUNT = 20


def read_json(path: pathlib.Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        raise SystemExit(f"invalid JSON evidence {path}: {exc}") from exc


def write_json(path: pathlib.Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def sha256_text(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def validate_prompt_set(args: argparse.Namespace) -> int:
    root = pathlib.Path(args.root)
    data = read_json(root / PROMPT_SET)
    prompts = data.get("prompts", [])
    ids = [p.get("id", "") for p in prompts if isinstance(p, dict)]
    diagnostics: list[str] = []
    if len(prompts) != PROMPT_COUNT:
        diagnostics.append("prompt-set-must-contain-exactly-20-prompts")
    if ids != sorted(ids):
        diagnostics.append("prompt-ids-must-be-ordinally-ordered")
    if len(set(ids)) != len(ids):
        diagnostics.append("prompt-ids-must-be-unique")
    if any(not p.get("expectedShape") for p in prompts if isinstance(p, dict)):
        diagnostics.append("expected-shape-required")
    payload = {
        "status": "valid" if not diagnostics else "invalid",
        "corpus_version": data.get("version", ""),
        "prompt_count": len(prompts),
        "prompt_ids": ids,
        "corpus_hash": sha256_text(json.dumps(data, sort_keys=True, separators=(",", ":"))),
        "loader_contract": "SkillBenchmarkPromptSet.LoadEmbeddedV1",
        "diagnostics": diagnostics,
    }
    if args.output:
        write_json(pathlib.Path(args.output), payload)
    return 0 if not diagnostics else 1


def budget_status(args: argparse.Namespace) -> int:
    data = read_json(pathlib.Path(args.budget)) if args.budget else None
    now = dt.datetime.fromisoformat(args.now.replace("Z", "+00:00")) if args.now else dt.datetime.now(dt.timezone.utc)
    status = "budget-unknown"
    if isinstance(data, dict):
        try:
            cap = float(data["monthly_cap"])
            consumed = float(data["consumed"])
            expires = dt.datetime.fromisoformat(str(data["expires_at"]).replace("Z", "+00:00"))
            cost_ok = bool(data.get("provider_cost_metadata_available", False))
            retry_storm = bool(data.get("retry_storm_detected", False))
            if cap > 0 and consumed >= 0 and expires > now and cost_ok and not retry_storm:
                status = "available" if consumed < cap else "budget-exhausted"
        except Exception:
            status = "budget-unknown"
    payload = {"status": status, "api_spend_allowed": status == "available"}
    if args.output:
        write_json(pathlib.Path(args.output), payload)
    return 0 if status == "available" else 2


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("validate-prompt-set")
    p.add_argument("--root", default=".")
    p.add_argument("--output")
    p.set_defaults(func=validate_prompt_set)
    b = sub.add_parser("budget-status")
    b.add_argument("--budget")
    b.add_argument("--now")
    b.add_argument("--output")
    b.set_defaults(func=budget_status)
    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
