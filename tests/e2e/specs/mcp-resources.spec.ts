import { expect, test } from '../fixtures/index.js';
import { McpClient, type McpResource, type McpTextResourceContents } from '../helpers/mcp-client.js';

test.describe('Story 5.3: MCP projection and skill resources', () => {
  test('lists projection resources and framework skill resources with Markdown metadata', async ({ request }) => {
    const client = new McpClient(request);
    await client.initialize();

    const result = await client.call<{ resources: McpResource[] }>('resources/list');
    const uris = result.resources.map((resource) => resource.uri);

    expect(uris).toContain('frontcomposer://Counter/projections/CounterProjection');
    expect(uris).toContain('frontcomposer://skills/index');
    expect(uris).toContain('frontcomposer://skills/manifest');

    for (const uri of uris.filter((value) => value.startsWith('frontcomposer://skills/'))) {
      expect(uri).toBe(uri.toLowerCase());
    }

    const projection = result.resources.find(
      (resource) => resource.uri === 'frontcomposer://Counter/projections/CounterProjection',
    );
    expect(projection?.mimeType).toBe('text/markdown');

    const skill = result.resources.find((resource) => resource.uri === 'frontcomposer://skills/index');
    expect(skill?.mimeType).toBe('text/markdown');
  });

  test('reads the skill manifest and serves only agent-reference Markdown for a skill resource', async ({
    request,
  }) => {
    const client = new McpClient(request);
    await client.initialize();

    const manifest = await client.call<{ contents: McpTextResourceContents[] }>('resources/read', {
      uri: 'frontcomposer://skills/manifest',
    });
    const manifestText = singleText(manifest.contents);

    expect(manifestText.uri).toBe('frontcomposer://skills/manifest');
    expect(manifestText.mimeType).toBe('text/markdown');
    expect(manifestText.text).toContain('manifestSchemaVersion');
    expect(manifestText.text).toContain('resourceCount');
    expect(manifestText.text).toContain('frontcomposer://skills/index');

    const index = await client.call<{ contents: McpTextResourceContents[] }>('resources/read', {
      uri: 'frontcomposer://skills/index',
    });
    const indexText = singleText(index.contents);

    expect(indexText.uri).toBe('frontcomposer://skills/index');
    expect(indexText.mimeType).toBe('text/markdown');
    expect(indexText.text).toContain('Use these resources as framework-owned defaults');
    expect(indexText.text).toContain('never contains tenant data');
    expect(indexText.text).not.toContain('This source is shared by human documentation');
    expect(indexText.text).not.toContain('frontcomposer:section narrative');
    expect(indexText.text).not.toContain('frontcomposer:section agent-reference');
  });

  test('reads a tenant-scoped projection as bounded Markdown without identity leakage', async ({ request }) => {
    const client = new McpClient(request);
    await client.initialize();

    const projection = await client.call<{ contents: McpTextResourceContents[] }>('resources/read', {
      uri: 'frontcomposer://Counter/projections/CounterProjection',
    });
    const projectionText = singleText(projection.contents);

    expect(projectionText.uri).toBe('frontcomposer://Counter/projections/CounterProjection');
    expect(projectionText.mimeType).toBe('text/markdown');
    expect(projectionText.text).toContain('Counter');
    expect(projectionText.text).toContain('counter-main');
    expect(projectionText.text).toContain('42');
    expect(projectionText.text).not.toContain('demo-tenant');
    expect(projectionText.text).not.toContain('demo-user');
  });

  test('returns a sanitized projection failure for malformed schema fingerprint input', async ({ request }) => {
    const client = new McpClient(request, undefined, {
      'x-frontcomposer-schema-fingerprint': 'Bearer eyJhbGciOiJIUzI1NiJ9.invalid',
    });
    await client.initialize();

    const projection = await client.call<{ contents: McpTextResourceContents[] }>('resources/read', {
      uri: 'frontcomposer://Counter/projections/CounterProjection',
    });
    const projectionText = singleText(projection.contents);

    expect(projectionText.uri).toBe('frontcomposer://Counter/projections/CounterProjection');
    expect(projectionText.mimeType).toBe('text/plain');
    expect(projectionText.text).toContain('malformed_resource');
    expect(projectionText.text).toContain('Projection resource request is invalid.');
    expect(projectionText.text).not.toContain('eyJhbGciOiJIUzI1NiJ9');
    expect(projectionText.text).not.toContain('demo-tenant');
    expect(projectionText.text).not.toContain('demo-user');
  });
});

const singleText = (contents: McpTextResourceContents[]): McpTextResourceContents => {
  expect(contents).toHaveLength(1);
  return contents[0];
};
