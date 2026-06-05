# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable for Story 6.1; no API endpoint behavior is introduced by Level-2 ProjectionTemplate overrides.

### E2E Tests
- [x] `tests/e2e/specs/projection-template-overrides.spec.ts` - Browser-level specimen coverage for registered Level-2 projection templates rendering before generated default bodies while preserving generated field renderers.

## Coverage
- API endpoints: 0/0 applicable for Story 6.1.
- UI features: 1/1 Story 6.1 Level-2 projection-template browser workflow covered.
- Existing default-lane xUnit/bUnit pins remain the primary AC1/AC2 contract coverage for diagnostics, registry resolution, host isolation, manifest determinism, and marker cache-key equality.

## Next Steps
- `npm run typecheck` from `tests/e2e` passed.
- `npx playwright test specs/projection-template-overrides.spec.ts --project=chromium --list` discovers 2 tests.
- Browser execution of `npx playwright test specs/projection-template-overrides.spec.ts --project=chromium` is blocked in this sandbox because the configured Kestrel web server cannot bind a socket (`System.Net.Sockets.SocketException (13): Permission denied`). CI or a local environment with socket binding remains the execution gate for this browser spec.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Integration.ProjectionTemplateMarkerTests -noLogo` passed 15/15 through the xUnit v3 in-process runner after VSTest socket transport was blocked.
- Keep solution-level `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` as the CI gate for the default .NET lane.
