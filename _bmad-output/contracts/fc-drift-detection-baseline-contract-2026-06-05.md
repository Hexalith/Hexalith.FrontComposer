# FC-DRIFT-DETECTION-BASELINE v1 Contract

Story: 7.4 - Opt-in drift detection vs. a baseline

Date: 2026-06-05

Status: implemented for review

## Scope

This contract records the v1 build-time generated-UI drift detection surface for SourceTools. Drift detection is opt-in, diagnostics-only, and compares current generated projection/command surface metadata against trusted checked-in baseline `AdditionalText` files.

The contract does not introduce a baseline-generation CLI, code fix, public drift API, generated-code rewrite path, or `System.CommandLine` dependency. Baseline authoring remains outside Story 7.4.

## Baseline Schema

| Field | Contract |
| --- | --- |
| Schema version | `frontcomposer.generated-ui-baseline.v1` |
| Algorithm | `frontcomposer-structural-v1` |
| Accepted file names | `frontcomposer.drift-baseline*.json`, `frontcomposer.generated-ui-baseline*.json` |
| Contract families | `projection`, `command`, `boundedContext` |
| Default declaration cap | `512` contracts per baseline file |
| Default property cap | `256` properties per declaration |
| Default baseline byte cap | `256 KiB` |
| Configurable byte cap | `HfcDriftMaxBaselineBytes`, range `1..10485760` |

Baseline contract identity is `family|type|boundedContext`; duplicate identities fail closed. Property identity is name-based with ordinal-ignore-case duplicate rejection during trust validation.

## MSBuild Options

| Option | Meaning |
| --- | --- |
| `HfcDriftDetectionEnabled` | Primary opt-in. Only `true` enables baseline loading and comparison. |
| `FrontComposerDriftDetectionEnabled` | Backward-compatible opt-in alias. |
| `HfcDriftBaselinePath` | Optional path selector. Must segment-align with one supplied candidate `AdditionalText`; otherwise HFC1059 fails closed. |
| `HfcDriftMaxDiagnostics` | Caps emitted drift/load diagnostics. Range `1..500`; default `50`. |
| `HfcDriftMaxBaselineBytes` | Caps UTF-8 baseline bytes. Range `1..10485760`; default `262144`. |
| `HfcDriftSeverity` | Applies only to HFC1065/HFC1066. Accepted values: `Warning`, `Error`, `Info`, `Information`. |
| `PublishTrimmed` / `PublishAot` | Enable the separate HFC1070 trim/AOT advisory path. |

Invalid numeric or severity values emit HFC1067 Warning and fall back to documented safe defaults. Invalid boolean opt-in values are treated as not enabled and do not start drift comparison.

## Diagnostic Dispositions

| ID | Default severity | Disposition |
| --- | --- | --- |
| HFC1058 | Warning | Drift is enabled but no trusted baseline candidate is supplied. First-run/missing-baseline warning. |
| HFC1059 | Error | Configured `HfcDriftBaselinePath` matches no candidate `AdditionalText`. |
| HFC1060 | Error | Baseline is empty, whitespace-only, malformed JSON, or otherwise invalid content. |
| HFC1061 | Error | Baseline schema version is unsupported. |
| HFC1062 | Error | Baseline algorithm version is unsupported. |
| HFC1063 | Error | Baseline byte, declaration, or property bounds are exceeded. |
| HFC1064 | Error | Baseline has duplicate identities, duplicate properties, or invariant violations. |
| HFC1065 | Warning by default | Structural drift: declaration/property add/remove/rename, type category, nullability, or bounded-context identity changes. Severity may be overridden by `HfcDriftSeverity`. |
| HFC1066 | Warning by default | Renderer or metadata drift: display, description, role, icon, policy, destructive, column priority, field group, format, relative-time, badge, and empty-state CTA changes. Severity may be overridden by `HfcDriftSeverity`. |
| HFC1067 | Warning | Invalid analyzer-config option. |
| HFC1068 | Warning | Diagnostics were truncated at `HfcDriftMaxDiagnostics`; records omitted count. |
| HFC1069 | Error | Baseline values are redaction-unsafe; comparison is suppressed so HFC1065/HFC1066 do not leak unsafe values. |
| HFC1070 | Warning | Separate trim/AOT advisory for reflection action-queue catalog evidence. It is not part of HFC1058-HFC1069 drift comparison. |

Trust failures suppress unsafe partial comparison. HFC1065/HFC1066 are the only drift diagnostics whose severity is affected by `HfcDriftSeverity`; trust-failure diagnostics remain cataloged warnings/errors.

## Diagnostic Payload

Every emitted drift diagnostic populates:

- `BaselinePath`
- `DeclarationPath`
- `DeclarationName`
- `MemberName`
- `DriftKind`
- `ExpectedShapeHash`
- `ActualShapeHash`
- `SchemaVersion`
- `AlgorithmVersion`
- `BoundedContext`

Messages retain the `What`, `Expected`, `Got`, `Fix`, and `DocsLink` shape. Help links use `https://hexalith.github.io/FrontComposer/diagnostics/HFCxxxx`.

Paths are normalized to repo-relative forward-slash paths or `<outside-project>` where applicable. Diagnostics must not leak Windows drive roots, absolute host paths, URI paths, traversal/control-character payloads, tenant/user/token sentinels, raw JSON payloads, or raw untrusted baseline text.

## Incremental And Output Constraints

The drift comparison pipeline is isolated from `CompilationProvider`. The tracked baseline-loading step name remains `LoadDriftBaselines`.

Drift detection is diagnostics-only. Enabling drift detection, changing baseline file ordering, UTF-8 BOM presence, CRLF/LF line endings, or repeated generator runs must not change generated hint names or generated output bytes.

HFC1070 is intentionally separate and may combine `CompilationProvider` only for adopter `IActionQueueProjectionCatalog` trim/AOT evidence.

## Historical Story Labels

Source, docs, and tests still contain historical `Story 9-1` labels around the drift detector. Story 7.4 treats those labels as brownfield provenance unless they create Epic 7 behavioral confusion. Current HFC1058-HFC1070 catalog rows and analyzer-release rows still reference Story 9-1 ownership; this story records that as pre-existing labeling debt and does not rewrite the diagnostics site ownership.

## Verification Evidence

Story 7.4 required no production source changes. One focused test pin (`FrontComposerDriftDetectionEnabledAlias_EnablesDriftComparison_WhenPrimaryOptionIsAbsent` in `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftAnalyzerConfigOptionsTests.cs`) was added to pin the documented `FrontComposerDriftDetectionEnabled` opt-in alias. Existing drift and diagnostic-governance pins plus the added alias pin verified the contract:

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings and 0 errors.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --filter "FullyQualifiedName~Drift&Category!=Performance&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` built successfully, then VSTest aborted before execution because socket creation is denied in this sandbox.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -class "*Drift*" -class- "*Benchmarks*"` passed 170/170 (includes the added alias pin).
- Focused diagnostic parity lane (`DriftDiagnosticCatalogTests`, `SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned`, descriptor/release-row parity methods) passed 25/25.
- Broad SourceTools in-process lane with default exclusions ran 1023 tests: 1020 passed, 3 failed. Failures were outside Story 7.4: missing `deferred-work.md` (`DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates`), existing DataGrid FC-DOC section gap (`FcDocComponentDocumentationContractTests`, `datagrid.md`), and IDE parity path-normalization expectation (`IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux`).
- Configured solution-level `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` aborted locally because VSTest cannot create its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`).
