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
`frontcomposer inspect` reports generated-output paths, project metadata, schema fingerprints, diagnostics, and machine-readable JSON for automation.

`frontcomposer migrate` supports dry-run and apply modes. Dry-run emits a report without changing source. Apply mode writes bounded, project-relative edits and records migration findings.

Exit codes:

| Code | Meaning |
| --- | --- |
| 0 | Command completed successfully. |
| 1 | Explicit fail flag promoted findings to non-zero: `--fail-on-findings`, `--fail-on-warning`, or `--fail-on-error`. |
| 2 | Invalid, unsupported, or ambiguous input. |
| 3 | Generated output unavailable for `inspect`; build the selected project or pass the correct configuration/framework. |
| 4 | Filesystem, cancellation, workspace setup, or apply/write failure. |

Report schemas must use project-relative forward-slash paths and redact usernames, tenant identifiers, secrets, absolute private paths, raw exceptions, and terminal control sequences.

Selection and paths:

| Surface | Contract |
| --- | --- |
| `--project` | Must resolve to an existing `.csproj`; `.fsproj` is unsupported in v1. |
| `--solution` | Must resolve to a `.sln` with exactly one supported `.csproj`; `.slnx`, malformed entries, unsupported project types, and multiple candidates fail closed. |
| discovery | With no explicit path, only a single `.csproj` in the current directory is accepted. |
| migration writes | Generated output, `bin`, `obj`, package caches, submodule paths, outside-root links, and changed targets are refused before mutation and revalidated immediately before write. |

`inspect --fail-on-warning` is stricter than `--fail-on-error`; when both are supplied, warnings or errors return exit code 1. JSON consumers can read warning/error counts from `summary` but should treat the process exit code as the applied fail behavior.

`migrate --format json` emits `schemaVersion`, `applied`, `summary.changed`, `summary.unchanged`, `summary.skipped`, `summary.failed`, `summary.manualOnly`, `summary.conflicts`, and `entries[]` containing `diagnosticId`, `kind`, `path`, `what`, `expected`, `got`, `fix`, `docsLink`, `diff`, and `formattingApplied`. `applied` is true only when apply mode completed and every planned write succeeded. Unified diffs are terminal-safe informational output, not a patch-applicability guarantee.

Migration source reads support UTF-8, UTF-8 BOM, UTF-16 LE/BE, and UTF-32 LE/BE. Unknown encodings and files larger than 16 MiB fail closed with sanitized guidance. Ctrl+C requests cancellation; a second Ctrl+C restores default process termination behavior. SIGTERM-specific handling remains outside the v1 local developer workflow contract.
<!-- hfc:reference:end -->

See [migration guidance](../how-to/migration-guides.md), [generated-output paths](generated-output.md), and Story 9-2 migration stubs under [`docs/migrations`](../migrations/index.md).
