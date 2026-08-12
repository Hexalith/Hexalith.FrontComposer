import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { expect, test } from '@playwright/test';

const REPO_ROOT = fileURLToPath(new URL('../../../', import.meta.url));
const BASE_CONTRACT = '_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md';
const SUCCESSOR_CONTRACT =
  '_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md';

test.describe('Story 9.3: FC-NIP explicit command target identity', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Contract coverage runs once in Chromium.');

  test('preserves the historical base and links the authoritative successor', async () => {
    const contract = await readRepoFile(BASE_CONTRACT);

    assertContainsAll(contract, [
      'Status: approved base decision; delivery completion rejected 2026-08-11',
      'EventStore command status',
      'Submit result payload',
      'Projection nudge',
      'Projection detail nudge metadata',
      'Pending-command registration metadata',
      'Generated command metadata',
      'Approved Payload Source',
      'FrontComposer-owned pending-command row metadata',
      'Story 9.2 is unblocked',
      'Resolution date:',
      'Approved successor: `fc-nip-command-target-identity-contract-2026-08-12.md`',
      'where target identity or outcome disposition is concerned, the successor is authoritative',
      'must not infer row identity by diffing visible grid rows',
      'marking every row in a lane',
      'treating a projection nudge as row identity',
      'The nudge can refresh a lane, but it carries no row key',
      'FrontComposer deliberately treats metadata as opaque',
      'AggregateId is insufficient',
      'Do not use EventStore ResultPayload',
      'EventStore command status remains a lifecycle/status source by `MessageId`',
      'MessageId',
      'ExpectedStatusSlot',
      'PriorStatusSlot',
      'CreatedAt',
      'TenantId',
      'UserId',
      'first-wins',
    ]);
  });

  test('pins the explicit provider and immutable pre-dispatch target snapshot', async () => {
    const rawContract = await readRawRepoFile(SUCCESSOR_CONTRACT);
    const contract = normalizeWhitespace(rawContract);

    assertContainsAll(contract, [
      'Status: approved successor decision',
      'explicit command-to-projection declaration',
      'ICommandTargetIdentityProvider<TCommand>',
      'SameAsSource',
      'before invoking asynchronous command dispatch',
      'Only after accepted dispatch',
      'MessageId',
      'ObservedAt',
    ]);

    assertTableRows(rawContract, '## Immutable Target Snapshot', [
      ['`ProjectionTypeName`', 'Exact projection named by the generated command-target descriptor.', 'Required; must resolve to that registered projection.'],
      ['`ViewKey`', 'Canonical generated view/lane identity selected by the descriptor and, when dynamic, returned through the typed provider then validated against the declared projection.', 'Required and non-empty. A route or visible grid is not a view-key source.'],
      ['`EntityKey`', 'Exact target key returned by the typed provider, or copied from the generated projection key snapshot only in declared `SameAsSource` mode.', 'Required and non-empty; EventStore `AggregateId` is not a substitute unless a later projection contract explicitly proves identity.'],
      ['`ChangeKind`', 'Declaration-fixed or typed-provider value: `Create`, `Update`, `StatusMove`, or `Delete`.', 'Required and known. `NoOp` is terminal materiality, not a change kind.'],
      ['`PriorStatus`', 'Typed-provider value, or copied from the explicit source snapshot for `SameAsSource`.', 'Required for `StatusMove`; otherwise optional.'],
      ['`ExpectedStatus`', 'Typed-provider destination value, or a declaration-fixed destination validated for the target view.', 'Required for `StatusMove` and whenever lane eligibility depends on destination status; otherwise optional.'],
      ['`CapturedAt`', 'FrontComposer `TimeProvider` at successful target resolution.', 'Required. It is never supplied by command fields or overwritten by a terminal timestamp.'],
    ]);

    assertContainsAll(contract, [
      'server allocates the exact key only after dispatch, FC-NIP suppresses the indicator',
      'typed post-dispatch identity proof',
      'must be available and copied exactly once during target resolution immediately before dispatch',
      'never re-read from or revalidated against a mutable or virtualized row',
      'command dispatch, transport acceptance, and command lifecycle continue under their existing semantics',
      '`LaneKey` carries the canonical target view/lane and becomes `NewItemIndicatorEntry.ViewKey`',
      '`PriorStatus` | `PriorStatusSlot`',
      '`ExpectedStatus` | `ExpectedStatusSlot`',
      '`ObservedAt` | `NewItemIndicatorEntry.CreatedAt`',
      '`CapturedAt` | No historical field',
      'bounded early-observation buffer/replay path',
      'different snapshot is a conflict',
      'including after that indicator is dismissed or expires',
      'Any unlisted outcome suppresses indicators by default',
      'Pre-accept failure, cancellation, timeout, malformed-message, unsupported, and future lifecycle outcomes',
      'approved at the human Story 9.3 `bmad-build` plan checkpoint on 2026-08-12',
    ]);
  });

  test('pins all eight command and terminal dispositions', async () => {
    const contract = await readRawRepoFile(SUCCESSOR_CONTRACT);

    assertTableRows(contract, '## Complete Outcome Disposition Matrix', [
      ['Standalone create', 'Typed provider resolves a valid `Create` snapshot before dispatch.', 'Confirmed + `Material`.', 'Publish only for the declared target view and entity. Missing or unknown target suppresses.'],
      ['Same-row update', 'Descriptor explicitly selects `SameAsSource`; the named pre-dispatch source snapshot is copied as an `Update` target.', 'Confirmed + `Material`.', 'Publish for that copied target. Never fall back to ambient source-row placement.'],
      ['Cross-row update', 'Typed provider resolves an `Update` target whose `EntityKey` may differ from the source.', 'Confirmed + `Material`.', 'Publish only for the provider-resolved target. Undeclared source reuse is invalid and suppresses.'],
      ['Status move', 'Typed provider resolves the target, `PriorStatus`, destination `ExpectedStatus`, and destination `ViewKey`.', 'Confirmed + `Material`.', 'Publish only in the destination lane and preserve both statuses. Missing destination status suppresses.'],
      ['Delete', 'Typed provider resolves a valid `Delete` target.', 'Confirmed + `Material`.', 'Preserve target metadata for lifecycle/audit; never publish a fresh-row indicator.'],
      ['Idempotent confirmation', 'A valid non-delete target was captured before dispatch.', '`IdempotentConfirmed` + `Material`.', 'Apply the same eligibility and existing ten-second TTL disposition as material confirmation; duplicate observation handling does not extend TTL. `NoOp` or `Unknown` suppresses.'],
      ['Rejected / needs review', 'Any valid or invalid declared target.', '`Rejected` or `NeedsReview`.', 'Never publish an indicator; preserve the lifecycle state.'],
      ['No-op', 'Any declared target.', 'Typed `NoOp`, including `EventCount == 0`, or `Unknown`.', 'Never publish an indicator. Status text and opaque payloads cannot upgrade it to `Material`.'],
    ]);
  });

  test('fails closed on every forbidden identity and materiality source', async () => {
    const contract = await readRepoFile(SUCCESSOR_CONTRACT);

    assertContainsAll(contract, [
      'ambient generated source-row placement',
      'command-property names',
      'current routes',
      'visible-row diffs',
      'projection nudges',
      'broad lane marking',
      'EventStore `AggregateId` as universal projection `EntityKey`',
      'opaque or domain-defined result payloads',
      'lifecycle/status text',
      'Unknown identity or materiality always fails closed',
      'There is no best-effort or source-row fallback',
    ]);
  });

  test('synchronizes product, architecture, and adopter truth without claiming completion', async () => {
    const prd = await readRepoFile('_bmad-output/planning-artifacts/prd.md');
    const planningArchitecture = await readRepoFile('_bmad-output/planning-artifacts/architecture.md');
    const publishedArchitecture = await readRepoFile('_bmad-output/project-docs/architecture.md');
    const dataGrid = await readRepoFile('docs/reference/components/datagrid.md');

    assertContainsAll(prd, [
      'Resolved 2026-08-12',
      SUCCESSOR_CONTRACT,
      'D-4 is resolved; Stories 9.4-9.8 still block FR-13/FR-26 completion and Epic 9 closure',
    ]);

    for (const architecture of [planningArchitecture, publishedArchitecture]) {
      assertContainsAll(architecture, [
        SUCCESSOR_CONTRACT,
        'ICommandTargetIdentityProvider<TCommand>',
        'SameAsSource',
        'CapturedAt',
        'ObservedAt',
        'Material',
        'NoOp',
        'Unknown',
      ]);
    }

    assertContainsAll(dataGrid, [
      SUCCESSOR_CONTRACT,
      'ICommandTargetIdentityProvider<TCommand>',
      'unknown identity or materiality suppresses the indicator',
      'Stories 9.4-9.8 still own',
    ]);
  });

  test('retains the existing row-cascade implementation as later-story gap evidence', async () => {
    const rowIdentity = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRowIdentity.cs',
    );
    const eventStoreStatusQuery = await readRepoFile(
      'src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs',
    );
    const commandFormEmitter = await readRepoFile(
      'src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs',
    );
    const razorEmitter = await readRepoFile(
      'src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs',
    );

    assertContainsAll(rowIdentity, [
      'projection row identity cascaded to generated command forms',
      'It must not be populated from raw',
      'command payloads or user-editable form values',
    ]);
    assertContainsAll(commandFormEmitter, [
      'CascadingParameter',
      'PendingCommandRowIdentity?.ProjectionTypeName',
      'PendingCommandRowIdentity?.LaneKey',
      'PendingCommandRowIdentity?.EntityKey',
      'PendingCommandRowIdentity?.ExpectedStatusSlot',
      'PendingCommandRowIdentity?.PriorStatusSlot',
    ]);
    assertContainsAll(eventStoreStatusQuery, [
      'MessageId: pendingCommand.MessageId',
      'string? AggregateId',
      'int? EventCount',
    ]);
    expect(eventStoreStatusQuery).not.toContain('EntityKey: status.AggregateId');
    expect(eventStoreStatusQuery).not.toContain('EntityKey:');
    expect(eventStoreStatusQuery).not.toContain('ProjectionTypeName:');
    expect(eventStoreStatusQuery).not.toContain('LaneKey:');
    expect(eventStoreStatusQuery).not.toContain('ExpectedStatusSlot:');
    expect(eventStoreStatusQuery).not.toContain('PriorStatusSlot:');
    expect(commandFormEmitter).not.toContain('EntityKey: status.AggregateId');
    expect(commandFormEmitter).not.toContain('ResultPayload');
    assertContainsAll(razorEmitter, [
      'PendingCommandRowIdentityFor(row)',
      'CascadingValue<global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity?>',
    ]);
  });
});

const assertContainsAll = (document: string, fragments: string[]): void => {
  for (const fragment of fragments) {
    expect(document, `Missing contract fragment: ${fragment}`).toContain(fragment);
  }
};

const assertTableRows = (document: string, heading: string, expectedRows: string[][]): void => {
  const actualRows = parseTableRows(document, heading);
  expect(actualRows).toEqual(expectedRows);
};

const parseTableRows = (document: string, heading: string): string[][] => {
  const lines = document.replace(/\r\n/g, '\n').split('\n');
  const headingIndex = lines.findIndex((line) => line.trim() === heading);
  expect(headingIndex, `${heading} heading is missing`).toBeGreaterThanOrEqual(0);
  const headerOffset = lines.slice(headingIndex + 1).findIndex((line) => line.trimStart().startsWith('|'));
  expect(headerOffset, `${heading} table header is missing`).toBeGreaterThanOrEqual(0);
  const headerIndex = headingIndex + 1 + headerOffset;
  const rows: string[][] = [];
  for (let index = headerIndex + 2; index < lines.length && lines[index].trimStart().startsWith('|'); index += 1) {
    rows.push(lines[index].trim().replace(/^\||\|$/g, '').split('|').map((cell) => cell.trim()));
  }
  return rows;
};

const readRepoFile = async (relativePath: string): Promise<string> => {
  const raw = await readRawRepoFile(relativePath);
  return normalizeWhitespace(raw);
};

const readRawRepoFile = async (relativePath: string): Promise<string> =>
  readFile(path.join(REPO_ROOT, relativePath), 'utf8');

const normalizeWhitespace = (value: string): string => value.replace(/\s+/g, ' ');
