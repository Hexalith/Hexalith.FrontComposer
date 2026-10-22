---
title: "CLI inspect and migrate"
description: "Reference for FrontComposer CLI inspect and migrate behavior, outputs, and exit-code semantics."
genre: reference
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-05-10
uid: frontcomposer.reference.cli
slug: reference/cli/
---

# CLI inspect and migrate

<!-- hfc:reference:start -->
`frontcomposer inspect` reports generated-output paths, project metadata, diagnostics, and
machine-readable JSON. It classifies generated filenames; v1 does not extract schema fingerprints
or parse descriptors inside manifest source. It reads top-level generated `.g.cs`/`.g.razor.cs`
files in `obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/`.

`inspect --format json` emits `schemaVersion: frontcomposer.cli.inspect.v1`, `project`,
`summary`, `generatedFiles[]`, and `diagnostics[]`. The summary counts generated files, forms
(`CommandForm`), grids (`ProjectionRazor`), registrations (`Registration`), MCP manifest files
(`mcpManifestEntries`), warnings, and errors. `mcpManifestEntries` counts files named
`FrontComposerMcpManifest.g.cs`, not their internal command/resource descriptors. The projection
template registration is `TemplateManifest`; other families include command renderers, pages,
lifecycle bridges, last-used subscribers, Fluxor features, actions, reducers, and unknown files.

Text and JSON share ordinal file ordering by related type, family name, then path. Diagnostics
are ordered ordinally by ID, then path. Counts reflect the filtered report. `--type` accepts an
exact metadata name or an unambiguous simple name; unknown or ambiguous names fail with code 2.
Global MCP/template manifests and diagnostics with null related type, such as `HFCM0002`, remain
in type-filtered output. Ordinary sidecar diagnostics without `relatedType` receive an empty
string and are excluded by `--type`.
`--severity` filters diagnostics by minimum severity: `hidden` keeps all, `info` (or `information`)
keeps Info/Warning/Error, `warning` keeps Warning/Error, and `error` keeps Error.
Both severity and type filters run before fail flags.

Top-level `*.diagnostics.json` sidecars accept arrays or `{ "diagnostics": [] }` documents.
Only IDs starting with `HFC` are reported. Missing optional string fields become empty strings;
handled malformed-JSON or read failures produce the warning sentinel `HFCM0002`. Only the
documented shapes with diagnostic object entries are supported. Unsupported valid JSON shapes,
such as scalar roots or non-object diagnostic entries, may throw outside handled failures without
a structured report or the documented exit-code guarantees. Unsafe sidecar paths are redacted. `--absolute-paths` explicitly opts generated-file paths into absolute output;
project and sidecar paths retain their relative/redacted behavior.

`frontcomposer migrate` requires explicit `--from` and `--to` versions. Use the
[classified index](../migrations/index.md) to find executable edges and manual-only upgrades.
Dry-run is the default (`--dry-run` makes it explicit) and writes no source. `--apply` atomically
replaces each eligible file through a same-directory temporary file after path/hash revalidation.
Earlier successful writes remain if a later file fails or cancellation occurs; there is no whole-run
rollback. Handled apply failures return code 4 and set `applied=false` on emitted reports.
Cancellation can return code 4 before any report is emitted; interrupted output may leave an
absent or partial report. Consumers must not require `applied` when no complete report is emitted.
Migration sidecars also require the documented diagnostic-object shapes: arrays of objects or
objects containing a `diagnostics` array of objects. Unsupported valid JSON shapes, including
scalar roots or non-object diagnostic entries, may throw outside handled failures without a
structured report or the documented exit-code guarantees.

Exit codes:

| Code | Meaning |
| --- | --- |
| 0 | Command completed successfully. |
| 1 | Explicit fail flag promoted findings to non-zero: `--fail-on-findings`, `--fail-on-warning`, or `--fail-on-error`. |
| 2 | Invalid, unsupported, or ambiguous input. |
| 3 | Generated output unavailable for `inspect`; build the selected project or pass the correct configuration/framework. |
| 4 | Filesystem, cancellation, workspace setup, or apply/write failure. |

Report path fields default to project-relative forward slashes; unsafe report paths are redacted.
Handled CLI failures print bounded messages without stack traces, but filesystem exception
messages may still contain private absolute paths. Single-line fields escape C0 controls
(`U+0000`–`U+001F`) and DEL (`U+007F`), normally with a 240-character content budget before
truncation markers. Multiline migration diffs retain carriage returns, line feeds, and tabs; they
escape other C0 controls and DEL. C1 controls
(`U+0080`–`U+009F`) remain unchanged. Diffs have an 8,000-character per-entry and 64,000-character
aggregate budget with truncation/omission markers. These protections do not
semantically scrub arbitrary usernames, tenant values, or secrets embedded in free-form sidecar
messages or source diffs. Review captured output before sharing it.

Selection and paths:

| Surface | Contract |
| --- | --- |
| `--project` | Must resolve to an existing `.csproj`; `.fsproj` is unsupported in v1. |
| `--solution` | Must resolve to a `.sln` with exactly one supported `.csproj`; `.slnx`, malformed entries, unsupported project types, and multiple candidates fail closed. |
| discovery | With no explicit path, only a single `.csproj` in the current directory is accepted. |
| migration writes | Generated output, `bin`, `obj`, `.git`, package caches, submodule paths, outside-root targets, file symlinks, linked ancestor directories/junctions below the selected project directory, and changed targets are refused before mutation and revalidated immediately before write. |

`inspect --fail-on-warning` is stricter than `--fail-on-error`; when both are supplied, warnings or errors return exit code 1. JSON consumers can read warning/error counts from `summary` but should treat the process exit code as the applied fail behavior.

When a report is emitted, `migrate --format json` emits `schemaVersion: frontcomposer.cli.migrate.v1`, `applied`, `summary.changed`, `summary.unchanged`, `summary.skipped`, `summary.failed`, `summary.manualOnly`, `summary.conflicts`, and `entries[]` containing `diagnosticId`, `kind`, `path`, `what`, `expected`, `got`, `fix`, `docsLink`, `diff`, and `formattingApplied`. `applied` is true only when apply mode completed and every planned write succeeded. Unified diffs are bounded informational output, not a patch-applicability guarantee.

Migration source reads support UTF-8, UTF-8 BOM, UTF-16 LE/BE, and UTF-32 LE/BE. Unknown encodings and files larger than 16 MiB fail closed with sanitized guidance. Ctrl+C requests cancellation; a second Ctrl+C restores default process termination behavior. SIGTERM-specific handling remains outside the v1 local developer workflow contract.
<!-- hfc:reference:end -->

See [migration guidance](../how-to/migration-guides.md), [generated-output paths](generated-output.md), and versioned migration guides under [`docs/migrations`](../migrations/index.md).
