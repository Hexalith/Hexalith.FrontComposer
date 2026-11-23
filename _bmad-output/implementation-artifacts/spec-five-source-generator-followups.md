---
title: 'Close five SourceTools generator follow-ups'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '2de5098e4a3a9bfaae5298d56c2a6045b148285e'
context: []
closes_deferred: [DW-1947, DW-1948, DW-1949, DW-1952, DW-1981]
---

<frozen-after-approval reason="Five open SourceTools findings selected from the FrontComposer backlog">

## Intent

Keep obsolete-warning suppression local to typed command prefill. Guard generated grid and non-grid projection callbacks before scheduling renderer work and when scheduled work runs. Verify that render-tree generation rejects both prefix and postfix decrement sequence arguments. Document all three HFC1016 message variants and explain where the diagnostic appears for an inherited metadata member. Make generated form fields and projection columns fall back to their humanized names when `[Display(Name)]` is blank, and trim a nonblank override.

</frozen-after-approval>

## Implementation Notes

The command renderer brackets `TrySetPropertyValue` with the obsolete-warning directives when typed assignments are present. Projection state callbacks check the existing disposal flag before dispatch and again before the queued work runs. The command form and projection transforms trim explicit field display names and use the existing humanized fallback for blank values. The render-tree regression exercises both decrement forms, and the HFC1016 reference matches the parser's three messages and source-location fallback.

## Review Triage Log

- Patched the disposal fixture to execute the emitted handler on a live view, with queued work during disposal, and after disposal. The source assertion continues to cover the grid and non-grid emission paths.
- Patched the sequence test to initialize a counter and give the rewriter a normal increment alongside the invalid form. Both decrement cases must survive as identifiable failures.
- Patched the warning test to compile an unrelated obsolete use in the generated source. The typed prefill compiles under warnings as errors, and the unrelated reference produces CS0618.
- Patched the provider-only renderer test to assert the absence of both obsolete-warning directives and the conversion helper.
- Patched the generator-driver fixture to check the parsed display attributes and emitted form localization flags. The projection checks the emitted header constants, including trimming an outer Unicode whitespace character.
- Patched the HFC1016 documentation test to require all three variants and the metadata location guidance.
- Added the canonical File List and recorded the verification results below.

## Verification

- The SourceTools test project built in Debug and Release on SDK 11.0.100 with zero warnings or errors.
- The focused Debug run passed 248 cases. After review, 13 targeted cases passed on the final test code, including the emitted teardown handler, obsolete-warning scope, display attributes, decrement arguments, and HFC1016 documentation.
- The serialized Release SourceTools suite ran 1,695 cases against the final generator changes. It passed 1,693, including the packaged-analyzer consumer and both drift benchmarks. `MigrationGuidanceDescribesCliSafeFixAndSyntheticManualBoundary` and `CliReferencePinsInspectCountsOrderingFilteringAndPathOptIn` failed against CLI docs and assertions inherited from `main`.
- `python3 -m unittest tests/eng/test_validate_contract_artifacts.py` passed all four cases. `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` and the BMAD story artifact validator passed.
- `aspire run` could not start because the Aspire CLI is unavailable in this environment. The generator was exercised through its test projects and the packaged consumer build.

## File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-source-generator-followups.md`
- `docs/diagnostics/HFC1016.md`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandFormTransform.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_FiveFields_FullPageBoundarySnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_FourFields_CompactInlineBoundarySnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_InlinePopoverSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_WithIconAttributeSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_WithoutIconUsesDefaultSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_TwoFields_CompactInlineSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_ZeroFields_InlineSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.BasicProjection_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.DescriptionWithEscapeEdgeCases_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.DisplayNameOverrides_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.EnumAndBadgeMappings_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.GuidTruncation_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.NullableProperties_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueNoEnumProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DashboardProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DashboardWrongShapeProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DetailRecordProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.StatusOverviewProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.TimelineProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.WhenStateTypoProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CommandRendererStaticAssignmentTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/CommandFormTransformTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RazorModelTransformTests.cs`
