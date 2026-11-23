---
title: "CLI inspect and migrate"
description: "Reference for FrontComposer CLI inspect and migrate behavior, outputs, and exit-code semantics."
genre: reference
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-11-20
uid: frontcomposer.reference.cli
slug: reference/cli/
---

# CLI inspect and migrate

<!-- hfc:reference:start -->
`frontcomposer inspect` reports generated-output paths, project metadata, diagnostics, and
machine-readable JSON. It classifies generated filenames; v1 does not extract schema fingerprints
or parse descriptors inside manifest source. The output root is `obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/`.
The CLI reads generated `.g.cs` and `.g.razor.cs` files directly under that root and under Roslyn’s
`Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.FrontComposerGenerator/`
subdirectory. If both contain the same filename, the compiler-emitted copy is used.
With `--build`, the CLI can infer a single target framework from the project. Supply `--framework`
to select among multiple targets. Pass a single MSBuild configuration name such as `Debug` or
`Release` to `--configuration`. Paths and MSBuild property separators are rejected with exit code 2.
Other generators are excluded.

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
Global MCP/template manifests and diagnostics remain in type-filtered output. Global diagnostics,
including the `HFCM0002` warning, report a null `relatedType` in JSON.
`--severity` filters diagnostics by minimum severity: `hidden` keeps all, `info` (or `information`)
keeps Info/Warning/Error, `warning` keeps Warning/Error, and `error` keeps Error.
Both severity and type filters run before fail flags.

Top-level `*.diagnostics.json` sidecars accept arrays or `{ "diagnostics": [] }` documents.
Only IDs starting with `HFC` are reported. Missing optional string fields other than `relatedType`
become empty strings. Malformed entries, unsupported JSON shapes, and per-file read errors produce
one `HFCM0002` warning per affected sidecar. Readable findings are still included. If the generated
directory cannot be enumerated, inspection returns exit code 3. Unsafe sidecar paths are redacted.
`--absolute-paths` opts generated-file paths into absolute output. Project and sidecar paths retain
their relative or redacted form.

`frontcomposer migrate` requires explicit `--from` and `--to` versions. Use the
[classified index](../migrations/index.md) to find and select executable CLI-supported edges and manual-only upgrades.
Dry-run is the default (`--dry-run` makes it explicit) and writes no source. `--apply` atomically
replaces each eligible file through a same-directory temporary file after path/hash revalidation.
Earlier successful writes remain if a later file fails or cancellation occurs; there is no whole-run
rollback. Handled apply failures return code 4 and set `applied=false` on emitted reports.
Cancellation during planning can return code 4 before a report is emitted. If cancellation
stops apply, the report lists completed writes and a failed entry for each planned file that
was left unwritten. A plan that skips every discovered document reports the skipped documents
without an unchanged entry.

Migration sidecars accept arrays of diagnostic objects or objects containing a `diagnostics`
array. Malformed or unreadable payloads produce one manual-only sentinel per sidecar while
valid sibling entries are still reported. An inaccessible sidecar directory produces a
manual-only sentinel for `obj`. A valid diagnostic whose source was missing or excluded is reported
as a manual finding with guidance to regenerate the diagnostics for the selected project.

Exit codes:

| Code | Meaning |
| --- | --- |
| 0 | Command completed successfully. |
| 1 | Explicit fail flag promoted findings to non-zero: `--fail-on-findings`, `--fail-on-warning`, or `--fail-on-error`. |
| 2 | Invalid, unsupported, or ambiguous input. |
| 3 | Generated output unavailable for `inspect`; build the selected project or pass the correct configuration/framework. |
| 4 | Filesystem, cancellation, workspace setup, or apply/write failure. |

Report path fields default to project-relative forward slashes; unsafe report paths are redacted.
Handled CLI failures print messages without stack traces. Handled filesystem failures print exception messages with escaped control characters; those messages can include absolute filesystem paths. Most are sanitized and bounded, but
filesystem exception messages may still contain private absolute paths, and unsupported project
extensions read from a `.sln` file are echoed without escaping or a length budget. Handled filesystem failures print exception messages with escaped control characters; those messages can
include absolute filesystem paths. Single-line fields escape C0 controls
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
| migration writes | Generated output, `bin`, `obj`, `.git`, package caches, submodule and other nested repository paths (any directory with a `.git` marker), outside-root targets, file symlinks, linked ancestor directories/junctions below the project directory, linked ancestor directories/junctions below the selected project directory, and changed targets are refused before mutation and revalidated immediately before write. |

`inspect --fail-on-warning` is stricter than `--fail-on-error`; when both are supplied, warnings or errors return exit code 1. JSON consumers can read warning/error counts from `summary` but should treat the process exit code as the applied fail behavior.

When a report is emitted, `migrate --format json` emits `schemaVersion: frontcomposer.cli.migrate.v1`, `applied`, `summary.changed`, `summary.unchanged`, `summary.skipped`, `summary.failed`, `summary.manualOnly`, `summary.conflicts`, and `entries[]` containing `diagnosticId`, `kind`, `path`, `what`, `expected`, `got`, `fix`, `docsLink`, `diff`, and `formattingApplied`. `applied` is true only when apply mode completed and every planned write succeeded. Unified diffs are bounded informational output, not a patch-applicability guarantee.

Migration source reads support UTF-8, UTF-8 BOM, UTF-16 LE/BE, and UTF-32 LE/BE. Unknown encodings and files larger than 16 MiB fail closed with sanitized guidance. Ctrl+C requests cancellation; a second Ctrl+C restores default process termination behavior. SIGTERM-specific handling remains outside the v1 local developer workflow contract.
<!-- hfc:reference:end -->

See [migration guidance](../how-to/migration-guides.md), [generated-output paths](generated-output.md), and versioned migration guides under [`docs/migrations`](../migrations/index.md).
