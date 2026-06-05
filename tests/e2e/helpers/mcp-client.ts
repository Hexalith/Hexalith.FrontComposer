import type { APIRequestContext, APIResponse } from '@playwright/test';

export interface JsonRpcResponse<T> {
  jsonrpc: '2.0';
  id: number | string;
  result?: T;
  error?: {
    code: number;
    message: string;
    data?: unknown;
  };
}

export interface McpResource {
  uri: string;
  name?: string;
  title?: string;
  description?: string;
  mimeType?: string;
}

export interface McpTextResourceContents {
  uri: string;
  mimeType?: string;
  text: string;
}

export class McpClient {
  private nextId = 1;
  private sessionId: string | undefined;

  public constructor(
    private readonly request: APIRequestContext,
    private readonly apiKey = process.env.FC_E2E_MCP_API_KEY ?? 'counter-e2e-mcp-key',
    private readonly extraHeaders: Record<string, string> = {},
  ) {}

  public async initialize(): Promise<unknown> {
    const result = await this.call('initialize', {
      protocolVersion: '2025-03-26',
      capabilities: {},
      clientInfo: {
        name: 'frontcomposer-e2e',
        version: '0.0.0',
      },
    });

    await this.notify('notifications/initialized');
    return result;
  }

  public async call<T = unknown>(method: string, params?: unknown): Promise<T> {
    const response = await this.post({
      jsonrpc: '2.0',
      id: this.nextId++,
      method,
      ...(params === undefined ? {} : { params }),
    });

    const message = await parseJsonRpcResponse<T>(response);
    if (message.error) {
      throw new Error(`${method} failed: ${message.error.code} ${message.error.message}`);
    }

    if (message.result === undefined) {
      throw new Error(`${method} returned no result`);
    }

    return message.result;
  }

  private async notify(method: string, params?: unknown): Promise<void> {
    const response = await this.post({
      jsonrpc: '2.0',
      method,
      ...(params === undefined ? {} : { params }),
    });

    if (!response.ok()) {
      throw new Error(`${method} failed with HTTP ${response.status()}: ${await response.text()}`);
    }
  }

  private async post(data: unknown): Promise<APIResponse> {
    const headers: Record<string, string> = {
      Accept: 'application/json, text/event-stream',
      'Content-Type': 'application/json',
      'X-FrontComposer-Mcp-Key': this.apiKey,
      ...this.extraHeaders,
    };
    if (this.sessionId) {
      headers['Mcp-Session-Id'] = this.sessionId;
    }

    const response = await this.request.post('/mcp', { data, headers });
    const responseSessionId = response.headers()['mcp-session-id'];
    if (responseSessionId) {
      this.sessionId = responseSessionId;
    }

    if (!response.ok()) {
      throw new Error(`MCP HTTP ${response.status()}: ${await response.text()}`);
    }

    return response;
  }
}

const parseJsonRpcResponse = async <T>(response: APIResponse): Promise<JsonRpcResponse<T>> => {
  const contentType = response.headers()['content-type'] ?? '';
  const body = await response.text();
  if (contentType.includes('text/event-stream')) {
    return parseSseJsonRpcResponse<T>(body);
  }

  return JSON.parse(body) as JsonRpcResponse<T>;
};

const parseSseJsonRpcResponse = <T>(body: string): JsonRpcResponse<T> => {
  const dataLines = body
    .split(/\r?\n/u)
    .filter((line) => line.startsWith('data:'))
    .map((line) => line.slice('data:'.length).trim())
    .filter((line) => line.length > 0 && line !== '[DONE]');

  if (dataLines.length === 0) {
    throw new Error(`MCP SSE response did not contain data: ${body}`);
  }

  return JSON.parse(dataLines[dataLines.length - 1]) as JsonRpcResponse<T>;
};
