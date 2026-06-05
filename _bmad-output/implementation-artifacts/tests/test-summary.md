# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable for Story 6.3; Level-4 full-view overrides are registration, registry, generated-render, and Blazor UI behavior, not an HTTP/API endpoint surface.

### E2E Tests
- [x] `tests/e2e/specs/level-4-full-view-overrides.spec.ts` - Browser-level Counter sample coverage for an adopter-registered `AddViewOverride<CounterProjection, CounterFullViewReplacement>` owning the projection body while explicitly delegated generated fields and Level-3 slot composition still render.

## Coverage
- API endpoints: 0/0 applicable for Story 6.3.
- UI features: 1/1 Story 6.3 Level-4 registered full-view browser workflow covered.
- Existing default-lane .NET tests remain the primary contract coverage for exact-role/any-role registry resolution, duplicate fail-closed behavior, invalid component fallback, HFC1043-HFC1045 runtime/startup diagnostic disposition, descriptor defensive copying, generated Level-4-over-Level-2 precedence, default fallback, host HFC2121 fault isolation, and safe generated delegate composition.

## Validation
- [x] `npm -C tests/e2e run typecheck` passed.
- [x] `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- [x] xUnit v3 in-process Shell registry lane passed: `ProjectionViewOverrideServiceCollectionExtensionsTests` 8/8.
- [x] xUnit v3 in-process Counter Level-4 lane passed: `CounterProjectionView_Level4Replacement_WinsWhenLevel2TemplateAlsoRegistered`, `CounterProjectionView_Level4Replacement_RendersInsideFrameworkEnvelope_AndUsesSafeFieldDelegates`, and `CounterProjectionView_Level4InvalidComponent_LogsHfc1043_AndRendersGeneratedDefault` 3/3.
- [ ] `npm -C tests/e2e run test:fc-level4` was attempted. The spec was discovered, but browser execution is blocked in this sandbox because Kestrel cannot bind a local socket: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] Focused `dotnet test` VSTest execution was attempted and aborted before test execution with the same local socket restriction; xUnit v3 in-process runner was used as the established fallback.

## Checklist
- [x] API tests generated if applicable: N/A, no API endpoint surface.
- [x] E2E tests generated because Story 6.3 has UI behavior.
- [x] Tests use standard Playwright APIs.
- [x] Tests cover the happy path: registered Level-4 replacement renders as the projection body after a real Counter command flow.
- [x] Tests cover critical fallback-adjacent behavior: Level-2 template markup and generated DataGrid body do not render under the Level-4 replacement, while explicitly delegated generated fields and Level-3 slot composition remain visible.
- [ ] All generated tests run successfully: browser execution is blocked by sandbox socket restrictions before the Counter host can start.
- [x] Tests use semantic locators where the UI exposes roles/labels, plus stable existing replacement and slot markers for the custom surfaces.
- [x] Tests have clear descriptions.
- [x] Browser workflow has no hardcoded waits or sleeps; host startup uses bounded readiness polling matching the existing Level-3 dedicated-host spec.
- [x] Tests are independent and start a dedicated Counter host with specimens disabled.
- [x] Test summary created.
- [x] Tests saved to appropriate directories.
- [x] Summary includes coverage metrics.

## Next Steps
- Run `npm -C tests/e2e run test:fc-level4` in CI or a local environment that permits Kestrel socket binding.
