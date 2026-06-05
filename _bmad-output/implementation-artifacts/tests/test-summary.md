# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/e2e/specs/mcp-fail-closed-security.spec.ts` - Playwright API/E2E coverage for the Story 5.4 MCP JSON-RPC security surface.
- [x] `tests/e2e/helpers/mcp-client.ts` - Extended MCP streamable HTTP helper with typed tool-call results and missing API-key support.

### E2E Tests
- [x] `tests/e2e/specs/mcp-fail-closed-security.spec.ts` - Validates authenticated `tools/list`, missing/invalid-auth `tools/list`, unauthenticated `tools/call`, unknown-tool argument redaction, lifecycle handle redaction, and projection auth redaction through the Counter sample `/mcp` endpoint.
- [x] `tests/e2e/package.json` - Added focused `test:mcp-security` script.

## Coverage

- API/protocol surfaces: 4/4 Story 5.4 MCP endpoint surfaces covered in E2E (`tools/list`, `tools/call`, lifecycle `frontcomposer.lifecycle.subscribe`, and `resources/read`).
- UI features: 0/0 applicable; Story 5.4 is an MCP protocol/security story with no browser UI workflow.
- E2E critical error groups: 5/5 endpoint-reachable fail-closed groups covered (missing/invalid auth, unauthenticated command call, unknown tool, malformed lifecycle handle, projection auth failure).
- Happy path: authenticated `tools/list` proves admitted generated tools plus the fixed lifecycle tool are visible without leaking tenant/user values.
- Critical error cases: missing auth, invalid API key, unauthenticated command call, unknown tool, malformed lifecycle handle, and projection auth failure.
- Existing in-process Story 5.4 tests retain broader policy-hidden, tenant-hidden, startup gate, projection taxonomy, skill resource, and handler-edge coverage.

## Validation

- [x] `npm --prefix tests/e2e run typecheck` - passed.
- [x] `dotnet build samples/Counter/Counter.Web/Counter.Web.csproj -c Release --no-restore -m:1 /nr:false` - passed.
- [x] `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release --no-restore -m:1 /nr:false` - passed.
- [ ] `npm --prefix tests/e2e run test:mcp-security` - blocked in this sandbox because Kestrel cannot bind a local socket: `System.Net.Sockets.SocketException (13): Permission denied`.

## Next Steps

- Run `npm --prefix tests/e2e run test:mcp-security` in a local/CI environment that permits binding `127.0.0.1:5070`.
