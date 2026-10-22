---
title: "FrontComposer Migration Guides"
description: "FrontComposer migration guide index for versioned breaking-change and skill-corpus evidence."
genre: how-to
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-07-11
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
| 4.5.0 | 4.6.0 | manual-only | [4.5 to 4.6](4.5-to-4.6.md) |
| 3.1.1 | 4.0.0 | manual-only | [3.1 to 4.0](3.1-to-4.0.md) |
| 9.1.0 | 9.2.0 | cli-supported | [9.1 to 9.2](9.1-to-9.2.md) |
| 1.12.0 | 2.0.0 | manual-only | [1.12 to 2.0](1.12-to-2.0.md) |
