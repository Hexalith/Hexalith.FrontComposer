---
title: 'Fix AppHost Startup Dependency Graph'
type: 'bugfix'
created: '2026-10-25'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '25e8a07198987b33cef052bdbc745f836cb6cbbf'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The local Debug AppHost cannot start: classic NuGet restore loses source-selection properties passed on project-reference edges, so Parties compiles against a published EventStore API while enabling source-only recovery, and Commons source assets are missing. Multiple AppHost paths also select different source/package modes for shared intermediate outputs.

**Approach:** Keep the existing topology and pinned dependencies, align the source-property vector across the AppHost's external project edges, and make its source-mode restore preserve that graph. Prove the restored graph selects the intended projects, build Debug, and verify the AppHost starts through Aspire with resource readiness checks and cleanup. Preserve explicit supported dependency-mode choices and Release package selection; no dependency updates, nested-submodule work, API removal or recovery bypass.

</frozen-after-approval>

## Implementation Notes

- Small reversible configuration repair in the owning FrontComposer repository; no missing user-visible choice, public API, data migration or irreversible operation. Use the one-shot route.
- Reproduced baseline `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json`: exit 2, two Commons.UniqueIds NETSDK1004 errors, Parties CS1061 and two transient copy warnings. Exact-target stop/describe confirms no AppHost running.
- EventStore source defines `RecoverNonExpiringPendingAsync`; `dotnet-inspect` confirms Client package 3.127.0 lacks it. Classic `NuGet.targets` converts project-reference items into paths and drops edge properties; static graph preserves them, but the trial also exposed mixed source-mode variants sharing assets. Do not change the domain recovery code.
- Planned footprint: source-reference properties and restore configuration in `deps.local.props`, the AppHost project and/or `Directory.Build.props`; reuse the combined UI's source vector and package-removal target; extend `tests/eng/test_apphost_dependency_modes.py` with real graph/restore checks. Keep scope narrow and do not change package catalogs, gitlinks, runtime topology or module source.
- Validation: restore graph/mode regressions, serialized Debug AppHost build, exact-target Aspire start/wait/describe and stop; record commands, counts and actual limitations here, with raw logs outside the checkout.
- User repository instructions prohibit staging/committing unless explicitly requested. No commit or push is part of this fix.

- Implemented one source-property vector in `deps.local.props`, defaulted it onto internal project edges so Counter/Shell cannot reintroduce package variants, and reused it from AppHost/combined UI. Enabled static graph restore only in source mode; explicit package modes and Release remain unchanged. The broad source-selection switch is not forwarded into modules.
- The new isolated AppHost restore regression initially failed twice on package UniqueIds, proving the incomplete AppHost-only normalization was insufficient. With internal edges normalized, all nine dependency-mode tests pass, including restored source recovery/UniqueIds, package overrides and Release selection. No module source changes were necessary for this correction.

- Corrected the graph now leaves only four CS0618 errors in the Tenants source dependency: three `ActorException` references and one `ActorResponseMismatch`. After inspecting the owning repository guidance/config/tests, replaced them with `QueryExecutionFailure` and `ResponseMismatch` in the two owning handlers. EventStore declares the old constants as exact aliases, so the wire failures and authorization behavior stay unchanged. No warning suppression or gitlink update is introduced. This is the minimal source compatibility correction needed to complete the requested AppHost fix; the preceding note about no module edits describes the graph correction only.

- Review correction supersedes the earlier symbolic-rename note: the new source constant names are absent from the pinned EventStore.Contracts 3.127.0 package. Each affected Tenants handler now declares a private constant for its existing stable failure wire value. Source and package compilation both pass with zero warnings/errors; no recovery or authorization behavior changes.
- Review added actual AppHost Debug compilation after the isolated source restore and a real `HexalithMemoriesFromSource=false` restore check. The first override test incorrectly expected Memories libraries on Parties.UI, which does not consume them. It now inspects the actual consuming Tenants UI and combined UI; its focused rerun passes. Full-suite green verification is pending below.

## Review Triage Log

- R1 — medium, defer: the isolated solution-entry restore can still overwrite shared assets with package variants because direct external solution projects bypass the root source vector. Review confirmed this predates the patch. AppHost-entry restore repairs its graph on normal start; the separate solution-entry follow-up is appended to the deferred-work ledger. No solution-wide coherence is claimed.
- R2 — high, patch: the initial canonical-symbol replacements failed against pinned EventStore.Contracts 3.127.0 with CS0117. Private stable wire-value constants preserve both source and package compatibility; isolated package and source test-project builds pass with zero warnings/errors.
- R3 — medium, patch: the restore-only regression missed the obsolete source aliases. It now compiles the actual AppHost in Debug with the isolated restored inputs, without warning suppression.
- R4 — medium, patch: evaluation alone did not prove explicit Memories package mode survived restore. The new restore case checks both package identities in the actual Tenants UI and combined UI assets.
- R5 — low, patch: the final notes above distinguish the private wire-value constants from the rejected symbolic rename; final build, startup, readiness and cleanup observations are recorded below. Historical failed attempts remain attributed separately.


## Final Verification — 2026-10-25

The startup dependency correction is implemented and independently reviewed. Root base is `25e8a07198987b33cef052bdbc745f836cb6cbbf` plus the current working-tree patch; Tenants base is `f364406dcfb7501c93db109d1ab99db562520fb2` plus the two handler edits. No commit or gitlink was changed. CI run link: unavailable; no hosted run was initiated.

| Check | Result |
| --- | --- |
| Complete dependency-mode regression file | 10 passed / 0 failed; 174.218s; includes isolated AppHost source restore **and Debug build**, actual Memories package override restore, and Release/evaluation controls |
| Full Debug AppHost build | Passed; zero warnings/errors |
| Normal Aspire start, including its normal parallel build | Exit 0; build zero warnings/errors; AppHost and dashboard started |
| Tenants source test-project build and affected handlers | Build passed with zero warnings/errors; 285 passed / 0 failed / 0 skipped/errors |
| Tenants package-mode compatibility build | Passed; zero warnings/errors |
| Independent review | All five findings dispositioned; final delta has no unresolved material issue; pre-existing solution-entry restore limitation deferred |
| Counter resource readiness | `aspire wait counter-web` passed, Healthy |
| Security / combined UI readiness | 60-second waits exited 17; backend readiness remains environment-blocked |
| Exact-target stop / describe | Stop exit 0; describe confirms no running AppHost |

Exact commands from the FrontComposer root unless noted:

```bash
python3 -m unittest tests/eng/test_apphost_dependency_modes.py -v
dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug -m:1 --verbosity minimal
aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
aspire wait counter-web --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --status healthy --timeout 30 --non-interactive
aspire wait security --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --status healthy --timeout 60 --non-interactive
aspire wait frontcomposer-ui --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --status healthy --timeout 60 --non-interactive
aspire describe --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --include-hidden --format Json
aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive
aspire describe --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json
dotnet build /home/administrator/projects/hexalith/frontcomposer/references/Hexalith.Tenants/tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -m:1 --artifacts-path /tmp/fc-apphost-tenants-tests-c5c_meb_ -p:RestoreUseStaticGraphEvaluation=true -p:HexalithFrontComposerRoot=/home/administrator/projects/hexalith/frontcomposer/ -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/frontcomposer/references/Hexalith.EventStore -p:HexalithTenantsRoot=/home/administrator/projects/hexalith/frontcomposer/references/Hexalith.Tenants -p:HexalithPartiesRoot=/home/administrator/projects/hexalith/frontcomposer/references/Hexalith.Parties -p:HexalithMemoriesRoot=/home/administrator/projects/hexalith/frontcomposer/references/Hexalith.Memories -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/frontcomposer/references/Hexalith.Commons -p:HexalithFrontComposerFromSource=true -p:HexalithEventStoreFromSource=true -p:HexalithTenantsFromSource=true -p:HexalithPartiesFromSource=true -p:HexalithMemoriesFromSource=true -p:HexalithCommonsFromSource=true
dotnet /tmp/fc-apphost-tenants-tests-c5c_meb_/bin/Hexalith.Tenants.Server.Tests/debug/Hexalith.Tenants.Server.Tests.dll -class '*TenantReadAuthorityQueryHandlerTests' -class '*GetTenantAuditQueryHandlerTests' -parallel none
```

From `references/Hexalith.Tenants`, the package-mode compatibility command was:

```bash
dotnet build src/Hexalith.Tenants/Hexalith.Tenants.csproj -c Debug -m:1 --artifacts-path /tmp/fc-apphost-tenants-package-tna8j1oo -p:HexalithEventStoreFromSource=false
```

**Remaining runtime prerequisite:** `docker version` exits 1 with “The command 'docker' could not be found in this WSL 2 distro” and requests Docker Desktop WSL integration. Docker Desktop's installed launcher reports its backend is already running. The 60-second Security wait still times out; Keycloak cannot start and dependent backend/UI resources wait. No security bypass or host-settings change was made. The user was asked to enable Docker Desktop → Settings → Resources → WSL Integration for this distro; complete backend readiness verification after that external change. Status remains `in-progress` for that pending runtime verification, despite successful AppHost compilation/startup. The owned AppHost is stopped.

Raw logs and reports remain local scratch under `/tmp/frontcomposer-apphost-*`, `/tmp/fc-apphost-tenants-*` and Aspire's normal log directory. The first extended 10-case test run had one incorrect test-harness expectation about Parties.UI's nonexistent Memories dependency; the corrected focused check and final complete 10/10 run pass. Historical graph-only trial failures and the initial canonical-symbol package probe retain their failed attribution. No committed evidence packet or runtime-identity record was created. No package version, nested submodule, approval gate, staging, commit or push changed.
