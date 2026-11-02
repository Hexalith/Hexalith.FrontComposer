import { expect, test } from '../fixtures/index.js';

test('palette keeps native combobox relationships across selection, empty results and reopening', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const trigger = page.getByTestId('fc-palette-trigger');
  await trigger.click();

  const palette = page.getByTestId('fc-palette-root');
  const input = palette.getByTestId('fc-palette-search').locator('input');
  await expect(input).toHaveAttribute('role', 'combobox');
  await expect(input).toHaveAccessibleName(/Search projections/);
  await expect(palette.getByRole('combobox')).toHaveCount(1);
  await expect(input).toBeFocused();

  const relationships = () => input.evaluate(control => {
    const native = control as HTMLInputElement;
    const listbox = document.querySelector('[data-testid="fc-palette-root"] [role="listbox"]');
    const selected = listbox?.querySelector('[role="option"][aria-selected="true"]') ?? null;
    return {
      controlsActualListbox: native.ariaControlsElements?.length === 1 && native.ariaControlsElements[0] === listbox,
      ownsSelectedOption: native.ariaActiveDescendantElement === selected && selected !== null,
      selected: selected?.textContent?.trim(),
    };
  });

  await input.fill('counter');
  await expect(palette.getByRole('option')).toHaveText(['CounterProjection', 'ConfigureCounterCommand']);
  await expect(input).toHaveAttribute('aria-expanded', 'true');
  await expect(input).toHaveAttribute('aria-autocomplete', 'list');
  await expect(input).toHaveAttribute('aria-haspopup', 'listbox');
  await expect.poll(relationships).toEqual({ controlsActualListbox: true, ownsSelectedOption: true, selected: 'CounterProjection' });
  await input.press('ArrowDown');
  await expect.poll(relationships).toEqual({ controlsActualListbox: true, ownsSelectedOption: true, selected: 'ConfigureCounterCommand' });
  await expect(input).toBeFocused();
  await input.press('ArrowUp');
  await expect.poll(relationships).toEqual({ controlsActualListbox: true, ownsSelectedOption: true, selected: 'CounterProjection' });

  await input.fill('configure');
  await expect(palette.getByRole('option')).toHaveText(['ConfigureCounterCommand']);
  await expect.poll(relationships).toEqual({ controlsActualListbox: true, ownsSelectedOption: true, selected: 'ConfigureCounterCommand' });

  await input.fill('no-match-native-palette-136');
  await expect(palette.getByRole('option')).toHaveCount(0);
  await expect.poll(() => input.evaluate(control => {
    const native = control as HTMLInputElement;
    return { active: native.ariaActiveDescendantElement, controlledCount: native.ariaControlsElements?.length ?? 0, expanded: native.getAttribute('aria-expanded') };
  })).toEqual({ active: null, controlledCount: 0, expanded: 'false' });

  await input.press('Escape');
  await expect(palette).toHaveCount(0);
  await expect(trigger).toBeFocused();
  await trigger.click();
  await input.fill('counter');
  await expect.poll(relationships).toEqual({ controlsActualListbox: true, ownsSelectedOption: true, selected: 'CounterProjection' });
  await expect(input).toHaveAttribute('aria-expanded', 'true');
  await expect(palette.getByRole('combobox')).toHaveCount(1);
  await expect(input).toBeFocused();
});

test('palette synchronizes delayed and replaced shadow inputs and stops on unregister', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const result = await page.evaluate(async () => {
    const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
    const keyboard = await import(/* @vite-ignore */ modulePath);
    const root = document.createElement('div');
    const host = document.createElement('div');
    host.dataset.testid = 'fc-palette-search';
    host.dataset.fcPaletteControls = 'delayed-palette-results';
    host.dataset.fcPaletteExpanded = 'true';
    host.dataset.fcPaletteActiveDescendant = 'delayed-option';
    host.setAttribute('aria-label', 'Delayed search');
    const shadow = host.attachShadow({ mode: 'open' });
    root.append(host);
    const results = document.createElement('div');
    results.id = 'delayed-palette-results';
    const listbox = document.createElement('ul');
    listbox.setAttribute('role', 'listbox');
    const option = document.createElement('li');
    option.id = 'delayed-option';
    option.setAttribute('role', 'option');
    listbox.append(option);
    results.append(listbox);
    root.append(results);
    document.body.append(root);
    const frames = async () => {
      await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
    };
    try {
      keyboard.registerPaletteKeyFilter(root);
      keyboard.unregisterPaletteKeyFilter(root);
      const stopped = document.createElement('input');
      shadow.append(stopped);
      await frames();
      const stoppedBeforeInitialization = stopped.getAttribute('role') === null;
      stopped.remove();

      keyboard.registerPaletteKeyFilter(root);
      await frames();
      const delayed = document.createElement('input');
      shadow.append(delayed);
      await frames();
      const delayedSynchronized = delayed.getAttribute('role') === 'combobox'
        && delayed.ariaControlsElements?.[0] === listbox
        && delayed.ariaActiveDescendantElement === option;
      delayed.remove();
      const replacement = document.createElement('input');
      shadow.append(replacement);
      await frames();
      const replacementSynchronized = replacement.getAttribute('role') === 'combobox'
        && replacement.ariaActiveDescendantElement === option;
      keyboard.unregisterPaletteKeyFilter(root);
      replacement.remove();
      const afterDispose = document.createElement('input');
      shadow.append(afterDispose);
      host.dataset.fcPaletteExpanded = 'false';
      await frames();
      return { stoppedBeforeInitialization, delayedSynchronized, replacementSynchronized, stoppedAfterDisposal: afterDispose.getAttribute('role') === null };
    } finally {
      keyboard.unregisterPaletteKeyFilter(root);
      root.remove();
    }
  });
  expect(result).toEqual({ stoppedBeforeInitialization: true, delayedSynchronized: true, replacementSynchronized: true, stoppedAfterDisposal: true });
});

test('palette retains host compatibility semantics when native reflection is unavailable', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.evaluate(() => {
    for (const prototype of [Element.prototype, HTMLElement.prototype, HTMLInputElement.prototype]) {
      for (const name of ['ariaControlsElements', 'ariaActiveDescendantElement']) {
        if (Object.getOwnPropertyDescriptor(prototype, name)?.configurable) Reflect.deleteProperty(prototype, name);
      }
    }
  });
  await page.getByTestId('fc-palette-trigger').click();
  const host = page.getByTestId('fc-palette-search');
  const input = host.locator('input');
  expect(await input.evaluate(control => 'ariaControlsElements' in control || 'ariaActiveDescendantElement' in control)).toBe(false);
  await input.fill('counter');
  await expect(host).toHaveAttribute('role', 'combobox');
  await expect(host).toHaveAttribute('aria-controls', 'fc-palette-results-listbox');
  await expect(host).toHaveAttribute('aria-activedescendant', 'fc-palette-result-0');
  await input.press('ArrowDown');
  await expect(host).toHaveAttribute('aria-activedescendant', 'fc-palette-result-1');
  await expect(input).toBeFocused();
  await input.fill('no-match-fallback-136');
  await expect(host).not.toHaveAttribute('aria-activedescendant');
  await expect(host).toHaveAttribute('aria-expanded', 'false');
});
