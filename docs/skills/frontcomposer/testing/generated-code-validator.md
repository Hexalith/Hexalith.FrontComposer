---
id: testing-generated-code-validator
title: Generated code validator
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/testing/generated-code-validator
order: 100
sourceDoc: docs/skills/frontcomposer/testing/generated-code-validator.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Mcp.Skills.GeneratedBoundedContextValidator]
---
<!-- frontcomposer:section narrative -->
# Generated Code Validator

Human docs can explain benchmark scoring.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Generated Code Validator

Run static project-shape admission before compile. Reject custom MSBuild targets, `Exec` tasks, unapproved package references, generated-file edits, local path imports, post-build hooks, and package-source mutation before any compile step.

Stable failure categories include compile, package-boundary, missing-registration, invalid-attribute, validation-shape, tenant-spoofing, generated-file-edit, test-scaffold, SourceTools-manifest, and unknown.
<!-- /frontcomposer:section -->
