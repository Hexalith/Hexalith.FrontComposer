# Source / Current-Fit Review — PRD rewrite 2026-09-08 (post-reconciliation)

- **Subject:** `_bmad-output/planning-artifacts/prd.md` (rewritten 2026-09-08) and `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md`
- **Reviewer lens:** every dated, numbered, or status claim checked against repository sources (read-only; `references/` not opened except `references/Hexalith.Builds/Props/Directory.Packages.props`)
- **Review date:** 2026-09-08

## Verdict

**Mostly reconciled; one gate is mis-specified.** The numeric and behavioural claims (FR-1, FR-4, FR-9, FR-11, FR-12/NFR-8, FR-13, FR-15, FR-19, FR-29, FR-30, §7 SDK/Fluent/EventStore, §11 package table, G-1, G-3, G-5, G-6, D-11 dates) match source. The material defect is **G-2 / D-11 / FR-24 / addendum §2**, which name `a8a50859…` as the Builds revision FrontComposer must pin and list "populate `evaluator_authorizations`" as remaining work; the repository moved past both on 2026-08-11 (`3f0e3595…`) and 2026-08-22 (`4eb33928…`), and `evaluator_authorizations` has been populated. As written, G-2 cannot be evaluated and would regress the repo if applied literally. Secondary staleness: Roslyn version, the "latest published release (4.3.0)" claim versus the v4.4.0 tag, the `HEXALITH_RELEASE_PUBLISH_ENABLED` freeze mechanism that REL-5 declares retired, and small ledger/bookkeeping omissions.

Finding counts: **critical 0 · high 1 · medium 4 · low 6**.

## Coverage (files read)

Planning / contracts
- `_bmad-output/planning-artifacts/prd.md`, `prd-addendum-2026-09-08.md`
- `_bmad-output/planning-artifacts/epics.md` (Epic 11 block 1644–2340, UX-DR7 line 150, FR-14 row 914)
- `_bmad-output/planning-artifacts/ux-design.md` (navigation section)
- `_bmad-output/planning-artifacts/architecture.md` (release evidence, approved Builds execution identity)
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`
- `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json`
- `_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md`
- `_bmad-output/contracts/fc-mcp-fail-closed-security-contract-2026-06-05.md`, `fc-mcp-schema-fingerprint-negotiation-2026-06-05.md`
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` (H1–H12 list, fix batches)
- `_bmad-output/project-docs/deployment-guide.md` (`HEXALITH_RELEASE_PUBLISH_ENABLED` row)

Implementation artifacts
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `rel-ai-1-release-evidence-ledger.md`, `rel-1…rel-5-*.md`, `gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md`
- `tests/9-8-live-acceptance.md`, `deferred-work.md` (DW-679)
- `spec-approve-builds-execution-sha-to-gitlink.md`, `spec-split-builds-catalog-gitlink-from-ci-cd-execution-sha.md`, `spec-fix-release-builds-execution-sha.md`, `spec-align-latest-hexalith-modules-and-simplify-ci.md`, `spec-bump-latest-hexalith-nuget-packages-2.md`, `spec-actions-33264036185-33264035739-fix-cicd-release.md`, `spec-pact-provider-reconciliation-2.md`
- `evidence/pact-provider-reconciliation/{provider-verification,run-evidence,apphost-smoke}.json`, `evidence/frontcomposer-story-11-24/**`

Source / config
- `global.json`, `Directory.Build.targets`, `CHANGELOG.md`, `eng/release-package-inventory.json`, `eng/release_compatibility.py`, `eng/dependency-graph-policy.json`, `docs/diagnostics/compatibility-suppressions.json`, `.github/workflows/release.yml`
- `references/Hexalith.Builds/Props/Directory.Packages.props` (versions only)
- `src/*/*.csproj`, `src/**/PublicAPI*.txt`
- `src/Hexalith.FrontComposer.Shell/Options/FcShellOptions.cs`, `Infrastructure/EventStore/ProjectionHubRetryPolicy.cs`, `Infrastructure/EventStore/ProjectionSubscriptionService.cs`, `Infrastructure/Tenancy/TenantContextException.cs`, `Services/StorageScopeResolver.cs`, `State/StorageKeys.cs`, `Badges/EventStoreActionQueueCountReader.cs`, `State/Navigation/ScopeReadinessGate.cs`, `Components/Layout/FrontComposerShell.razor.css`, `Components/Rendering/DataGridDensityMetrics.cs`
- `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`, `Emitters/CommandFormEmitter.cs`, `Diagnostics/DiagnosticDescriptors.cs`, `Transforms/RazorModelTransform.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs`, `Extensions/FrontComposerMcpServiceCollectionExtensions.cs`, `Invocation/FrontComposerMcpCommandInvoker.cs`
- `git tag -l 'v4*'` (tag dates)

Not verifiable from this repo (restricted): Hexalith.Builds `domain-release.yml` at the pinned SHA (lives under `references/`), GitHub issue 17 live state, NuGet.org publication state of v4.2.0–v4.4.0.

## Findings

### F-1 · HIGH · §0.1 G-2, §4 FR-24 bullet (line 428), §12 D-11, addendum §2 "Upstream state"

**PRD claim:**
> "FrontComposer pins the owner-accepted Hexalith.Builds revision `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`, `evaluator_authorizations` in `eng/dependency-graph-policy.json` are populated, and one end-to-end sealed CI→Release handoff (AD-13/AD-15) has been proven." (G-2 pass condition)
> "Remaining FrontComposer work: pin Builds and reusable refs to that SHA, populate `evaluator_authorizations`, prove sealed AD-13/AD-15 handoffs end to end." (addendum §2)

**Source evidence:**
- `.github/workflows/release.yml:17` `BUILDS_EXECUTION_SHA: 4eb33928a1d8c7775f97221cf9edc171db0cb5f8`; `:321` `uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@4eb33928a1d8c7775f97221cf9edc171db0cb5f8`.
- `eng/dependency-graph-policy.json:346–508` `"evaluator_authorizations": { "ci": [ { "stage": "ci", … "commit": "4eb33928…" } ], … }` — populated, not empty.
- `_bmad-output/implementation-artifacts/spec-align-latest-hexalith-modules-and-simplify-ci.md` (created 2026-08-11, `status: done`): "FrontComposer CD still embeds obsolete Builds commit `a8a50859…`" → "align FrontComposer gitlinks, workflow pins, and active evaluator closures to the resulting immutable Builds commit `3f0e3595…`".
- `spec-split-builds-catalog-gitlink-from-ci-cd-execution-sha.md` (2026-08-16, done): catalog gitlink and execution SHA are now separate identities; `spec-bump-latest-hexalith-nuget-packages-2.md` advances the execution SHA to `4eb33928…` (2026-08-22).
- `architecture.md` names `3f0e3595…` as the approved execution identity (itself one hop stale versus `4eb33928…`).
- `gov-1-…md:9` `status: done` while `:23` `Status: in-progress` and `:121/:156` still say the registry is "empty" — the story body is the stale source the PRD inherited.

**Why it matters:** `a8a50859…` was superseded four weeks before the rewrite; two of the three G-2 sub-conditions are already satisfied in a different way (pin to `4eb33928…`, populated authorizations), and pinning back to `a8a50859…` would be a regression. The gate as written can never turn green.

**Fix:** Rewrite G-2 as: "FrontComposer's execution pins (`release.yml` `BUILDS_EXECUTION_SHA` and the `uses:` refs) and the active `evaluator_authorizations` closures agree on one immutable Builds commit (currently `4eb33928…`), the catalog gitlink is separately authenticated, and one sealed AD-13/AD-15 handoff has been proven on a released tag." Move `a8a50859…` to D-11 history only ("owner-accepted revision that unblocked issue 17; execution identity later advanced to `3f0e3595…` on 2026-08-11 and `4eb33928…` on 2026-08-22"). Delete "populate `evaluator_authorizations`" from remaining work in the addendum. Add an OI to update `architecture.md` and the GOV-1 story body.

### F-2 · MEDIUM · §4 release bullet (line 422), §12 D-6

**PRD claim:**
> "There is no automatic `workflow_run` publication and no caller-side freeze guard; the standing freeze is Builds-hosted (`HEXALITH_RELEASE_PUBLISH_ENABLED`)." / D-6: "…exact-SHA `workflow_dispatch`, Builds-hosted freeze".

**Source evidence:**
- `rel-5-provision-signing-identity-and-first-governed-release.md:65` "The transitional `workflow_run` and `HEXALITH_RELEASE_PUBLISH_ENABLED` path is retired. There is exactly one publication path, and it is the operator-dispatched protected production job."
- `sprint-status.yaml:294–295` "The transitional workflow_run and HEXALITH_RELEASE_PUBLISH_ENABLED path is retired."
- `rel-4-enforce-temporary-release-freeze.md:26` (earlier, 2026-08-09) still describes the variable as the standing freeze; `deployment-guide.md:108` still documents the variable.
- The pinned Builds `domain-release.yml` is under `references/` and was not opened, so the live mechanism is unverifiable here.

**Fix:** Follow the latest done source (REL-5): state that publication is gated solely by the operator `workflow_dispatch` + protected production environment approval, and either drop the `HEXALITH_RELEASE_PUBLISH_ENABLED` claim or mark it "REL-4 wording; REL-5 declares this path retired — reconcile deployment-guide.md" as an OI.

### F-3 · MEDIUM · §4 FR-25 bullet (line 435), §12 A4 (line 664)

**PRD claim:**
> "the compatibility gate baseline currently tracks the latest published release (`4.3.0`)." / A4: "tags v4.2.0, v4.3.0, and v4.4.0 correspond to published NuGet packages … inferred from compatibility-gate baselines and the Builds catalog pin (`4.3.0`)".

**Source evidence:**
- `git tag`: `v4.4.0 2026-09-07` exists; `CHANGELOG.md` has a v4.4.0 entry; `docs/diagnostics/compatibility-suppressions.json:3` `"currentRelease": "v4.4"`.
- `eng/release_compatibility.py:17` `PUBLISHED_BASELINE_VERSION = "4.3.0"`; `Directory.Build.targets` `FrontComposerPackageValidationBaselineVersion` = `4.3.0`; Builds catalog `HexalithFrontComposer` = `4.3.0`.

**Why it matters:** If v4.4.0 is published, 4.3.0 is not "the latest published release"; if it is not, A4 is false and G-1's tag list is wrong. The PRD uses the 4.3.0 pin as evidence for A4 while simultaneously treating v4.4.0 as published — the two claims are inconsistent.

**Fix:** Change FR-25 to "tracks the previous published line (`4.3.0`) while `currentRelease` is `v4.4`", and state in A4 that the 4.3.0 pin evidences only v4.3.0's publication; v4.4.0's status is unverified until its ledger row exists (OI-8).

### F-4 · MEDIUM · §7 "Runtime and framework" (line 514)

**PRD claim:**
> "…Blazor, Fluxor, Roslyn 5.6.0, ModelContextProtocol SDK, SignalR, OIDC, NUlid."

**Source evidence:** `references/Hexalith.Builds/Props/Directory.Packages.props` pins the Roslyn (`Microsoft.CodeAnalysis.*`) family at `5.9.0`; SourceTools uses central versions. (Also verified there: Fluxor `6.11.0`, ModelContextProtocol `2.2.0`, NUlid `1.7.3`, Fluent UI `5.0.0-rc.5-26219.1`.)

**Fix:** Either remove the literal ("Roslyn, version pinned by the selected Builds catalog") as the PRD already does for Fluent UI, or update to `5.9.0`.

### F-5 · MEDIUM · §8.2 Epic 11 bullet (line 539)

**PRD claim:**
> "Stories 11.0–11.24 are all done"

**Source evidence:** `epics.md` Epic 11 headings jump 11.9 → 11.11; there is no Story 11.10 (it was split into 11.11–11.14). `sprint-status.yaml:143` still records `epic-11: in-progress` (all constituent stories `done`).

**Fix:** "Stories 11.0–11.9 and 11.11–11.24 are done (11.10 was split into 11.11–11.14); `sprint-status.yaml` still shows `epic-11: in-progress` pending epic-level closure (OI)."

### F-6 · LOW · §4 FR-24 bullet (line 426)

**PRD claim:**
> "historical releases with blocked or invalid evidence (v3.2.2, v4.0.0, v4.0.1) stay …"

**Source evidence:** `rel-ai-1-release-evidence-ledger.md:86` `| v3.2.1 | … | invalid; rebuilt bytes | blocked; publish_authorized=false |` — v3.2.1 has the same disposition; `:30–31` "complete historical reconciliation now covers `v3.2.1`, `v3.2.2`, `v4.0.0`, and `v4.0.1`". `:90` v4.1.0 is `valid … v3` (superseded), so correctly excluded.

**Fix:** Add v3.2.1 to the list (and in the addendum "Ledger state" bullet).

### F-7 · LOW · §0.1 G-3, §12 D-12, NFR-11

**PRD claim:**
> "Open. Approved `3.91.1`; `currentCompatibility` is `3.103.0` with `migrationApprovalClaimed: false`." / "live Pact provider evidence for that identity is mandatory (NFR-11)."

**Source evidence:** `evidence/pact-provider-reconciliation/run-evidence.json` `capturedAt 2026-09-08T08:03:12Z`, `exitCode 0`; `provider-verification.json` `verificationMode: live-compatibility`, `finalVerdict: passed`, 19/19 interactions; `apphost-smoke.json` `eventStoreReleaseVersion: "3.103.0"`, `finalVerdict: passed`. Live provider evidence therefore already exists for the *current* identity (3.103.0), captured the day of the rewrite; the only open item is the dated migration approval (or rollback).

**Fix:** Update G-3 state to "Open on migration approval only: live-compatibility Pact provider evidence for 3.103.0 passed 2026-09-08 (`evidence/pact-provider-reconciliation/`)". Cite the evidence path in the Evidence column.

### F-8 · LOW · §0.1 G-1 / FR-24 / A4 wording

**PRD claim:**
> "every later published tag (v4.2.0, v4.3.0, v4.4.0)"

**Source evidence:** `rel-ai-1-release-evidence-ledger.md:63–66` (2026-08-14): "Further production publication remains unauthorized until that sign-off." Tags v4.2.0 (2026-08-30), v4.3.0 (2026-09-05), v4.4.0 (2026-09-07) were cut after that statement with no ledger rows.

**Fix:** Say explicitly in G-1/FR-24 that these three tags were created while the ledger recorded publication as unauthorized, so each needs a retroactive row *and* a disposition (compliant / fallback-approved / non-compliant), not merely "a row".

### F-9 · LOW · §12 D-11 "Contract frontmatter still says 'revision pending' — OI-7"

**PRD claim:** verified correct, but incomplete.

**Source evidence:** `g2-hexalith-builds-inline-pre-publish-gate-request.md:19` frontmatter "accepted immutable revision pending" while the body records `a8a50859…` accepted (2026-08-08). Given F-1, the accepted revision is also no longer the execution identity.

**Fix:** Widen OI-7 to "update the g2 request frontmatter to the accepted revision and note that execution identity has since advanced (see D-11 history)".

### F-10 · LOW · §5.0 / FR-2 consequence (line 172) and FR-3 (line 179)

**PRD claim:**
> "An invalid `[CommandTarget]` declaration fails with HFC1005 (invalid attribute argument)" / "HFC1001–HFC1070 diagnostic cataloged in `docs/diagnostics/`."

**Source evidence:** `DiagnosticDescriptors.cs` — HFC1005 = `InvalidAttributeArgument` (reused for FC-NIP); range ends at `HFC1070` (`:814–817`). Both correct. HFC0001 (migration path, Story 11.13) is outside the stated `HFC1001–HFC1070` range and is not mentioned in FR-3.

**Fix:** Add "plus HFC0001 (QueryRequest migration) and the HFC21xx shell runtime series (e.g. HFC2105)" or rephrase FR-3 to "the HFC diagnostics cataloged in `docs/diagnostics/`".

### F-11 · LOW · Addendum §2 "The GOV-1 story frontmatter says `done` while its body says in-progress"

**PRD claim:** correct as stated, but the addendum then adopts the body's stale checklist (empty registry) as the remaining work.

**Source evidence:** `gov-1-…md:9` `status: done`; `:23` `Status: in-progress`; `:121` "landed as a closed, empty registry"; contradicted by `eng/dependency-graph-policy.json:346+` (populated).

**Fix:** State that the story body is stale relative to the policy file and derive remaining G-2 work from `release.yml`/policy, not from the story checklist (ties to F-1).

## Verified correct (load-bearing claims)

- **G-1:** ledger rows exist for v3.2.1, v3.2.2, v4.0.0, v4.0.1, v4.1.0, v4.1.1 only; v4.1.1 is "published-byte-verified / fallback-approved; pending owner FR24 sign-off" (ledger 2026-08-14). v4.2.0–v4.4.0 have tags and no rows.
- **G-3 / D-12 / §7:** `frontcomposer-eventstore-approved-runtime-identity-v1.json` approved source `bb94d93e…`, package `3.91.1`, Builds `a8a50859…`; `currentCompatibility` `3.103.0`, `migrationApprovalClaimed: false`. Story 11.24 done 2026-08-29.
- **G-5:** `tests/9-8-live-acceptance.md` live proof passed 2026-08-27; `sprint-status.yaml` `epic-9: in-progress` with E9-AI-1…6 open; stories 9.1–9.8 `done`. DW-679 open in `deferred-work.md`.
- **G-6:** no Hexalith.Tenants bootstrap evidence artifact exists under `_bmad-output/implementation-artifacts/evidence/` or `tests/`.
- **§8.2 header:** `sprint-status.yaml` `last_updated: 2026-08-29`.
- **REL-1…REL-5:** all story files `status: done`; REL-5 dated 2026-08-04 for the unsigned/RFC 3161 decision; NuGet.org signer-policy confirmation recorded 2026-08-13.
- **FR-24 trigger model:** `release.yml` is `workflow_dispatch` only, exact-SHA `uses:` on `domain-release.yml`, `verify-source` consumes the AD-13 handoff; no `workflow_run` trigger and no caller-side freeze guard.
- **FR-12 / NFR-8 / NFR-9:** `ProjectionHubRetryPolicy.MaxDelayMilliseconds = 30_000` (jittered exponential, unbounded attempts); `FcShellOptions` `ProjectionFallbackPollingIntervalSeconds = 15`, `MaxProjectionFallbackPollingLanes = 8` (range 1–100), `ProjectionReconnectedNoticeDurationMs = 3_000`; `ProjectionSubscriptionService.ClosedRestartTimeout = 10 s`.
- **FR-15 budgets:** `TimeoutActionThresholdMs = 10_000`, `PendingCommandPollingIntervalMs = 1_000`, `MaxPendingCommandPollingDurationMs = 120_000`, `CommandDispatchRetryDelayMs = 250`, `CommandTargetResolutionTimeoutMs = 500`.
- **FR-1 five files:** `FrontComposerGenerator.cs:201–205` emits `.g.razor.cs`, `Feature.g.cs`, `Actions.g.cs`, `Reducers.g.cs`, `Registration.g.cs`. HFC1003 = non-partial projection; HFC1006 = missing `MessageId`; HFC1009 = no public parameterless ctor.
- **FR-9:** `FrontComposerShell.razor.css:31` `max-inline-size: var(--fc-page-max-inline-size, 75rem)`; FC-LYT contract confirmed 2026-06-21 (FullWidth default); `DataGridDensityMetrics.cs:25` Compact = 32.
- **FR-11:** `RazorModelTransform.cs:93` `columns.Count > 15` → HFC1029 + `FcColumnPrioritizer`.
- **FR-13:** `CommandFormEmitter.cs` EventIds 5912 (`CommandFormTargetResolutionFailed`) / 5913 (`CommandFormTargetResolutionSucceeded`); HFC1005 reused, no new FC-NIP ID.
- **FR-19 / D-14:** `McpSchemaNegotiationResultKind` matches the D-14 kinds; `tools/list` returns empty `Tools` on gate failure (fail closed); missing gate registrations throw `InvalidOperationException`; wire tokens `unknown_tool`, `unknown_resource`, `Request failed.`, `schema-mismatch` present in `src/Hexalith.FrontComposer.Mcp/`.
- **FR-29 table:** H2/M1→11.1, H6→11.2, H4→11.3, H7/H9→11.4, H10→11.7, H11→11.8, H12→11.19 all match story Change Logs in `epics.md`; H1/H3/H5/H8 were the PR #48 minor batch (epics.md 1645–1648); 11.20–11.23 analyzer phases done (11.23 on 2026-08-08).
- **FR-30:** `TenantContextException` exists; `ProjectionSubscriptionService` group key = `{ProjectionType, TenantId, Scope}`; `StorageKeys.BuildKey` → `{tenant}:{user}:{feature}[:{discriminator}]`; `StorageScopeResolver` logs HFC2105 `StoragePersistenceSkipped` when tenant/user missing; `EventStoreActionQueueCountReader.GetCountAsync` returns 0 for null/whitespace tenant; `ScopeReadinessGate` dispatches `StorageReadyAction` only with tenant+user.
- **NFR-1 / §7:** `global.json` SDK `10.0.400`, `rollForward: latestPatch`; Fluent UI `5.0.0-rc.5-26219.1` from the Builds catalog.
- **§11 package table:** TFMs, `pack_as_tool` for the CLI, `Contracts.UI` baseline override, `Shell/PublicAPI.FcTbl.Shipped.txt`, `Testing/PublicAPI.Shipped.txt` all match `eng/release-package-inventory.json`, the `.csproj` files, and on-disk baselines; AppHost is never published.
- **D-11 dates:** issue 17 closed 2026-07-20, reopened 2026-08-08, owner-accepted revision `a8a50859…` (g2 request); frontmatter still "revision pending" (OI-7 valid).
- **Addendum §2 ceilings** (4,096 edges; 64 MiB; 1 MiB; 4 MiB; 16,384 files; 16 MiB; 256 MiB) match `eng/dependency-graph-policy.json` limits.
