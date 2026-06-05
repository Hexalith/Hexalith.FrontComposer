# FC-DIAGNOSTICS-CATALOG v1 Contract

Story: 7.3 - Surface the HFC diagnostic catalog

Date: 2026-06-05

Status: implemented for review

## Scope

This contract records the v1 FrontComposer diagnostic-catalog surface for HFC diagnostics that are governed under `docs/diagnostics`, surfaced through SourceTools build diagnostics, and consumed by `frontcomposer inspect` sidecar reporting.

The contract does not introduce a second production catalog table. The authoritative machine-readable source remains `docs/diagnostics/diagnostic-registry.json`.

## Authoritative Sources

| Surface | Authority |
| --- | --- |
| Registry/governance rows | `docs/diagnostics/diagnostic-registry.json` |
| Registry rules and stub contract | `docs/diagnostics/README.md` |
| Symbolic diagnostic constants | `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` |
| Roslyn analyzer descriptors | `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` |
| Roslyn release tracking | `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` |
| Published docs stubs | `docs/diagnostics/HFC*.md` |
| CLI sidecar reader and filtering | `src/Hexalith.FrontComposer.Cli/InspectCommand.cs` |

## Severity Rules

For active or reserved HFC1001-HFC1070 SourceTools descriptor rows, the following values must agree:

- `DiagnosticDescriptor.DefaultSeverity`
- `AnalyzerReleases.Unshipped.md` severity column
- `docs/diagnostics/diagnostic-registry.json` `compilerSeverity`
- `docs/diagnostics/HFCxxxx.md` front-matter `severity`

`Error` descriptors are build-breaking under normal Roslyn analyzer/source-generator behavior. `Warning` and `Info` descriptors are non-error diagnostics by default, but project builds that set `TreatWarningsAsErrors=true` can promote warnings according to normal compiler rules.

Runtime channels are independent. `runtimeLogLevel`, `panelSeverity`, `cliExitBehavior`, and `mcpCategory` are registry-owned channel fields and must not be inferred from `compilerSeverity` unless the registry row explicitly maps both channels.

## Lifecycle And Phase Semantics

Registry lifecycle values have these meanings:

| Lifecycle | Meaning |
| --- | --- |
| `active` | The ID is available for the channel(s) declared by the registry row. |
| `reserved` | The ID is allocated but not yet emitted in production for the declared future behavior. |
| `deprecated` | The ID remains recognized and points to migration/deprecation guidance. |
| `retired` | The ID is not emitted and must not be reused. |
| `removed-in-major` | The ID belongs to removed behavior and must only appear as compatibility or migration evidence. |

Story 7.3 records channel consistency; it does not prove that every cataloged descriptor has a production parser or emitter call site.

## Known Phase Caveats

HFC1038-HFC1041 remain honest as Level 3 customization diagnostics with current startup/runtime or call-site evidence. Prior Epic 6 evidence records that build-time SourceTools emission for Level 3 slot registrations is not implemented or proven today.

HFC1042-HFC1046 remain honest as Level 4 customization diagnostics with current reserved/catalog/startup/runtime evidence. Prior Epic 6 evidence records that build-time SourceTools emission for Level 4 registrations is not implemented or proven today; HFC1046 overlaps the Story 6.4 HFC1050-HFC1055 accessibility analyzer lane and must not be claimed as proven Level 4 build-time emission.

HFCM9002 remains a synthetic sidecar evidence path from Story 7.2. There is no production SourceTools sidecar emitter for HFCM9002 yet, so Story 7.3 does not claim adopter builds produce that migration diagnostic sidecar.

## Inspect Filtering Semantics

`frontcomposer inspect --severity <level>` applies threshold semantics after sidecars are read and before `--type`, `--fail-on-warning`, and `--fail-on-error` are evaluated:

| Requested level | Included severities |
| --- | --- |
| `hidden` | Hidden, Info, Warning, Error |
| `info` | Info, Warning, Error |
| `warning` | Warning, Error |
| `error` | Error |

Invalid severity values return `ExitCodes.InvalidArguments` (`2`).

Sidecar diagnostics retain sanitized `id`, `severity`, `relatedType`, `path`, `what`, `expected`, `got`, `fix`, and `docsLink` fields. Non-HFC compiler diagnostics are ignored. Malformed or unreadable sidecars emit the deterministic warning sentinel `HFCM0002`. Absolute host paths, URI paths, drive-qualified paths, traversal, and control characters must not leak into text or JSON output.

## Verification Evidence

Story 7.3 added or updated these focused pins:

- `InspectSeverity_UsesThresholdSemanticsInJsonOutput`
- `InspectSeverity_UsesThresholdSemanticsInTextSummary`
- `InspectSeverity_InvalidValue_ReturnsInvalidArguments`
- `InspectFailFlags_AreEvaluatedAfterSeverityAndTypeFiltering`
- `SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned`

Final build and test results are recorded in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
