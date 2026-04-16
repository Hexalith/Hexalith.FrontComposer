import type { BrowserContext } from '@playwright/test';

export interface SessionIdentity {
  tenantId: string;
  userId: string;
}

/**
 * Seeds the Blazor shell session storage with tenant + user identity.
 * Mirrors the contract used by `DemoUserContextAccessor` in the Counter sample
 * and the `IStorageService` abstraction from the Shell.
 */
export const seedDemoSession = async (context: BrowserContext, identity: SessionIdentity): Promise<void> => {
  const baseURL = process.env.BASE_URL ?? 'https://localhost:7000';
  await context.addInitScript(
    ({ tenantId, userId }) => {
      window.localStorage.setItem('hfc.session.tenantId', tenantId);
      window.localStorage.setItem('hfc.session.userId', userId);
    },
    identity,
  );
  await context.addCookies([
    {
      name: 'hfc-demo-tenant',
      value: identity.tenantId,
      url: baseURL,
    },
  ]);
};
