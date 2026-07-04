# Test Automation Summary

## Story 10.4 - HFCM9002 production-emission decision

### Generated Tests
- [x] `tests/e2e/specs/hfcm9002-production-emission-decision.spec.ts` - added focused Playwright repository-governance and CLI process-boundary coverage for the HFCM9002 production-emission decision.
- [x] `tests/e2e/package.json` - added `test:story-10-4` for the focused Chromium lane with `PLAYWRIGHT_SKIP_WEBSERVER=1`.

### API Tests
- [x] Not applicable - Story 10.4 changes CLI/docs governance and does not introduce HTTP API endpoints.

### E2E Tests
- [x] `tests/e2e/specs/hfcm9002-production-emission-decision.spec.ts` - verifies the not-approved decision record, adopter-facing docs boundary, synthetic sidecar JSON/text behavior, fail-on-findings behavior, and hostile sidecar path redaction.

### Coverage
- API endpoints: 0/0 applicable.
- UI runtime features: 0/0 applicable because Story 10.4 is CLI/docs governance.
- HFCM9002 governance/process cases: 4/4 authored and 4/4 passing. The suite covers the decision artifact, normal-build/adopter-build wording guards, synthetic manual-only HFCM9002 text/JSON output, `--fail-on-findings`, and `__sidecar__/...` hostile-path redaction.

### Validation
- [x] `npm --prefix tests/e2e run typecheck` passed.
- [x] `npm --prefix tests/e2e run test:story-10-4` passed 4/4.
- [x] The Playwright runner uses `PLAYWRIGHT_SKIP_WEBSERVER=1` because Story 10.4 does not require the Counter sample host.

### Checklist
- [x] API tests generated if applicable: N/A, no endpoint surface.
- [x] E2E tests generated for the existing CLI/docs governance surface.
- [x] Tests use standard Playwright and Node subprocess/filesystem APIs already used by repository CLI/docs specs.
- [x] Tests cover happy paths: recorded not-approved decision, explicit synthetic-only docs wording, and valid synthetic sidecar output.
- [x] Tests cover critical error cases: accidental adopter/normal-build production sidecar promises, `--fail-on-findings` manual-only promotion, and hostile sidecar path redaction.
- [x] Tests have clear descriptions, no hardcoded waits, and no order dependency.
- [x] Test summary updated with coverage metrics and exact validation results.

## Story 10.3 - CLI text-output parity guard

### Generated Tests
- [x] `tests/e2e/specs/cli-text-output-parity.spec.ts` - added focused Playwright CLI coverage for inspect and migrate text-output parity at the process boundary.
- [x] `tests/e2e/package.json` - added `test:story-10-3` for the focused Chromium lane with `PLAYWRIGHT_SKIP_WEBSERVER=1`.

### API Tests
- [x] Not applicable - Story 10.3 hardens CLI process output and does not introduce HTTP API endpoints.

### E2E Tests
- [x] `tests/e2e/specs/cli-text-output-parity.spec.ts` - repository E2E coverage runs the FrontComposer CLI against disposable temp projects and verifies JSON/text parity for shared summary fields, filtering before fail flags, migrate `--fail-on-findings`, and default path redaction.

### Coverage
- API endpoints: 0/0 applicable.
- UI runtime features: 0/0 applicable because Story 10.3 is CLI contract hardening.
- CLI parity cases: 2/2 authored and 2/2 passing. The suite covers inspect JSON/text summaries and filtered text fail flags, plus migrate JSON/text summaries and text fail-on-findings behavior for changed, manual-only, and unchanged outcomes. Diff-budget parity remains covered by the existing C# CLI renderer tests because synthesizing aggregate diff exhaustion through Playwright would add slow fixture churn without improving the CLI boundary.

### Validation
- [x] `npm --prefix tests/e2e run typecheck` passed.
- [x] `npm --prefix tests/e2e run test:story-10-3` passed 2/2.
- [x] The Playwright runner prefers the existing Release CLI binary when present to avoid sandbox-blocked NuGet restore, and falls back to `dotnet run --no-restore` for clean CI workspaces.

### Checklist
- [x] API tests generated if applicable: N/A, no endpoint surface.
- [x] E2E tests generated for the existing CLI process surface.
- [x] Tests use standard Playwright and Node subprocess/filesystem APIs already used by repository CLI specs.
- [x] Tests cover happy paths: inspect and migrate text summaries mirror JSON contract values.
- [x] Tests cover critical error cases: inspect filtering occurs before fail flags; migrate text `--fail-on-findings` returns actionable for safe-fix/manual-only and success for unchanged-only.
- [x] Tests use semantic process-boundary assertions, have clear descriptions, no hardcoded waits, and no order dependency.
- [x] Test summary updated with coverage metrics and exact validation results.

## Story 10.2 - Adopter-facing historical-label cleanup

### Generated Tests
- [x] `tests/e2e/specs/adopter-historical-label-cleanup.spec.ts` - added focused Playwright documentation-governance coverage for the Story 10.2 adopter-facing cleanup.
- [x] `tests/e2e/package.json` - added `test:story-10-2` for the focused Chromium lane with `PLAYWRIGHT_SKIP_WEBSERVER=1`.

### API Tests
- [x] Not applicable - Story 10.2 changes documentation/governance surfaces and does not introduce HTTP API endpoints.

### E2E Tests
- [x] `tests/e2e/specs/adopter-historical-label-cleanup.spec.ts` - repository E2E coverage reads the published adopter docs and retained registry/provenance surfaces directly, matching the existing contract-docs Playwright pattern.

### Coverage
- API endpoints: 0/0 applicable.
- UI runtime features: 0/0 applicable because Story 10.2 is docs/governance-only.
- Documentation governance cases: 3/3 authored and 3/3 passing. The suite covers current-contract wording across four adopter pages, preservation of 9.1/9.2 version and migration API facts, and AC2 retention of Story 9 provenance in metadata/internal registry surfaces only.

### Validation
- [x] `npm run test:story-10-2` passed 3/3.
- [x] `npm run typecheck` passed.
- [ ] Existing required docs and broad solution blockers remain as recorded in the Story 10.2 Test Evidence: full `pwsh ./eng/validate-docs.ps1 -SkipDocFx` is blocked by pre-existing snippet/MSBuild failures, and the solution `dotnet test` lane is socket/NuGet-audit blocked locally.

### Checklist
- [x] API tests generated if applicable: N/A, no endpoint surface.
- [x] E2E tests generated for the existing documentation governance surface.
- [x] Tests use standard Playwright and Node filesystem APIs already used by repository contract-docs specs.
- [x] Tests cover the happy path: adopter-facing docs name current contracts/features.
- [x] Tests cover critical error cases: stale Story 9 ownership labels in visible body copy, accidental replacement of product version/API facts, and accidental removal of allowed provenance metadata.
- [x] Tests have clear descriptions, no hardcoded waits, and no order dependency.
- [x] Test summary updated with coverage metrics and exact validation results.

## Story 10.1 - Mechanical story evidence reconciliation

### Generated Tests
- [x] `eng/tests/test_validate_story_artifacts.py` - added focused review-verifier coverage proving an incomplete review reports `workflow_not_complete` separately from the red `artifact_validation_failed` gate case.

### API Tests
- [x] Not applicable - Story 10.1 changes repository governance tooling and story-automator verification behavior, not HTTP API endpoints.

### E2E Tests
- [x] `eng/tests/test_validate_story_artifacts.py` - standard-library integration tests exercise temporary git repositories and the story-automator review verifier path end to end for the tooling workflow.

### Coverage
- API endpoints: 0/0 applicable.
- UI features: 0/0 applicable.
- Governance/tooling regression cases: 8/8 authored and 8/8 passing. The review-promotion gate was implemented in `.agents/skills/bmad-story-automator/src/story_automator/core/success_verifiers.py` during the 2026-07-04 Senior Developer Review (the `.agents` files were writable), so `review_completion` now runs `eng/validate-story-artifacts.py` and returns `artifact_validation_failed` before accepting a `done` review. The review also added `test_dotfile_file_list_entry_reconciles_without_stripping_leading_dot` after fixing a leading-dot parsing bug and a brittle bare-basename task-evidence check in the validator.

### Validation
- [x] `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py` passed.
- [x] `python3 -m unittest eng.tests.test_validate_story_artifacts.StoryArtifactValidatorTests` passed 5/5.
- [x] `python3 -m unittest eng.tests.test_validate_story_artifacts.ReviewVerifierTests.test_incomplete_review_reports_workflow_not_complete` passed 1/1.
- [x] `python3 -m unittest eng.tests.test_validate_story_artifacts` passed 8/8 after the review-promotion gate was implemented. `ReviewVerifierTests.test_artifact_validation_failure_prevents_done_review_completion` now passes because `review_completion` invokes `eng/validate-story-artifacts.py` and returns `artifact_validation_failed` before accepting a `done` review.
- [x] Implementation fix landed in `.agents/skills/bmad-story-automator/src/story_automator/core/success_verifiers.py` (`_artifact_validation_gate`) during the 2026-07-04 Senior Developer Review; the earlier dev-story sandbox write blocker did not apply in the review session.

### Checklist
- [x] API tests generated if applicable: N/A, no endpoint surface.
- [x] E2E/integration tests generated for the existing governance workflow surface.
- [x] Tests use standard Python `unittest` and subprocess APIs already used in this repo tooling lane.
- [x] Tests cover happy path taxonomy for incomplete reviews and critical error cases for File List drift, stale checked tasks, documented unrelated files, and submodule-pointer paths.
- [x] Tests have clear descriptions, no hardcoded waits, and no order dependency.
- [x] Test summary updated with coverage metrics and the exact remaining implementation blocker.

## Story 9.2 - Wire `FcNewItemIndicator` producer and generated-grid consumer

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStorePendingCommandStatusQueryTests.cs` - added aggregate-id-only negative coverage proving EventStore status polling does not promote `AggregateId` into FC-NIP row identity metadata.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` - added generated command form coverage proving pending registrations do not fabricate runtime row/lane/status metadata.
- [x] `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` - extended the Playwright contract spec to pin Story 9.2's blocked gate and current no-smuggling source evidence.

### API Tests
- [x] Not applicable - Story 9.2 remains blocked by the missing framework-controlled row-identity payload and no HTTP API endpoint was changed.

### E2E Tests
- [x] `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` - runs in Chromium with `PLAYWRIGHT_SKIP_WEBSERVER=1` because the tested surface is repository contracts/story/source evidence, not the Counter sample host.

### Coverage
- API endpoints: 0/0 applicable.
- UI runtime features: 0/0 new runtime producer/consumer behavior implemented because the Story 9.2 blocking gate remains active.
- Contract/seam regressions: 3/3 focused additions cover aggregate-id-only EventStore status input, generated form metadata fabrication, and Story 9.2 blocked-source evidence.

### Validation
- [x] Direct xUnit v3 fallback passed 21/21: `EventStorePendingCommandStatusQueryTests`.
- [x] Direct xUnit v3 fallback passed 34/34: `CommandFormEmitterTests`.
- [x] `PLAYWRIGHT_SKIP_WEBSERVER=1 npx playwright test specs/fc-nip-row-identity-contract.spec.ts --project=chromium` passed 4/4.
- [ ] `DiffEngine_Disabled=true dotnet test ...Shell.Tests.csproj --filter "FullyQualifiedName~EventStorePendingCommandStatusQueryTests" -m:1 /nr:false` built the focused project, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`; direct xUnit fallback was used.
- [ ] `DiffEngine_Disabled=true dotnet test ...SourceTools.Tests.csproj --filter "FullyQualifiedName~CommandFormEmitterTests" -m:1 /nr:false` built the focused project, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`; direct xUnit fallback was used.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false --no-restore` built into test assemblies, then VSTest aborted across reached assemblies with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] Initial Playwright execution without `PLAYWRIGHT_SKIP_WEBSERVER=1` failed before browser assertions because Counter.Web/Kestrel could not bind a socket: `System.Net.Sockets.SocketException (13): Permission denied`.

### Checklist
- [x] API tests generated if applicable: N/A, no endpoint surface changed.
- [x] E2E tests generated/updated for the repository contract/story evidence surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy-path contract evidence and critical negative cases: aggregate-id-only input and generated metadata fabrication.
- [x] Tests have clear descriptions, no hardcoded waits, and no order dependency.
- [x] Test summary updated with coverage metrics and exact local blockers.

## Story 9.1 - Confirm the FC-NIP row-identity producer contract

### Generated Tests
- [x] `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` - added Playwright-side contract automation for the FC-NIP payload decision, Story 9.2 blocking gap, no-guessing guardrails, and FC-NIP ownership wording.

### API Tests
- [x] Not applicable - Story 9.1 confirms a documentation/contract payload decision and does not introduce an API endpoint.

### E2E Tests
- [x] `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` - runs in Chromium with `PLAYWRIGHT_SKIP_WEBSERVER=1` because the tested surface is repository contract/docs, not the sample host.
- [x] `tests/e2e/package.json` - added `test:fc-nip` focused lane.

### Coverage
- API endpoints: 0/0 applicable.
- UI runtime features: 0/0 applicable for Story 9.1.
- Contract/documentation decisions: 3/3 focused Playwright tests cover minimum payload fields, blocked producer gap, forbidden identity guessing paths, and cross-document FC-NIP ownership.

### Validation
- [x] `npm run test:fc-nip` passed 3/3.
- [x] `npm run typecheck` passed.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated for the existing contract/docs surface in the project Playwright workspace.
- [x] Tests use standard Playwright APIs.
- [x] Tests cover the happy path: confirmed minimum payload and FC-NIP ownership references.
- [x] Tests cover critical error cases: blocked Story 9.2 producer wiring, insufficient `AggregateId`, forbidden `ResultPayload`, projection nudge, row diffing, and broad lane marking paths.
- [x] Tests have clear descriptions, no hardcoded waits, and no order dependency.
- [x] Test summary updated with coverage metrics.


## Story 8.7 - Status as colored icon

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcStatusIconTests.cs` - added exhaustive status-icon slot mapping, localized accessible-name, focusable target, tooltip anchoring, and no-`FluentBadge` pins.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/BadgeProjectionRenderTests.cs` - updated generated runtime render coverage to require `fc-status-icon` instances with contextual `aria-label`s and slot attributes, plus fail-soft plain text for unannotated and out-of-range enum states.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs` and `RoleSpecificProjectionApprovalTests.cs` - updated generator assertions for `FcStatusIcon`, preserving nullable, plain-text, and unknown fallback paths.
- [x] SourceTools Verify snapshots intentionally regenerated for badge-column and role-specific projection approvals.

### E2E Tests
- [x] `tests/e2e/specs/specimen-accessibility.spec.ts` - updated the type specimen to expect six `fc-status-icon` instances and added focus/hover/tap tooltip coverage.
- [x] `npm run typecheck` in `tests/e2e` passed.
- [ ] Local Playwright execution is blocked before browser assertions because Counter.Web/Kestrel cannot bind a socket in this sandbox: `System.Net.Sockets.SocketException (13): Permission denied`.

### Coverage
- AC1: generated `[ProjectionBadge]` enum members now render `FcStatusIcon` with Success checkmark-circle, Danger dismiss-circle, Neutral question-circle, Warning warning glyph, Info info-circle, and Accent star with `Color.Accent`.
- AC2: icon targets are focusable, carry contextual localized `aria-label`s, and anchor `FluentTooltip` with `UseTooltipService=false`.
- AC3: `FcDesaturatedBadge`, `FcStatusFilterChips`, navigation count/"New" badges, and home/count surfaces remain `FluentBadge` pill surfaces; focused regression lane is green.
- Critical fail-soft paths: generated render coverage now proves declared unannotated enum members render as plain text and out-of-range enum values render the localized `Unknown` fallback without a status icon.
- AC4: data grid docs, component inventory, diagnostics comments, generator comments, tests, and snapshots updated for the status-icon model while preserving existing resource keys.
- AC5: Release builds and `FluentConformanceTests` pass; no package changes, raw interactive controls, legacy tokens, accent background fills, or `obj/**` edits were introduced.

### Validation
- [x] RED phase: focused Shell test lane failed before implementation because `FcStatusIcon` and `StatusIconTable` did not exist.
- [x] `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] QA gap fix: added runtime generated-render coverage for unannotated and unknown enum states in `BadgeProjectionRenderTests`.
- [x] Focused Shell direct xUnit v3 lane passed 19/19: `FcStatusIconTests` and `BadgeProjectionRenderTests`.
- [x] Focused non-generated badge regression lane passed 62/62: `FcDesaturatedBadgeTests`, `FcStatusFilterChipsTests`, `FrontComposerNavigationTests`, `FrontComposerNavigationCapabilityBadgeTests`, and `FluentConformanceTests`.
- [x] Focused SourceTools direct xUnit v3 lane passed 41/41 during dev-story; QA re-run of badge emitters, role-specific approvals, and scope guardrail passed 28/28.
- [x] Broad Shell non-Contract direct xUnit v3 lane passed 2005/2005.
- [ ] Broad SourceTools direct xUnit v3 default lane ran 1031 tests with 1029 passing and 2 pre-existing Story 8.6 `docs/reference/components/page-toolbar.md` FC-DOC failures outside Story 8.7.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` built into test assemblies, then VSTest aborted across assemblies with `System.Net.Sockets.SocketException (13): Permission denied`. The run also reported skipped nested Tenants/Memories projects per submodule rules and NuGet audit network blocking.
- [ ] `npm run test:a11y -- --grep "status icons|type specimen renders"` / `npm --prefix tests/e2e run test -- --grep "status icons|type specimen renders" --project=chromium` failed before browser assertions because Counter.Web/Kestrel could not bind: `System.Net.Sockets.SocketException (13): Permission denied`.

### Checklist
- [x] Tests cover generated status icon happy paths, every `BadgeSlot`, localization, tooltip anchoring, generator parity, generated fail-soft enum paths, and non-generated badge regression surfaces.
- [x] E2E coverage was updated and typechecked; local browser execution is socket-blocked.
- [x] Story-owned focused lanes are green through the direct xUnit v3 in-process runner.
- [x] Test summary updated with exact counts, snapshot regeneration, VSTest blocker, and Playwright/Kestrel blocker.

## Story 8.6 - Reusable FcPageToolbar

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageToolbarTests.cs` - added reusable toolbar bUnit coverage for default rendering, search value/callback, accessible names, stable selectors, optional filter popover, view menu, right-aligned actions, optional tabs, and aggregate-list composition through `FcAggregateListPage.Toolbar`.
- [x] `samples/Counter/Counter.Specimens/FrontComposerPageToolbarSpecimen.razor` - added a browser-visible Story 8.6 specimen route with live search state, filter popover content, view menu items, refresh action, and tab state.

### E2E Tests
- [x] `tests/e2e/specs/page-toolbar.spec.ts` - added focused Playwright coverage for the toolbar user workflow, narrow viewport wrapping, and blocking axe scan.
- [x] `tests/e2e/page-objects/page-toolbar-specimen.page.ts` - added semantic locators for the toolbar specimen route.
- [x] `tests/e2e/specimens/frontcomposer-specimen-manifest.json` - registered the page-toolbar specimen in the shared specimen accessibility manifest.
- [x] `tests/e2e/package.json` - added `test:fc-page-toolbar` for the focused Story 8.6 Chromium lane.
- [x] `npm --prefix tests/e2e run typecheck` passed for the existing Playwright workspace.
- [ ] Local Playwright browser execution remains blocked before browser assertions because Kestrel cannot bind a socket in this sandbox.

### Coverage
- AC1: `FcPageToolbar` renders one `role="toolbar"` row with `FluentTextInput TextInputType.Search`, stable selectors, optional filter trigger/popover, optional view menu, and right-aligned actions without changing `FcPageHeader` landmarks.
- AC2: optional `FluentTabs` render only when tabs are supplied; `ActiveTabId` and `ActiveTabIdChanged` stay caller-owned through `FcPageToolbarTab`.
- AC3: `FcAggregateListPage.Toolbar` composes `<FcPageToolbar>` through the existing `FcPageHeader.Actions` seam; no `FcPageHeader` or `FcAggregateListPage` parameter changes were needed.
- AC4: `FluentConformanceTests` stayed green; no raw interactive controls, legacy Fluent tokens, accent background fills, package changes, or PublicAPI baseline changes were introduced.
- AC5: `docs/reference/components/page-toolbar.md` was authored and linked from `docs/reference/components/index.md`; `docs/toc.yml` was not changed.
- Browser E2E coverage now targets the full `FcPageToolbar` workflow through the Counter specimen host: search callback state, filter popover, view menu, right-aligned action callback, tabs callback, narrow viewport reachability, and axe blocking checks.

### Validation
- [x] `node -e "JSON.parse(...)"` parsed `tests/e2e/package.json` and `tests/e2e/specimens/frontcomposer-specimen-manifest.json` successfully.
- [x] `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings / 0 errors after adding the Story 8.6 specimen route.
- [x] `npm --prefix tests/e2e run typecheck` passed after adding the Story 8.6 Playwright spec and page object.
- [x] `PLAYWRIGHT_SKIP_WEBSERVER=1 npm --prefix tests/e2e run test:fc-page-toolbar -- --list` discovered 3 Chromium tests in `page-toolbar.spec.ts`.
- [x] `PLAYWRIGHT_SKIP_WEBSERVER=1 npm --prefix tests/e2e run test -- --list specs/specimen-accessibility.spec.ts --project=chromium` discovered the shared `page-toolbar specimen is nonblank and passes blocking axe gate` manifest test.
- [x] `git diff --check -- ...Story 8.6 files...` passed.
- [x] RED phase: new `FcPageToolbarTests` failed the Release Shell.Tests build before implementation because `FcPageToolbar` and `FcPageToolbarTab` did not exist.
- [x] `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] Focused Shell direct xUnit v3 lane passed 46/46: `FcPageToolbarTests`, `FcPageHeaderTests`, `FcAggregateListPageTests`, and `FluentConformanceTests`.
- [x] Broad Shell direct xUnit v3 non-Contract lane passed 1987/1987 with `-trait- Category=Performance -trait- Category=e2e-palette -trait- Category=NightlyProperty -trait- Category=Quarantined -trait- Category=Contract`.
- [x] `npm --prefix tests/e2e run typecheck` passed.
- [ ] `npm --prefix tests/e2e run test:fc-page-toolbar` failed before browser assertions because the Counter web server could not bind Kestrel: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `pwsh ./eng/validate-docs.ps1` failed during DocFX metadata before page validation because Roslyn/MSBuild build-host socket creation is blocked: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `pwsh ./eng/validate-docs.ps1 -SkipDocFx` reached repo-wide validation but failed on pre-existing docs issues unrelated to Story 8.6: existing compile snippets in `customization-gradient-cookbook.md`, `test-generated-components.md`, and `getting-started.md`, plus stale producer hashes for Stories 9.1-9.3 artifacts.
- [ ] `pwsh ./eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` still failed on the same pre-existing stale producer hashes for Stories 9.1-9.3.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 /nr:false` failed in the Tenants submodule because nested `Hexalith.Tenants/Hexalith.Memories/...` projects are not initialized; nested submodules were not initialized per repository rules. The run also reported NuGet vulnerability data access blocked for `api.nuget.org:443`.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` built into test assemblies, then VSTest aborted across assemblies with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `npm --prefix tests/e2e run test:fc-shell-chrome` failed before browser assertions because the Counter web server could not bind Kestrel: `System.Net.Sockets.SocketException (13): Permission denied`.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: toolbar-specific browser spec, page object, specimen route, manifest entry, and npm script were added; local browser execution is socket-blocked, and the e2e workspace typecheck passed.
- [x] Tests use standard xUnit v3, Shouldly, bUnit, and Fluent component test patterns.
- [x] Tests cover happy paths, optional-slot absence, filter/menu/tabs/actions paths, callback wiring, aggregate-list composition, narrow viewport reachability, and blocking a11y checks.
- [x] Story-owned focused and broad Shell lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Test summary updated with counts, docs validation caveats, solution VSTest blocker, and Playwright/Kestrel blocker.

### Senior Developer Review (AI) - 2026-06-25
- [x] HIGH dead-CSS fix: `FcPageToolbar.razor.css` scoped every rule with the component `[b-hash]`, but the `.razor` has no raw HTML element to carry that scope, so AC1's right-aligned actions and the search width/wrap sizing were inert (the Story 8.4 trap; confirmed against the generated `scopedcss` bundle). Moved layout to rendered inline `Style` (`margin-inline-start:auto`, `flex/min/max-width`, filter-panel `min-width`) and deleted the dead CSS file.
- [x] Added `FcPageToolbar_AppliesLayoutViaRenderedInlineStyle_NotDeadScopedCss` proving the right-alignment and search sizing are on rendered DOM, not dead CSS.
- [x] Release Shell.Tests build re-verified 0 warnings / 0 errors after the fix.
- [x] Focused direct xUnit v3 lane (`FcPageToolbarTests`, `FcPageHeaderTests`, `FcAggregateListPageTests`, `FluentConformanceTests`) passed **47/47** (was 46/46; +1 regression test).
- [x] Broad Shell non-Contract direct xUnit v3 lane passed **1988/1988** (was 1987/1987); `FluentConformanceTests` stayed green (inline styles are pure layout, no raw controls / legacy tokens / accent fills).
- [ ] VSTest and Playwright browser lanes remain socket-blocked locally (unchanged sandbox limitation).
- Noted-not-changed (LOW): inner `data-testid`s and the default filter-trigger `id` are hardcoded literals per the story task contract, so multiple toolbars per page would collide — acceptable for the single-instance MVP, documented for future multi-instance hosts.

## Story 8.5 - Icon+label navigation rail and projection flyout

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs` - replaced full-nav/collapsed-rail assertions with unified 72px/48px rail, `FluentMenu` flyout, route `data-href`, active context, and badge pins.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationNavEntryTests.cs` - migrated explicit entry, policy, disabled reason, orphan context, and single-active assertions to flyout menu semantics.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`, `Story13AccessibilityPrimitivesTests.cs`, and `Story14ShellStringOwnershipTests.cs` - updated shell width and localized navigation landmark pins for the rail.
- [x] Deleted stale `FcCollapsedNavRailTests.cs`; replacement rail-mode coverage lives in `FrontComposerNavigationTests`.

### E2E Tests
- [x] `tests/e2e/page-objects/shell.page.ts` - remapped legacy `fullNav` / `collapsedRail` locators to the Story 8.5 72px and 48px rail modes, and added Counter flyout/projection locators plus a keyboard-open helper.
- [x] `tests/e2e/specs/sidebar-responsive.spec.ts` - updated responsive, persistence, and Counter projection flyout checks for the unified rail.
- [x] `tests/e2e/specs/sidebar-responsive.spec.ts` - added keyboard coverage for Space/Enter opening, Escape close with focus return, keyboard menu-item activation, single active route pinning, and light/dark rail visual plus axe checks.
- [ ] Local Playwright execution remains blocked before browser assertions because Kestrel cannot bind a socket in this sandbox.

### Coverage
- AC1: Desktop expanded rail is 72px; Desktop collapsed and CompactDesktop rail are 48px; Tablet/Phone still omit the navigation layout item.
- AC2: bounded-context tiles use Fluent buttons/icons, localized accessible names, count badges, "New" badges, filled active icon variants, `aria-current`, and an accent border thread instead of accent background fill.
- AC3: flyout content includes visible projections and explicit nav entries, including orphan entry contexts, with capability seen dispatch before navigation.
- AC4/AC5: `FluentMenu` owns trigger anchoring, menu role, item roles, and roving behavior; bUnit pins trigger id, role/testid splatting, route attributes, disabled reasons, and single-active semantics. Playwright now covers keyboard open, Escape focus return, keyboard activation, and light/dark a11y/visual checks, but browser execution is Kestrel-blocked locally.

### Validation
- [x] RED phase: new Story 8.5 navigation tests failed against the old split implementation (`fc-navigation-rail` missing; flyout menu missing) before implementation.
- [x] `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] Focused Shell direct xUnit v3 lane passed 113/113: `FrontComposerNavigationTests`, `FrontComposerNavigationCapabilityBadgeTests`, `FrontComposerNavigationNavEntryTests`, `FcHamburgerToggleTests`, `FrontComposerShellTests`, `Story13AccessibilityPrimitivesTests`, `Story14ShellStringOwnershipTests`, and `FluentConformanceTests`.
- [x] Broad Shell direct xUnit v3 non-Contract lane passed 1983/1983 with `-notrait Category=Performance -notrait Category=e2e-palette -notrait Category=NightlyProperty -notrait Category=Quarantined -notrait Category=Contract`.
- [x] `npm --prefix tests/e2e run typecheck` passed.
- [x] `npm --prefix tests/e2e run test -- --list specs/sidebar-responsive.spec.ts --project=chromium` discovered 13 Story 8.5 Chromium tests.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` built into test assemblies, then VSTest aborted across assemblies with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `npm --prefix tests/e2e run test:chromium -- specs/sidebar-responsive.spec.ts` failed before browser assertions because the Counter web server could not bind Kestrel: `System.Net.Sockets.SocketException (13): Permission denied`.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests updated for the UI rail/flyout flow; local browser execution is socket-blocked.
- [x] Tests use standard xUnit v3, Shouldly, bUnit, Fluent component, and Playwright project patterns.
- [x] Tests cover happy paths, active-route edge cases, disabled/policy-gated entries, orphan contexts, responsive rail modes, keyboard open/close/activation, focus return, and light/dark visual/a11y guardrails.
- [x] Story-owned focused and broad Shell lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Test summary updated with counts, typecheck evidence, and local blockers.

### Senior Developer Review (AI) re-run - 2026-06-25
- [x] Release build re-verified 0 warnings / 0 errors after auto-fixes.
- [x] Focused lane (`FrontComposerNavigationTests`, `FrontComposerNavigationCapabilityBadgeTests`, `FrontComposerNavigationNavEntryTests`, `FcHamburgerToggleTests`, `FrontComposerShellTests`, `Story13AccessibilityPrimitivesTests`, `Story14ShellStringOwnershipTests`, `FluentConformanceTests`) passed 111/111 via the direct xUnit v3 in-process runner.
- [x] Broad Shell non-Contract lane passed 1984/1984 (two stale UI-coupled nav-group tests removed; reducer behavior still covered by `NavigationReducerTests`).
- [x] AC2 fix: the "filled active icon variant" (line 19) was previously a no-op — `FcFluentIcons.Apps20(Filled)` reused the Regular SVG path, so active/rest tiles rendered an identical glyph. Added a distinct `AppsFilledPath` (denser app-grid) for the Filled variant only; `Apps20(Regular)` and the Story-8.3 logo cell are unchanged, so `FrontComposerShellTests` solid-path pins stay green.
- [ ] VSTest/Playwright browser lanes remain socket-blocked locally (unchanged from dev-story; recorded above).

## Story 8.4 - Compact default density and grid polish

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityPrecedenceTests.cs` - updated resolver coverage for Desktop/CompactDesktop no-preference Compact defaults and Tablet/Phone Comfortable forcing.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsTests.cs` and `tests/Hexalith.FrontComposer.Shell.Tests/State/HydrationTests.cs` - pinned bootstrap Comfortable hydration followed by real Desktop/CompactDesktop viewport recompute to Compact.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs` - pinned reset-to-default Compact behavior and forced-note resolver behavior when factory default would otherwise be Compact.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/DataGridDensityMetricsTests.cs` - pinned Compact/Comfortable/Roomy virtualization row-height values.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` - added static CSS guard for generated projection-grid class, `--fc-spacing-unit`, and `--colorSubtleBackgroundHover`.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs` - pinned generated `Class`, `GenerateHeader`, `ItemSize`, and `SetKey(_density)` together.

### E2E Tests
- [x] `tests/e2e/specs/settings-persistence.spec.ts` - added fresh-session Compact default, Restore defaults back-to-Compact, and Tablet forced-Comfortable coverage using the existing settings workflow.
- [x] `tests/e2e/page-objects/settings.page.ts` - added a semantic page-object action for the dialog `Restore defaults` button.
- [x] `tests/e2e/specs/fc-tbl-contract.spec.ts` - extended generated-grid render contract coverage to require `.fc-projection-grid`, inherited compact spacing, and `body[data-fc-density="compact"]` for specimen sessions.
- [x] `tests/e2e/tsconfig.json` - corrected the stale TypeScript `ignoreDeprecations` value so the e2e workspace typecheck runs under the installed compiler.
- [ ] Local Playwright execution remains blocked before browser assertions because Kestrel cannot bind a socket in this sandbox.

### Coverage
- AC1: Desktop and CompactDesktop unset sessions resolve Compact after viewport measurement; Tablet/Phone still force Comfortable.
- AC2: live preference selection, persisted `DensityLevel?` schema, reload hydration, and Restore defaults behavior remain covered.
- AC3: generated-grid styling is scoped to `.fc-projection-grid`, uses density spacing and Fluent 2 hover token, and keeps row-height metrics unchanged at 32/44/56.
- AC4: standard and status-overview generated grids emit Fluent v5 `DataGridGeneratedHeaderType.Sticky` and do not use the v4 enum name.

### Validation
- [x] RED phase: focused Shell lane failed 8 expected tests before implementation; SourceTools virtualization lane failed 1 expected test before emitter changes.
- [x] `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] Focused Shell direct xUnit v3 lane passed 50/50: density precedence/effects/hydration/settings/dialog metrics plus `FluentConformanceTests`.
- [x] Focused SourceTools virtualization lane passed 8/8.
- [x] SourceTools emitter approval lane passed 29/29 after intentionally updating generated-output snapshots.
- [x] Broad SourceTools in-process default lane passed 1026/1026.
- [x] Broad Shell in-process lane excluding socket-bound Contract/Pact tests passed 1987/1987 after intentionally updating two Shell generated render snapshots.
- [x] `npm --prefix tests/e2e run typecheck` passed after correcting `ignoreDeprecations`.
- [x] `npm --prefix tests/e2e run test -- --list specs/settings-persistence.spec.ts specs/fc-tbl-contract.spec.ts --project=chromium` discovered 10 tests in 2 files.
- [ ] Full Shell in-process lane ran 1990 tests: 1989 passed, 1 Pact Contract test failed because PactNet could not start a local mock server in this sandbox.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false --no-restore` built through test assemblies, then VSTest aborted across assemblies with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `npm run test --prefix tests/e2e -- specs/settings-persistence.spec.ts specs/fc-tbl-contract.spec.ts --project=chromium` failed before browser assertions because the Counter web server could not bind Kestrel: `System.Net.Sockets.SocketException (13): Permission denied`.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated/updated if UI exists; local browser execution is socket-blocked.
- [x] Tests use standard xUnit v3, Shouldly, bUnit, Verify, and Playwright project patterns.
- [x] Tests cover happy paths and critical density precedence/settings reset/grid render-contract regressions.
- [x] Story-owned focused lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Test summary updated with counts, typecheck evidence, and local blockers.

## Story 7.1 - frontcomposer inspect

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - added focused inspect pins for generated-file family mapping, v1 MCP manifest file-count semantics, build arguments, fail flags after filtering, warning-only `--fail-on-error` behavior, sidecar optional fields, non-HFC filtering, malformed sidecar sentinels, and hostile path redaction.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/CliHelpTests.cs` - added a help-text pin proving unsupported `--summary` is no longer advertised.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - (Senior Developer Review, AI) added `InspectText_SummaryLineReportsWarningAndErrorTotals` pinning text/JSON summary parity for `Warnings`/`Errors` totals in default text output.

### API Tests
- [x] Not applicable - Story 7.1 has no HTTP API endpoint surface.

### E2E Tests
- [x] CLI E2E-style coverage uses the existing in-process `CliApplication.RunAsync` pattern for the `frontcomposer inspect` user workflow.
- [x] Browser E2E tests are not applicable - Story 7.1 is a CLI inspection feature with no browser-visible UI.

### Coverage
- CLI inspect JSON schema and summary: covered.
- Generated-file family mapping: projection grids, command forms, command renderers, command pages, Fluxor artifacts, registrations, `FrontComposerMcpManifest.g.cs`, and `__FrontComposerProjectionTemplatesRegistration.g.cs` covered.
- Diagnostic handling: severity/type filtering, fail flags, warning-only threshold behavior, missing optional sidecar fields, non-HFC filtering, malformed sidecar sentinel, and hostile sidecar path redaction covered.
- Build behavior: `EmitCompilerGeneratedFiles=true` and `CompilerGeneratedFilesOutputPath=obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer` covered by a narrow build-argument seam; process-level `--build` remains validated by build output and CI.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore -v:minimal` passed with 0 warnings / 0 errors.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -parallel none -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests -class Hexalith.FrontComposer.Cli.Tests.CliHelpTests` passed 17/17 (16 dev + 1 Senior Developer Review parity pin).
- [x] (Senior Developer Review, AI) Full CLI in-process assembly ran 48 tests: 46 passed, 2 failed. The only failures are pre-existing `MigrationCommandTests` solution-selection tests (last committed in Story 11.3, commit `9530136`), unchanged by Story 7.1 and outside the inspect surface.
- [ ] `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-build -v:minimal` aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` from the VSTest socket transport.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: N/A for browser UI; CLI workflow covered through in-process command execution.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] All generated tests run successfully in the focused in-process lane.
- [x] Tests use proper locators: N/A for CLI; assertions target semantic CLI JSON/text fields and exit codes.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Next Steps
- Run the normal VSTest lane in CI or a local environment that permits socket creation.

## Story 7.2 - frontcomposer migrate

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - added focused migration pins for clean `applied=true`, invalid `--format`, mutually exclusive `--dry-run`/`--apply`, `--fail-on-findings`, `nameof(...)` false-positive prevention, excluded path segments, JSON diff budgets, and the two prior solution-selection failures.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - (QA Generate E2E Tests, AI) added public CLI output pins for `schemaVersion: frontcomposer.cli.migrate.v1`, safe-fix entry fields, migration docs link, sanitized diff payload, and manual-only `--fail-on-findings` exit behavior.
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` - (Senior Developer Review, AI) added `MigrationText_CapsPerEntryAndAggregateDiffs` pinning AC6 text-mode per-entry (8,000) and aggregate (64,000) diff-budget parity with the JSON path, closing the previously untested text render path.

### API Tests
- [x] Not applicable - Story 7.2 has no HTTP API endpoint surface.

### E2E Tests
- [x] CLI E2E-style coverage uses the existing in-process `CliApplication.RunAsync` pattern for the `frontcomposer migrate` user workflow.
- [x] Browser E2E tests are not applicable - Story 7.2 is a CLI migration feature with no browser-visible UI.

### Coverage
- Migration catalog: `9.1.0 -> 9.2.0` edge pinned.
- Dry-run/apply: dry-run default/no write, clean apply `applied=true`, idempotent rerun `unchanged`, source-hash drift failure, same-directory temp-file semantics covered by source audit.
- Path safety: `bin`, `obj`, `.git`, `packages`, `.nuget`, `nupkgs`, `/generated/`, submodule root, out-of-project redaction, and hostile sidecar sentinel paths covered.
- Diagnostics: `HFCM9001` safe fix, comments/`nameof` negative controls, unsupported code actions as manual-only, no FixAll, and synthetic `HFCM9002` sidecar manual-only behavior covered.
- Output/failure behavior: JSON schema, entry kinds, summary counts, docs link, diff payload, diff budgets, sanitizer behavior, invalid arguments, unsupported edges, source size/encoding fail-closed behavior, and `--fail-on-findings` for changed/manual-only/unchanged outcomes covered.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests` passed 39/39.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -parallel none -class- Hexalith.FrontComposer.Cli.Tests.ToolPackagingSmokeTests` passed 57/57.
- [x] (QA Generate E2E Tests, AI) `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings / 0 errors after adding the QA pins.
- [x] (QA Generate E2E Tests, AI) `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests` passed 39/39 after adding the QA pins.
- [x] (QA Generate E2E Tests, AI) `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -parallel none -class- Hexalith.FrontComposer.Cli.Tests.ToolPackagingSmokeTests` passed 57/57 after adding the QA pins.
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:RestoreIgnoreFailedSources=true -p:NuGetAudit=false` passed with 0 warnings / 0 errors.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` failed during restore because NuGet vulnerability data access to `api.nuget.org:443` is blocked in this sandbox.
- [ ] Full CLI in-process assembly without exclusions ran 58 tests: 57 passed, 1 environmental packaging smoke failure (`ToolPackagingSmokeTests.DotnetToolPackage_CanInstallAndRunFromLocalManifest`) due NuGet network/tool-cache access.
- [ ] `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~MigrationCommandTests"` compiled, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` from the local socket transport.
- [ ] (QA Generate E2E Tests, AI) `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~MigrationCommandTests" -v:minimal` compiled, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied` from the local socket transport.
- [x] (Senior Developer Review, AI) `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Debug -m:1 /nr:false` passed with 0 warnings / 0 errors after the review fixes (text aggregate diff-budget parity + dead-local removal).
- [x] (Senior Developer Review, AI) Ran the tests via the **direct xUnit v3 in-process executable** (`./bin/Debug/net10.0/Hexalith.FrontComposer.Cli.Tests`), which does not use the VSTest socket transport: `-class Hexalith.FrontComposer.Cli.Tests.MigrationCommandTests` passed **40/40** (39 dev + 1 review pin), and the full CLI in-process assembly passed **59/59** with 0 skipped (incl. `ToolPackagingSmokeTests`, which passed in this environment).
- [x] (Senior Developer Review, AI) AC7 resolution: the two prior solution-selection failures (`ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically`, `ProjectSelection_RejectsSolutionProjectsOutsideSolutionRoot`) are confirmed **passing**, so AC7 is met by the tests-pass branch rather than an environmental reclassification.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: N/A for browser UI; CLI workflow covered through in-process command execution.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] All generated tests run successfully in the focused in-process lane.
- [x] Tests use proper locators: N/A for CLI; assertions target semantic CLI JSON/text fields and exit codes.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Story 7.3 - surface the HFC diagnostic catalog

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Cli.Tests/InspectCommandTests.cs` - added mixed Hidden/Info/Warning/Error threshold pins for JSON and text inspect output, invalid `--severity` validation, and updated fail-flag expectations after threshold filtering.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` - added `SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned`, deriving active/reserved SourceTools rows from the registry and pinning descriptor, release-row, registry, and docs-stub severity parity.
- [x] `tests/e2e/specs/diagnostic-catalog-inspect.spec.ts` - added Playwright process-level CLI E2E coverage for `frontcomposer inspect` severity threshold filtering, invalid severity exit handling, malformed-sidecar `HFCM0002`, non-HFC sidecar filtering, JSON schema, text summary counts, and absolute path redaction.

### API Tests
- [x] Not applicable - Story 7.3 has no HTTP API endpoint surface.

### E2E Tests
- [x] Browser UI E2E tests are not applicable - Story 7.3 changes CLI inspect filtering and diagnostic catalog governance.
- [x] CLI E2E coverage now runs through Playwright by shelling out to the `frontcomposer` CLI project against disposable generated-output sidecars.
- [x] `tests/e2e/package.json` - added `test:fc-diagnostics` for the focused Story 7.3 CLI E2E lane.

### Coverage
- Inspect severity threshold semantics: hidden includes Hidden/Info/Warning/Error; info includes Info/Warning/Error; warning includes Warning/Error; error includes Error only.
- Inspect invalid severity remains `ExitCodes.InvalidArguments` (`2`).
- Inspect warning/error summary counts are calculated after severity filtering in text and JSON output.
- `--fail-on-warning` / `--fail-on-error` remain evaluated after severity and type filtering.
- Sidecar HFC filtering, optional fields, malformed-sidecar `HFCM0002`, and path sanitization remain covered by existing inspect pins.
- HFC1001-HFC1070 SourceTools catalog parity is covered through registry-derived descriptor/release-row/docs-stub checks.
- HFC1056/HFC1057 parser emission remains covered by existing focused parser pins.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors.
- [x] `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests` passed 18/18.
- [x] (Senior Developer Review, AI) `./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -class Hexalith.FrontComposer.Cli.Tests.InspectCommandTests` passed 19/19 after adding `InspectSeverity_Hidden_IncludesNonCanonicalSeverities`, which pins that `--severity hidden` includes non-canonical-severity sidecar entries (the AC2 include-all level) while `error` still excludes them.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -method Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticRegistryTests.SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned` passed 1/1.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticDescriptorTests -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticCatalogTests` passed 24/24.
- [x] Focused HFC1056/HFC1057 parser lane passed 7/7 via direct xUnit v3 in-process runner.
- [x] `DOTNET_CLI_HOME=/tmp/frontcomposer-dotnet-home DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests -noLogo -noColor -class- Hexalith.FrontComposer.Cli.Tests.ToolPackagingSmokeTests` passed 60/60.
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:RestoreIgnoreFailedSources=true` passed with 0 warnings / 0 errors.
- [x] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run typecheck` passed.
- [x] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run test:fc-diagnostics` passed 3/3 in Chromium with `PLAYWRIGHT_SKIP_WEBSERVER=1`.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false --no-restore` failed in `src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj` with `NU1301` because this sandbox blocks `api.nuget.org:443`.
- [ ] Full CLI in-process assembly without exclusions ran 61 tests: 60 passed, 1 environmental packaging smoke failure (`ToolPackagingSmokeTests.DotnetToolPackage_CanInstallAndRunFromLocalManifest`). First run failed on read-only `/home/administrator/.dotnet/toolResolverCache`; rerun with `DOTNET_CLI_HOME=/tmp/frontcomposer-dotnet-home` failed on blocked NuGet access.
- [ ] Broad `DiagnosticRegistryTests` class ran 115 tests: 114 passed, 1 pre-existing governance failure `Story112_LedgerRowsMapToOneOfThreeFinalStates` because `deferred-work.md` is missing. The Story 7.3 catalog parity method passed separately.
- [ ] Configured solution-level `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` aborted locally because VSTest cannot create its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`), even with `-m:1 /nr:false`.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: N/A for browser UI; CLI workflow covered through Playwright process-level command execution.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] Story-owned generated tests run successfully in focused in-process and Playwright CLI E2E lanes.
- [x] Tests use proper locators: N/A for CLI/catalog governance; assertions target semantic CLI JSON/text fields, exit codes, and catalog metadata.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Story 7.4 - opt-in drift detection vs. a baseline

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftAnalyzerConfigOptionsTests.cs` - added a focused alias opt-in pin proving `FrontComposerDriftDetectionEnabled=true` enables drift comparison when `HfcDriftDetectionEnabled` is absent.
- [x] Existing drift tests under `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/**` cover opt-in gating, candidate baseline naming, trust failures, structural drift, metadata drift, diagnostic payloads, ordering/truncation, byte stability, incremental caching, redaction, and HFC1070 trim/AOT isolation.
- [x] Existing diagnostic governance tests cover HFC1058-HFC1070 descriptor, registry, docs-stub, `FcDiagnosticIds`, and `AnalyzerReleases.Unshipped.md` parity.

### API Tests
- [x] Not applicable - Story 7.4 has no HTTP API endpoint surface.

### E2E Tests
- [x] Browser E2E tests are not applicable - Story 7.4 is a build-time SourceTools diagnostic feature.
- [x] Generator-driver style SourceTools tests cover the adopter build-time flow with analyzer-config options and `AdditionalText` baselines.

### Coverage
- Opt-in behavior: `HfcDriftDetectionEnabled` and `FrontComposerDriftDetectionEnabled` enable comparison; disabled drift ignores candidate baselines and leaves generated output stable.
- Baseline trust: HFC1058-HFC1064 and HFC1069 fail closed for missing, configured-path mismatch, empty/malformed/unsupported/oversized/duplicated/invariant-violating, and redaction-unsafe baselines.
- Drift classification: HFC1065 covers structural declaration/property/type/nullability/bounded-context drift; HFC1066 covers renderer and metadata-impacting changes.
- Diagnostic contract: payload property bag, help links, message shape, path normalization, redaction, deterministic ordering, and truncation are covered.
- Incremental/output stability: `LoadDriftBaselines` tracked step name, no `CompilationProvider` dependency for HFC1058-HFC1069 comparison, and diagnostics-only byte stability are covered.
- HFC1070: trim/AOT advisory remains separately registered and compilation-backed only for action-queue catalog evidence.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings / 0 errors after the QA alias pin.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -method Hexalith.FrontComposer.SourceTools.Tests.Drift.Baseline.DriftAnalyzerConfigOptionsTests.FrontComposerDriftDetectionEnabledAlias_EnablesDriftComparison_WhenPrimaryOptionIsAbsent` passed 1/1.
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -class "*Drift*" -class- "*Benchmarks*"` passed 170/170 after the QA alias pin.
- [x] Focused diagnostic parity lane passed 25/25 via direct xUnit v3 in-process runner: `DriftDiagnosticCatalogTests`, `SourceToolsHfc1001ThroughHfc1070_SeverityChannelsStayAligned`, descriptor/release-row parity methods.
- [x] Broad SourceTools in-process lane with default exclusions ran 1023 tests: 1020 passed, 3 failed. Failures are outside Story 7.4: `DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates` (`deferred-work.md` missing), `FcDocComponentDocumentationContractTests.EveryComponentPageContainsAllRequiredSections(datagrid.md)`, and `IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux`.
- [ ] `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --filter "FullyQualifiedName~Drift&Category!=Performance&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` built, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` aborted locally because VSTest cannot create its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`).

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: N/A for browser UI; generator-driver SourceTools tests cover the build-time flow.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] Story-owned focused lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Tests use proper locators: N/A for build-time SourceTools diagnostics; assertions target Roslyn diagnostics, generated outputs, and catalog metadata.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Story 7.5 - Testing library bUnit host and deterministic fakes

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs` - added host wiring pins for JSInterop override, service replacement, TimeProvider registration, domain assembly de-duplication, direct-composition `DuringHostSetup` store initialization, command cancellation/context/lifecycle evidence, query not-modified/empty evidence, projection page not-modified evidence, query/page cancellation, all five deterministic fault modes, redaction/truncation, and generated Counter command dispatch through public Testing APIs.
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` - added package README inclusion pin, hardened Release pack commands with `-m:1 /nr:false`, and made the clean temporary consumer restore/build locally from packed packages without repo-relative project references.

### API Tests
- [x] Public Testing package API baseline remains enforced by `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline`.
- [x] Intentional public API addition: `TestQueryService.NotModifiedWith<T>(IReadOnlyList<T>, string?)`, recorded in `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`.

### E2E Tests
- [x] Browser E2E tests are not applicable - Story 7.5 is a bUnit Testing package story.
- [x] Generated Counter projection and command flows are exercised through bUnit using adopter-facing Testing APIs only.
- [x] Clean temporary consumer package restore/build validates package consumption without repo-relative project references.

### Coverage
- Host wiring: localization, FluentUI components, Shell defaults, in-memory storage, user context, command/query/page-loader fakes, TimeProvider, fault provider, Loose JSInterop default, JSInterop override, culture restoration, direct-composition replacement-before-initialization, and option-driven store initialization covered.
- Fakes: command lifecycle order and deterministic IDs, command context/evidence/redaction, query success/not-modified/empty paths, projection page success/not-modified/empty paths, cancellation before evidence capture, bounded evidence, and parallel context isolation covered.
- Faults: Drop, Delay, PartialDelivery, Reorder, and ReconnectNudge covered with deterministic timestamp/tenant/user/correlation evidence and bounded retention.
- Package/public API: public API baseline, README/package baseline file inclusion, dependency exclusions, Release pack, and clean temporary consumer restore/build covered.

### Validation
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors in 33.96s after the QA projection not-modified evidence pin.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests` passed 23/23 via direct xUnit v3 in-process runner (22/22 before the Senior Developer Review redaction regression pin).
- [x] Senior Developer Review (auto-fix) added `RedactedEvidenceFormatter_Format_RedactsSecretValuesContainingCommas`, which failed before the JSON-string-aware redaction fix in `Evidence.cs` and passes after it; re-verified Release solution build at 0 warnings / 0 errors.
- [ ] `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release -m:1 /nr:false` compiled, then VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false --no-build -c Release` aborted locally because VSTest cannot create its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`) across test assemblies before executing tests.

### Checklist
- [x] API tests generated if applicable: public API baseline and package boundary pins updated.
- [x] E2E tests generated if UI exists: N/A for browser UI; bUnit generated Counter paths and clean consumer package flow cover adopter-facing flows.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases.
- [x] Story-owned focused lane runs successfully through the direct xUnit v3 in-process runner.
- [x] Tests use proper locators: bUnit assertions target semantic component markup and service evidence, not brittle external services.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Story 8.1 - Neutral header chrome and footer framing

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` - added focused shell chrome pins for the neutral header background/divider, default footer neutral frame with `FluentText`, and adopter-supplied footer content inside the framed footer chrome.
- [x] `tests/e2e/specs/shell-chrome.spec.ts` - added focused Playwright coverage that drives the live shell through the existing settings theme control, then asserts header/footer computed styles resolve to `--colorNeutralBackground2`/`--colorNeutralStroke2`, the header does not use the accent surface fill, and title/action contrast remains WCAG AA in light and dark themes.
- [x] `tests/e2e/package.json` - added `test:fc-shell-chrome` for the focused Story 8.1 browser lane.

### API Tests
- [x] Not applicable - Story 8.1 has no HTTP API endpoint surface.

### E2E Tests
- [x] Browser a11y/visual evidence remains owned by `tests/e2e/specs/specimen-accessibility.spec.ts`.
- [x] Story 8.1 browser chrome assertions are now generated in `tests/e2e/specs/shell-chrome.spec.ts`.
- [ ] Local Playwright execution was blocked before browser launch because Kestrel could not create a listening socket in this sandbox; CI remains the browser/a11y/visual gate.

### Coverage
- Header chrome: top-level shell header `FluentStack` keeps `height: 48px`, `padding: 0 12px`, and `HorizontalAlignment.SpaceBetween`, and now uses `--colorNeutralBackground2` plus `--colorNeutralStroke2`.
- Footer chrome: default and adopter-supplied footer content render inside the same neutral `FluentStack` frame with `min-height: 36px`.
- Browser chrome: live shell header/footer computed styles are pinned against Fluent neutral tokens in light and dark themes, with accent-surface regression and contrast checks.
- Fluent governance: no legacy Fluent v4/FAST token was introduced; the focused Shell legacy-token governance method passes.

### Validation
- [x] RED phase: `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -method Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests.HeaderChrome_UsesNeutralSurfaceAndDivider -method Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests.DefaultFooterChrome_UsesNeutralFrameAndFluentText -method Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests.AdopterSuppliedFooter_RendersInsideNeutralFrame` failed 3/3 before the Razor change, proving the new assertions covered the missing behavior.
- [x] `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors.
- [x] Focused Story 8.1 direct xUnit v3 lane passed 3/3 after implementation.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests` passed 27/27.
- [x] `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -method Hexalith.FrontComposer.Shell.Tests.Governance.FluentConformanceTests.Shell_styles_use_no_legacy_fluent_v4_tokens_except_migration_backlog` passed 1/1.
- [x] (QA Generate E2E Tests, AI) `./tests/e2e/node_modules/.bin/tsc --noEmit --ignoreDeprecations 5.0 -p tests/e2e/tsconfig.json` passed for the generated Playwright spec under the installed TypeScript 5.9.3 compiler.
- [ ] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run typecheck` failed before type-checking because local `node_modules` contains stale `typescript@5.9.3` while `package.json`/`package-lock.json` require `typescript@6.0.3`; TS 5.9 rejects `tsconfig.json` `ignoreDeprecations: "6.0"`.
- [ ] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run test:fc-shell-chrome` failed before browser assertions executed because Kestrel could not create a listening socket: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` failed during restore with `NU1900` because this sandbox cannot access NuGet vulnerability data at `api.nuget.org:443`.
- [ ] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:NuGetAudit=false` built Story 8.1-owned Shell projects, then failed in `Hexalith.Tenants.UI` because nested `Hexalith.Memories` submodule projects are intentionally not initialized under the repository submodule rules.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` failed during restore with the same `NU1900` NuGet vulnerability-data network blocker.
- [ ] `npm --prefix tests/e2e run test:a11y` failed before tests executed because the specimen web server could not bind a Kestrel socket: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `npm --prefix tests/e2e run test:visual:update` failed with the same Kestrel socket permission blocker before refreshing light/dark visual baselines.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: `tests/e2e/specs/shell-chrome.spec.ts` now covers Story 8.1 browser chrome in light/dark themes; local execution blocked by sandbox socket restrictions.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover custom/default footer paths and token-governed header/footer chrome.
- [x] Story-owned focused lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Tests use existing user-visible controls, visible shell text, and computed Fluent token assertions without hardcoded waits.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics and local blockers.

## Story 8.3 - Brand/logo cell in header-start

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellParameterSurfaceTests.cs` - appended `HeaderLogo:RenderFragment` and `ShowDefaultHeaderLogo:Boolean` to the locked metadata-order parameter surface.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs` - added default no-logo preservation, adopter logo ordering/precedence, and opt-in default decorative `FcFluentIcons.Apps20()` logo cell coverage.
- [x] `samples/Counter/Counter.Web/Components/Layout/HeaderDefaultLogoLayout.razor` and `HeaderCustomLogoLayout.razor` - added specimen-only shell layouts that exercise opt-in default and adopter-supplied header logo states in the browser host.
- [x] `samples/Counter/Counter.Web/Components/Pages/HeaderDefaultLogoSpecimen.razor` and `HeaderCustomLogoSpecimen.razor` - added focused specimen routes for Story 8.3 E2E coverage.
- [x] `tests/e2e/specs/shell-chrome.spec.ts` - added Story 8.3 Playwright assertions for zero-config no-logo preservation, opt-in default decorative logo markup, custom adopter logo markup, and header-start/title DOM ordering.

### API Tests
- [x] Public component API shape is covered by the parameter-surface metadata-order test.
- [x] No HTTP API endpoint surface applies to this story.

### E2E Tests
- [x] `tests/e2e/specs/shell-chrome.spec.ts` now includes rendered-browser Story 8.3 coverage.
- [ ] Local `test:fc-shell-chrome` execution remains blocked before browser assertions because Kestrel cannot create a listening socket in this sandbox.

### Coverage
- Zero-config shell: no `data-testid="fc-shell-brand-logo"` is emitted when `HeaderLogo` is null and `ShowDefaultHeaderLogo` is false; default `FcHamburgerToggle` and title remain present.
- Adopter logo: supplied `HeaderLogo` renders inside the stable brand-logo cell after adopter `HeaderStart` content and before the app title.
- Precedence: supplied `HeaderLogo` wins over `ShowDefaultHeaderLogo=true`; the framework default Apps path is not emitted in that branch.
- Default opt-in logo: `ShowDefaultHeaderLogo=true` renders a decorative brand-logo cell with the existing `FcFluentIcons.Apps20()` SVG path.
- Browser routes: zero-config, opt-in default logo, and adopter logo states are reachable through the Counter specimen host and discovered by Playwright.
- Parameter surface: new parameters are append-only after `ContentLabelledBy`; existing names/types/order remain unchanged.
- Governance: Fluent conformance and accent-as-background guards remain green; no new dependencies, raw interactive controls, Fluent v4/FAST tokens, or accent surface backgrounds were added.

### Validation
- [x] RED phase: `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "FullyQualifiedName~FrontComposerShellTests|FullyQualifiedName~FrontComposerShellParameterSurfaceTests" -m:1 /nr:false` failed before implementation because `HeaderLogo` and `ShowDefaultHeaderLogo` did not exist.
- [x] `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] Focused header/logo + parameter direct xUnit v3 lane passed 31/31: `FrontComposerShellTests` and `FrontComposerShellParameterSurfaceTests`.
- [x] `FluentConformanceTests` direct xUnit v3 lane passed 17/17.
- [x] Broad Shell direct xUnit v3 lane excluding socket-bound Pact namespace passed 1973/1973.
- [x] (QA Generate E2E Tests, AI) `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors after adding the specimen routes.
- [x] (QA Generate E2E Tests, AI) `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] (QA Generate E2E Tests, AI) `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellTests -class Hexalith.FrontComposer.Shell.Tests.Components.Layout.FrontComposerShellParameterSurfaceTests -class Hexalith.FrontComposer.Shell.Tests.Governance.FluentConformanceTests -class Hexalith.FrontComposer.Shell.Tests.Integration.CounterWebIntegrationTests` passed 49/49.
- [x] (QA Generate E2E Tests, AI) `PLAYWRIGHT_SKIP_WEBSERVER=1 npx playwright test specs/shell-chrome.spec.ts --project=chromium --list` from `tests/e2e` discovered 5 shell-chrome tests, including the 3 new Story 8.3 tests.
- [ ] Full Shell direct xUnit v3 lane ran 1976 tests: 1975 passed, 1 Pact test failed because PactNet could not start a local mock server in this sandbox.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` restored/built until test execution, then VSTest aborted across assemblies with `System.Net.Sockets.SocketException (13): Permission denied` from the local socket transport. The run also reported the expected uninitialized nested `Hexalith.Memories` project skips and a NuGet audit network warning.
- [ ] (QA Generate E2E Tests, AI) `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~FrontComposerShellTests|FullyQualifiedName~FrontComposerShellParameterSurfaceTests|FullyQualifiedName~FluentConformanceTests|FullyQualifiedName~CounterWebIntegrationTests" -m:1 /nr:false` aborted before execution because VSTest could not create its local socket listener: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] (QA Generate E2E Tests, AI) `npm --prefix tests/e2e run typecheck` failed before type-checking because local `node_modules` contains stale TypeScript that rejects `tsconfig.json` `ignoreDeprecations: "6.0"`.
- [ ] `npm --prefix tests/e2e run test:fc-shell-chrome` failed before browser assertions because the Counter web server could not bind Kestrel: `System.Net.Sockets.SocketException (13): Permission denied`.
- [x] `git diff --check` passed with no whitespace errors; Git reported LF-to-CRLF normalization warnings only.
- [x] (Senior Developer Review, AI) `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors after the review fix.
- [x] (Senior Developer Review, AI) `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class …FrontComposerShellTests -class …FrontComposerShellParameterSurfaceTests -class …FluentConformanceTests` passed **48/48** after adding the adopter-logo a11y assertion.
- [x] (Senior Developer Review, AI) Re-ran the three Story 8.3 logo tests by name (`HeaderLogo_WhenNotProvidedAndDefaultDisabled_EmitsNoBrandLogoCell`, `HeaderLogo_WhenProvided_RendersBetweenHeaderStartAndAppTitle`, `HeaderLogo_WhenDefaultLogoOptedIn_RendersDecorativeAppsIconCell`) → 3/3 passed.
- [x] (Senior Developer Review, AI) Auto-fixed 1 Low: added `adopterLogoCell.GetAttribute("aria-hidden").ShouldBeNull()` to `HeaderLogo_WhenProvided_RendersBetweenHeaderStartAndAppTitle`, pinning that an adopter-supplied logo is not marked decorative even when `ShowDefaultHeaderLogo=true`. 0 Critical/High/Medium. Status moved review → done.

### Checklist
- [x] API tests generated if applicable: parameter-surface API guard updated.
- [x] E2E tests generated if UI exists: Story 8.3 browser assertions and specimen routes were added to `shell-chrome.spec.ts`; execution remains locally socket-blocked.
- [x] Tests use standard bUnit/xUnit v3/Shouldly APIs.
- [x] Tests cover the happy path and safe default path.
- [x] Tests cover the critical precedence/error-prone path where custom logo overrides the opt-in default mark.
- [x] Story-owned focused lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Tests use stable selectors, semantic page assertions, and DOM order checks without hardcoded waits.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics and local blockers.

## Story 8.2 - Accent-as-thread policy and regression guard

### Generated Tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` - added `Shell_chrome_styles_never_use_accent_as_surface_background`, a Shell `.css`/`.razor` static-scan guard that fails when `background` or `background-color` declarations reference `var(--fc-color-accent)` or `var(--fc-accent-base-color)`.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` - added matcher-level QA pins for forbidden `background`/`background-color` declarations, including CSS `var(..., fallback)` syntax, and allowed non-background accent thread uses.

### API Tests
- [x] Not applicable - Story 8.2 has no HTTP API endpoint surface.

### E2E Tests
- [x] No new browser E2E was required; Story 8.2 reuses Story 8.1 shell-chrome browser coverage for rendered neutral header/footer behavior.
- [ ] Local `test:fc-shell-chrome` execution remains blocked before browser assertions because Kestrel cannot create a listening socket in this sandbox.

### Coverage
- Architecture rule: `_bmad-output/project-docs/architecture.md` §4.1 already states that the accent is a thread, not a chrome fill, so no architecture edit was required.
- Forbidden uses: `background:` and `background-color:` declarations in Shell `.css`/`.razor` source now fail if their value references the Shell accent bridge variables, including fallback-valued CSS variable calls.
- Allowed uses: custom property definitions, foreground color, border/outline/focus, active navigation, links, primary affordances, badges, selected-state accent thread uses, and future accent-left-bar-style shadows are not flagged.
- Allowlist discipline: the accent-surface allowlist is empty and includes stale-entry detection.
- Story 8.1 preservation: neutral header/footer bUnit pins pass unchanged.

### Validation
- [x] `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] (QA Generate E2E Tests, AI) `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false --no-restore` passed with 0 warnings / 0 errors after adding matcher pins.
- [x] RED phase: temporarily adding `background: var(--fc-color-accent);` to `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css` made `Shell_chrome_styles_never_use_accent_as_surface_background` fail 1/1 with the expected repository-relative offender path; the temporary violation was removed before final validation.
- [x] Focused new guard direct xUnit v3 lane passed 1/1 after removing the temporary violation.
- [x] Full `FluentConformanceTests` governance class passed 6/6, including the existing legacy-token guard and the new accent-as-background guard.
- [x] (QA Generate E2E Tests, AI) Full `FluentConformanceTests` governance class passed 17/17, including the new matcher-level forbidden/allowed declaration pins.
- [x] Story 8.1 shell chrome direct xUnit v3 lane passed 3/3: `HeaderChrome_UsesNeutralSurfaceAndDivider`, `DefaultFooterChrome_UsesNeutralFrameAndFluentText`, and `AdopterSuppliedFooter_RendersInsideNeutralFrame`.
- [x] (QA Generate E2E Tests, AI) Focused Story 8.1 preservation lane passed 28/28 via direct xUnit v3: `FrontComposerShellTests` plus `SlotMappingRegressionTests`.
- [x] (QA Generate E2E Tests, AI) Matcher-focused direct xUnit v3 lane passed 11/11: `Accent_surface_guard_flags_background_declarations` and `Accent_surface_guard_allows_thread_declarations`.
- [x] Shell in-process assembly excluding Contract tests passed 1964/1964 via the direct xUnit v3 runner.
- [ ] Full Shell in-process assembly ran 1967 tests: 1966 passed, 1 Pact Contract test failed because PactNet could not start a local mock server in this sandbox.
- [ ] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` restored/built until test execution, then VSTest aborted across assemblies with `System.Net.Sockets.SocketException (13): Permission denied` from the local socket transport.
- [ ] `npm --prefix tests/e2e run test:fc-shell-chrome` failed before browser assertions because the Counter web server could not bind Kestrel: `System.Net.Sockets.SocketException (13): Permission denied`.

### Checklist
- [x] API tests generated if applicable: N/A, no HTTP API endpoint surface.
- [x] E2E tests generated if UI exists: no new browser test required; Story 8.1 shell-chrome E2E remains the rendered-browser coverage and is locally socket-blocked.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path.
- [x] Tests cover the critical regression path with a RED-phase temporary violation.
- [x] Story-owned focused lanes run successfully through the direct xUnit v3 in-process runner.
- [x] Tests use source scanning rather than brittle rendered markup for this governance policy.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary updated.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics and local blockers.
