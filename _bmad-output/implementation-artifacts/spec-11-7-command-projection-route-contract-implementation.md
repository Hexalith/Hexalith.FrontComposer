---
title: 'Story 11.7: Command/projection route-contract implementation'
type: 'bugfix'
created: '2026-07-11T00:00:00+02:00'
status: 'done'
baseline_revision: 'df16faa5a446e5e75c727f4f9bf9008b1d154ca0'
final_revision: 'd481e8fdd9dfa2504e81c5d07f5caacc55e90918'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '_bmad-output/project-context.md'
  - '_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md'
  - '_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md'
warnings: [oversized]
---

<intent-contract>

## Intent

**Problem:** Generated full-page commands register `/commands/{BoundedContext}/{CommandTypeName}`, but palette and empty-state activation still advertise `/domain/{kebab}/{kebab}`. Reachability also claims every command has a page even though Inline and CompactInline commands do not, so framework-owned activation can dead-end.

**Approach:** Put the netstandard-safe generated-command route algorithm in the Contracts kernel, consume it from SourceTools and Shell, and emit truthful full-page reachability metadata. Pin the operator journey from module workspace through palette activation to the rendered generated page while preserving FC-IA-1 module/tab shapes.

## Boundaries & Constraints

**Always:** Preserve segment casing and the generator's safe-character replacement; reduce command FQNs to their simple type name; keep `Default` fallback semantics; retain authorization, write-access, full-page reachability, internal-route, recent-route, active-context, and validated query-parameter behavior. Keep SourceTools netstandard2.0-compatible and dependent only on Contracts. Treat `/{module}` as the default workspace and `/{module}/{tab}` as a secondary tab/flyout destination.

**Block If:** A real generated FullPage route cannot be represented without a breaking wire/schema change, or completing the root-repository behavior requires editing a `references/Hexalith.*` submodule. Record that external dependency rather than changing a submodule without explicit approval.

**Never:** Emit new `/domain/...` command links; change projection routes, EventStore `/api/v1/commands` endpoints, command density, MCP/schema fingerprints, generated output paths, or command names; hand-edit generated output; add broad route aliases. Historical recent-route parsing may remain transitional, but must not be advertised.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Full-page FQN | `Commerce.SubmitOrderCommand`, BC `Commerce` | `/commands/Commerce/SubmitOrderCommand`; route metadata says reachable | Safe internal navigation |
| Inline/compact command | Registered command with no emitted page | Excluded from palette and CTA activation | No dead link rendered |
| Missing BC | Generator model without BC | `/commands/Default/{TypeName}` | Same fallback in producer and consumer |
| Unsafe segment | Spaces or path punctuation | Same case-preserving `-` replacement in generator and shell | Cannot escape the internal path |
| Canonical navigation | `/commands/Commerce/SubmitOrderCommand` | Current bounded context is `commerce` | Old `/domain/...` parsing may remain read-only compatibility |
| Module workspace | `/counter` and secondary `/counter/{tab}` | Root/default and tab/flyout remain path encoded | No query-only tab selection introduced |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs` -- public generated registration payload; needs append-only full-page command metadata.
- `src/Hexalith.FrontComposer.Contracts/Routing/GeneratedCommandRoute.cs` -- new shared route-value algorithm for Shell and SourceTools.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/{CommandRendererTransform,RegistrationModel,RegistrationModelTransform}.cs` -- route producer and equatable registration IR.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs` -- emits known full-page membership from command density.
- `src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs` -- compatibility facade; keeps projection kebab helper but delegates command routes.
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs` -- merges/clones metadata and answers reachability truthfully with legacy-manifest compatibility.
- `src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs` -- recognizes the canonical command family.
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` and `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs` -- framework activation surfaces.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/{FcCommandPalette.razor.cs,FcPaletteResultList.razor}` -- interactive dispatcher and option-event path required by the browser activation surface.
- `tests/e2e/specs/route-contract.spec.ts` -- browser-level command activation and module/tab contract pin.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.Contracts/Routing/GeneratedCommandRoute.cs` and `tests/Hexalith.FrontComposer.Contracts.Tests/Routing/GeneratedCommandRouteTests.cs` -- implement and pin one pure, netstandard2.0 route builder covering FQN reduction, fallback, casing, sanitization, and validation.
- `src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs` -- add optional full-page command membership without changing existing call-site meaning; distinguish legacy manifests with absent metadata from generator-known empty membership.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs`, `RegistrationModel.cs`, and `RegistrationModelTransform.cs` -- consume the shared builder and carry density-derived full-page membership through fully equatable IR.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RegistrationModelTransformTests.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RegistrationEmitterTests.cs`, and `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs` -- emit membership for FullPage only and pin generated `[Route]`/manifest parity for normal, default, FQN, and sanitized cases.
- `src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs`, `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Routing/CommandRouteBuilderTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Registration/FrontComposerRegistryTests.cs` -- delegate canonical construction; merge/clone metadata; return false for known Inline/CompactInline commands, true for known FullPage commands, and preserve documented legacy-manifest behavior.
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs`, `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionEmptyPlaceholderTests.cs` -- make palette/recent/focus and rendered CTA hrefs use the canonical reachable page without weakening safety or authorization gates.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor`, and `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs` -- keep post-render state updates on the Blazor dispatcher and compile option clicks as real event handlers so the browser activation surface is interactive.
- `src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/BoundedContextRouteParserTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` -- resolve `/commands/{BC}/{Type}` to the real BC while retaining projection/module parsing.
- `tests/e2e/specs/route-contract.spec.ts` and `tests/e2e/package.json` -- from `/counter`, pin the module root/default and secondary flyout path shape, open the palette, activate Configure Counter, then assert canonical URL and visible generated form. Do not edit Tenants or other submodules.

**Acceptance Criteria:**
- Given normal, default-BC, namespaced, and sanitized command models, when SourceTools emits a FullPage component and registration, then generated metadata and every Shell activation resolve to the identical `/commands/{BoundedContext}/{CommandTypeName}` route.
- Given a known Inline or CompactInline command, when palette or empty-state candidates are built, then it is not advertised as a navigable generated page; given a FullPage command, activation closes the palette, navigates, and records only the canonical recent route.
- Given an authorized writable projection empty-state command, when the CTA renders, then its href is the existing canonical generated page and all existing authorization, write-access, reachability, and internal-route suppression still apply.
- Given canonical command navigation, when Shell derives current context, then it selects the command bounded context rather than `commands`; existing projection and module routes remain unchanged.
- Given the Counter browser host at its module root, when the operator activates Configure Counter through the palette, then the browser lands on `/commands/Counter/ConfigureCounterCommand` and renders `Configure Counter command form`; module root/default and secondary `/{module}/{tab}` evidence remains path-encoded.
- Given the completed change, when repository scans and contract tests run, then framework-owned command activation contains no new `/domain/...` target and EventStore command/status endpoints are unchanged.

## Spec Change Log

- 2026-07-11: Distilled the route-contract implementation from FC-ROUTE and FC-IA-1, with an explicit root-repository/submodule boundary.
- 2026-07-11: Review hardened public API compatibility, route-segment safety, truthful reachability merge/activation behavior, legacy-recent suppression, and browser interaction coverage without expanding the route family.

## Review Triage Log

Review layers: adversarial general, edge-case hunter, verification-gap audit, and intent-contract audit. Overlapping raw findings were consolidated by root cause.

**Patched:**
- Preserved `DomainManifest`'s existing eight-argument constructor and eight-value deconstruction contract by making `FullPageCommands` a non-positional init property; added a compatibility test.
- Rejected whitespace-only and relative-marker route segments, normalized missing bounded contexts to `Default` throughout renderer metadata, and decoded canonical route context safely while requiring a complete `/commands/{BC}/{Type}` shape.
- Made explicit generated reachability override legacy assumptions for the same command in either merge order; validated invalid membership both during startup actions and later registration.
- Made reflection fallback fail closed when it can prove command membership but cannot prove a generated page; rechecked reachability at activation to protect against stale results.
- Removed the default command preview that could be starved by the projection cap; retained query-driven, authorization-aware command discovery.
- Suppressed historical `/domain/...` entries from hydrated recent results while retaining parser compatibility.
- Fixed the rendered palette interaction boundary: lifecycle awaits stay on the Blazor dispatcher, option clicks compile as events, and search publishes immediately instead of waiting for blur.
- Strengthened verification with a real parent/child click test, invalid-metadata tests, API compatibility coverage, and a browser journey that navigates module root, secondary projection path, palette search/click, and generated command page.

**Rejected with rationale:**
- Simple-name route collisions are constrained by the canonical FC-ROUTE contract and remain invalid domain input; changing the route shape would violate the story's explicit route contract.
- Cross-bounded-context reachability ambiguity requires the same command FQN to be registered as different logical commands; generated command types have one FQN and one bounded context. Hand-authored duplicate misuse remains outside the generated contract.
- A throwing or null static `Manifest` is a malformed registration type and intentionally fails startup; silently swallowing it would conceal an invalid generated-registration contract.
- SourceTools/Contracts version skew is governed by the version-locked analyzer package and copied analyzer dependency; supporting independently mixed package versions is not part of this route change.
- The generic recommendation to avoid solution-level tests conflicts with this repository's project context, which explicitly requires solution-level verification.

**External boundary:** FC-IA-1's Tenants reference application remains in a read-only root-declared submodule. The Counter root host proves the root Shell module/tab contract; a Tenants migration requires explicit submodule authority and is not silently folded into this story.

Triage outcome: all in-scope high/medium correctness and verification gaps were patched; no unresolved in-scope blocker remains. Follow-up review is recommended because the fixes touch public contract compatibility and runtime activation safety.

### 2026-07-11 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 0
- defer: 3: (high 0, medium 2, low 1)
- reject: 8: (high 0, medium 0, low 8)
- addressed_findings:
  - none

Follow-up review pass over the committed change (`df16faa5..d481e8fd`) with four independent Opus reviewers (adversarial-general, edge-case-hunter, verification-gap, intent-alignment). No code changes were made this pass. Highlights:
- The removed `ConfigureAwait(false)` in `FcCommandPalette.OnAfterRenderAsync` is covered by a pre-existing file-level `#pragma warning disable CA2007`; an independent Shell rebuild under `TreatWarningsAsErrors` reported 0 warnings / 0 errors, so no CA2007 regression exists.
- The `Default` bounded-context fallback for commands (and resulting `/commands/Default/{Type}` route collisions for distinct commands sharing a simple type name) is mandated by the intent's I/O matrix and was already rejected in the first pass as invalid domain input constrained by the canonical FC-ROUTE contract — **reject** (out of scope on intent authority).
- Startup `HFC1601` fail-fast on an invalid generated manifest, the dead `UriFormatException` catch in `BoundedContextRouteParser`, a vacuous `ShouldNotContain("@onclick")` test assertion, the convoluted whitespace guard, the `/domain/` filter misfiring only for an adopter bounded context literally named `Domain`, and the un-gated e2e journey (adequately backed by generator↔Shell parity-by-construction unit tests) were all **rejected** as noise, intended behavior, or out-of-scope infrastructure.
- Three genuinely new, real findings were **deferred** to `deferred-work.md` as new entries (fail-closed reflection-fallback has no regression test; command-vs-projection `Default`/namespace fallback asymmetry can detach an un-annotated co-located CTA; activation re-check logs a semantically wrong diagnostic id). None is a trivial patch or a spec/intent defect, so no repair loopback was triggered.

## Design Notes

Use optional `FullPageCommands`-style metadata: `null` denotes a legacy hand-authored manifest whose old assumption remains compatible; a generated empty collection explicitly means no command page; a generated membership entry means the page exists. Merge and clone logic must preserve that distinction. A shared Contracts route helper is justified because both netstandard2.0 SourceTools and net10 Shell already depend downward on Contracts; Shell must not reference SourceTools.

FC-IA-1's Tenants reference implementation is outside the writable root repository and cannot be silently changed. This story pins the root Shell contract with the Counter host and leaves any Tenants query-tab migration as an explicit external dependency.

## Verification

**Commands:**
- `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -f netstandard2.0 -c Release -m:1 /nr:false` -- shared helper keeps the kernel clean.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj -c Release` -- Contracts edge cases pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release` -- generator route/registration parity passes without snapshot drift.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- default Shell lane passes.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "Category=e2e-palette"` -- deterministic palette flow passes.
- `npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e run test:route-contract` -- browser activation reaches the generated page.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" && dotnet build Hexalith.FrontComposer.slnx -c Release && git diff --check` -- broad gates pass.

## File List

- `_bmad-output/implementation-artifacts/spec-11-7-command-projection-route-contract-implementation.md`
- `src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs`
- `src/Hexalith.FrontComposer.Contracts/Routing/GeneratedCommandRoute.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/RegistrationModel.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/RegistrationModelTransform.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Registration/DomainManifestCompatibilityTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Routing/GeneratedCommandRouteTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionEmptyPlaceholderTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Registration/FrontComposerRegistryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Routing/CommandRouteBuilderTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/BoundedContextRouteParserTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RegistrationEmitterTests.BoundedContextDisplayLabel_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RegistrationEmitterTests.SingleProjection_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RegistrationEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RegistrationModelTransformTests.cs`
- `tests/e2e/package.json`
- `tests/e2e/specs/route-contract.spec.ts`

## Auto Run Result

Status: done

Summary: Implemented the canonical generated FullPage command route `/commands/{BoundedContext}/{CommandTypeName}` across Contracts, SourceTools, Shell activation, and browser navigation. Generated manifests now distinguish FullPage commands from Inline/CompactInline commands without breaking the existing `DomainManifest` constructor/deconstruction API; registry merge, fallback discovery, palette search/activation, CTA reachability, recent-route hydration, and current-context parsing all honor the same truthful contract. The Counter browser journey proves module root, secondary projection path, palette search/click, canonical command URL, and visible generated form.

Files changed:
- Contracts: added the shared netstandard-safe route builder and append-only `DomainManifest.FullPageCommands` metadata, with route and public-API compatibility tests.
- SourceTools: normalized Default context, derived reachability from command density, emitted init-only manifest metadata, and shipped the Contracts analyzer dependency alongside SourceTools; generator transforms, snapshots, and integration tests were updated.
- Shell: delegated command route construction, hardened registry merge/validation and fallback discovery, recognized canonical command contexts, suppressed legacy recent targets, rechecked reachability at activation, and repaired the rendered palette dispatcher/search/click interaction path.
- Tests/e2e: expanded Shell activation/CTA/parser/registry coverage and added the `test:route-contract` Playwright journey.
- Story artifact: recorded intent, review triage, verification, final revision, and external boundaries in this specification.

Review findings breakdown: 15 consolidated patch items applied, 0 deferred, 5 rejected with rationale. No intent gap or bad-spec loopback was required. Follow-up review recommendation: true, because review-driven fixes touched public API compatibility, startup validation, activation safety, and the browser interaction boundary.

Verification:
- `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- Contracts passed 267/267; SourceTools passed 1063/1063.
- The broad no-restore solution run passed CLI 67/67, Contracts 267/267, MCP 372/372, Shell 2154/2154, SourceTools 1063/1063, Testing 46/46, and benchmark tests 2/2.
- Final standard Shell lane passed 2151/2151; `Category=e2e-palette` passed 4/4.
- `npm --prefix tests/e2e run typecheck` passed; the Chromium route-contract browser test passed 1/1.
- Story artifact validation passed against the baseline with unrelated `.agents/**` workspace artifacts explicitly excluded.
- `git diff --check` passed for the story diff; pre-commit and post-commit commitlint passed.

Residual risks and artifacts:
- A restore-enabled broad solution attempt remains blocked by unrelated existing Parties/UI `Hexalith.Commons.UniqueIds` NuGet version conflicts; the restored/no-restore build and all relevant test assemblies pass.
- FC-IA-1's Tenants reference application remains an explicit read-only submodule dependency and was not modified.
- Unrelated workspace artifacts were intentionally left untouched and uncommitted: modified `.agents/skills/aspire/SKILL.md`; untracked `.agents/skills/aspire-deployment/`, `.agents/skills/aspire-init/`, `.agents/skills/aspire-monitoring/`, `.agents/skills/aspire-orchestration/`, and `.agents/skills/aspire/references/`.

## Follow-up Review Result (2026-07-11)

Status: done

A fresh follow-up review pass ran over the committed change (`df16faa5..d481e8fd`) using four independent Opus reviewers (adversarial-general, edge-case-hunter, verification-gap, intent-alignment). See the `2026-07-11 — Review pass` entry in the Review Triage Log for the full breakdown.

Outcome: intent_gap 0, bad_spec 0, patch 0, defer 3, reject 8. No code changes were made and no spec repair loopback was triggered; the change remains as committed.

Findings breakdown:
- Rejected (8): `/commands/Default/{Type}` route collisions for distinct commands sharing a simple type name (mandated by the intent I/O matrix; already rejected first-pass as invalid domain input under the canonical FC-ROUTE contract); startup `HFC1601` fail-fast on an invalid generated manifest (intended); dead `UriFormatException` catch in `BoundedContextRouteParser`; a vacuous `ShouldNotContain("@onclick")` test assertion (paired with a real click assertion); the convoluted whitespace guard in `GeneratedCommandRoute.Build`; the `/domain/` recent-route filter misfiring only for an adopter bounded context literally named `Domain`; the un-gated `test:route-contract` e2e (adequately backed by generator↔Shell route parity-by-construction unit tests plus the isolated command-page render test); and the untested cross-namespace `Default` merge (subset of the mandated Default rule).
- Deferred (3, appended to `deferred-work.md` as new entries): the fail-closed reflection fallback (`FullPageCommands = []`) has no regression test; the command-vs-projection bounded-context fallback asymmetry (`Default` vs namespace-last-segment) can silently detach an un-annotated co-located projection/command CTA; and the palette activation re-check logs the semantically wrong diagnostic id `HFC2111_PaletteHydrationEmpty`.

Verification performed:
- The verification-gap reviewer independently rebuilt `Hexalith.FrontComposer.Shell` under `TreatWarningsAsErrors=true` (incremental and forced non-incremental) with 0 warnings / 0 errors, confirming the removed `ConfigureAwait(false)` calls in `FcCommandPalette.OnAfterRenderAsync` do not regress CA2007 (a pre-existing file-level `#pragma warning disable CA2007` covers them).
- Working tree carries no code drift from the verified-green committed HEAD — only this spec artifact, the deferred-work ledger, and unrelated `.agents/**`/`sprint-status.yaml` residual artifacts differ — so the story's original full green verification (Contracts 267/267, SourceTools 1063/1063, Shell 2151/2151, e2e-palette 4/4, route-contract Playwright 1/1, `dotnet build -c Release` clean) stands.

Follow-up review recommendation: false. This pass made no review-driven code changes; there is nothing new to re-review.

Residual risks:
- The three deferred items above remain open in `deferred-work.md` for the orchestrator to schedule; each is real but either an accepted intent consequence, a missing regression pin over already-correct behavior, or a low-severity logging-clarity nit.
- Unrelated workspace artifacts (`.agents/**`, `sprint-status.yaml`) remain uncommitted and were left in place.
