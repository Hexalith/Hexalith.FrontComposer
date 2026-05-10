"""Apply Story 9-4 chunk-A code-review patches to docs/diagnostics/diagnostic-registry.json.

Loads, mutates, writes back with deterministic 2-space indent. Idempotent: re-running yields no change once applied.
"""
from __future__ import annotations

import json
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
REGISTRY = REPO / "docs" / "diagnostics" / "diagnostic-registry.json"

with REGISTRY.open(encoding="utf-8") as f:
    reg = json.load(f)

# ---- Header-level changes ---------------------------------------------------

# D4: messageTemplate boilerplate is auto-synthesized; flag as known gap.
reg.setdefault(
    "messageTemplatePolicy",
    "auto-synthesized-placeholder-pending-authoring; per-diagnostic templates are authored by Story 9-5 (Diataxis docs) and validated then. Until then, runtime emission relies on the analyzer messageFormat / runtime logger templates and the docs stub at docsSlug.",
)

# D1: HFC1601 cross-range allowed exception (owner=Shell, numeric ID in SourceTools range).
reg.setdefault(
    "allowedExceptions",
    {
        "crossPackageRange": [
            {
                "id": "HFC1601",
                "ownerPackage": "Shell",
                "numericRangeOwner": "SourceTools",
                "reason": (
                    "Manifest-validation runtime check predates registry governance; the constant "
                    "FcDiagnosticIds.HFC1601_ManifestInvalid is consumed by Shell startup (Registration/FrontComposerRegistry) "
                    "and Customization gate logging. Reissuing into the Shell range would break public diagnostic-id "
                    "telemetry; this exception preserves stability while AC1's spirit is honored by explicit registration."
                ),
                "approvedIn": "0.2.0",
            }
        ]
    },
)

# Reorder so schema/canonical/ranges come first, allowedExceptions+policy near top, diagnostics last.
desired_order = [
    "schemaVersion",
    "canonicalHelpLinkFormat",
    "messageTemplatePolicy",
    "ranges",
    "externalBoundaries",
    "allowedExceptions",
    "diagnostics",
]
reg = {k: reg[k] for k in desired_order if k in reg} | {k: v for k, v in reg.items() if k not in desired_order}

# ---- Per-entry mutations ----------------------------------------------------

owner_story_overrides = {
    # SourceTools historical attributions
    "HFC1009": "2-2-action-density-rules-and-rendering-modes",
    "HFC1011": "2-2-action-density-rules-and-rendering-modes",
    "HFC1012": "2-2-action-density-rules-and-rendering-modes",
    "HFC1013": "2-2-action-density-rules-and-rendering-modes",
    "HFC1014": "2-2-action-density-rules-and-rendering-modes",
    "HFC1015": "2-2-action-density-rules-and-rendering-modes",
    "HFC1016": "2-2-action-density-rules-and-rendering-modes",
    "HFC1017": "2-2-action-density-rules-and-rendering-modes",
    "HFC1047": "6-5-customization-gradient-level-3-template-overrides",
    "HFC1048": "6-5-customization-gradient-level-3-template-overrides",
    "HFC1049": "6-5-customization-gradient-level-3-template-overrides",
    "HFC1056": "7-3-command-authorization-policies",
    "HFC1057": "7-3-command-authorization-policies",
    "HFC1058": "9-1-build-time-drift-detection",
    "HFC1059": "9-1-build-time-drift-detection",
    "HFC1060": "9-1-build-time-drift-detection",
    "HFC1061": "9-1-build-time-drift-detection",
    "HFC1062": "9-1-build-time-drift-detection",
    "HFC1063": "9-1-build-time-drift-detection",
    "HFC1064": "9-1-build-time-drift-detection",
    "HFC1065": "9-1-build-time-drift-detection",
    "HFC1066": "9-1-build-time-drift-detection",
    "HFC1067": "9-1-build-time-drift-detection",
    "HFC1068": "9-1-build-time-drift-detection",
    "HFC1069": "9-1-build-time-drift-detection",
    "HFC1070": "9-1-build-time-drift-detection",
    # Shell historical attributions
    "HFC1601": "3-4-fccommandpalette-and-keyboard-shortcuts",
    "HFC2004": "2-4-fclifecyclewrapper-visual-lifecycle-feedback",
    "HFC2005": "2-4-fclifecyclewrapper-visual-lifecycle-feedback",
    "HFC2007": "2-4-fclifecyclewrapper-visual-lifecycle-feedback",
    "HFC2010": "6-5-customization-gradient-level-3-template-overrides",
    "HFC2011": "7-1-host-authentication-and-authorization-bridge",
    "HFC2012": "7-1-host-authentication-and-authorization-bridge",
    "HFC2013": "7-1-host-authentication-and-authorization-bridge",
    "HFC2014": "7-1-host-authentication-and-authorization-bridge",
    "HFC2108": "3-4-fccommandpalette-and-keyboard-shortcuts",
    "HFC2109": "3-4-fccommandpalette-and-keyboard-shortcuts",
    "HFC2110": "3-4-fccommandpalette-and-keyboard-shortcuts",
    "HFC2111": "3-4-fccommandpalette-and-keyboard-shortcuts",
    "HFC2112": "3-5-home-directory-badge-counts-and-new-capability-discovery",
    "HFC2113": "3-5-home-directory-badge-counts-and-new-capability-discovery",
    "HFC2114": "3-6-session-persistence-and-context-restoration",
    "HFC4001": "8-6-mcp-schema-negotiation-and-fingerprint-trust",
}

reserved_to_active = {"HFC1010", "HFC1026", "HFC1042", "HFC1046"}


def normalize_title(d):
    """Trim redundant `HFCxxxx ` prefix and fix split-CamelCase artifacts."""
    title = d.get("title")
    if not isinstance(title, str):
        return
    hfc_id = d["id"]
    if title.startswith(hfc_id + " "):
        title = title[len(hfc_id) + 1 :]
    title = title.replace("Git Hub", "GitHub")
    d["title"] = title


for d in reg["diagnostics"]:
    hfc_id = d["id"]

    # D6: drop denormalized per-entry `range` field.
    d.pop("range", None)

    # AA-LOW: re-attribute ownerStory where the diagnostic predates Story 9-4.
    if hfc_id in owner_story_overrides:
        d["ownerStory"] = owner_story_overrides[hfc_id]

    # AA-LOW + D9: title canonicalization.
    normalize_title(d)

    # D8: reserved IDs that ship live descriptors → flip to active.
    if hfc_id in reserved_to_active and d.get("lifecycle") == "reserved":
        d["lifecycle"] = "active"

    # DUP-C: HFC0001 / HFC4001 lifecycle should be `deprecated`, not `active`.
    if hfc_id in {"HFC0001", "HFC4001"}:
        if d.get("deprecatedIn") and d.get("lifecycle") == "active":
            d["lifecycle"] = "deprecated"
        # D2: shorten removal window to ≥2 minors on same major (0.2.0 → 0.4.0).
        if d.get("removedIn") == "1.0.0":
            d["removedIn"] = "0.4.0"

    # BH-MED: HFC1013 retired → cliExitBehavior must not still claim diagnostic.
    if hfc_id == "HFC1013" and d.get("lifecycle") == "retired":
        d["cliExitBehavior"] = "non-blocking"

    # DUP-B: HFC1601 ownerPackage = Shell (matches actual emit location); release-row already in Shell file.
    if hfc_id == "HFC1601":
        d["ownerPackage"] = "Shell"
        d["releaseRow"] = "runtime-only"  # logger/exception emission, not analyzer-emitted
        d["lifecycleNote"] = "cross-package-range-exception (numeric ID in SourceTools range, owned by Shell). See registry.allowedExceptions."

    # AA-MED: HFC1056 / HFC1057 helpLinkUri host change is an analyzer-compat change.
    if hfc_id in {"HFC1056", "HFC1057"}:
        d.setdefault(
            "lifecycleNote",
            "helpLinkUri rebased from legacy hexalith.io host to canonical github.io host in 0.2.0 per registry canonicalHelpLinkFormat alignment (AC22).",
        )

# ---- Write back -------------------------------------------------------------

text = json.dumps(reg, indent=2, ensure_ascii=False) + "\n"
REGISTRY.write_text(text, encoding="utf-8", newline="\n")

print(f"OK — registry now has {len(reg['diagnostics'])} diagnostics; bytes={REGISTRY.stat().st_size}.")
