import { expect, test } from '../fixtures/index.js';
import { McpClient, type McpCallToolResult, type McpResource, type McpTextResourceContents } from '../helpers/mcp-client.js';

const lifecycleTool = 'frontcomposer.lifecycle.subscribe';
const visibleCommand = 'Counter.BatchIncrementCommand.Execute';
const absentCommand = 'Counter.DoesNotExistCommand.Execute';
const secretArgument = 'Bearer eyJhbGciOiJIUzI1NiJ9.agent-secret';
const invalidApiKey = 'Bearer eyJhbGciOiJIUzI1NiJ9.invalid-mcp-key';

test.describe('Story 5.4: MCP fail-closed security gates', () => {
  test('lists only admitted tools plus lifecycle for an authenticated tenant context', async ({ request }) => {
    const client = new McpClient(request);
    await client.initialize();

    const result = await client.call<{ tools: Array<{ name: string; title?: string; description?: string }> }>(
      'tools/list',
    );
    const names = result.tools.map((tool) => tool.name);

    expect(names).toContain(lifecycleTool);
    expect(names).toContain(visibleCommand);
    expect(JSON.stringify(result)).not.toContain('Specimens.PolicyDenied');
    expect(JSON.stringify(result)).not.toContain('Specimens.PolicyAllowed');
    expect(JSON.stringify(result)).not.toContain('demo-tenant');
    expect(JSON.stringify(result)).not.toContain('demo-user');
  });

  test('returns an empty tools list without protocol error when auth is missing or invalid', async ({ request }) => {
    for (const client of [new McpClient(request, null), new McpClient(request, invalidApiKey)]) {
      await client.initialize();

      const result = await client.call<{ tools: McpResource[] }>('tools/list');

      expect(result.tools).toEqual([]);
      expect(JSON.stringify(result)).not.toContain('auth_failed');
      expect(JSON.stringify(result)).not.toContain('tenant_missing');
      expect(JSON.stringify(result)).not.toContain(invalidApiKey);
    }
  });

  test('does not leak auth details or arguments when tool calls are unauthenticated', async ({ request }) => {
    const client = new McpClient(request, invalidApiKey);
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
    expect(body).not.toContain(invalidApiKey);
    expect(body).not.toContain('auth_failed');
    expect(body).not.toContain('tenant_missing');
    expect(body).not.toContain('demo-tenant');
    expect(body).not.toContain('demo-user');
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

  test('collapses projection auth failures to the public unknown-resource response', async ({ request }) => {
    const client = new McpClient(request, invalidApiKey);
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
    expect(body).not.toContain(invalidApiKey);
    expect(body).not.toContain('demo-tenant');
    expect(body).not.toContain('demo-user');
  });
});

const singleText = (result: McpCallToolResult) => {
  expect(result.content).toHaveLength(1);
  return result.content[0];
};

const singleResourceText = (contents: McpTextResourceContents[]): McpTextResourceContents => {
  expect(contents).toHaveLength(1);
  return contents[0];
};
