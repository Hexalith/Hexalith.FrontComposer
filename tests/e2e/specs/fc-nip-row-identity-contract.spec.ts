import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { expect, test } from '@playwright/test';

const REPO_ROOT = fileURLToPath(new URL('../../../', import.meta.url));

test.describe('Story 9.1: FC-NIP row identity producer contract', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Contract E2E coverage runs once in Chromium.');

  test('pins the minimum row-identity payload and approved producer source', async () => {
    const contract = await readRepoFile('_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md');

    for (const candidate of [
      'EventStore command status',
      'Submit result payload',
      'Projection nudge',
      'Projection detail nudge metadata',
      'Pending-command registration metadata',
      'Generated command metadata',
    ]) {
      expect(contract, `${candidate} disposition is missing`).toContain(candidate);
    }

    for (const payloadField of [
      'ViewKey',
      'EntityKey',
      'MessageId',
      'ProjectionTypeName',
      'ExpectedStatusSlot',
      'CreatedAt',
      'TenantId',
      'UserId',
      'first-wins',
    ]) {
      expect(contract, `${payloadField} payload field is missing`).toContain(payloadField);
    }

    expect(contract).toContain('approved payload source');
    expect(contract).toContain('Approved Payload Source');
    expect(contract).toContain('FrontComposer-owned pending-command row metadata');
    expect(contract).toContain('Story 9.2 is unblocked');
    expect(contract).toContain('Resolution date:');
    expect(contract).toContain('AggregateId is insufficient');
    expect(contract).toContain('Do not use EventStore ResultPayload');
    expect(contract).toContain('EventStore command status remains a lifecycle/status source by `MessageId`');
  });

  test('preserves the no-guessing guardrails for fresh-row indicators', async () => {
    const contract = await readRepoFile('_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md');

    expect(contract).toContain('must not infer row identity by diffing visible grid rows');
    expect(contract).toContain('marking every row in a lane');
    expect(contract).toContain('treating a projection nudge as row identity');
    expect(contract).toContain('The nudge can refresh a lane, but it carries no row key');
    expect(contract).toContain('FrontComposer deliberately treats metadata as opaque');
  });

  test('names FC-NIP ownership across adopter and architecture documents', async () => {
    const fcTbl = await readRepoFile('_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md');
    const fcCmd = await readRepoFile('_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md');
    const architecture = await readRepoFile('_bmad-output/project-docs/architecture.md');
    const dataGrid = await readRepoFile('docs/reference/components/datagrid.md');

    expect(fcTbl).toContain('Epic 9 / FC-NIP');
    expect(fcTbl).toContain('Story 9.1 confirms the row-identity payload');
    expect(fcTbl).toContain('Story 9.2 wires the producer');

    expect(fcCmd).toContain('Row-level `FcNewItemIndicator` producer wiring is out of scope for FC-CMD v1');
    expect(fcCmd).toContain('Epic 9 / FC-NIP owns');

    expect(architecture).toContain('Fresh-row indicators are not produced from the projection nudge seam');
    expect(architecture).toContain('FC-NIP owns the post-MVP command outcome payload and producer wiring');

    expect(dataGrid).toContain('Automatic row-level producer wiring');
    expect(dataGrid).toContain('Epic 9 / FC-NIP');
    expect(dataGrid).toContain('current projection nudge does not include row identity');
  });

  test('pins Story 9.2 ready gate and current no-smuggling source evidence', async () => {
    const story = await readRepoFile(
      '_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md',
    );
    const eventStoreStatusQuery = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs',
    );
    const commandFormEmitter = await readRepoFile(
      'src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs',
    );

    expect(story).toContain('Status: ready-for-dev');
    expect(story).toContain('Story 9.2 is ready for a focused implementation pass');
    expect(story).toContain('FrontComposer-owned pending-command row metadata');
    expect(story).toContain('do not add best-effort producer code');
    expect(story).toContain('EventStorePendingCommandStatusQuery` currently reads EventStore status by pending `MessageId`');
    expect(story).toContain('CommandFormEmitter` currently registers pending commands with `CorrelationId`, `MessageId`, and `CommandTypeName` only');

    expect(eventStoreStatusQuery).toContain('MessageId: pendingCommand.MessageId');
    expect(eventStoreStatusQuery).toContain('string? AggregateId');
    expect(eventStoreStatusQuery).not.toContain('EntityKey: status.AggregateId');
    expect(eventStoreStatusQuery).not.toContain('ProjectionTypeName:');
    expect(eventStoreStatusQuery).not.toContain('LaneKey:');
    expect(eventStoreStatusQuery).not.toContain('ExpectedStatusSlot:');

    expect(commandFormEmitter).toContain('generator-known framework metadata is limited to CorrelationId,');
    expect(commandFormEmitter).toContain('MessageId, and CommandTypeName at form-emit time');
    expect(commandFormEmitter).toContain('CommandTypeName: typeof(');
    expect(commandFormEmitter).not.toContain('ProjectionTypeName:');
    expect(commandFormEmitter).not.toContain('LaneKey:');
    expect(commandFormEmitter).not.toContain('EntityKey:');
    expect(commandFormEmitter).not.toContain('ExpectedStatusSlot:');
  });
});

const readRepoFile = async (relativePath: string): Promise<string> => {
  const raw = await readFile(path.join(REPO_ROOT, relativePath), 'utf8');
  // Collapse whitespace runs (including newlines / CRLF) so a benign markdown reflow does not
  // silently break these substring guards.
  return raw.replace(/\s+/g, ' ');
};
