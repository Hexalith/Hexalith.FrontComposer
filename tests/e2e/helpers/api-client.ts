import { request, type APIRequestContext } from '@playwright/test';

export interface ApiClientOptions {
  baseURL?: string;
  tenantId?: string;
  userId?: string;
  extraHeaders?: Record<string, string>;
}

export const createApiClient = async (options: ApiClientOptions = {}): Promise<APIRequestContext> => {
  const baseURL = options.baseURL ?? process.env.API_URL ?? 'https://localhost:7001';
  return request.newContext({
    baseURL,
    ignoreHTTPSErrors: true,
    extraHTTPHeaders: {
      'Content-Type': 'application/json',
      ...(options.tenantId ? { 'X-Tenant-Id': options.tenantId } : {}),
      ...(options.userId ? { 'X-User-Id': options.userId } : {}),
      ...options.extraHeaders,
    },
  });
};
