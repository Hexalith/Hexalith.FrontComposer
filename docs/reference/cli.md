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
| 0 | Command completed without blocking findings. |
| 1 | Validation, IO, restore, or migration failure. |
| 2 | Dry-run found migration work that requires review before apply. |

Report schemas must use project-relative forward-slash paths and redact usernames, tenant identifiers, secrets, absolute private paths, raw exceptions, and terminal control sequences.
<!-- hfc:reference:end -->

See [migration guidance](../how-to/migration-guides.md), [generated-output paths](generated-output.md), and Story 9-2 migration stubs under [`docs/migrations`](../migrations/index.md).
