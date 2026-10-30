# IDE Parity Matrix

This document is the authoritative parity contract for Hexalith FrontComposer v1 IDE behavior. No prose claim in onboarding, release notes, samples, or issue templates overrides this matrix.

Visual Studio is the calibration IDE used to baseline the suite. It is not the reference IDE. The matrix below defines the contract for Visual Studio, JetBrains Rider, and Visual Studio Code with C# Dev Kit.

## Version Pins

| Surface | Pin |
| --- | --- |
| Active repository .NET SDK | 10.0.403 |
| SourceTools package | Hexalith.FrontComposer.SourceTools local v0.1 story-9-3 |
| Generated output path contract | obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs (canonical: `Hexalith.FrontComposer.Contracts.Conformance.GeneratedOutputPathContract.Template`) |
| Generated output path contract version | v1 |
| Visual Studio | Visual Studio 2022 17.13+ on Windows |
| Rider | JetBrains Rider 2026.1.x on Windows, macOS, and Linux |
| VS Code | VS Code stable with C# Dev Kit pinned minor |

C# Dev Kit is required for VS Code parity. Microsoft account and proprietary license implications are adopter prerequisites. OmniSharp-only VS Code is unsupported in v1.

The manual IDE checks behind the `lastValidated` date (2026-05-09) ran on .NET SDK 10.0.302; manual IDE
behavior on 10.0.403 has not been revalidated yet. A revalidation is recorded in its story or spec as the
commands and manual steps run, their results, the commit SHA, and the CI run link; no evidence files are
committed.

## Gate Semantics

Repo-owned Must failures block CI or release when they involve generated paths, XML docs, diagnostics, symbol output, report sanitization, or NFR8 generator performance. Vendor IDE UI or indexing differences block release only when evidence points to a FrontComposer generated-output defect. Otherwise, the row records a pinned-version limitation and a revalidation issue before release.

Generated files remain read-only by design. Rename parity means: edit the domain symbol, rebuild/regenerate, inspect generated Razor/Fluxor/MCP output through generated files or `frontcomposer inspect`, and verify drift diagnostics or generated output update. Direct generated-file rename is unsupported.

## Matrix

| Row ID | Capability | Tier | Visual Studio Evidence | Rider Evidence | VS Code + C# Dev Kit Evidence | Fixture | Validation | Owner | Last Verified | Limitation | Gate |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| IDE-MUST-001 | Generated type completion | Must | Visual Studio 2022 17.13+ calibration evidence required | Rider 2026.1.x manual release evidence | VS Code stable + C# Dev Kit manual evidence | `samples/IdeParityCounter` | CI generated symbols + manual completion capture | SourceTools | 2026-05-09 | Vendor popups are manual evidence | CI plus release checklist |
| IDE-MUST-002 | XML-doc hover content | Must | Visual Studio 2022 17.13+ quick-info capture | Rider 2026.1.x quick-doc capture | C# Dev Kit hover capture | `samples/IdeParityCounter` | CI source XML docs + manual hover evidence | SourceTools | 2026-05-09 | Vendor formatting can differ | CI blocks missing docs |
| IDE-MUST-003 | Go To Definition to generated source | Must | Generated path or virtual generated source recorded | Generated source navigation recorded | Generated source navigation recorded | `samples/IdeParityCounter` | Debug/Release and multi-TFM path contract | SourceTools | 2026-05-09 | Virtual views are acceptable when symbol identity matches | CI blocks path regressions |
| IDE-MUST-004 | HFC diagnostics squiggles | Must | HFC IDs and help links captured | HFC IDs and help links captured | HFC IDs and help links captured | `samples/IdeParityCounter` | DiagnosticDescriptor metadata and IDE evidence | SourceTools | 2026-05-09 | Final per-ID pages land in Stories 9-4/9-5 | CI blocks metadata regressions |
| IDE-MUST-005 | Solution-wide symbol search | Must | Generated symbols searched after rebuild | Generated symbols searched after rebuild | Generated symbols searched after rebuild | `samples/IdeParityCounter` | Deterministic symbol list + manual index evidence | SourceTools | 2026-05-09 | Vendor indexes may lag until rebuild | Release checklist |
| IDE-MUST-006 | NFR8 incremental-generator behavior | Must | Calibration row records warm update budget | Manual vendor evidence may reference CI timing | Manual vendor evidence may reference CI timing | `samples/IdeParityCounter` | Existing benchmark boundary under 500 ms warm update | SourceTools | 2026-05-09 | IDE UI latency is separate vendor evidence | CI blocks performance regressions |
| IDE-SHOULD-001 | Find All References | Should | Manual evidence or inspect fallback | Manual evidence or inspect fallback | Manual evidence or inspect fallback | `samples/IdeParityCounter` | Documented workflow | SourceTools | 2026-05-09 | Vendor reference indexing differs | Release checklist warning |
| IDE-SHOULD-002 | Rename workflow | Should | Domain edit -> rebuild -> inspect | Domain edit -> rebuild -> inspect | Domain edit -> rebuild -> inspect | `samples/IdeParityCounter` | Documented workflow | SourceTools | 2026-05-09 | Direct generated-file rename is unsupported | Release checklist warning |
| IDE-SHOULD-003 | Analyzer code-fix application | Should | Limitation recorded unless code fix exists | Limitation recorded unless code fix exists | Limitation recorded unless code fix exists | `samples/IdeParityCounter` | Documented limitation | SourceTools | 2026-05-09 | Story 9-3 does not add code fixes | Release checklist |
| IDE-SHOULD-004 | Generator-host debugging | Should | Contributor workflow documented | Contributor workflow documented | Contributor workflow documented | `samples/IdeParityCounter` | `CONTRIBUTING.md` guidance | SourceTools | 2026-05-09 | Contributor-only, not adopter onboarding | Release checklist |
| IDE-REMOTE-001 | VS Code remote/container path | Must | Not applicable | Not applicable | Dev Container/C# Dev Kit prerequisite evidence | `samples/IdeParityCounter` | Dev Container config + manual legal validation | SourceTools | 2026-05-09 | CI does not install extensions or accept licenses | Release checklist blocks |
| IDE-VERSION-001 | Version revalidation | Must | Pin monitored | Pin monitored | Pin monitored | `samples/IdeParityCounter` | Configured pins produce GitHub issue or dry-run artifact | SourceTools | 2026-05-09 | No IDE or extension install/update | CI/release blocks |
| IDE-OOS-001 | Custom IDE extensions | Out-of-scope | Out of scope | Out of scope | Out of scope | `samples/IdeParityCounter` | Scope guardrail | SourceTools | 2026-05-09 | Needs separate product/architecture decision | Not a Story 9-3 gate |

## Remote Development Limits

Dev Containers, Remote-SSH, and GitHub Codespaces can be supported only when C# Dev Kit prerequisites are satisfied by the developer or release-validation environment. CI may validate repo-owned outputs and configured versions, but it must not install IDEs or extensions, accept licenses, authenticate Microsoft accounts, persist telemetry, or log account identifiers.
