#!/usr/bin/env python3
"""Release-run disposition classifier for independent post-publication verification.

Extracted from ``release-evidence.yml`` so governance tests can execute the same
fail-closed topology rules used in CI (governed-attempt / no-releasable /
rejected-before-publication) without relying on source-text pins alone.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

KNOWN_JOB_NAMES = frozenset({
    "verify-source",
    "plan-release",
    "prepare-candidate",
    "release",
    "release / release",
    "release / governed-release",
    "verify-publication",
    "emit-verification-handoff",
})
REQUIRED_JOBS = ("verify-source", "plan-release", "emit-verification-handoff")
PUBLICATION_JOBS = ("prepare-candidate", "release", "verify-publication")
AUTHORIZED_CLASSIFICATIONS = frozenset({"ready", "fallback-approved"})


class DispositionError(ValueError):
    """Raised when release-run topology or readiness evidence is invalid."""


def _closed(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise DispositionError(f"duplicate API member: {key}")
        value[key] = item
    return value


def load_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=_closed)


def classify_release_run(
    *,
    run: dict[str, Any],
    jobs: list[Any],
    expected_run_id: int,
    expected_run_attempt: int,
    expected_conclusion: str,
    expected_head_sha: str,
    ad15_candidate: str | None = None,
) -> dict[str, Any]:
    """Classify an authenticated operator Release run into a disposition record."""
    if not isinstance(run, dict) or not isinstance(jobs, list):
        raise DispositionError("malformed Release run/jobs API response")
    expected = {
        "id": expected_run_id,
        "run_attempt": expected_run_attempt,
        "event": "workflow_dispatch",
        "status": "completed",
        "conclusion": expected_conclusion,
        "head_branch": "main",
        "head_sha": expected_head_sha,
        "path": ".github/workflows/release.yml",
    }
    if any(run.get(key) != value for key, value in expected.items()):
        raise DispositionError("Release run coordinate does not match the authenticated operator dispatch")
    if re.fullmatch(r"[0-9a-f]{40}", run["head_sha"]) is None:
        raise DispositionError("Release run head SHA is malformed")
    if any(not isinstance(job, dict) or job.get("status") != "completed" for job in jobs):
        raise DispositionError("Release job topology is malformed or incomplete")
    names = [job.get("name") for job in jobs]
    if any(name not in KNOWN_JOB_NAMES for name in names) or len(names) != len(set(names)):
        raise DispositionError(f"Release job topology is unknown or ambiguous: {names}")
    by_name = {job["name"]: job for job in jobs}
    for required in REQUIRED_JOBS:
        if required not in by_name:
            raise DispositionError(f"Release job topology lacks {required}")
    governed = by_name.get("release / release")
    if governed is not None and governed.get("conclusion") not in {"skipped", None}:
        disposition = "governed-publication-attempt"
        governed_attempt = True
    elif (
        run.get("conclusion") == "success"
        and by_name["verify-source"].get("conclusion") == "success"
        and by_name["plan-release"].get("conclusion") == "success"
    ):
        for publication_job in PUBLICATION_JOBS:
            if publication_job in by_name and by_name[publication_job].get("conclusion") != "skipped":
                raise DispositionError(f"successful no-release topology unexpectedly ran {publication_job}")
        disposition = "no-releasable-commits"
        governed_attempt = False
    else:
        disposition = "rejected-before-publication"
        governed_attempt = False
    candidate = ad15_candidate or run["head_sha"]
    if ad15_candidate is not None and re.fullmatch(r"[0-9a-f]{40}", ad15_candidate) is None:
        raise DispositionError("AD-15 candidate SHA is malformed")
    return {
        "decision_contract": "frontcomposer.release-run-disposition.v2",
        "status": disposition,
        "governed_attempt": governed_attempt,
        "run_id": run["id"],
        "run_attempt": run["run_attempt"],
        "conclusion": run["conclusion"],
        "head_sha": run["head_sha"],
        "candidate": candidate,
        "jobs": [{"name": job["name"], "conclusion": job.get("conclusion")} for job in jobs],
    }


def require_published_readiness(readiness: Any) -> None:
    """Fail closed when downloaded sealed readiness was not publish-authorized before publication."""
    if not isinstance(readiness, dict):
        raise DispositionError("release readiness must be a JSON object")
    if readiness.get("publish_authorized") is not True or readiness.get("classification") not in AUTHORIZED_CLASSIFICATIONS:
        raise DispositionError("published release was not authorized by its sealed readiness evidence")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    classify = sub.add_parser("classify")
    classify.add_argument("--run", required=True)
    classify.add_argument("--jobs", required=True)
    classify.add_argument("--expected-run-id", required=True, type=int)
    classify.add_argument("--expected-run-attempt", required=True, type=int)
    classify.add_argument("--expected-conclusion", required=True)
    classify.add_argument("--expected-head-sha", required=True)
    classify.add_argument("--ad15-handoff")
    classify.add_argument("--output", required=True)
    classify.add_argument("--github-output")
    readiness = sub.add_parser("require-published-readiness")
    readiness.add_argument("--readiness", required=True)
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        if args.command == "classify":
            run = load_json(Path(args.run))
            envelope = load_json(Path(args.jobs))
            jobs = envelope.get("jobs") if isinstance(envelope, dict) else None
            if not isinstance(envelope, dict) or not isinstance(jobs, list):
                raise DispositionError("malformed Release jobs API response")
            if envelope.get("total_count") != len(jobs):
                raise DispositionError("ambiguous or paginated Release jobs API response")
            ad15_candidate = None
            if args.ad15_handoff:
                ad15_path = Path(args.ad15_handoff)
                if ad15_path.is_file():
                    ad15 = load_json(ad15_path)
                    if isinstance(ad15, dict) and isinstance(ad15.get("candidate"), str):
                        ad15_candidate = ad15["candidate"]
            result = classify_release_run(
                run=run,
                jobs=jobs,
                expected_run_id=args.expected_run_id,
                expected_run_attempt=args.expected_run_attempt,
                expected_conclusion=args.expected_conclusion,
                expected_head_sha=args.expected_head_sha,
                ad15_candidate=ad15_candidate,
            )
            Path(args.output).write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
            if args.github_output:
                with Path(args.github_output).open("a", encoding="utf-8") as handle:
                    handle.write(f"governed-attempt={'true' if result['governed_attempt'] else 'false'}\n")
                    handle.write(f"disposition={result['status']}\n")
                    handle.write(f"candidate={result['candidate']}\n")
            print(json.dumps({"ok": True, "disposition": result["status"], "governed_attempt": result["governed_attempt"]}, sort_keys=True))
            return 0
        if args.command == "require-published-readiness":
            readiness = load_json(Path(args.readiness))
            require_published_readiness(readiness)
            print(json.dumps({"ok": True, "publish_authorized": True}, sort_keys=True))
            return 0
        raise DispositionError(f"unsupported command {args.command!r}")
    except DispositionError as exc:
        print(json.dumps({"ok": False, "error": str(exc)}, sort_keys=True), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
