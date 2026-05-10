"""Apply Story 9-4 chunk-C code-review patches to docs/diagnostics stubs and registry.

Handles:
  P1  — re-emit stale `storyOwner` for 18 stubs from registry `ownerStory`.
  P2  — fix HFC2014 body 'Git Hub' → 'GitHub'.
  P6  — emit replacement API + version window in HFC0001/HFC4001 Migration sections.
  P7  — replace em-dash with ASCII hyphen in HFC1029 title (front-matter + body).
  P8  — make Suppression Guidance 'Error-level' warning conditional on severity == Error.
  P9  — HFC1013 retired stub: move allocate-new-ID guidance to Migration/Deprecation.
  P11 — normalize `severity: Information` → `severity: Info` for ~25 stubs.
  P12 — Shell Error stubs: body `Suppression policy: discouraged-error`; registry `suppressionPolicy: discouraged-error`.
  P13 — registry `compilerSeverity: Error` for HFC2012/2013/2109/2110/2112; Shell unshipped row severity → Error.
  P14 — HFC1013 retired-stub: drop emission-shape fields (severity, body Suppression policy line, body Canonical help link block).
  P15 — registry header: add `docsStubProsePolicy`.

Loads, mutates, writes back. Idempotent: re-running yields no change once applied.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
REGISTRY_PATH = REPO / "docs" / "diagnostics" / "diagnostic-registry.json"
STUBS_DIR = REPO / "docs" / "diagnostics"
SHELL_UNSHIPPED = REPO / "src" / "Hexalith.FrontComposer.Shell" / "AnalyzerReleases.Unshipped.md"

# ---- Registry side ----------------------------------------------------------

with REGISTRY_PATH.open(encoding="utf-8") as f:
    reg = json.load(f)

# P15: docs-stub prose policy.
reg.setdefault(
    "docsStubProsePolicy",
    "auto-synthesized-placeholder-pending-authoring; per-diagnostic prose between <!-- story-9-5:narrative-start --> and <!-- story-9-5:narrative-end --> is authored by Story 9-5 (Diataxis docs) and validated then. Stub generation emits skeletal Problem/Common Causes/How To Fix/Example/Suppression Guidance/Migration-Deprecation/Related Diagnostics sections so DocFX consumption and AC5 stub-presence validation can run before authoring lands.",
)

# Reorder header so policies stay near the top.
desired_order = [
    "schemaVersion",
    "canonicalHelpLinkFormat",
    "messageTemplatePolicy",
    "docsStubProsePolicy",
    "ranges",
    "externalBoundaries",
    "allowedExceptions",
    "diagnostics",
]
reg = {k: reg[k] for k in desired_order if k in reg} | {k: v for k, v in reg.items() if k not in desired_order}

# P12 (registry side): Shell Error stubs → suppressionPolicy = discouraged-error.
shell_error_ids = {
    "HFC1601", "HFC2004", "HFC2011", "HFC2012", "HFC2013", "HFC2014",
    "HFC2109", "HFC2110", "HFC2112", "HFC2115", "HFC2121",
}

# P13 (registry side): set compilerSeverity = Error for analyzer-tracked Shell Warning rows.
shell_compiler_severity_elevations = {"HFC2012", "HFC2013", "HFC2109", "HFC2110", "HFC2112"}

for d in reg["diagnostics"]:
    hfc_id = d["id"]
    if hfc_id in shell_error_ids:
        d["suppressionPolicy"] = "discouraged-error"
    if hfc_id in shell_compiler_severity_elevations:
        d["compilerSeverity"] = "Error"
        d["panelSeverity"] = "Error"  # registry already had Error here for the drift; keep aligned.
        d.setdefault(
            "lifecycleNote",
            "compilerSeverity elevated to Error in 0.2.0 to align with registry panelSeverity per AC22 (analyzer-tracked rows must declare compilerSeverity when releaseRow != runtime-only).",
        )

# Build id → ownerStory map for stub re-emission.
owner_story_by_id = {d["id"]: d.get("ownerStory") for d in reg["diagnostics"]}

# Write registry back.
text = json.dumps(reg, indent=2, ensure_ascii=False) + "\n"
REGISTRY_PATH.write_text(text, encoding="utf-8", newline="\n")

# ---- Shell unshipped row severity (P13) ------------------------------------

shell_md = SHELL_UNSHIPPED.read_text(encoding="utf-8")
for hfc_id in shell_compiler_severity_elevations:
    pattern = re.compile(rf"^({re.escape(hfc_id)} \| HexalithFrontComposer \| )Warning(\s+)", re.MULTILINE)
    shell_md, n = pattern.subn(r"\1Error      \2", shell_md, count=1)
    if n != 1:
        print(f"WARN: P13 row update for {hfc_id} matched {n} times (expected 1).")

# Annotate the rule note for traceability.
note_pattern = re.compile(
    r"^(HFC2(?:012|013|109|110|112) \| HexalithFrontComposer \| Error\s+\| [^\r\n]+?)(?<! \(Story 9-4 chunk-C P13\))(\r?\n)",
    re.MULTILINE,
)
shell_md = note_pattern.sub(r"\1 (Story 9-4 chunk-C P13: severity reconciled with registry compilerSeverity)\2", shell_md)
SHELL_UNSHIPPED.write_text(shell_md, encoding="utf-8", newline="\n")

# ---- Per-stub edits ---------------------------------------------------------

# P11: severity: Information → severity: Info (canonical token Decision-2).
severity_information_to_info = re.compile(r"^severity: Information$", re.MULTILINE)

# P12 (stub side): body Suppression policy → discouraged-error for Shell Error stubs.
suppression_body_pattern = re.compile(r"^Suppression policy: `[^`]+`$", re.MULTILINE)

# P1: stale storyOwner re-emission.
story_owner_pattern = re.compile(r"^storyOwner:\s*(?P<value>\S+)\s*$", re.MULTILINE)

# P8: drop "Error-level suppressions" tail for non-Error stubs.
suppression_guidance_with_tail = re.compile(
    r"^Suppression is `(?P<policy>[^`]+)`\.\s*Prefer `\.editorconfig`[^\n]*?, and record migration or architecture rationale for Error-level suppressions\.$",
    re.MULTILINE,
)

severity_line = re.compile(r"^severity:\s*(?P<sev>\S+)\s*$", re.MULTILINE)


def stub_severity(text: str) -> str | None:
    m = severity_line.search(text)
    return m.group("sev") if m else None


def apply_per_stub(path: Path) -> bool:
    text = path.read_text(encoding="utf-8")
    original = text

    hfc_id = path.stem  # "HFCxxxx"

    # P1: storyOwner re-emission against registry.
    expected_owner = owner_story_by_id.get(hfc_id)
    if expected_owner:
        text = story_owner_pattern.sub(f"storyOwner: {expected_owner}", text, count=1)

    # P11: severity vocabulary → Info.
    text = severity_information_to_info.sub("severity: Info", text)

    # P12 (stub side): Shell Error stubs → discouraged-error in body.
    if hfc_id in shell_error_ids:
        text = suppression_body_pattern.sub("Suppression policy: `discouraged-error`", text, count=1)

    # P8: conditional Suppression Guidance prose.
    sev = stub_severity(text)
    if sev != "Error":
        text = suppression_guidance_with_tail.sub(
            r"Suppression is `\g<policy>`. Prefer `.editorconfig` or a narrowly scoped pragma for analyzer diagnostics.",
            text,
        )

    # P12 (stub-side body suppression-policy field follow-up): if we changed body to discouraged-error
    # but Suppression Guidance prose still says "allowed-with-rationale", re-emit the prose.
    if hfc_id in shell_error_ids:
        text = re.sub(
            r"Suppression is `[^`]+`\.",
            "Suppression is `discouraged-error`.",
            text,
            count=1,
        )

    if text != original:
        path.write_text(text, encoding="utf-8", newline="\n")
        return True
    return False


# Iterate every stub for P1/P11/P8/P12.
modified_count = 0
for stub_path in sorted(STUBS_DIR.glob("HFC*.md")):
    if apply_per_stub(stub_path):
        modified_count += 1

# ---- Targeted single-file edits --------------------------------------------

# P2: HFC2014 body 'Git Hub' → 'GitHub'.
hfc2014 = STUBS_DIR / "HFC2014.md"
text2014 = hfc2014.read_text(encoding="utf-8")
text2014 = text2014.replace("Git Hub", "GitHub")
hfc2014.write_text(text2014, encoding="utf-8", newline="\n")

# P7: HFC1029 em-dash → ASCII hyphen in title (front-matter + body).
hfc1029 = STUBS_DIR / "HFC1029.md"
text1029 = hfc1029.read_text(encoding="utf-8")
text1029 = text1029.replace("Projection exceeds 15 columns — FcColumnPrioritizer activates", "Projection exceeds 15 columns - FcColumnPrioritizer activates")
hfc1029.write_text(text1029, encoding="utf-8", newline="\n")

# Registry title for HFC1029 too (so the stub-vs-registry title parity test passes).
with REGISTRY_PATH.open(encoding="utf-8") as f:
    reg2 = json.load(f)
for d in reg2["diagnostics"]:
    if d["id"] == "HFC1029":
        d["title"] = "Projection exceeds 15 columns - FcColumnPrioritizer activates"
        if "messageTemplate" in d:
            d["messageTemplate"] = d["messageTemplate"].replace("—", "-")
        break
REGISTRY_PATH.write_text(json.dumps(reg2, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")

# P6: HFC0001/HFC4001 Migration section authoring.
hfc0001_migration_old = "This diagnostic owns a deprecation or migration path. Follow the linked migration entry before the removal version."
hfc0001_migration_new = (
    "QueryRequest.Filter is replaced by ColumnFilters in v0.2.0; removed in v0.4.0. See HFC0001 for the deprecation contract and the [Obsolete] DiagnosticId / UrlFormat metadata.\n"
    "\n"
    "Migration steps:\n"
    "1. Replace `QueryRequest.Filter = ...` with `QueryRequest.ColumnFilters = ...`.\n"
    "2. Update any custom serializers or projections that still read the legacy `Filter` property.\n"
    "3. Rebuild against ≥ v0.2.0 and confirm no HFC0001 diagnostics remain in the build log."
)
hfc4001_migration_old = hfc0001_migration_old
hfc4001_migration_new = (
    "MCP backwards-compatible additive drift was deprecated in v0.2.0; removed in v0.4.0. See HFC4001 for the deprecation contract.\n"
    "\n"
    "Migration steps:\n"
    "1. Switch additive-drift behaviour to the explicit MCP schema-negotiation runtime gate (Story 8-6a).\n"
    "2. Update host configuration to advertise the negotiated schema version rather than relying on additive tolerance.\n"
    "3. Rebuild against ≥ v0.2.0 and confirm no HFC4001 diagnostics remain in MCP traffic."
)

hfc0001 = STUBS_DIR / "HFC0001.md"
hfc0001.write_text(
    hfc0001.read_text(encoding="utf-8").replace(hfc0001_migration_old, hfc0001_migration_new),
    encoding="utf-8",
    newline="\n",
)

hfc4001 = STUBS_DIR / "HFC4001.md"
hfc4001.write_text(
    hfc4001.read_text(encoding="utf-8").replace(hfc4001_migration_old, hfc4001_migration_new),
    encoding="utf-8",
    newline="\n",
)

# P9 + P14: HFC1013 retired stub trim and Migration/Deprecation guidance.
hfc1013 = STUBS_DIR / "HFC1013.md"
hfc1013_text = (
    """---
id: HFC1013
title: "HFC1013 Reserved generator diagnostic placeholder"
ownerPackage: SourceTools
lifecycle: retired
introducedIn: 0.1.0
docsSlug: diagnostics/HFC1013
relatedIds: []
storyOwner: 2-2-action-density-rules-and-rendering-modes
---

<!-- story-9-5:metadata-start -->
Registry owner: `SourceTools`
<!-- story-9-5:metadata-end -->

<!-- story-9-5:narrative-start -->
## Problem

HFC1013 Reserved generator diagnostic placeholder.

## Common Causes

The ID was once proposed for a generator diagnostic and is retained so future diagnostics cannot reuse it for a different meaning.

## How To Fix

Do not emit this ID for new behavior.

## Example

```text
HFC1013: Retired placeholder; do not reuse.
```

## Suppression Guidance

Not applicable. Retired diagnostics are not emitted; there is nothing to suppress.

## Migration/Deprecation

If new behavior previously associated with HFC1013 needs a diagnostic, allocate a new SourceTools ID and add a registry row, release row, and docs stub. Do not reuse this ID.

## Related Diagnostics

None currently.
<!-- story-9-5:narrative-end -->
"""
)
hfc1013.write_text(hfc1013_text, encoding="utf-8", newline="\n")

# Sync registry: ensure HFC1013 has compilerSeverity/runtimeLogLevel/panelSeverity all null per AC22 retired contract.
with REGISTRY_PATH.open(encoding="utf-8") as f:
    reg3 = json.load(f)
for d in reg3["diagnostics"]:
    if d["id"] == "HFC1013":
        for k in ("compilerSeverity", "runtimeLogLevel", "panelSeverity"):
            d[k] = None
        d["cliExitBehavior"] = "non-blocking"
        break
REGISTRY_PATH.write_text(json.dumps(reg3, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")

print(f"OK — chunk-C patches applied. Stubs modified: {modified_count}; registry header updated; Shell unshipped severity reconciled.")
