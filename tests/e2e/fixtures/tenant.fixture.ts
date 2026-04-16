import { test as base } from '@playwright/test';

export interface TenantContext {
  tenantId: string;
  userId: string;
}

export type TenantFixtures = {
  tenant: TenantContext;
};

export const tenantTest = base.extend<TenantFixtures>({
  tenant: async ({}, use) => {
    const tenantId = process.env.DEFAULT_TENANT_ID ?? 'demo-tenant';
    const userId = process.env.DEFAULT_USER_ID ?? 'demo-user';
    await use({ tenantId, userId });
  },
});
