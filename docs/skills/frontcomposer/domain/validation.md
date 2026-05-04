---
id: domain-validation
title: Validation rules
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/domain/validation
order: 50
sourceDoc: docs/skills/frontcomposer/domain/validation.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Contracts.Communication.CommandValidationException, Hexalith.FrontComposer.Contracts.Communication.EventStoreValidation]
---
<!-- frontcomposer:section narrative -->
# Validation

This docs section can teach validation design.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Validation Rules

Validate command inputs before submission. Keep validation in domain/application code and tests; do not bypass validation or authorization to make examples compile. Agent-authored commands must not add tenant, user, claims, token, or policy decision fields.
<!-- /frontcomposer:section -->
