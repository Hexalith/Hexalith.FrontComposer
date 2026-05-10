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


def load_prompt_contract(root: pathlib.Path) -> tuple[dict[str, Any], list[dict[str, Any]], list[str]]:
    data = read_json(root / PROMPT_SET)
    prompts = data.get("prompts", [])
    if not isinstance(prompts, list):
        raise SystemExit("prompt set must contain a prompts array")
    typed_prompts = [p for p in prompts if isinstance(p, dict)]
    if len(typed_prompts) != len(prompts):
        raise SystemExit("prompt set contains non-object prompt entries")
    ids = [str(p.get("id", "")) for p in typed_prompts]
    return data, typed_prompts, ids


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


def run_benchmark(args: argparse.Namespace) -> int:
    root = pathlib.Path(args.root)
    data, prompts, ids = load_prompt_contract(root)
    budget = read_json(pathlib.Path(args.budget_artifact)) if args.budget_artifact else {}
    budget_status_value = str(budget.get("status", "budget-unknown"))
    api_spend_allowed = bool(budget.get("api_spend_allowed", False))
    provider_payload = read_json(pathlib.Path(args.provider_results)) if args.provider_results else None
    provider_result_items = provider_payload.get("results", []) if isinstance(provider_payload, dict) else []
    provider_results = {
        str(item.get("prompt_id", "")): item
        for item in provider_result_items
        if isinstance(item, dict)
    }

    diagnostics: list[str] = []
    results: list[dict[str, Any]] = []
    if len(prompts) != PROMPT_COUNT or ids != sorted(ids) or len(set(ids)) != len(ids):
        diagnostics.append("invalid-prompt-contract")

    for prompt in prompts:
        prompt_id = str(prompt.get("id", ""))
        provider_result = provider_results.get(prompt_id)
        if not api_spend_allowed:
            status = budget_status_value if budget_status_value in {"budget-exhausted", "budget-unknown"} else "budget-blocked"
            passed = False
            category = "budget-blocked"
        elif provider_result is None:
            status = "provider-result-missing"
            passed = False
            category = "invalid-evidence"
        else:
            status = str(provider_result.get("status", "invalid-evidence"))
            passed = bool(provider_result.get("compile_succeeded")) and bool(provider_result.get("validator_succeeded")) and status == "valid"
            category = str(provider_result.get("failure_category", "none" if passed else "invalid-evidence"))
        results.append({
            "prompt_id": prompt_id,
            "status": status,
            "passed": passed,
            "failure_category": category,
            "expected_shape_count": len(prompt.get("expectedShape", [])),
        })

    passed_count = sum(1 for item in results if item["passed"])
    invalid_count = sum(1 for item in results if item["status"] not in {"valid", "legitimate-miss"})
    pass_rate = passed_count / len(results) if results else 0
    if len(results) != PROMPT_COUNT:
        diagnostics.append("run-must-cover-exactly-20-prompts")
    if invalid_count:
        diagnostics.append("invalid-evidence-present")
    if pass_rate < 0.80 and invalid_count == 0:
        diagnostics.append("one-shot-pass-rate-below-80-percent")

    payload = {
        "status": "valid" if not diagnostics else "invalid",
        "classification": "pass" if not diagnostics else ("budget-blocked" if not api_spend_allowed else "invalid-evidence"),
        "corpus_version": data.get("version", ""),
        "corpus_hash": sha256_text(json.dumps(data, sort_keys=True, separators=(",", ":"))),
        "prompt_count": len(results),
        "prompt_ids": ids,
        "passed_count": passed_count,
        "invalid_evidence_count": invalid_count,
        "pass_rate": pass_rate,
        "budget_status": budget_status_value,
        "provider_results_supplied": provider_payload is not None,
        "baseline_write_decision": "candidate-evidence-only",
        "results": results,
        "diagnostics": diagnostics,
    }
    write_json(pathlib.Path(args.output), payload)
    return 0 if not diagnostics else 2


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
    r = sub.add_parser("run-benchmark")
    r.add_argument("--root", default=".")
    r.add_argument("--budget-artifact", required=True)
    r.add_argument("--provider-results")
    r.add_argument("--output", required=True)
    r.set_defaults(func=run_benchmark)
    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
