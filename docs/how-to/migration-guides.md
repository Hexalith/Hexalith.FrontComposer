---
title: "Apply migration guidance"
description: "Use migration-guide requirements when FrontComposer APIs or shipped skill corpus examples change."
genre: how-to
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-05-10
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

Why it changed: Story 9-2 moved CLI migration reports to dry-run/apply semantics and Story 9-4 reserved the related HFC governance rows.

Affected packages: `Hexalith.FrontComposer.Cli`, `Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer.Shell`.

Analyzer/code-fix availability: no automatic code fix is shipped for this v1 page; the CLI migration report records the finding and apply step.

Skill-corpus evidence: `docs/skills/frontcomposer/migration/versioned-corpus-rules.md` remains the producer input.

Related links: [CLI inspect and migrate](../reference/cli.md), [diagnostics](../reference/diagnostics/index.md), and [migration stubs](../migrations/9.1-to-9.2.md).
