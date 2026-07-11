---
title: "Apply migration guidance"
description: "Use migration-guide requirements when FrontComposer APIs or shipped skill corpus examples change."
genre: how-to
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-07-11
uid: frontcomposer.how-to.migration-guides
slug: how-to/migration-guides/
fromVersion: "9.1.0"
toVersion: "9.2.0"
diagnosticId: "HFCM9001"
skillCorpusImpact: "review-required"
codeFixAvailable: false
---

# Apply migration guidance

Migration pages are required when a shipped skill corpus example breaks, even when the semantic-version bucket is minor.

Old code:

```csharp no-compile reason="Obsolete API shown only as migration input."
services.AddFrontComposerDebugOverlay();
```

New code:

```csharp no-compile reason="Host setup snippet requires the application service collection."
services.AddFrontComposerDevMode();
```

Why it changed: the CLI migration contract moved migration reports to dry-run/apply semantics, and the HFC diagnostic governance contract reserves the related rows.

Affected packages: `Hexalith.FrontComposer.Cli`, `Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer.Shell`.

Analyzer/code-fix availability: no automatic code fix is shipped for this v1 page; the CLI migration report records the finding and apply step.

Skill-corpus evidence: `docs/skills/frontcomposer/migration/versioned-corpus-rules.md` remains the producer input.

Related links: [CLI inspect and migrate](../reference/cli.md), [diagnostics](../reference/diagnostics/index.md), and [migration stubs](../migrations/9.1-to-9.2.md).

For the Contracts/Contracts.UI package split, runtime/testing ownership moves, and composed-query
migration, use the [FrontComposer 1.12 to 2.0 guide](../migrations/1.12-to-2.0.md). That major-version
guide is manual: HFC0001 identifies flattened query usage, while package/namespace moves require an
explicit reference and source audit.
