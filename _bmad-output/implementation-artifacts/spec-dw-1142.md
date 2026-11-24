---
title: 'DW-1142 Bound LastUsed storage key length'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Cap LastUsed storage keys at a documented maximum. Keys within the cap keep the existing format; longer keys use a versioned format that hashes oversized segments. Values stored under the old unbounded key must stay readable until the next record replaces them, with boundary and collision tests.

</frozen-after-approval>

## Implementation Notes

- `FrontComposerStorageKey.MaxKeyLength` is 512. A key over that length uses `frontcomposer:lastused:v2:...`, where a segment longer than 96 characters or starting with `!` becomes `!` plus its lowercase SHA-256 hex digest. The longest bounded key is 382 characters. `Build` now rejects `:` in the verbatim command and property segments, so the four-segment legacy and five-segment bounded shapes cannot collide.
- `BuildWithLegacyFallback` (internal) returns the bounded key and the legacy key it replaced. `LastUsedValueProvider` reads the legacy key only when the bounded key is empty. `Record` writes the bounded key before removing the legacy key, and clears the legacy key before the bounded key.
- Tests: `FrontComposerStorageKeyTests` (16 facts, including a hard-coded golden key and ordered storage calls); the FsCheck round-trip property expects hashed segments for oversized inputs.

## Review Triage Log

- medium, patched — a `:` in the command or property let a legacy key equal a bounded key or parse as one; `Build` now rejects `:` there.
- low, rejected — `TryParse` does not report the format; it has no production caller, and adding a field changes its public return type.
- low, patched — the `TryParse` segment comment and the `BuildWithLegacyFallback` return doc were out of date.
- low, rejected — the extra legacy remove and read cost storage calls only for keys over 512 characters, which the recorded decision keeps on purpose.
- low, patched — the provider remarks now describe the fallback read, the migration, and that an earlier release cannot read migrated values; clearing removes the legacy key first.
- medium, patched — the "worst case" test built the shortest bounded key; added a 382-character longest-key test and a hard-coded golden key that pins 512, 96, `!` and `v2`.
- low, patched — the migration tests now use `Received.InOrder`.
- low, patched — added a provider test where a nested generic command name alone triggers the bounded key and both keys are read once.
- low, patched — the look-alike test now builds both keys with the same tenant, user and property and asserts they differ.
- low, patched — the FsCheck helper explains why only the length rule applies to URL-encoded segments.
