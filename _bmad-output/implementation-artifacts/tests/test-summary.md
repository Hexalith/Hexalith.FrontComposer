# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable - Story 3.6 behavior is exercised through existing .NET status-query unit tests; the Counter E2E host does not expose a direct HTTP command-status API.

### E2E Tests
- [x] `tests/e2e/specs/command-lifecycle-budgets.spec.ts` - Story 3.6 budget contract and degraded-to-confirmed browser workflow.

## Coverage
- Budget contract defaults: 8/8 covered.
- Degraded UI browser workflow: 1/1 covered.
- Late terminal replacement after degraded UI: 1/1 covered.
- Command-status polling internals: covered by existing focused .NET tests recorded in story 3.6; no direct browser API surface exists in the Counter stub host.

## Validation
- [x] `npm --prefix tests/e2e run typecheck` - passed.
- [x] `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 /nr:false` - passed, 0 warnings / 0 errors.
- [x] `PLAYWRIGHT_SKIP_WEBSERVER=1 npx playwright test specs/command-lifecycle-budgets.spec.ts --project=chromium --grep "budget contract"` - passed.
- [ ] `npx playwright test specs/command-lifecycle-budgets.spec.ts --project=chromium` - attempted; this sandbox blocked Kestrel socket binding with `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] Earlier Chromium browser launch attempts were also blocked before test code executed by `sandbox_host_linux.cc:41` shutdown permission failure.

## Next Steps
- Run the Story 3.6 Playwright spec in CI with the default E2E lane.
- Keep the focused .NET command polling tests as the authoritative coverage for oldest-first caps, uncertainty handling, disabled polling, and `NeedsReview` expiry.
