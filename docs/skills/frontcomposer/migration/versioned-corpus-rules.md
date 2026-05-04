---
id: migration-versioned-corpus-rules
title: Versioned corpus migration rules
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/migration/versioned-corpus-rules
order: 110
sourceDoc: docs/skills/frontcomposer/migration/versioned-corpus-rules.md
narrative: true
references: true
migrationOwner: Story 9-5
publicApiReferences: [Hexalith.FrontComposer.Mcp.Skills.SkillCorpusReleaseGuard]
---
<!-- frontcomposer:section narrative -->
# Versioned Corpus Rules

Human docs can link release notes and migration guides.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Versioned Corpus Migration Rules

Skill examples are compatibility artifacts. If a framework change breaks a shipped skill example, include a migration guide reference, old/new example, analyzer or fix-it owner where applicable, and the corpus update in the same change.

Story 8-5 provides stable corpus IDs and manifest schema version metadata only. Schema fingerprints, negotiation, and migration deltas belong to Story 8-6.
<!-- /frontcomposer:section -->
