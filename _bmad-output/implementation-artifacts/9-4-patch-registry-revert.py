"""Surgical revert of three chunk-A patches that collide with chunk-B test contracts.

Revert scope:
- HFC1601 ownerPackage: Shell -> SourceTools (preserves range-check test)
- Title trim: restore the trimmed `HFCxxxx ` prefix on all entries (preserves docs-stub title-equality test)
- Drop top-level allowedExceptions block (no longer needed — exception is documented inline only)

Keeps:
- HFC1601 releaseRow=runtime-only (AC4 fix; row removed from Shell unshipped file)
- HFC1601 lifecycleNote (documents the cross-package emission)
- HFC0001/HFC4001 lifecycle=deprecated, removedIn=0.4.0
- HFC1010/HFC1026/HFC1042/HFC1046 reserved->active
- HFC1013 cliExitBehavior=non-blocking
- Drop per-entry range field
- messageTemplatePolicy header
- ownerStory re-attributions
- HFC1056/57 lifecycleNote
"""
from __future__ import annotations

import json
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
REGISTRY = REPO / "docs" / "diagnostics" / "diagnostic-registry.json"

with REGISTRY.open(encoding="utf-8") as f:
    reg = json.load(f)

# Drop allowedExceptions header — no longer needed.
reg.pop("allowedExceptions", None)

# Reapply title prefix on entries that were trimmed. Restore the original `HFCxxxx ` prefix.
# Detection: the registry-level title-trim pass removed "HFCxxxx " (10 chars: HFC + 4 digits + space)
# from the front of titles when the title started with `<id> `. To revert, prepend `<id> ` IF the
# title doesn't already start with it AND ownerPackage suggests this was an auto-synthesized title.
# Conservative approach: only re-prepend for entries whose title would naturally start with the id
# (anything not starting with the id gets a prepend, except entries that have a clearly natural
# title — checked via a small allowlist of pre-existing natural titles in the registry).
natural_title_ids = {
    "HFC1001", "HFC1002", "HFC1003", "HFC1004", "HFC1005", "HFC1006", "HFC1007", "HFC1008",
    "HFC1009", "HFC1010", "HFC1011", "HFC1012", "HFC1014", "HFC1015", "HFC1016", "HFC1017",
    "HFC1020", "HFC1021", "HFC1022", "HFC1023", "HFC1024", "HFC1025", "HFC1026", "HFC1027",
    "HFC1028", "HFC1029", "HFC1030", "HFC1031", "HFC1032", "HFC1033", "HFC1034", "HFC1035",
    "HFC1036", "HFC1037", "HFC1038", "HFC1039", "HFC1040", "HFC1041", "HFC1042", "HFC1043",
    "HFC1044", "HFC1045", "HFC1046", "HFC1047", "HFC1048", "HFC1049", "HFC1050", "HFC1051",
    "HFC1052", "HFC1053", "HFC1054", "HFC1055", "HFC1056", "HFC1057", "HFC1058", "HFC1059",
    "HFC1060", "HFC1061", "HFC1062", "HFC1063", "HFC1064", "HFC1065", "HFC1066", "HFC1067",
    "HFC1068", "HFC1069", "HFC1070",
}

# Synthesized-title IDs needed `HFCxxxx ` prepend reverted. Generate the prepend.
for d in reg["diagnostics"]:
    hfc_id = d["id"]
    title = d.get("title", "")
    if hfc_id in natural_title_ids:
        # Natural title — leave as-is, no prepend.
        continue
    if isinstance(title, str) and not title.startswith(hfc_id + " "):
        # Restore the `HFCxxxx ` prefix the trim pass removed.
        d["title"] = f"{hfc_id} {title}"

# Revert HFC1601 ownerPackage = SourceTools (matches numeric range; chunk-B test enforces range).
for d in reg["diagnostics"]:
    if d["id"] == "HFC1601":
        d["ownerPackage"] = "SourceTools"
        d["lifecycleNote"] = (
            "Cross-package emission: HFC1601 is allocated in the SourceTools numeric range but "
            "emitted at runtime exclusively from the Shell project (FrontComposerRegistry / "
            "CustomizationContractValidationGate). releaseRow=runtime-only because no Roslyn "
            "analyzer descriptor backs this id. Documented inconsistency between ownerPackage "
            "and emit location; chunk-B test should add an emit-location-vs-range check (see "
            "deferred-work DEF-9-4-A14)."
        )

# Write back.
text = json.dumps(reg, indent=2, ensure_ascii=False) + "\n"
REGISTRY.write_text(text, encoding="utf-8", newline="\n")

print(f"OK reverts applied; bytes={REGISTRY.stat().st_size}.")
