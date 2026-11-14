---
title: "FrontComposer Migration Guides"
description: "FrontComposer migration guide index for versioned breaking-change and skill-corpus evidence."
genre: how-to
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-11-14
uid: frontcomposer.migrations.index
slug: migrations/
---

# FrontComposer Migration Guides

The private CLI migration catalog is authoritative for executable version edges. Pass exact
`--from` and `--to` versions: unknown, reversed, skipped, or ambiguous requests fail closed.
`migrationMode: cli-supported` identifies executable guidance; `migrationMode: manual-only`
identifies package/API upgrades that require manual reference, namespace, and source changes.
The classifications below match each guide's frontmatter. Version labels in filenames may be
shortened; use the exact versions in this table and the guide metadata when planning an upgrade.

| fromVersion | toVersion | migrationMode | Guide |
| --- | --- | --- | --- |
| 5.0.0 | 6.0.0 | manual-only | [5.0 to 6.0](#50-to-60) |
| 4.7.6 | 5.0.0 | manual-only | [4.7 to 5.0](4.7-to-5.0.md) |
| 4.5.0 | 4.6.0 | manual-only | [4.5 to 4.6](4.5-to-4.6.md) |
| 3.1.1 | 4.0.0 | manual-only | [3.1 to 4.0](3.1-to-4.0.md) |
| 9.1.0 | 9.2.0 | cli-supported | [9.1 to 9.2](9.1-to-9.2.md) |
| 1.12.0 | 2.0.0 | manual-only | [1.12 to 2.0](1.12-to-2.0.md) |

## 5.0 to 6.0

This is a manual package/framework upgrade; the CLI does not execute this edge. Install the .NET 11 SDK (this repository pins **11.0.100**) on developer machines and CI runners, update consumer `global.json` pins, and retarget runtime applications and libraries from `net10.0` to `net11.0`. Deploy the .NET 11 runtime or matching ASP.NET Core runtime wherever those applications run.

Upgrade every FrontComposer package reference used by a solution together to **6.0.0**, including Contracts, Contracts.UI, Schema, Shell, Mcp, Testing, SourceTools, and the CLI tool where used. Keep analyzer/kernel consumers on the retained `netstandard2.0` faces; the SDK migration does not require converting those consumers to a runtime-only framework. Restore, rebuild, and run the consumer's tests against the coordinated package set, then verify its UI and host behavior before rollout.

Release qualification compares 6.0.0 against published 5.0.0. The [compatibility policy](../diagnostics/README.md) records 28 exact enum metadata comparisons and four exact net10.0 framework drops, bounded to v6.0 and expiring at v6.1. Package checks and direct net10.0-to-net11.0 runtime API comparisons remain active; these framework records do not approve unrelated API removals. The release owner must advance the baseline and retire the migration records after publication.
