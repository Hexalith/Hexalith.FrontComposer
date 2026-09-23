# Local two-tenant Counter proof

This opt-in AppHost resource supports Story 13.1's live production-adapter test. Commands go through EventStore; the fixture rebuilds per-tenant rows from the resulting events. Its query state is intentionally ephemeral and is seeded again by each test run.

From the repository root:

1. Start the AppHost with `TenantScopeFixture__Enabled=true aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive`.
2. Run `aspire wait security --non-interactive`, `aspire wait eventstore --non-interactive`, and `aspire wait counter-scope-fixture --non-interactive`.
3. Obtain short-lived tokens for the local `tenant-a-user` and `tenant-b-user` from the Keycloak realm's `hexalith-eventstore` public client. Set `FC_TEN_SCOPE_EVENTSTORE_URL=https://localhost:7141`, the `FC_TEN_SCOPE_TENANT_A/B` values to the matching local tenants, `FC_TEN_SCOPE_USER_A/B` to each token's `sub`, and `FC_TEN_SCOPE_TOKEN_A/B` to the tokens. Keep tokens out of command output and committed files.
4. Run `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -class '*LiveTwoTenantProductionAdapterTests'`. The test submits commands, waits for SignalR notifications, and checks distinct query rows and counts.
5. Run `aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive` when finished.

The fixture is local-only. It is excluded from Release solution builds and is disabled in ordinary AppHost runs.
