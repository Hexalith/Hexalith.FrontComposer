---
title: '13.9 [A · FLUENT-APP-1] Decide the Exact Fluent UI V5 Posture'
type: 'chore'
created: '2026-10-21'
status: 'done'
story_id: '13.9'
baseline_commit: '247410b8b35050abf3f06ec72d6bcf3f1552b150'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FLUENT-APP-1/OI-4/D-13 has no decision. Planning still describes a Fluent RC posture while the root-selected catalog is GA, and UX-A..UX-F evidence was produced against three different catalog identities.

**Approach:** Record the owners' explicit, dated decision as a receipt in this spec, verified against the root gitlink catalog and the identity each evidence set actually bound to, then reconcile PRD, epics, sprint tracking and the deferred ledger to that outcome.

## Boundaries & Constraints

**Always:** Read identity via `git ls-tree HEAD references/Hexalith.Builds` + `git show <sha>:Props/Directory.Packages.props`, never the local checkout. The receipt names signers and roles, date, decision, package IDs/version, Builds revision, RC/GA, supported names (each verified in the pinned package), unsupported names, breaking-change rule, re-decision trigger, accessibility/visual revalidation trigger, accessibility evidence requirement, per-set evidence identity, rationale and consequences. Cite evidence; do not copy it.

**Never:** Infer approval from this workflow, green tests or catalog selection. Project one identity's evidence onto another. Rewrite or relabel 13.2–13.8 evidence or statuses, including the stale UX-A..UX-F `proposed` aliases and the Story 13.6 row. Close or reopen OI-10, OI-16, OI-19, G-4, PRD-APP-1 or the reviewer gate. Change runtime code, tests, catalog, gitlinks, dependencies or published `docs/` (DOC-C/Story 15.4 owns doc correction).

**Decisions (2026-10-21):** after the accept/reject/defer comparison, the requesting user, acting as both Product Owner and Architect, instructed "apply recommended":

1. **Not approved (AC3).** Target identity: `Microsoft.FluentUI.AspNetCore.Components` + `.Icons` `5.0.1` (GA), selected by Builds `5d03f1db9c38db34ecac7fe6048937478ffb82cc` at `530994af` and by `17d0c6ffec5fb3c8d8058e67cb85644460cb5256` at `247410b8`. It is not approved because no single identity has green UX-A..UX-F evidence. OI-4, FLUENT-APP-1, D-13 and G-4 stay open; Story 13.9 completes on this recorded decision. Residuals: (a) one green hosted Quality run at the gitlink identity, with the 12 WebKit failures fixed or dispositioned (Gate 3a depends on the existing Gate 2b entries), followed by a Product Owner + Architect re-decision citing it; (b) the unresolvable `--fc-color-info` alias; (c) Story 13.6 and UX-A..UX-F alias tracker drift, owned but unchanged here.
2. **Signers:** the requesting user as Product Owner and Architect (session label Administrator; git author Jérôme Piquot). The 2026-09-22 proposal's Release Owner line (`:303`) is superseded by PRD OI-4/D-13 and the epics; no Release Owner co-signature.
3. **Accessibility evidence requirement:** one green hosted Quality run on a `main` commit whose gitlink selects the identity (Chromium, Firefox, WebKit, `accessibility-visual` and Gate 3a bUnit), cited by run URL. Human AT/zoom/device evidence remains under OI-16 and must name the same identity.
4. **Lifecycle:** the GA re-decision trigger fired on 2026-09-30 and is consumed. After acceptance, a patch bump needs a green Quality run on the bump commit plus a dated one-line Architect addendum to the receipt naming the exact version and run; a minor or major bump needs a new decision; a breaking change is a FrontComposer major with conformance and accessibility/visual evidence (D-13).

</frozen-after-approval>

## Code Map

- Catalog: Components + Icons `5.0.1` at gitlink `5d03f1db9c38db34ecac7fe6048937478ffb82cc` (`530994af`) and at `17d0c6ffec5fb3c8d8058e67cb85644460cb5256` (HEAD `247410b8`, moved by the 09:15 `/pushall` sync). The identity is package IDs + exact version; the receipt records the selecting Builds revisions as provenance, and a Builds move that leaves the Fluent version unchanged is not a posture change. History: `5.0.0-rc.5-26219.1` from `e017948a` (2026-08-22); `5.0.0` at `38a59499` (2026-09-30, Builds `212583e0`); `5.0.1` at `b102b85b` (2026-10-17, Builds `4f53d9f3`).
- Evidence identity (local runs): 13.2 rc.5; 13.3 rc.5→5.0.0, local checkout ahead of gitlink on 09-30 (`spec-13-3-…:583`); 13.4 5.0.0 (`Builds@608c164c`, `spec-13-4-…:751`); 13.5 5.0.0; 13.6 5.0.0→5.0.1, final three-engine run proven 5.0.1 by DLL SHA-256 `82718d81…` (`evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/candidate.json:76`) with 18 visual failures, Shell .NET 3709/7 failed (`spec-13-6-rerun-browser-dotnet-gates.md:101-102`), primary spec still `in-review`; 13.7 5.0.1, .NET/docs only.
- Hosted Quality run `39554582583` on HEAD `530994af` binds to the gitlink identity (5.0.1) by checkout: `accessibility-visual`, Chromium and Firefox all-spec jobs pass. WebKit has 417 passed / 12 failed / 22 skipped, all in `accessibility-state-matrix.spec.ts` (`:241,:265,:647,:664,:689`, light and dark), with no ledger entry. `build-and-test` is red at Gate 2b governance (tracked: DW-1800, DW-1965), so the Gate 3a bUnit lane is skipped.
- Manual AT/zoom/device: every MAN-136-* gate `not performed` (`13-6-accessibility-result.md:228-234,306-310`).
- Names resolving in 5.0.1 package (`lib.module.js`, assembly types): accent role = brand ramp from `IThemeService.SetThemeAsync(new ThemeSettings(AccentColor, 0, 0, mode, false))` (`FrontComposerShell.razor.cs:749`), consumed as `--colorBrandForeground1` (`FrontComposerShell.razor.css:10`); `--colorBrandBackground`, `--colorNeutralBackground1..3`, `--colorNeutralStroke2`, `--colorNeutralForeground1..3`, `--colorStatus{Success,Warning,Danger}Foreground1`; `FluentProviders`, `FluentAccordion`, `FluentBadge`, `FluentTooltip`, `FluentStack`, `FluentGrid`, `FluentLayout`, `ThemeSettings`, `ThemeMode`. Unresolvable: `--colorStatusInfoForeground1` (status tokens exist only for success/warning/danger), so `--fc-color-info` (`FrontComposerShell.razor.css:15`) is dead; it has no consumers and is pinned by `SlotMappingRegressionTests.cs:18,39`.
- Guards already at 5.0.1, unchanged: `FluentConformanceTests.cs:27`, `PackageBoundaryTests.cs:16,112`.
- Planning: epics `:3729-3750` (ACs), `:1742` stale rc.4 label; PRD D-13 `:694`, OI-4 `:721`, G-4 `:44`, RC wording `:568,:647,:705`. Sprint: `sprint-status.yaml:209` (13.9), `:978` FLUENT-APP-1 alias, E9-APP-1 `action_items` `:722-746` (shape precedent). Ledger: last ID DW-1984 (format precedent).
- Precedent: `spec-13-8-accept-the-completed-fresh-row-live-proof.md` § Product Decision Receipt.

## Tasks & Acceptance

**Execution:**
- [x] This spec -- append `## Product Decision Receipt` with every Always field, the four decisions, a per-set evidence identity table and the consequences -- the receipt is the decision of record.
- [x] `_bmad-output/planning-artifacts/prd.md` -- dated 2026-10-21 inline updates to D-13, OI-4 and the `:568/:647/:705` posture lines: GA trigger fired, 5.0.1 target not approved, receipt linked, both still open -- G-4, OI-10, OI-16, OI-19 and Table A stay unchanged.
- [x] `_bmad-output/planning-artifacts/epics.md` -- dated Story 13.9 disposition and a dated note at `:1742`; set `updated` -- preserve history while removing the misleading current label.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- keep the `:978` alias `open` and add the receipt ref; add an open FLUENT-APP-1 `action_items` entry shaped like E9-APP-1 with `decision: "not-approved"`; move the 13.9 row and this spec to done together after review.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- append DW-1985 (residual a; Quality/Test role, then Product Owner + Architect re-decision), DW-1986 (residual b; FrontComposer Shell maintainer) and DW-1987 (residual c; sprint tracking owner), each with status, origin, source_spec, owner, reason and closure -- cite DW-1800/DW-1965 instead of duplicating Gate 2b.

**Acceptance Criteria:**
- Given the gitlink catalog and per-set evidence identities, when the receipt is recorded, then it is named and dated, states "not approved" for the exact target identity with every Always field, and every supported name resolves in the 5.0.1 package while every unsupported name does not.
- Given the not-approved decision, when tracking reconciles, then OI-4, FLUENT-APP-1, D-13 and G-4 read open with a dated receipt link, and OI-10, OI-16, OI-19 and PRD-APP-1 are unchanged.
- Given the three residuals, when appended, then each has an owner and closure condition and none duplicates the existing Gate 2b entries.
- Given Stories 13.2–13.8, when 13.9 completes, then their evidence and statuses are unchanged from baseline.

## Product Decision Receipt

**Decision date:** 2026-10-21. **Decision:** **not approved** (AC3). **Signers:** Administrator, the requesting user (git author Jérôme Piquot), acting explicitly as both **Product Owner** and **Architect**, instructed “apply recommended” after the accept/reject/defer comparison. This records that human decision, not approval inferred from implementation, catalog selection or test results. The Release Owner line in the [2026-09-22 proposal](../planning-artifacts/sprint-change-proposal-2026-09-22.md) is superseded for this decision by PRD OI-4/D-13 and Story 13.9; a Release Owner co-signature is not required.

### Exact target and catalog provenance

The target is **GA**, with both package IDs pinned to **5.0.1**:

- `Microsoft.FluentUI.AspNetCore.Components` — `5.0.1`.
- `Microsoft.FluentUI.AspNetCore.Components.Icons` — `5.0.1`.

The authority is the root-selected Builds gitlink, read with `git ls-tree <candidate> references/Hexalith.Builds`, then `git -C references/Hexalith.Builds show <selected-sha>:Props/Directory.Packages.props`. The local checkout is not the version authority.

| FrontComposer candidate | Selecting Builds revision | Components / Icons | Use |
| --- | --- | --- | --- |
| `530994afdaba96567752e1a0a264a4b13a5cf909` | `5d03f1db9c38db34ecac7fe6048937478ffb82cc` | `5.0.1` / `5.0.1` | Hosted Quality evidence below. |
| Spec baseline `247410b8b35050abf3f06ec72d6bcf3f1552b150` | `17d0c6ffec5fb3c8d8058e67cb85644460cb5256` | `5.0.1` / `5.0.1` | The 2026-10-21 09:15 sync provenance recorded in this spec. |
| Implementation HEAD `7069a11177c10d9c1f486d8fd2f6e373872c9fce` | `f8599224457dda99e7c0ae4870147dd51b464feb` | `5.0.1` / `5.0.1` | Subsequent existing sync, independently checked on 2026-10-21. |

The identity is the two package IDs and their exact versions. These Builds revisions are selecting provenance; a Builds move retaining both Fluent versions is not a posture change. Evidence remains bound to its own candidate/revision and is not promoted to another candidate by this version equality.

### Verified supported and unsupported names

The `5.0.1` package's `staticwebassets/Microsoft.FluentUI.AspNetCore.Components.lib.module.js` resolves all these Fluent 2 token keys (the JavaScript theme keys omit the CSS `--` prefix): `--colorBrandForeground1`, `--colorBrandBackground`, `--colorNeutralBackground1`, `--colorNeutralBackground2`, `--colorNeutralBackground3`, `--colorNeutralStroke2`, `--colorNeutralForeground1`, `--colorNeutralForeground2`, `--colorNeutralForeground3`, `--colorStatusSuccessForeground1`, `--colorStatusWarningForeground1`, and `--colorStatusDangerForeground1`.

The exact active accent role is **`--colorBrandForeground1`**. The brand ramp is created through **`IThemeService.SetThemeAsync(ThemeSettings)`**, using **`new ThemeSettings(AccentColor, 0, 0, mode, false)`** in [FrontComposerShell](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs#L749), and [the existing accent alias](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css#L10) consumes that foreground role. The seed configures the ramp; it does not authorize a chrome fill or a new palette.

Public type inspection of the package's `lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.dll` confirms **`FluentProviders`, `FluentAccordion`, `FluentBadge`, `FluentTooltip`, `FluentStack`, `FluentGrid`, `FluentLayout`, `IThemeService`, `ThemeSettings`, and `ThemeMode`**. Member inspection confirms the `SetThemeAsync(ThemeSettings)` overload and the constructor parameters `Color`, `HueTorsion`, `Vibrancy`, `Mode`, `IsExact`. These are verified names under this pin, not an approval of the target posture or a promise of an internal toolbar composition.

**Unsupported:** `--colorStatusInfoForeground1` does not resolve in `5.0.1`; the package defines status foreground roles for success, warning and danger, with no info counterpart. Consequently, [the existing `--fc-color-info` alias](../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css#L15) is unresolvable. It has no runtime consumers, but [SlotMappingRegressionTests](../../tests/Hexalith.FrontComposer.Shell.Tests/SlotMappingRegressionTests.cs#L18) pins it. [DW-1986](deferred-work.md#dw-1986-disposition-the-unresolvable-fluent-info-color-alias) owns its disposition; this receipt neither legitimizes nor changes it. Legacy v4/FAST names remain outside the supported Fluent 2 token contract.

### Evidence identity by set

Versions below apply to both Components and Icons. The source records are cited, not relabelled; their local results do not jointly establish one green UX-A..UX-F identity.

| Set | Identity actually evidenced | Evidence and limits |
| --- | --- | --- |
| UX-A / 13.2 | `5.0.0-rc.5-26219.1` (RC) | [Story 13.2 verification](spec-13-2-make-shell-navigation-and-route-focus-deterministic.md#verification); its baseline `812de2fe` selects Builds `2326f983bad14d5398ee55bf2bdf6c86b63c39ea`, whose catalog selects rc.5. This is historical RC evidence. |
| UX-B / 13.3 | rc.5 → `5.0.0` (GA) | [Story 13.3](spec-13-3-preserve-focus-and-input-through-command-safety-outcomes.md#verification), especially its 2026-09-30 iteration 10 note: local Builds `212583e` selected `5.0.0` while the root gitlink `fdebf0fa` still selected rc.5. The mismatch and failed checks remain part of that record. |
| UX-C / 13.4 | `5.0.0` (GA) | [Story 13.4 verification](spec-13-4-announce-projection-and-command-state-without-noise.md#verification); iteration 48 names Builds `608c164c3637240fed2dd92dbfdaaf581ba9846d`, whose catalog selects `5.0.0`. Local focused passes do not claim hosted or all-engine green evidence. |
| UX-D / 13.5 | `5.0.0` (GA) | [Story 13.5 verification](spec-13-5-preserve-fresh-row-meaning-across-visual-and-data-changes.md#verification); its baseline `985d3e70` selects the same Builds `608c164c…` / `5.0.0` catalog. Its broad failures/skips remain unchanged. |
| UX-E / 13.6 | `5.0.0` → `5.0.1` (GA); final three-engine run `5.0.1` | [Reviewed candidate](evidence/story-13-6/2026-10-18-repeatable-browser-reviewed/candidate.json) binds the executing Fluent DLL SHA-256 `82718d81e3d28d7a8b80bfb6a543390678ab136bfc4af02af11f55101d613964`, matching the `5.0.1` net10.0 package DLL. [Final rerun verification](spec-13-6-rerun-browser-dotnet-gates.md#verification) records 18 visual failures and Shell 3,709 total / 7 failed; it is not green evidence. The primary spec remains `in-review`; the sprint row remains `done`. |
| UX-F / 13.7 | `5.0.1` (GA) | [Story 13.7 verification](spec-13-7-provide-reusable-ux-assertions-for-adopters.md#verification), including the catalog-expectation correction to `5.0.1`; baseline `b9e1490c` selects Builds `332b2c144defba546fe0a09626a4a72f2340e22f`. This is .NET/docs evidence, not a browser revalidation of UX-A..UX-E. |
| Hosted Quality | `5.0.1` (GA), at `530994af` / Builds `5d03f1db…` | [Run 39554582583](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/39554582583), 2026-10-21: `accessibility-visual`, Chromium and Firefox all-spec jobs pass. [WebKit](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/39554582583/job/119071255411) records 417 passed / 12 failed / 22 skipped; failures are in `accessibility-state-matrix.spec.ts` at lines 241, 265, 647, 664 and 689 across light/dark cases. [Build-and-test](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/39554582583/job/119071255355) fails Gate 2b; Gate 3a bUnit is skipped. This run is red. |
| Manual AT / zoom / device | No performed evidence; no identity acceptance | [Story 13.6 human checklist](13-6-accessibility-result.md#human-evidence-checklist--leave-each-box-open-until-observed) retains every MAN-136-* gate as `not performed`. OI-16 owns the human observations and must name the same identity. |

### Rationale, lifecycle and consequences

1. **Not approved:** no single exact identity has green UX-A..UX-F evidence. The historical sets span RC, `5.0.0` GA and `5.0.1` GA, and the hosted target-identity run is red. **OI-4, FLUENT-APP-1, D-13 and G-4 remain open.** Story 13.9 completes on this recorded negative decision after review; completion is not Fluent or overall Product approval.
2. **Re-decision and accessibility requirement:** obtain **one green hosted Quality run, cited by run URL, on a `main` commit whose root gitlink selects the exact target identity**, covering Chromium, Firefox, WebKit, `accessibility-visual` and Gate 3a bUnit. Fix or explicitly disposition the 12 WebKit failures first; Gate 3a depends on the existing Gate 2b work. [DW-1985](deferred-work.md#dw-1985-obtain-green-hosted-fluent-501-evidence-and-a-new-owner-decision) belongs first to the Quality/Test role, then to Product Owner + Architect for a dated re-decision citing that run. It references existing [DW-1800](deferred-work.md#dw-1800-quality-gate-2b-on-current-main-fails-centralpackageversions-catalog-inheritance-and-releaseworkflow-builds-gitlink-sha-checks) (archived/done historical governance lineage) and [DW-1965](deferred-work.md#dw-1965-gate-2b-is-red-on-a-pre-existing-eventstore-runtime-identity-assertion-unrelated-to-story-1127) (open), without duplicating or reopening either. Human AT/zoom/device evidence stays under **OI-16**, names the same package identity and cannot be inferred from the hosted pass.
3. **Upgrade and revalidation rules:** the GA re-decision trigger fired on **2026-09-30**, when the gitlink first selected `5.0.0` GA, and is consumed by this decision. After acceptance, a **patch** bump requires a green Quality run on the bump commit plus a dated one-line **Architect** addendum to this receipt naming the exact version and run URL. A **minor or major** bump requires a new Product Owner + Architect decision. Every Fluent identity change triggers conformance and accessibility/visual revalidation at that identity; earlier evidence is never projected forward. A **breaking Fluent change is a FrontComposer major version**, with conformance and accessibility/visual evidence under D-13.
4. **Bounded consequences:** [DW-1986](deferred-work.md#dw-1986-disposition-the-unresolvable-fluent-info-color-alias) assigns the dead info alias to the FrontComposer Shell maintainer. [DW-1987](deferred-work.md#dw-1987-reconcile-story-136-and-ux-alias-tracker-drift) assigns the Story 13.6/UX-A..UX-F tracker drift to the sprint tracking owner; the historical rows/evidence remain untouched here. Published doc correction remains **DOC-C / Story 15.4**. OI-10, OI-16, OI-19, PRD-APP-1, Table A and the reviewer gate are unchanged. G-4 still requires those prerequisite closures, the refreshed digest-bound reviewer gate and D-9 re-approval; this receipt supplies none of them.

## Implementation Notes

- Loaded the entire spec and its sole frontmatter context, `epic-13-context.md`, before implementation. Changes are confined to this receipt and the four authorized planning/tracking artifacts.
- Rechecked catalog provenance at implementation HEAD `7069a11177c10d9c1f486d8fd2f6e373872c9fce`; its pre-existing Builds move retains `5.0.1`. Frozen intent and baseline remain unchanged. The four pre-existing gitlink moves since `247410b8` are recorded separately from this story's empty runtime/dependency scope.
- All receipt/planning/ledger work is implemented and reviewed. The spec and Story 13.9 sprint row are `done` together; no approval gate closes through that transition.


## Spec Change Log

## Review Triage Log

Review completed 2026-10-21 with Blind Hunter, Edge Case Hunter and Verification Gap reviewers. Edge Case Hunter returned no findings; Verification Gap found no new deterministic behavior requiring automated-test coverage. Every Blind Hunter finding is assessed separately below.

| Finding | Verdict and route | Evidence / disposition |
| --- | --- | --- |
| BH-1: hosted Gate 2b failures are not covered by DW-1800/DW-1965 | medium; patch completed (DW-1985 routing) | Read-only failed-job inspection confirms `PackableProjects_UsePackageValidationBaselinePolicy`, `ToolchainPins_MatchApprovedDotnetAndAspireVersions`, `PackageInventory_IsExplicitLockstepAndReviewable` and `QualityWorkflow_Gate4ValidatesEveryPackableLibrary`. DW-1800 is archived/done and DW-1965 concerns a different EventStore assertion. DW-1985 now distinguishes these historical references and assigns current blocker coordination to Quality/Test without duplicating the implementation fixes; the original DW entries remain unchanged. This verified job detail supersedes the Code Map's inherited “tracked” shorthand. |
| BH-2: Quality does not rerun UX-A's mounted host-router tests | medium; defer (pre-existing CI coverage) | `Hexalith.FrontComposer.slnx:63-65` excludes UI.Tests from Release and `quality.yml:561` runs the Release solution. Story 13.2 already documents this host/source boundary. The frozen owner requirement is a green hosted Quality run followed by a new owner decision, not automatic acceptance or a claim that every historical test ran; preserve that requirement and separately record the existing coverage limitation. |
| BH-3: UX-A/UX-D execution identities are inferred from baseline catalogs | maybe-false; rejected (spec-only recommendation) | The table cites selected baseline catalogs, not executing-asset hashes, for these two historical sets; only UX-E has a DLL binding. Actual local execution identities would require historical restore/execution assets. Treat UX-A/UX-D versions as baseline-selected provenance, with execution identity unverified; they supply no target approval. The proposed table edit changes this build's spec, which this workflow excludes from review patches. |
| BH-4: skipped cases could silently count as complete acceptance | false; rejected | The receipt explicitly reports WebKit's 22 skips and requires a later named Product Owner + Architect decision, so a green run does not automatically claim complete coverage. Existing browser tests contain documented engine-capability/Chromium-only skip reasons (`specimen-accessibility.spec.ts:363`, `responsive-accessibility.spec.ts:94`, `accessibility-state-matrix.spec.ts:448`); this negative decision neither changes those policies nor asserts those cases ran in every engine. |
| BH-5: hosted browser artifact references have short retention | medium; defer (pre-existing retention policy) | `quality.yml:955,1048` retains accessibility/browser artifacts for 14 days. A run URL is durable metadata, not permanent screenshots/traces. Record retention/disposition work separately without copying evidence into this receipt; any later acceptance needs its own current evidence and owner decision. |
| BH-6: typography names are absent from the verified inventory | false; rejected | The receipt says “all these” enumerated names resolve, not that it exhaustively certifies every Fluent API. The UX design names FluentText typography roles, but the reviewer demonstrated no unresolved typography API or wrong enumerated name. Published name parity belongs to DOC-C/15.4; the proposed edit is confined to this build's spec. |
| BH-7: unsupported info alias is not a mandatory acceptance dependency | false; rejected | The frozen owner decision assigns the alias its own DW-1986 residual and requires a fresh named re-decision; it does not make DW-1986 a separate automatic hard gate. Sprint FLUENT-APP-1 explicitly lists all three residuals. No current acceptance or alias support is claimed; requiring an additional closure condition would change the owner-approved policy. |
| BH-8: verification prose abbreviates the constructor/token commands | low; rejected (spec-only recommendation) | The package/version/asset and inspection flags are recorded, but the constructor example uses an ellipsis. Expanding verification prose would be a direct reproducibility improvement only in this build's spec; the workflow explicitly rejects findings whose fix edits that spec. The actual supported names were independently resolved; no unexecuted test or missing required check was found. |

## Verification

**Commands:**
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-9-decide-the-exact-fluent-ui-v5-posture.md --candidate HEAD` -- expected: exit 0 with a `## File List`.
- `git diff --check` and a YAML parse of `sprint-status.yaml` -- expected: clean.
- Re-run the identity commands; grep the 5.0.1 package for each supported name (non-zero) and each unsupported name (zero).
- `git diff --stat 247410b8 -- src tests docs references` -- expected: empty.


### Results — 2026-10-21

- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-9-decide-the-exact-fluent-ui-v5-posture.md --candidate HEAD` — **exit 0**, `Story artifact validation passed`, with all five File List paths reconciled. Baseline remains `247410b8b35050abf3f06ec72d6bcf3f1552b150`; candidate is `7069a11177c10d9c1f486d8fd2f6e373872c9fce`. The initial run reported the pre-existing sync as unmapped. The existing process-disposition grammar now truthfully accounts for its workflow-spec introduction while retaining four gitlinks as unowned; neither history nor validation policy changed.
- `git diff --check` — **exit 0**. `yaml.safe_load` parses `sprint-status.yaml`; assertions confirm the open/not-approved FLUENT action and all 13.2–13.8 development-status values are unchanged from baseline. PRD G-4/OI-10/OI-16/OI-19 rows, the frozen 13.9 intent, prior story specs/evidence, UX-A..UX-F aliases and PRD-APP-1 are preserved.
- `git ls-tree HEAD references/Hexalith.Builds`, then `git -C references/Hexalith.Builds show f8599224457dda99e7c0ae4870147dd51b464feb:Props/Directory.Packages.props` — both package IDs select **5.0.1**. The same object-based check confirms both selecting revisions named in frozen intent and the historical identities in the receipt table. No local-checkout catalog inference is used.
- The pinned `5.0.1` module contains each of the twelve supported token keys at least once and `colorStatusInfoForeground1` **zero** times. Its net10.0 XML metadata and `dnx dotnet-inspect -y -- type --package Microsoft.FluentUI.AspNetCore.Components@5.0.1 --tfm net10.0 --tsv` confirm all ten named public types. `dnx dotnet-inspect -y -- member IThemeService --package Microsoft.FluentUI.AspNetCore.Components@5.0.1 --tfm net10.0 -m SetThemeAsync --tsv` and the corresponding `member ThemeSettings ... --ctor --tsv` resolve the exact overload/constructor. Type enumeration exited 0 with an unrelated generic-constraint dependency-resolution warning; no generic-constraint compatibility claim is made.
- SHA-256 of the pinned package's net10.0 Components DLL is `82718d81e3d28d7a8b80bfb6a543390678ab136bfc4af02af11f55101d613964`, exactly matching the preserved final Story 13.6 candidate binding. `gh run view 39554582583 --json url,headSha,conclusion,jobs` and the WebKit/build-and-test failed logs confirm the hosted candidate/job outcomes cited in the receipt. These are read-only checks of existing evidence, not new green evidence.
- `git diff --stat 247410b8 -- src tests docs references` — **four pre-existing gitlink changes**, not the expected empty output: Builds, Memories, Parties and Tenants were moved by `7069a111` before implementation. `git diff --stat HEAD -- src tests docs references` is **empty**. No runtime, test, published-doc, catalog, dependency or gitlink change belongs to this story. The nonempty historical baseline range is disclosed, not hidden by moving the baseline or reverting user work.
- No new runtime tests or browser recapture are needed for this decision-only implementation. All three review layers completed. One DW-1985 attribution patch was verified; two pre-existing evidence limitations were deferred. The coordinated spec/sprint `done` transition is complete; Fluent/accessibility/Product gates and DW-1985–DW-1987 remain open.

### Post-review checks — 2026-10-21

Strict story-artifact validation and `git diff --check` pass after the routing patch and review deferrals. Independent Python/YAML assertions preserve the frozen intent, earlier action items, all 13.2–13.8 statuses/evidence, UX aliases, PRD-APP-1 and protected PRD rows. Object-based catalog checks confirm both package IDs at 5.0.1 for all three selecting revisions. Pinned XML metadata confirms the ten public types, `SetThemeAsync(ThemeSettings)` and `ThemeSettings.#ctor(System.String,System.Double,System.Double,Microsoft.FluentUI.AspNetCore.Components.ThemeMode,System.Boolean)`; all twelve supported JavaScript token keys exist and the info key does not. The executing DLL hash matches the preserved Story 13.6 binding. No runtime, dependency or published-doc change was introduced.

## Commit Scope Dispositions

- `7069a11177c10d9c1f486d8fd2f6e373872c9fce` | `process` | Pre-existing /pushall sync introduced the approved Story 13.9 workflow spec before implementation and moved Builds, Memories, Parties and Tenants gitlinks. The spec introduction is process setup, not this receipt's delivery or runtime evidence; the four unrelated dependency changes remain unowned. Baseline and validator are unchanged.

## File List

- `_bmad-output/implementation-artifacts/spec-13-9-decide-the-exact-fluent-ui-v5-posture.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
