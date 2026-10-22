# Local two-tenant Counter proof

This opt-in AppHost resource supports Story 13.1's live production-adapter test. Commands go through EventStore; the fixture rebuilds per-tenant rows from the resulting events. Its query state is intentionally ephemeral and is seeded again by each test run.

From the repository root:

1. Start the AppHost with `TenantScopeFixture__Enabled=true aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive`.
2. Run `aspire wait security --non-interactive`, `aspire wait eventstore --non-interactive`, and `aspire wait counter-scope-fixture --non-interactive`.
3. Obtain short-lived tokens for the local `tenant-a-user` and `tenant-b-user` from the Keycloak realm's `hexalith-eventstore` public client. Set `FC_TEN_SCOPE_EVENTSTORE_URL=https://localhost:7141`, the `FC_TEN_SCOPE_TENANT_A/B` values to the matching local tenants, `FC_TEN_SCOPE_USER_A/B` to each token's `sub`, and `FC_TEN_SCOPE_TOKEN_A/B` to the tokens. Keep tokens out of command output and committed files.
4. Run `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -class '*LiveTwoTenantProductionAdapterTests'`. The test submits commands, waits for SignalR notifications, and checks distinct query rows and counts.
5. Run `aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive` when finished.

The fixture is local-only. It is excluded from Release solution builds and is disabled in ordinary AppHost runs.

## Candidate revalidation requirement — 2026-10-22

Before approving a candidate that changes tenant/user scope resolution, authorization, scoped storage,
production query/count/subscription adapters, server routing or claims, the AppHost/realm/fixture,
or the selected EventStore implementation, run this existing proof again with **one Passed test and
zero skips**. Repeat after any further relevant change; bind the receipt to the resulting candidate.
The Performance category remains advisory in ordinary Quality runs. A green default lane, a skipped
Performance lane, or the historical Story 13.1 pass does not satisfy this candidate requirement.

Reuse `src/Hexalith.FrontComposer.AppHost/KeycloakRealms/hexalith-realm.json` and the opt-in resource
above. The realm contains the two fixture users and their distinct local tenants. Discover actual
resource endpoints with exact-target `aspire describe` after `aspire wait`; confirm `security`,
`eventstore` and `counter-scope-fixture` are ready before acquiring tokens. Obtain each user's
short-lived token through the realm's existing public client. Hold tokens and decoded `sub` values
in process memory and pass them only through the test child process's environment. Do not print
token responses, place credentials in command arguments, enable shell tracing, or write token files.
Stop if either identity's required scope/claim is missing; do not relax authorization to run the proof.

Build the individual Shell test project in Debug, then use a fresh, empty result directory outside
the checkout for each candidate. The direct xUnit runner supports this exact focused TRX command
(`RESULTS` below denotes that fresh directory):

```bash
DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*LiveTwoTenantProductionAdapterTests' -result-trx "$RESULTS/live-two-tenant.trx"
python3 .github/scripts/ci_governance.py validate-mtp-evidence --results-dir "$RESULTS" --expected-test Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.LiveTwoTenantProductionAdapterTests.ProvisionedTenants_QueryCountAndSubscribe_StayIsolated --expected-trx-files 1 --require-tests --require-distinct-modules
```

Require both commands to succeed. The existing validator checks the exact fully qualified Passed
identity, so an exit-zero skipped test or unrelated passing test fails the approval boundary. The test
must exercise distinct query rows/counts, tenant-specific SignalR notifications, and scoped storage keys.

Retain a bounded sanitized receipt in the candidate's existing verification record: full root revision
plus patch/input SHA-256 identities when dirty, EventStore and realm/fixture revision or input hashes,
SDK/runtime versions, exact commands, UTC date, one Passed/zero Failed/zero Skipped result, TRX hash,
and validator exit code. Keep only sanitized results; endpoints, tokens, tenant/user values and raw
payloads do not belong in the receipt. If provisioning or identity prerequisites are unavailable,
record the exact failing command/diagnostic, retain this action as open, and rerun before approval.
