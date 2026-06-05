import { expect, test } from '../fixtures/index.js';
import { McpClient, type McpCallToolResult } from '../helpers/mcp-client.js';
import {
  getGeneratedSchemaFingerprintHeader,
  staleSourceToolsFingerprintHeader,
} from '../helpers/mcp-schema-fingerprints.js';

const schemaHeaderName = 'x-frontcomposer-schema-fingerprint';
const visibleCommand = 'Counter.BatchIncrementCommand.Execute';
const hiddenPolicyCommand = 'Specimens.PolicyDeniedSpecimenCommand.Execute';
const exactCommandFingerprint = getGeneratedSchemaFingerprintHeader(visibleCommand);
const malformedFingerprint = 'frontcomposer.schema.sha256.v1.sourcetools-blob:' + 'A'.repeat(64);
const commandArguments = {
  Amount: 4,
  Note: 'QA story 5.5 schema negotiation',
  EffectiveDate: '2026-06-05T00:00:00Z',
};

test.describe('Story 5.5: MCP schema fingerprint negotiation', () => {
  test('allows a generated command call with the exact descriptor fingerprint', async ({ request }) => {
    const client = new McpClient(request, undefined, {
      [schemaHeaderName]: exactCommandFingerprint,
    });
    await client.initialize();

    const result = await client.call<McpCallToolResult>('tools/call', {
      name: visibleCommand,
      arguments: commandArguments,
    });

    expect(result.isError).toBeFalsy();
    expect(singleText(result).text).toBe('Command acknowledged.');
    expect(result.structuredContent?.state).toBe('Acknowledged');
    expect(JSON.stringify(result)).not.toContain('demo-tenant');
    expect(JSON.stringify(result)).not.toContain('demo-user');
  });

  test('rejects a stale generated command fingerprint without echoing schema or argument details', async ({
    request,
  }) => {
    const client = new McpClient(request, undefined, {
      [schemaHeaderName]: staleSourceToolsFingerprintHeader,
    });
    await client.initialize();

    const result = await client.call<McpCallToolResult>('tools/call', {
      name: visibleCommand,
      arguments: commandArguments,
    });
    const body = JSON.stringify(result);

    expect(result.isError).toBe(true);
    expect(singleText(result).text).toContain('Schema is not compatible');
    expect(result.structuredContent?.category).toBe('schema-mismatch');
    expect(result.structuredContent?.docsCode).toBe('HFC-SCHEMA-MISMATCH');
    expect(body).not.toContain(staleSourceToolsFingerprintHeader);
    expect(body).not.toContain(commandArguments.Note);
    expect(body).not.toContain('demo-tenant');
    expect(body).not.toContain('demo-user');
  });

  test('still applies current-server validation after exact schema negotiation succeeds', async ({
    request,
  }) => {
    const client = new McpClient(request, undefined, {
      [schemaHeaderName]: exactCommandFingerprint,
    });
    await client.initialize();

    const result = await client.call<McpCallToolResult>('tools/call', {
      name: visibleCommand,
      arguments: {
        ...commandArguments,
        TenantId: 'attacker-tenant',
      },
    });
    const body = JSON.stringify(result);

    expect(result.isError).toBe(true);
    expect(singleText(result).text).toBe('Request failed.');
    expect(result.structuredContent).toBeUndefined();
    expect(body).not.toContain('schema-mismatch');
    expect(body).not.toContain('attacker-tenant');
  });

  test('fails closed for malformed command fingerprint headers without echoing the raw header', async ({
    request,
  }) => {
    const client = new McpClient(request, undefined, {
      [schemaHeaderName]: malformedFingerprint,
    });
    await client.initialize();

    const result = await client.call<McpCallToolResult>('tools/call', {
      name: visibleCommand,
      arguments: commandArguments,
    });
    const body = JSON.stringify(result);

    expect(result.isError).toBe(true);
    expect(singleText(result).text).toBe('Request failed.');
    expect(result.structuredContent).toBeUndefined();
    expect(body).not.toContain(malformedFingerprint);
    expect(body).not.toContain(commandArguments.Note);
  });

  test('keeps hidden-equivalent precedence over stale schema diagnostics', async ({ request }) => {
    const client = new McpClient(request, undefined, {
      [schemaHeaderName]: staleSourceToolsFingerprintHeader,
    });
    await client.initialize();

    const result = await client.call<McpCallToolResult>('tools/call', {
      name: hiddenPolicyCommand,
      arguments: {
        RecordId: 'FC-SCHEMA-HIDDEN-001',
        Reason: 'policy-hidden command with stale schema',
      },
    });
    const body = JSON.stringify(result);

    expect(result.isError).toBe(true);
    expect(singleText(result).text).toBe('Request failed.');
    expect(result.structuredContent?.category).toBe('unknown_tool');
    expect(body).not.toContain('schema-mismatch');
    expect(body).not.toContain(hiddenPolicyCommand);
    expect(body).not.toContain('Specimens.PolicyDenied');
    expect(body).not.toContain('FC-SCHEMA-HIDDEN-001');
    expect(body).not.toContain(staleSourceToolsFingerprintHeader);
  });
});

const singleText = (result: McpCallToolResult) => {
  expect(result.content).toHaveLength(1);
  return result.content[0];
};
