#!/usr/bin/env python3
"""CI governance helpers for flaky-test quarantine, reintroduction, and duration budgets."""

from __future__ import annotations

import argparse
import datetime as dt
import glob
import hashlib
import html
import json
import os
import pathlib
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from typing import Any


QUARANTINE_MARKER = "<!-- frontcomposer:flaky-test-quarantine -->"
REINTRO_MARKER = "<!-- frontcomposer:quarantine-reintroduction -->"
CI_DIET_MARKER = "<!-- frontcomposer:ci-diet -->"
REQUIRED_LABELS = ["flaky-test", "ci-governance", "codex-automation"]
QUARANTINE_FILTER = "Category=Quarantined"
MAIN_FILTER = "Category!=Quarantined"
MAX_FIELD = 600
MAX_SUMMARY_BYTES = 64 * 1024


@dataclass(frozen=True)
class TestResult:
    identity: str
    display_name: str
    outcome: str
    attempt: int
    sha: str
    run_url: str
    source_path: str = ""
    category: str = ""
    seed: str = ""
    failure: str = ""


def sanitize(value: Any, max_len: int = MAX_FIELD) -> str:
    text = "" if value is None else str(value)
    text = text.replace("\r", " ").replace("\n", " ")
    text = re.sub(r"(?i)bearer\s+[A-Za-z0-9._~+/=-]+", "Bearer [REDACTED]", text)
    text = re.sub(r"\b(?:ghp|github_pat|glpat|xox[baprs])-[-A-Za-z0-9_]{12,}\b", "[REDACTED_TOKEN]", text)
    text = re.sub(r"\b[A-Z]:\\[^ ]+", "[LOCAL_PATH]", text)
    text = re.sub(r"(?<![\w/])/(?:home|Users|var|tmp)/[^ ]+", "[LOCAL_PATH]", text)
    text = re.sub(r"(?i)\b(tenant|tenantid|user|userid|commandpayload)\s*[:=]\s*[^,; ]+", r"\1=[REDACTED]", text)
    text = html.escape(text, quote=False)
    text = text.replace("|", "\\|")
    if text.startswith("::"):
        text = "\\" + text
    return text[:max_len] + ("..." if len(text) > max_len else "")


def read_json(path: str | pathlib.Path) -> Any:
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as exc:
        raise SystemExit(f"invalid evidence: cannot read JSON {path}: {exc}") from exc


def write_text(path: str | pathlib.Path, content: str) -> None:
    p = pathlib.Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    encoded = content.encode("utf-8")
    if len(encoded) > MAX_SUMMARY_BYTES:
        encoded = encoded[:MAX_SUMMARY_BYTES] + b"\n\n[truncated]\n"
    p.write_bytes(encoded)


def write_json(path: str | pathlib.Path, payload: Any) -> None:
    p = pathlib.Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def latest_files(patterns: list[str]) -> list[str]:
    files: list[str] = []
    for pattern in patterns:
        files.extend(glob.glob(pattern, recursive=True))
    return sorted(set(files), key=lambda p: os.path.getmtime(p))


def parse_trx(path: str, sha: str = "", run_url: str = "") -> list[TestResult]:
    try:
        root = ET.parse(path).getroot()
    except Exception as exc:
        raise ValueError(f"malformed TRX {path}: {exc}") from exc

    tests_by_id: dict[str, dict[str, str]] = {}
    for unit in root.findall(".//{*}UnitTest"):
        test_id = unit.attrib.get("id", "")
        method = unit.find(".//{*}TestMethod")
        tests_by_id[test_id] = {
            "class": method.attrib.get("className", "") if method is not None else "",
            "method": method.attrib.get("name", "") if method is not None else "",
        }

    parsed: list[TestResult] = []
    for index, result in enumerate(root.findall(".//{*}UnitTestResult"), start=1):
        test_id = result.attrib.get("testId", "")
        method = tests_by_id.get(test_id, {})
        display_name = result.attrib.get("testName", method.get("method", "unknown"))
        class_name = method.get("class", "")
        method_name = method.get("method", "")
        identity = ".".join(part for part in [class_name, method_name] if part) or display_name
        outcome = result.attrib.get("outcome", "Unknown")
        failure = ""
        message = result.find(".//{*}Message")
        stack = result.find(".//{*}StackTrace")
        if message is not None and message.text:
            failure = message.text
        if stack is not None and stack.text:
            failure = f"{failure} {stack.text}".strip()
        parsed.append(
            TestResult(
                identity=sanitize(identity),
                display_name=sanitize(display_name),
                outcome=sanitize(outcome),
                attempt=index,
                sha=sanitize(sha),
                run_url=sanitize(run_url),
                failure=sanitize(failure),
            )
        )
    return parsed


def summarize_quarantine(args: argparse.Namespace) -> int:
    trx_files = latest_files([os.path.join(args.results_dir, "**", "*quarantine*.trx")])
    diagnostics: list[str] = []
    results: list[TestResult] = []
    for trx in trx_files:
        try:
            results.extend(parse_trx(trx, args.sha, args.run_url))
        except ValueError as exc:
            diagnostics.append(sanitize(exc))

    total = len(results)
    failed = sum(1 for r in results if r.outcome.lower() == "failed")
    passed = sum(1 for r in results if r.outcome.lower() == "passed")
    invalid = len(diagnostics) > 0
    classification = "zero-quarantined"
    if invalid:
        classification = "invalid evidence"
    elif failed:
        classification = "advisory quarantine failure"
    elif total:
        classification = "quarantine pass"

    payload = {
        "classification": classification,
        "total": total,
        "passed": passed,
        "failed": failed,
        "sha": sanitize(args.sha),
        "run_url": sanitize(args.run_url),
        "diagnostics": diagnostics,
        "results": [r.__dict__ for r in results[:50]],
    }

    md = [
        "## Quarantined Test Results",
        "",
        f"Classification: **{classification}**",
        f"Total: {total}",
        f"Passed: {passed}",
        f"Failed: {failed}",
    ]
    if not trx_files:
        md.append("No quarantine TRX files were found; this is reported as zero quarantined tests.")
    for diagnostic in diagnostics:
        md.append(f"- Invalid evidence: {diagnostic}")
    for result in results[:25]:
        md.append(f"- {result.outcome}: `{result.identity}` attempt {result.attempt}")

    write_text(args.markdown, "\n".join(md) + "\n")
    write_json(args.json, payload)
    if args.github_step_summary:
        with open(args.github_step_summary, "a", encoding="utf-8") as f:
            f.write("\n" + "\n".join(md) + "\n")
    return 0


def require_trusted_context(args: argparse.Namespace) -> None:
    allowed_event = args.event_name in {"schedule", "workflow_dispatch", "workflow_run", "push"}
    protected = str(args.ref_protected).lower() == "true"
    trusted_ref = args.ref in {"refs/heads/main", "main"} or protected
    fork = str(args.from_fork).lower() == "true"
    if not allowed_event or not trusted_ref or fork:
        raise SystemExit(
            "governance write action rejected: trusted protected-branch, schedule, or manual context required"
        )


def normalize_evidence(raw: Any) -> list[TestResult]:
    items = raw.get("results", raw) if isinstance(raw, dict) else raw
    if not isinstance(items, list):
        raise SystemExit("invalid evidence: expected a list of test results")
    results: list[TestResult] = []
    for i, item in enumerate(items, start=1):
        if not isinstance(item, dict):
            raise SystemExit("invalid evidence: every result must be an object")
        identity = sanitize(item.get("identity") or item.get("fullyQualifiedName") or item.get("test"))
        outcome = sanitize(item.get("outcome", "")).lower()
        sha = sanitize(item.get("sha", ""))
        if not identity or outcome not in {"passed", "failed"} or not sha:
            raise SystemExit(f"invalid evidence: result {i} requires identity, passed/failed outcome, and sha")
        results.append(
            TestResult(
                identity=identity,
                display_name=sanitize(item.get("displayName", identity)),
                outcome=outcome,
                attempt=int(item.get("attempt", i)),
                sha=sha,
                run_url=sanitize(item.get("runUrl", "")),
                source_path=sanitize(item.get("sourcePath", "")),
                category=sanitize(item.get("category", "")),
                seed=sanitize(item.get("seed", "")),
                failure=sanitize(item.get("failure", "")),
            )
        )
    return results


def same_revision_or_window(results: list[TestResult], window: dict[str, Any] | None) -> bool:
    shas = {r.sha for r in results}
    if len(shas) == 1:
        return True
    if not window:
        return False
    required = ["from_sha", "to_sha", "source", "owner"]
    if not all(sanitize(window.get(k, "")) for k in required):
        return False
    approved_shas = {
        sanitize(window.get("from_sha", "")),
        sanitize(window.get("to_sha", "")),
    }
    if isinstance(window.get("shas"), list):
        approved_shas.update(sanitize(sha) for sha in window["shas"])
    approved_shas.discard("")
    return shas.issubset(approved_shas)


def classify_flake(args: argparse.Namespace) -> int:
    raw = read_json(args.evidence)
    results = normalize_evidence(raw)
    identities = {r.identity for r in results}
    if len(identities) != 1:
        raise SystemExit("invalid evidence: flake classification requires one stable test identity")
    outcomes = {r.outcome for r in results}
    window = raw.get("approved_evidence_window") if isinstance(raw, dict) else None
    is_flaky = outcomes == {"passed", "failed"} and same_revision_or_window(results, window)
    identity = next(iter(identities))
    stable_id = hashlib.sha256(identity.encode("utf-8")).hexdigest()[:12]
    diagnostics: list[str] = []
    if outcomes != {"passed", "failed"}:
        diagnostics.append("mixed pass/fail evidence was not present")
    if not same_revision_or_window(results, window):
        diagnostics.append("mixed evidence used unrelated SHAs without an approved evidence window")

    issue_title = f"Flaky test quarantine proposal: {identity}"
    marker = f"{QUARANTINE_MARKER} id={stable_id}"
    recurrence_count = int(raw.get("recurrence_count", 0)) if isinstance(raw, dict) else 0
    failure_window = sanitize(raw.get("failure_window", "same protected-branch revision") if isinstance(raw, dict) else "same protected-branch revision")
    repeat_note = "Yes" if recurrence_count > 0 else "No"
    issue_body = "\n".join(
        [
            marker,
            "",
            "## Root-cause hypothesis",
            sanitize(args.hypothesis or "owner-needed: root cause not yet confirmed"),
            "",
            "## Owner",
            sanitize(args.owner or "owner-needed"),
            "",
            "## Failure window",
            failure_window,
            "",
            "## Repeat flake",
            f"{repeat_note}; recurrence count: {recurrence_count}",
            "",
            "## Evidence",
            *[
                f"- {r.outcome} attempt {r.attempt} sha `{r.sha}` {r.run_url}".rstrip()
                for r in results[:25]
            ],
            "",
            "## Recommended quarantine metadata",
            f"// frontcomposer-quarantine: issue=<link this issue> owner={sanitize(args.owner or 'owner-needed')} reason={sanitize(args.hypothesis or 'owner-needed')} reintroduction=5-nightly-passes",
            '[Trait("Category", "Quarantined")]',
        ]
    )
    payload = {
        "classification": "flaky" if is_flaky else "not-flaky",
        "decision": "open-or-update-issue-and-pr" if is_flaky else "no-write",
        "identity": identity,
        "stable_id": stable_id,
        "issue_title": issue_title,
        "issue_body": issue_body,
        "required_labels": REQUIRED_LABELS,
        "missing_labels_behavior": "record missing labels in issue/PR body",
        "diagnostics": diagnostics,
    }
    if is_flaky and args.source_root:
        payload.update(build_quarantine_patch(pathlib.Path(args.source_root), identity, args.owner, args.hypothesis))
    if args.output:
        write_json(args.output, payload)
    if args.apply:
        require_trusted_context(args)
        apply_github_proposal(payload, args.branch_prefix)
    return 0


def build_quarantine_patch(root: pathlib.Path, identity: str, owner: str, hypothesis: str) -> dict[str, Any]:
    method = identity.split(".")[-1]
    candidates: list[pathlib.Path] = []
    for path in root.glob("tests/**/*.cs"):
        if "\\obj\\" in str(path) or "\\bin\\" in str(path) or path.name.endswith(".g.cs"):
            continue
        text = path.read_text(encoding="utf-8")
        if re.search(rf"\b(?:async\s+)?(?:Task|ValueTask|void)\s+{re.escape(method)}\s*\(", text):
            candidates.append(path)
    if len(candidates) != 1:
        return {
            "source_mapping": "ambiguous",
            "manual_patch_required": True,
            "manual_patch_reason": f"expected exactly one source candidate for {identity}, found {len(candidates)}",
        }
    path = candidates[0]
    rel = path.relative_to(root).as_posix()
    text = path.read_text(encoding="utf-8")
    if '[Trait("Category", "Quarantined")]' in text:
        return {"source_mapping": "already-quarantined", "manual_patch_required": False, "source_path": rel}
    metadata = (
        f'    // frontcomposer-quarantine: issue=<link this issue> owner={sanitize(owner or "owner-needed")} '
        f'reason={sanitize(hypothesis or "owner-needed")} reintroduction=5-nightly-passes\n'
        '    [Trait("Category", "Quarantined")]\n'
    )
    pattern = re.compile(rf"(^\s*\[(?:Fact|Theory)[^\n]*\]\s*\n)(\s*public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+{re.escape(method)}\s*\()", re.MULTILINE)
    if not pattern.search(text):
        return {
            "source_mapping": "ambiguous",
            "manual_patch_required": True,
            "manual_patch_reason": f"could not find a simple Fact/Theory method declaration for {identity}",
            "source_path": rel,
        }
    patched = pattern.sub(rf"\1{metadata}\2", text, count=1)
    return {
        "source_mapping": "unambiguous",
        "manual_patch_required": False,
        "source_path": rel,
        "patch": patched,
    }


def gh(args: list[str], input_text: str | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(["gh", *args], input=input_text, text=True, check=False, capture_output=True)


def apply_github_proposal(payload: dict[str, Any], branch_prefix: str) -> None:
    if payload.get("classification") != "flaky":
        raise SystemExit("refusing to apply: candidate is not classified as flaky")
    labels = gh(["label", "list", "--json", "name"])
    available = {item["name"] for item in json.loads(labels.stdout or "[]")} if labels.returncode == 0 else set()
    labels_to_apply = [label for label in REQUIRED_LABELS if label in available]
    missing = [label for label in REQUIRED_LABELS if label not in available]
    body = payload["issue_body"] + ("\n\nMissing labels: " + ", ".join(missing) if missing else "")
    issue = gh(["issue", "list", "--state", "open", "--search", payload["stable_id"], "--json", "number"])
    issue_number = None
    if issue.returncode == 0:
        matches = json.loads(issue.stdout or "[]")
        issue_number = str(matches[0]["number"]) if matches else None
    if issue_number:
        gh(["issue", "edit", issue_number, "--body", body])
    else:
        cmd = ["issue", "create", "--title", payload["issue_title"], "--body", body]
        for label in labels_to_apply:
            cmd += ["--label", label]
        created = gh(cmd)
        if created.returncode != 0:
            raise SystemExit("governance write failure: could not create quarantine issue")
        issue_number = created.stdout.strip().split("/")[-1]
    if payload.get("manual_patch_required"):
        return
    branch = f"{branch_prefix}{payload['stable_id']}"
    subprocess.run(["git", "checkout", "-B", branch], check=True)
    path = pathlib.Path(payload["source_path"])
    path.write_text(payload["patch"], encoding="utf-8")
    subprocess.run(["git", "add", str(path)], check=True)
    subprocess.run(["git", "commit", "-m", f"test: quarantine flaky {payload['stable_id']}"], check=True)
    subprocess.run(["git", "push", "-u", "origin", branch], check=True)
    pr_body = f"{QUARANTINE_MARKER} id={payload['stable_id']}\n\nLinks issue #{issue_number}.\n\nThis PR adds quarantine metadata and keeps reintroduction at 5 valid nightly passes."
    pr = gh(["pr", "create", "--title", f"test: quarantine flaky test {payload['stable_id']}", "--body", pr_body])
    if pr.returncode != 0:
        raise SystemExit("governance write failure: could not create quarantine PR")


def reintroduction(args: argparse.Namespace) -> int:
    raw = read_json(args.evidence)
    evidence_items: list[dict[str, Any]]
    if isinstance(raw, dict) and isinstance(raw.get("evidences"), list):
        evidence_items = [item for item in raw["evidences"] if isinstance(item, dict)]
    elif isinstance(raw, list):
        evidence_items = [item for item in raw if isinstance(item, dict)]
    elif isinstance(raw, dict):
        evidence_items = [raw]
    else:
        evidence_items = []
    if not evidence_items:
        raise SystemExit("invalid reintroduction evidence: at least one evidence item is required")

    state = read_json(args.state) if args.state and pathlib.Path(args.state).exists() else {}
    decisions = []
    for evidence in evidence_items:
        identity = sanitize(evidence.get("identity", ""))
        if not identity:
            raise SystemExit("invalid reintroduction evidence: identity is required")
        current = state.get(identity, {"pass_count": 0, "recurrence_count": 0, "last_sha": ""})
        invalid_reasons = []
        if evidence.get("outcome") != "passed":
            invalid_reasons.append("outcome-not-passed")
        if evidence.get("filter") != QUARANTINE_FILTER:
            invalid_reasons.append("wrong-filter")
        for key in ["canceled", "partial", "dynamic_skip", "rerun_only", "malformed", "missing_evidence", "changed_identity"]:
            if evidence.get(key):
                invalid_reasons.append(key.replace("_", "-"))
        if not evidence.get("protected_branch_sha"):
            invalid_reasons.append("missing-protected-branch-sha")

        if invalid_reasons:
            current["pass_count"] = 0
            action = "reset"
        else:
            current["pass_count"] = int(current.get("pass_count", 0)) + 1
            current["last_sha"] = sanitize(evidence["protected_branch_sha"])
            action = "open-reintroduction-pr" if current["pass_count"] >= 5 else "track"
        current["last_updated"] = dt.datetime.now(dt.timezone.utc).isoformat()
        current["invalid_reasons"] = invalid_reasons
        state[identity] = current
        decisions.append({"identity": identity, "action": action, "invalid_reasons": invalid_reasons})

    overall_action = "open-reintroduction-pr" if any(item["action"] == "open-reintroduction-pr" for item in decisions) else decisions[-1]["action"]
    payload = {REINTRO_MARKER: True, "identity": decisions[0]["identity"], "action": overall_action, "items": decisions, "state": state}
    if args.output_state:
        write_json(args.output_state, state)
    if args.output:
        write_json(args.output, payload)
    if args.apply:
        require_trusted_context(args)
        apply_reintroduction_update(payload, state, args)
    return 0


def build_reintroduction_patch(root: pathlib.Path, identity: str) -> dict[str, Any]:
    method = identity.split(".")[-1]
    candidates: list[tuple[pathlib.Path, str]] = []
    for path in root.glob("tests/**/*.cs"):
        if "obj" in path.parts or "bin" in path.parts or path.name.endswith(".g.cs"):
            continue
        text = path.read_text(encoding="utf-8")
        if method in text and '[Trait("Category", "Quarantined")]' in text:
            candidates.append((path, text))
    if len(candidates) != 1:
        return {
            "source_mapping": "ambiguous",
            "manual_patch_required": True,
            "manual_patch_reason": f"expected exactly one quarantined source candidate for {identity}, found {len(candidates)}",
        }

    path, text = candidates[0]
    rel = path.relative_to(root).as_posix()
    pattern = re.compile(
        rf"(^[ \t]*// frontcomposer-quarantine:[^\n]*\r?\n)?^[ \t]*\[Trait\(\"Category\", \"Quarantined\"\)\]\r?\n(?=[\s\S]{{0,500}}\b{re.escape(method)}\s*\()",
        re.MULTILINE,
    )
    patched, count = pattern.subn("", text, count=1)
    if count != 1:
        return {
            "source_mapping": "ambiguous",
            "manual_patch_required": True,
            "manual_patch_reason": f"could not remove one quarantine metadata block for {identity}",
            "source_path": rel,
        }
    return {
        "source_mapping": "unambiguous",
        "manual_patch_required": False,
        "source_path": rel,
        "patch": patched,
    }


def apply_reintroduction_update(payload: dict[str, Any], state: dict[str, Any], args: argparse.Namespace) -> None:
    body = "\n".join(
        [
            REINTRO_MARKER,
            "",
            "## Quarantine reintroduction state",
            "",
            "```json",
            json.dumps(state, indent=2, sort_keys=True),
            "```",
        ]
    )
    existing = gh(["issue", "list", "--state", "open", "--search", REINTRO_MARKER, "--json", "number"])
    number = None
    if existing.returncode == 0:
        matches = json.loads(existing.stdout or "[]")
        number = str(matches[0]["number"]) if matches else None
    if number:
        edited = gh(["issue", "edit", number, "--body", body])
        if edited.returncode != 0:
            raise SystemExit("governance write failure: could not update quarantine reintroduction state issue")
    else:
        created = gh(["issue", "create", "--title", "Quarantine reintroduction state", "--body", body])
        if created.returncode != 0:
            raise SystemExit("governance write failure: could not create quarantine reintroduction state issue")

    reintroduction_items = [
        item for item in payload.get("items", [])
        if item.get("action") == "open-reintroduction-pr"
    ]
    if not reintroduction_items:
        return
    if not args.source_root:
        raise SystemExit("governance write failure: source root is required to open a reintroduction PR")

    base_ref = subprocess.run(["git", "rev-parse", "HEAD"], text=True, check=True, capture_output=True).stdout.strip()
    for item in reintroduction_items:
        identity = item["identity"]
        patch = build_reintroduction_patch(pathlib.Path(args.source_root), identity)
        if patch.get("manual_patch_required"):
            raise SystemExit(f"governance write failure: {patch.get('manual_patch_reason')}")

        stable_id = hashlib.sha256(identity.encode("utf-8")).hexdigest()[:12]
        branch = f"{args.branch_prefix}{stable_id}"
        subprocess.run(["git", "checkout", "-B", branch, base_ref], check=True)
        source_path = pathlib.Path(patch["source_path"])
        source_path.write_text(patch["patch"], encoding="utf-8")
        if args.output_state:
            state_path = pathlib.Path(args.output_state)
            if state_path.exists():
                subprocess.run(["git", "add", str(state_path)], check=False)
        subprocess.run(["git", "add", str(source_path)], check=True)
        subprocess.run(["git", "commit", "-m", f"test: reintroduce stable quarantine {stable_id}"], check=True)
        subprocess.run(["git", "push", "-u", "origin", branch], check=True)
        pr_body = "\n".join(
            [
                f"{REINTRO_MARKER} id={stable_id}",
                "",
                f"Removes quarantine metadata for `{sanitize(identity)}` after 5 valid nightly passes.",
                "",
                "The reintroduction state issue has been updated before this PR was opened.",
            ]
        )
        pr = gh(["pr", "create", "--title", f"test: reintroduce quarantined test {stable_id}", "--body", pr_body])
        if pr.returncode != 0:
            raise SystemExit("governance write failure: could not create reintroduction PR")


def parse_run_date(value: str) -> dt.date | None:
    try:
        return dt.datetime.fromisoformat(value.replace("Z", "+00:00")).date()
    except ValueError:
        return None


def has_three_consecutive_breach_days(runs: list[dict[str, Any]]) -> bool:
    breached_days = {
        day
        for run in runs
        if (day := parse_run_date(run["timestamp"])) is not None
        and run["duration_seconds"] > 15 * 60
    }
    if len(breached_days) < 3:
        return False
    streak = 0
    previous: dt.date | None = None
    for day in sorted(breached_days):
        streak = streak + 1 if previous and day == previous + dt.timedelta(days=1) else 1
        if streak >= 3:
            return True
        previous = day
    return False


def duration_monitor(args: argparse.Namespace) -> int:
    raw = read_json(args.evidence)
    runs = raw.get("runs", raw) if isinstance(raw, dict) else raw
    if not isinstance(runs, list):
        raise SystemExit("invalid duration evidence: expected runs list")
    valid_runs = []
    for run in runs:
        if not isinstance(run, dict):
            raise SystemExit("invalid duration evidence: run must be object")
        valid_runs.append(
            {
                "timestamp": sanitize(run.get("timestamp", "")),
                "run_id": sanitize(run.get("run_id", "")),
                "commit": sanitize(run.get("commit", "")),
                "workflow": sanitize(run.get("workflow", "")),
                "lane": sanitize(run.get("lane", "")),
                "lane_type": sanitize(run.get("lane_type", "")),
                "duration_seconds": int(run.get("duration_seconds", 0)),
                "conclusion": sanitize(run.get("conclusion", "")),
                "blocking": bool(run.get("blocking", False)),
                "protected_branch": bool(run.get("protected_branch", False)),
            }
        )
    full_ci = [r for r in valid_runs if r["lane_type"] == "full-ci" and r["protected_branch"] and r["conclusion"] not in {"cancelled", "skipped"}]
    breaches = [r for r in full_ci if r["duration_seconds"] > 15 * 60]
    suspected = sorted(full_ci, key=lambda r: r["duration_seconds"], reverse=True)[:5]
    action = "open-or-update-ci-diet-issue" if has_three_consecutive_breach_days(full_ci) else "record-only"
    payload = {
        "marker": CI_DIET_MARKER,
        "action": action,
        "budgets": {"inner-loop": 300, "full-ci": 720, "nightly": 2700, "mandatory_ci_diet": 900},
        "runs": valid_runs,
        "suspected_slow_lanes": suspected,
    }
    lines = [
        "## CI Duration Governance",
        "",
        f"Action: **{action}**",
        f"Full-CI protected-branch breaches over 15 minutes: {len(breaches)}",
        "",
        "| Lane | Duration seconds | Run | Commit |",
        "| --- | ---: | --- | --- |",
    ]
    for r in suspected:
        lines.append(f"| {r['lane']} | {r['duration_seconds']} | {r['run_id']} | `{r['commit']}` |")
    if args.output:
        write_json(args.output, payload)
    if args.markdown:
        write_text(args.markdown, "\n".join(lines) + "\n")
    if args.apply and action == "open-or-update-ci-diet-issue":
        require_trusted_context(args)
        labels = gh(["label", "list", "--json", "name"])
        available = {item["name"] for item in json.loads(labels.stdout or "[]")} if labels.returncode == 0 else set()
        labels_to_apply = [label for label in ["ci-governance", "codex-automation"] if label in available]
        missing = [label for label in ["ci-governance", "codex-automation"] if label not in available]
        body = "\n".join(lines) + f"\n\n{CI_DIET_MARKER}\n"
        if missing:
            body += "\nMissing labels: " + ", ".join(missing) + "\n"
        existing = gh(["issue", "list", "--state", "open", "--search", CI_DIET_MARKER, "--json", "number"])
        number = None
        if existing.returncode == 0:
            matches = json.loads(existing.stdout or "[]")
            number = str(matches[0]["number"]) if matches else None
        if number:
            edited = gh(["issue", "edit", number, "--body", body])
            if edited.returncode != 0:
                raise SystemExit("governance write failure: could not update CI-diet issue")
        else:
            cmd = ["issue", "create", "--title", "CI diet required: full CI exceeded 15 minutes for 3 days", "--body", body]
            for label in labels_to_apply:
                cmd += ["--label", label]
            created = gh(cmd)
            if created.returncode != 0:
                raise SystemExit("governance write failure: could not create CI-diet issue")
    return 0


def validate_metadata(args: argparse.Namespace) -> int:
    root = pathlib.Path(args.root)
    failures: list[str] = []
    for path in root.glob("tests/**/*.cs"):
        if "bin" in path.parts or "obj" in path.parts:
            continue
        lines = path.read_text(encoding="utf-8").splitlines()
        for i, line in enumerate(lines):
            if '[Trait("Category", "Quarantined")]' in line:
                context = "\n".join(lines[max(0, i - 3) : i + 1])
                if "frontcomposer-quarantine:" not in context:
                    failures.append(f"{path}: missing quarantine metadata comment")
                for token in ["issue=", "owner=", "reason=", "reintroduction="]:
                    if token not in context:
                        failures.append(f"{path}: missing {token} quarantine metadata")
    if failures:
        for failure in failures:
            print(failure, file=sys.stderr)
        return 1
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    q = sub.add_parser("summarize-quarantine")
    q.add_argument("--results-dir", required=True)
    q.add_argument("--markdown", required=True)
    q.add_argument("--json", required=True)
    q.add_argument("--sha", default=os.environ.get("GITHUB_SHA", ""))
    q.add_argument("--run-url", default="")
    q.add_argument("--github-step-summary", default=os.environ.get("GITHUB_STEP_SUMMARY", ""))
    q.set_defaults(func=summarize_quarantine)

    f = sub.add_parser("classify-flake")
    f.add_argument("--evidence", required=True)
    f.add_argument("--output")
    f.add_argument("--source-root", default="")
    f.add_argument("--owner", default="owner-needed")
    f.add_argument("--hypothesis", default="owner-needed")
    f.add_argument("--apply", action="store_true")
    f.add_argument("--branch-prefix", default="codex/quarantine-")
    f.add_argument("--event-name", default=os.environ.get("GITHUB_EVENT_NAME", ""))
    f.add_argument("--ref", default=os.environ.get("GITHUB_REF", ""))
    f.add_argument("--ref-protected", default=os.environ.get("GITHUB_REF_PROTECTED", "false"))
    f.add_argument("--from-fork", default="false")
    f.set_defaults(func=classify_flake)

    r = sub.add_parser("reintroduction")
    r.add_argument("--evidence", required=True)
    r.add_argument("--state")
    r.add_argument("--output-state")
    r.add_argument("--output")
    r.add_argument("--apply", action="store_true")
    r.add_argument("--source-root", default="")
    r.add_argument("--branch-prefix", default="codex/reintroduce-")
    r.add_argument("--event-name", default=os.environ.get("GITHUB_EVENT_NAME", ""))
    r.add_argument("--ref", default=os.environ.get("GITHUB_REF", ""))
    r.add_argument("--ref-protected", default=os.environ.get("GITHUB_REF_PROTECTED", "false"))
    r.add_argument("--from-fork", default="false")
    r.set_defaults(func=reintroduction)

    d = sub.add_parser("duration-monitor")
    d.add_argument("--evidence", required=True)
    d.add_argument("--output")
    d.add_argument("--markdown")
    d.add_argument("--apply", action="store_true")
    d.add_argument("--event-name", default=os.environ.get("GITHUB_EVENT_NAME", ""))
    d.add_argument("--ref", default=os.environ.get("GITHUB_REF", ""))
    d.add_argument("--ref-protected", default=os.environ.get("GITHUB_REF_PROTECTED", "false"))
    d.add_argument("--from-fork", default="false")
    d.set_defaults(func=duration_monitor)

    m = sub.add_parser("validate-quarantine-metadata")
    m.add_argument("--root", default=".")
    m.set_defaults(func=validate_metadata)

    args = parser.parse_args()
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
