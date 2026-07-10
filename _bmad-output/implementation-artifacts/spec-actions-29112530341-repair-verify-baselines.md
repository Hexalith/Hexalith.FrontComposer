---
title: 'Repair release Verify baselines'
type: 'bugfix'
created: '2026-07-10'
status: 'done'
route: 'one-shot'
---

# Repair release Verify baselines

## Intent

**Problem:** Release run 29112530341 fails because five committed Verify snapshots retain legacy byte endings after the Verify dependency update, even though their displayed payloads still match.

**Approach:** Regenerate only the stale baselines with the pinned Verify version, preserve their payloads, and validate every release test project plus the release evidence parser.

## Suggested Review Order

**Shell persistence baseline**

- Align the navigation wire-format lock with Verify's current UTF-8 snapshot representation.
  [`NavigationPersistenceSnapshotTests.BlobSchemaLocked.verified.txt:1`](../../tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationPersistenceSnapshotTests.BlobSchemaLocked.verified.txt#L1)

**Source generator baselines**

- Preserve the basic projection model while adopting Verify's current file ending.
  [`AttributeParserTests.Parse_BasicProjection_ProducesCorrectIR.verified.txt:1`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_BasicProjection_ProducesCorrectIR.verified.txt#L1)

- Preserve record projection parsing while adopting Verify's current file ending.
  [`AttributeParserTests.Parse_RecordProjection_ProducesCorrectIR.verified.txt:1`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_RecordProjection_ProducesCorrectIR.verified.txt#L1)

- Preserve global-namespace parsing while adopting Verify's current file ending.
  [`AttributeParserTests.Parse_GlobalNamespaceProjection_HandlesEmptyNamespace.verified.txt:1`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_GlobalNamespaceProjection_HandlesEmptyNamespace.verified.txt#L1)

- Preserve multi-attribute parsing while adopting Verify's current file ending.
  [`AttributeParserTests.Parse_MultiAttributeProjection_ExtractsBoundedContextAndRole.verified.txt:1`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_MultiAttributeProjection_ExtractsBoundedContextAndRole.verified.txt#L1)
