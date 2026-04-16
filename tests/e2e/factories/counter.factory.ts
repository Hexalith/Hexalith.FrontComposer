import { faker } from '@faker-js/faker';

export interface IncrementCommand {
  commandId: string;
  tenantId: string;
  userId: string;
  amount: number;
  issuedAt: string;
}

export type IncrementCommandOverrides = Partial<IncrementCommand>;

export const buildIncrementCommand = (overrides: IncrementCommandOverrides = {}): IncrementCommand => ({
  commandId: faker.string.ulid(),
  tenantId: overrides.tenantId ?? 'demo-tenant',
  userId: overrides.userId ?? 'demo-user',
  amount: faker.number.int({ min: 1, max: 10 }),
  issuedAt: new Date().toISOString(),
  ...overrides,
});

export const buildIncrementCommands = (count: number, overrides: IncrementCommandOverrides = {}): IncrementCommand[] =>
  Array.from({ length: count }, () => buildIncrementCommand(overrides));
