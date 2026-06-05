# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/e2e/specs/mcp-schema-fingerprint-negotiation.spec.ts` - Playwright MCP JSON-RPC coverage for Story 5.5 schema fingerprint negotiation.
- [x] `tests/e2e/helpers/mcp-schema-fingerprints.ts` - Reads generated Counter MCP descriptor fingerprints from Release/Debug source-generator output for exact-match E2E requests.

### E2E Tests
- [x] `tests/e2e/specs/mcp-schema-fingerprint-negotiation.spec.ts` - Covers exact command fingerprint success, stale fingerprint command rejection, current-server validation after exact negotiation, malformed header fail-closed behavior, and hidden-tool precedence over schema diagnostics.
- [x] `tests/e2e/package.json` - Added focused `test:mcp-schema` script.

## Coverage

- API/protocol surfaces: 1/1 Story 5.5 side-effecting MCP command surface covered in E2E (`tools/call`).
- UI features: 0/0 applicable; Story 5.5 is an MCP protocol/schema negotiation story with no browser UI workflow.
- Happy path: exact generated descriptor fingerprint allows `Counter.BatchIncrementCommand.Execute` to acknowledge successfully.
- Critical error cases: stale supported fingerprint returns sanitized `schema-mismatch`; malformed uppercase fingerprint header fails closed without raw header echo; hidden policy command with stale fingerprint returns `unknown_tool`, not schema diagnostics.
- Current-server validation: exact schema negotiation still rejects caller-supplied `TenantId` before dispatch-visible success.
- Existing in-process MCP tests retain broader parser, negotiator, command gate, projection gate, hidden precedence, and aggregate integrity coverage.

## Validation

- [x] `npm --prefix tests/e2e run typecheck` - passed.
- [x] `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release -m:1 /nr:false` - passed.
- [x] `npm --prefix tests/e2e run test:mcp-schema -- --list` - discovered 5 tests.
- [x] `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests.dll -noLogo -noColor -method "*Schema*" -method "*AuthContextAccessor*" -method "*ToolAdmission*" -method "*CommandInvokerSchemaGate*" -method "*ProjectionReaderSchemaGate*" -method "*ProjectionReaderSchemaTaxonomy*" -method "*AggregateManifestIntegrity*"` - 132/132 passed via xUnit v3 in-process fallback.
- [x] `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests.dll -noLogo -noColor` - 358/358 passed via xUnit v3 in-process fallback.
- [ ] `npm --prefix tests/e2e run test:mcp-schema` - blocked in this sandbox because Kestrel cannot bind a local socket: `System.Net.Sockets.SocketException (13): Permission denied`.
- [ ] `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release --no-build ...` - blocked in this sandbox by VSTest socket startup: `System.Net.Sockets.SocketException (13): Permission denied`.

## Next Steps

- Run `npm --prefix tests/e2e run test:mcp-schema` in a local/CI environment that permits binding `127.0.0.1:5070`.
