import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { expect, test } from '@playwright/test';

const REPO_ROOT = fileURLToPath(new URL('../../../', import.meta.url));
const BASE_CONTRACT = '_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md';
const SUCCESSOR_CONTRACT =
  '_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md';
const FC_TBL_CONTRACT = '_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md';
const FC_CMD_CONTRACT =
  '_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md';
const STORY_9_2 =
  '_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md';
const INDICATOR_STATE_SERVICE =
  'src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs';
const PENDING_STATE_SERVICE =
  'src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs';
const PENDING_POLLING_COORDINATOR =
  'src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs';
const PENDING_OUTCOME_RESOLVER =
  'src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs';

// These checks read the filesystem only — they never open a page. The `--project=chromium` flag in
// the `test:fc-nip` npm script already pins execution to a single project, so a browserName guard
// here would only add a way for the whole suite to skip silently instead of failing.
test.describe('Story 9.3: FC-NIP explicit command target identity', () => {
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
      'ViewKey',
      'EntityKey',
      'ProjectionTypeName',
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
      ['`TenantId`', 'Framework-owned tenant accessor at target resolution.', 'Required and non-empty. It is never read from command fields or tool input.'],
      ['`UserId`', 'Framework-owned user accessor at target resolution.', 'Required and non-empty. It is never read from command fields or tool input.'],
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
      '| `TenantId` | `TenantId` |',
      '| `UserId` | `UserId` |',
      'bounded early-observation buffer/replay path',
      'different snapshot is a conflict',
      'including after that indicator is dismissed or expires',
      'Any unlisted outcome suppresses indicators by default',
      'Pre-accept failure, cancellation, timeout, malformed-message, unsupported, and future lifecycle outcomes',
      'approved at the human Story 9.3 `bmad-build` plan checkpoint on 2026-08-12',
    ]);

    // The declaration authoring surface is the sole legitimate target source.
    assertContainsAll(contract, [
      'Declaration Authoring Surface',
      'is an attribute applied to the command type',
      'Neither a DI registration, a configuration entry, a naming convention, nor a runtime call may act as a declaration',
      'are a duplicate registration: resolution fails closed',
    ]);

    // Scope, precedence, and SameAsSource validity.
    assertContainsAll(contract, [
      'Publication requires that the active tenant and user at eligible terminal observation equal the',
      'captured pair; any inequality suppresses FC-NIP publication',
      'A disagreement is not resolved by precedence: it fails closed',
      'A declared `SameAsSource` mode is valid only with `ChangeKind = Update`',
      '`SameAsSource` combined with `Create`, `StatusMove`, or `Delete` is an invalid declaration and fails closed',
    ]);

    // Terminal materiality as a closed set, not bare tokens a heading would satisfy.
    assertContainsAll(contract, [
      '`Material`, `NoOp`, or `Unknown`',
      '`Material` means the typed terminal adapter has affirmative evidence',
      '`NoOp` means the typed adapter has affirmative no-work evidence',
      '`Unknown` means evidence is absent, malformed, unsupported, contradictory',
      'Both `NoOp` and `Unknown` suppress the indicator',
      'Lifecycle text is never parsed to determine materiality',
    ]);

    // The seven behavioural rules routed to Story 9.4, plus the non-blocking preallocation deferral.
    assertContainsAll(contract, [
      'a bounded provider-resolution deadline',
      'empty or non-ULID `MessageId`',
      'separates a duplicate re-observation from a conflict',
      'canonicalization plus comparison ordinality',
      'maximum `CapturedAt`-to-`ObservedAt` age and a clock-skew rule',
      'capacity, eviction policy, and overflow disposition',
      'invalidation events that discard a captured snapshot before terminal observation',
      'Deferred framework preallocation (does not block Story 9.8)',
      'provider-reported exact',
      'already carried by the typed command before dispatch is sufficient',
      'key first allocated after dispatch remains indicator-ineligible',
    ]);

    // Opt-in migration: the live row-cascade regression and its explicit handling.
    assertContainsAll(contract, [
      'FC-NIP is **opt-in per command**',
      'publishes fresh-row indicators **with no declaration of any kind**',
      'No implicit or generated declaration closes that gap',
      'the historical cascade is not silently promoted into a `SameAsSource`',
      'build-time SourceTools diagnostic',
      "migrates this repository's own `[Command]` samples",
      'fresh-row indicators now require a declaration',
    ]);

    // The three sentences a later edit could reverse to manufacture completion.
    assertContainsAll(contract, [
      'FR-13, FR-26, and Epic 9 remain open through Story 9.8',
      'This records approved semantics, not Story 9.3 completion',
      'Story 9.3 does not add a public runtime API, change EventStore, or implement generated/runtime behavior',
    ]);

    // Bind the ten-second prose to the constant that implements it.
    expect(contract).toContain('ten-second TTL');
    const indicatorState = await readRepoFile(INDICATOR_STATE_SERVICE);
    expect(indicatorState).toContain('DefaultLifetime = TimeSpan.FromSeconds(10)');
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
      'ambient generated source-row placement or an undeclared cascading row context;',
      'command-property names such as `Id`, `EntityId`, `AggregateId`, or `Status`;',
      'current routes, query strings, selected tabs, visible rows, or virtualized-row instances;',
      'visible-row diffs, projection nudges, unrelated refreshes, or broad lane marking;',
      'EventStore `AggregateId` as universal projection `EntityKey`;',
      'opaque or domain-defined result payloads; or',
      'lifecycle/status text.',
      'Provider failure, cancellation, missing registration',
      'Unknown identity or materiality always fails closed',
      'There is no ambient-source fallback',
      'There is no best-effort or source-row fallback',
    ]);
  });

  test('synchronizes product, architecture, and adopter truth without claiming completion', async () => {
    const prd = await readRepoFile('_bmad-output/planning-artifacts/prd.md');
    const planningArchitecture = await readRepoFile('_bmad-output/planning-artifacts/architecture.md');
    const publishedArchitecture = await readRepoFile('_bmad-output/project-docs/architecture.md');
    const dataGrid = await readRepoFile('docs/reference/components/datagrid.md');
    const fcTbl = await readRepoFile(FC_TBL_CONTRACT);
    const fcCmd = await readRepoFile(FC_CMD_CONTRACT);

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
        '`Material`, `NoOp`, or `Unknown`',
      ]);
    }

    // The published DocFX site must not leak internal planning paths and must describe the
    // explicit target producer boundary now shipped by Story 9.4.
    assertContainsAll(dataGrid, [
      'producer wiring uses an explicit command-to-projection `[CommandTarget]`',
      '`SameAsSource` is valid only for `Update`',
      'Only a confirmed or idempotent-confirmed `Material` terminal observation',
      'ambient row placement are never target',
      'must equal the declared projection\'s canonical generated view key',
      'at most once per accepted `MessageId`',
      'queues bounded circuit-local convergence work',
      'retry runs before status transport polling',
      'never re-queries command status, changes the outcome, or retries the indicator decision',
    ]);
    expect(dataGrid).not.toContain('_bmad-output');
    expect(dataGrid).toContain('ICommandTargetIdentityProvider<TCommand>');

    // Sibling FC-TBL / FC-CMD ownership wording must track the successor decision.
    for (const sibling of [fcTbl, fcCmd]) {
      assertContainsAll(sibling, [
        'Epic 9 / FC-NIP',
        'fc-nip-command-target-identity-contract-2026-08-12.md',
        'Stories 9.4-9.8 own implementation and composed/live acceptance',
      ]);
      expect(sibling).not.toContain('Story 9.1 confirms');
      expect(sibling).not.toContain('Story 9.2 wires');
    }

    expect(fcCmd).toContain(
      'Row-level `FcNewItemIndicator` producer wiring is out of scope for FC-CMD v1',
    );
  });

  test('pins the converged producer boundary while retaining the explicit row cascade', async () => {
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
    const storyNineTwo = await readRepoFile(STORY_9_2);
    const pendingState = await readRepoFile(PENDING_STATE_SERVICE);
    const pollingCoordinator = await readRepoFile(PENDING_POLLING_COORDINATOR);
    const outcomeResolver = await readRepoFile(PENDING_OUTCOME_RESOLVER);

    assertContainsAll(rowIdentity, [
      'projection row identity cascaded to generated command forms',
      'It must not be populated from raw',
      'command payloads or user-editable form values',
    ]);
    assertContainsAll(eventStoreStatusQuery, [
      'MessageId: pendingCommand.MessageId',
      'string? AggregateId',
      'int? EventCount',
    ]);
    expect(eventStoreStatusQuery).not.toContain('EntityKey:');
    expect(eventStoreStatusQuery).not.toContain('ProjectionTypeName:');
    expect(eventStoreStatusQuery).not.toContain('LaneKey:');
    expect(eventStoreStatusQuery).not.toContain('ExpectedStatusSlot:');
    expect(eventStoreStatusQuery).not.toContain('PriorStatusSlot:');

    // The form emitter may read the row cascade only inside an explicit SameAsSource branch;
    // all terminal mutation and accepted association go through the resolver.
    assertContainsAll(commandFormEmitter, [
      'CascadingParameter',
      'CommandTypeName: typeof(',
      'form.CommandTarget?.ResolutionMode == CommandTargetResolutionMode.SameAsSource',
      'ResolveCommandTargetAsync(_model, cts.Token)',
      'PendingCommandOutcomeResolver.AssociateAccepted',
      'PendingCommandOutcomeResolver.Resolve',
    ]);
    expect(commandFormEmitter).not.toContain('PendingCommandState.ResolveTerminal');
    expect(commandFormEmitter).not.toContain('PendingCommandState.Register');
    expect(commandFormEmitter).not.toContain('EntityKey: status.AggregateId');
    expect(commandFormEmitter).not.toContain('ResultPayload');
    assertContainsAll(pendingState, [
      '_lifecycleConvergenceDeadlines',
      'TryConvergeLifecycle(canonicalMessageId)',
      'ConvergeLifecycle(int maximumAttempts)',
      'LifecycleMatches(terminal, lifecycleState)',
      'catch (Exception ex) when (ex is not OperationCanceledException)',
    ]);
    assertContainsAll(outcomeResolver, [
      '_indicatorDecisions.Contains(messageId!)',
      'RecordIndicatorDecision(messageId!)',
      '_ = _indicatorDecisions.Add(messageId)',
      'TryPublishIndicator(publication)',
      'TryGetCommittedRegistration(registration)',
      '!string.Equals(delegatedMessageId, canonicalMessageId, StringComparison.Ordinal)',
    ]);
    expect(outcomeResolver).not.toContain('_indicatorDecisions.Remove');
    expect((outcomeResolver.match(/_indicatorDecisions\.Clear\(\)/g) ?? []).length).toBe(1);
    const reservationSnapshotIndex = pollingCoordinator.indexOf('_pendingCommands.Snapshot()');
    const convergenceIndex = pollingCoordinator.indexOf(
      'concreteState.ConvergeLifecycle(convergenceBudget)',
      reservationSnapshotIndex,
    );
    const pollingSnapshotIndex = pollingCoordinator.indexOf(
      '_pendingCommands.Snapshot()',
      convergenceIndex,
    );
    const transportIndex = pollingCoordinator.indexOf('_statusQuery', pollingSnapshotIndex);
    expect(reservationSnapshotIndex).toBeGreaterThanOrEqual(0);
    expect(convergenceIndex).toBeGreaterThan(reservationSnapshotIndex);
    expect(pollingSnapshotIndex).toBeGreaterThan(convergenceIndex);
    expect(transportIndex).toBeGreaterThan(pollingSnapshotIndex);
    expect(pollingCoordinator).toContain('hasPendingTransport ? Math.Max(0, budget - 1) : budget');
    assertContainsAll(razorEmitter, [
      'PendingCommandRowIdentityFor(row)',
      'CascadingValue<global::Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRowIdentity?>',
    ]);

    // Story 9.2's delivery record stays pinned: its no-smuggling prohibition is the reason the
    // emitter assertions above are worth making.
    assertContainsAll(storyNineTwo, [
      'Status: done',
      'FrontComposer-owned pending-command row metadata',
      'Source-level wiring was proven',
      'Do not hide FC-NIP row identity in optional EventStore/domain-defined `ResultPayload`',
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

/**
 * Parses the first Markdown table following `heading`, bounded to that heading's own section. The
 * scan stops at the next Markdown heading so a deleted table fails instead of silently binding to a
 * later section's table, and the separator row is verified rather than assumed. Mirrors
 * `FcNipRowIdentityProducerContractTests.ParseTableRows`.
 */
const parseTableRows = (document: string, heading: string): string[][] => {
  const lines = document.replace(/\r\n/g, '\n').split('\n');
  const headingIndex = lines.findIndex((line) => line.trim() === heading);
  expect(headingIndex, `${heading} heading is missing`).toBeGreaterThanOrEqual(0);

  const sectionEndOffset = lines.slice(headingIndex + 1).findIndex((line) => line.trimStart().startsWith('#'));
  const sectionEnd = sectionEndOffset < 0 ? lines.length : headingIndex + 1 + sectionEndOffset;

  const headerOffset = lines
    .slice(headingIndex + 1, sectionEnd)
    .findIndex((line) => line.trimStart().startsWith('|'));
  expect(headerOffset, `${heading} section contains no table`).toBeGreaterThanOrEqual(0);
  const headerIndex = headingIndex + 1 + headerOffset;
  expect(headerIndex + 2, `${heading} table is truncated`).toBeLessThanOrEqual(sectionEnd);
  expect(
    /^\|[\s:|-]+\|$/.test(lines[headerIndex + 1].trim()),
    `${heading} table is missing its separator row`,
  ).toBe(true);

  const rows: string[][] = [];
  for (let index = headerIndex + 2; index < sectionEnd && lines[index].trimStart().startsWith('|'); index += 1) {
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
