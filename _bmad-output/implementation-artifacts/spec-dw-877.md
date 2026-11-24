---
title: 'DW-877 Reject policy names outside Unicode Normalization Form C'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Reject `[RequiresPolicy]` names that are not in Unicode Normalization Form C, both at generation time (HFC1056) and in the `RequiresPolicyAttribute` constructor. Policy lookup is ordinal, so such names could never match their composed registration. Non-ASCII letters in composed form stay valid.

</frozen-after-approval>

## Implementation Notes

- Decision: keep non-ASCII letters (the existing contract) and require NFC, the narrowest change that closes the ordinal-lookup mismatch. Combining marks were already rejected by the letter-or-digit rule. The remaining gap was letter-only non-NFC text such as the Kelvin sign (U+212A) or decomposed Hangul jamo.
- The generator check lives in `FrontComposerGenerator.ParseCommand`, beside the DW-876 policy-attribute identity check, because the mutation ledger seals `CommandParser.cs` (21 entries, checked by `eng/check-mutation-triage.py`). The HFC1056 message follows the Expected/Got/Fix shape and echoes both names in full, with non-ASCII characters escaped as `\uXXXX`.
- `RequiresPolicyAttribute.IsWellFormed` applies the same rule. `HFC1056.md` documents the cause, the host-side fix, the breaking change, the combining-mark case, out-of-scope compatibility characters, and that invariant globalization skips the check (verified with a probe: `IsNormalized` returns true for the Kelvin sign there).
- Validation: `CommandParserTests` 61/61; `RequiresPolicyAttributeTests` 12/12; `eng/check-mutation-triage.py` reports no failures; `eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` passed.

## Review Triage Log

- medium, patched as documentation — under invariant globalization `IsNormalized` reports every string as normalized (probe-verified), so the check is skipped there. The XML doc, the generator comment and `HFC1056.md` now state the ICU requirement. Switching to ASCII-only names would break the existing non-ASCII contract.
- low, patched — `HFC1056.md` now explains that a combining mark fails the character rule and how to fix it; the sealed parser message cannot change here.
- low, patched — the echo no longer truncates, so the received and expected names always differ.
- low, patched — removed the unreachable control-character branch from the escape helper.
- low, patched — the message now follows the Expected/Got/Fix shape.
- low, rejected — the normalization error appears once the parser's own model errors are fixed; surfacing it earlier would duplicate parser work.
- low, patched — the attribute XML doc says the rule spans the parser and the generator, and the attribute tests now cover the decomposed Hangul and padded cases.
- low, patched — accepted cases assert that the quoted policy name is emitted verbatim.
- low, patched — the docs say matching is ordinal and case-insensitive without normalization, and that the host registration must be normalized too.
- medium, patched — the Migration section now records the breaking change.
- low, patched — compatibility characters that are already normalized are documented as out of scope.
- false — the ledger entry is closed when the change is finalized.
- low, rejected — the constructor message lists the whole contract, as it already did for the other rules.
