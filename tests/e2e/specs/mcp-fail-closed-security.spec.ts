import { fileURLToPath } from 'node:url';

import type { APIRequestContext } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { spawnOwnedProcess, stopOwnedProcess, waitForOwnedServer, type OwnedProcess } from '../helpers/owned-process.js';
import { McpClient, sampleMcpApiKey, type McpCallToolResult, type McpTextResourceContents } from '../helpers/mcp-client.js';

const lifecycleTool = 'frontcomposer.lifecycle.subscribe';
const admittedTools = [
  lifecycleTool,
  'Counter.CreateCounterCommand.Execute',
  'Counter.UpdateCounterCommand.Execute',
  'Default.IncrementCommand.Execute',
  'Counter.BatchIncrementCommand.Execute',
  'Counter.ConfigureCounterCommand.Execute',
].sort();
const visibleCommand = 'Counter.BatchIncrementCommand.Execute';
const absentCommand = 'Counter.DoesNotExistCommand.Execute';
const secretArgument = 'Bearer eyJhbGciOiJIUzI1NiJ9.agent-secret';
const invalidApiKey = 'Bearer eyJhbGciOiJIUzI1NiJ9.invalid-mcp-key';
// Compiled into Counter.Web and registered only in the Test environment: an authenticated principal the sample
// gates never admit.
const nonAdmittedApiKey = 'counter-e2e-mcp-non-admitted-key';
const nonAdmittedIdentity = ['probe-tenant', 'probe-user'];
const admittedIdentity = ['demo-tenant', 'demo-user'];

test.describe('Story 5.4: MCP fail-closed security gates', () => {
  test('lists only admitted tools plus lifecycle for an authenticated tenant context', async ({ request }) => {
    const client = new McpClient(request);
    await client.initialize();

    const result = await client.call<{ tools: Array<{ name: string; title?: string; description?: string }> }>(
      'tools/list',
    );
    const names = result.tools.map((tool) => tool.name);

    expect(names.sort()).toEqual(admittedTools);
    expect(JSON.stringify(result)).not.toContain('Specimens.PolicyDenied');
    expect(JSON.stringify(result)).not.toContain('Specimens.PolicyAllowed');
    expect(JSON.stringify(result)).not.toContain('demo-tenant');
    expect(JSON.stringify(result)).not.toContain('demo-user');
  });

  test('rejects a missing or invalid API key with 401 before MCP dispatch', async ({ request }) => {
    await expectUnauthenticatedProbesRejected(request);
  });

  test('lists no command tools for an authenticated but non-admitted principal', async ({ request }) => {
    const client = new McpClient(request, nonAdmittedApiKey);
    await client.initialize();

    const result = await client.call<{ tools: Array<{ name: string }> }>('tools/list');
    const body = JSON.stringify(result);

    expect(result.tools.map((tool) => tool.name)).toEqual([lifecycleTool]);
    expect(body).not.toContain('Specimens.');
    expect(body).not.toContain('auth_failed');
    expect(body).not.toContain('tenant_missing');
    expectNoIdentityOrKeyEcho(body);
  });

  test('does not leak identity details or arguments when a non-admitted principal calls a tool', async ({ request }) => {
    const client = new McpClient(request, nonAdmittedApiKey);
    await client.initialize();

    const result = await client.call<McpCallToolResult>('tools/call', {
      name: visibleCommand,
      arguments: {
        Amount: 7,
        Note: secretArgument,
        EffectiveDate: '2026-06-05T00:00:00Z',
      },
    });
    const body = JSON.stringify(result);

    expect(result.isError).toBe(true);
    expect(singleText(result).text).toBe('Request failed.');
    expect(body).not.toContain(secretArgument);
    expect(body).not.toContain(visibleCommand);
    expect(body).not.toContain('auth_failed');
    expect(body).not.toContain('tenant_missing');
    expectNoIdentityOrKeyEcho(body);
  });

  test('returns an opaque unknown-tool payload without echoing raw arguments', async ({ request }) => {
    const client = new McpClient(request);
    await client.initialize();

    const result = await client.call<McpCallToolResult>('tools/call', {
      name: absentCommand,
      arguments: {
        RecordId: secretArgument,
        Reason: 'tenant demo-tenant user demo-user policy Specimens.PolicyDenied',
      },
    });
    const body = JSON.stringify(result);

    expect(result.isError).toBe(true);
    expect(singleText(result).text).toBe('Request failed.');
    expect(result.structuredContent?.category).toBe('unknown_tool');
    // Fail-closed parity (AC3): the unknown-tool envelope never echoes requestedToolName, so an
    // absent tool is indistinguishable from a tenant/policy-hidden one. Echoing it would be a
    // tool-existence oracle across the transport.
    expect(result.structuredContent?.requestedToolName).toBeUndefined();
    expect(body).not.toContain(absentCommand);
    expect(body).not.toContain(secretArgument);
    expect(body).not.toContain('Specimens.PolicyDenied');
    expect(body).not.toContain('demo-tenant');
    expect(body).not.toContain('demo-user');
  });

  test('does not echo malformed lifecycle handles or command arguments', async ({ request }) => {
    const client = new McpClient(request);
    await client.initialize();

    const result = await client.call<McpCallToolResult>('tools/call', {
      name: lifecycleTool,
      arguments: {
        correlationId: 'Bearer eyJhbGciOiJIUzI1NiJ9.lifecycle-handle',
        messageId: '01JZ0R5K9N8W4Y7V3Q2P6C1A0B',
      },
    });
    const body = JSON.stringify(result);

    expect(result.isError).toBe(true);
    expect(singleText(result).text).toBe('Request failed.');
    expect(result.structuredContent?.category).toBe('unknown_tool');
    expect(body).not.toContain('eyJhbGciOiJIUzI1NiJ9');
    expect(body).not.toContain('01JZ0R5K9N8W4Y7V3Q2P6C1A0B');
    expect(body).not.toContain('correlationId');
    expect(body).not.toContain('messageId');
  });

  test('collapses projection visibility failures for a non-admitted principal to the public unknown-resource response', async ({ request }) => {
    const client = new McpClient(request, nonAdmittedApiKey);
    await client.initialize();

    const projection = await client.call<{ contents: McpTextResourceContents[] }>('resources/read', {
      uri: 'frontcomposer://Counter/projections/CounterProjection',
    });
    const projectionText = singleResourceText(projection.contents);
    const body = JSON.stringify(projection);

    expect(projectionText.uri).toBe('frontcomposer://Counter/projections/CounterProjection');
    expect(projectionText.mimeType).toBe('text/plain');
    expect(projectionText.text).toBe('Projection resource is not available.');
    expect(body).toContain('unknown_resource');
    expect(body).not.toContain('auth_failed');
    expect(body).not.toContain('tenant_missing');
    expectNoIdentityOrKeyEcho(body);
  });
});


test.describe('Counter sample Development HTTP authentication', () => {
  let host: OwnedProcess | undefined;
  let baseUrl: string;
  test.afterEach(async () => {
    if (host) await stopOwnedProcess(host);
  });
  test.beforeEach(async () => {
    const projectDirectory = new URL('../../../samples/Counter/Counter.Web/', import.meta.url);
    const assembly = fileURLToPath(new URL('bin/Release/net10.0/Counter.Web.dll', projectDirectory));
    host = spawnOwnedProcess('dotnet', [assembly, '--urls', 'http://127.0.0.1:0'], {
      cwd: projectDirectory,
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        DOTNET_ENVIRONMENT: 'Development',
        Hexalith__FrontComposer__Specimens__Enabled: 'true',
      },
    }, 60_000);
    await expect.poll(() => [...host!.listeningOrigins][0]).toBeTruthy();
    baseUrl = [...host.listeningOrigins][0];
    await waitForOwnedServer(host, baseUrl, Date.now() + 10_000);
  });

  test('admits the exact API-key tool inventory and rejects missing, invalid or Test-only HTTP credentials', async ({ playwright }) => {
    const request = await playwright.request.newContext({ baseURL: baseUrl });
    try {
      const client = new McpClient(request);
      await client.initialize();
      const result = await client.call<{ tools: Array<{ name: string }> }>('tools/list');
      expect(result.tools.map((tool) => tool.name).sort()).toEqual(admittedTools);
      expect(JSON.stringify(result)).not.toContain('Specimens.');
      // The non-admitted probe key is registered only for the Test host, so Development rejects it too.
      await expectUnauthenticatedProbesRejected(request, [nonAdmittedApiKey]);
    } finally {
      await request.dispose();
    }
  });
});

const expectNoIdentityOrKeyEcho = (body: string) => {
  for (const secret of [...admittedIdentity, ...nonAdmittedIdentity, nonAdmittedApiKey, invalidApiKey]) {
    expect(body).not.toContain(secret);
  }
};

const expectUnauthenticatedProbesRejected = async (request: APIRequestContext, extraKeys: string[] = []) => {
  for (const key of [null, invalidApiKey, ...extraKeys]) {
    const response = await request.post('/mcp', {
      headers: {
        Accept: 'application/json, text/event-stream',
        ...(key === null ? {} : { 'X-FrontComposer-Mcp-Key': key }),
      },
      data: { jsonrpc: '2.0', id: 1, method: 'initialize', params: {
        protocolVersion: '2025-03-26', capabilities: {}, clientInfo: { name: 'auth-probe', version: '0.0.0' },
      } },
    });
    expect(response.status(), `initialize with ${key === null ? 'no key' : 'an unaccepted key'}`).toBe(401);
    const body = await response.text();
    expect(body).not.toContain('The FrontComposer MCP API key was rejected');
    expect(body).not.toContain(sampleMcpApiKey);
    expectNoIdentityOrKeyEcho(body);
  }
};

const singleText = (result: McpCallToolResult) => {
  expect(result.content).toHaveLength(1);
  return result.content[0];
};

const singleResourceText = (contents: McpTextResourceContents[]): McpTextResourceContents => {
  expect(contents).toHaveLength(1);
  return contents[0];
};
