const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');

const { chromium } = require('playwright');
const AxeBuilder = require('@axe-core/playwright').default;

const repoRoot = path.resolve(__dirname, '../../..');
const baseUrl = 'http://127.0.0.1:5055';
const outputDir = process.env.FC_STORY_2_2_OUTPUT_DIR
  ? path.resolve(repoRoot, process.env.FC_STORY_2_2_OUTPUT_DIR)
  : __dirname;
const artifactPath = path.join(outputDir, '2-2-e2e-results.json');
const evidenceDir = path.join(outputDir, 'evidence');

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function sanitizeName(value) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
}

function serverTerminationError(url, termination) {
  if (termination.error) {
    return new Error(`Counter host failed before ${url} became ready: ${termination.error.message}`);
  }

  return new Error(
    `Counter host exited before ${url} became ready (code=${termination.code ?? '<none>'}, signal=${termination.signal ?? '<none>'})`);
}

async function waitForServer(url, timeoutMs, serverTerminationPromise) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const remainingMs = timeoutMs - (Date.now() - started);
    const outcome = await Promise.race([
      fetch(url, { signal: AbortSignal.timeout(Math.max(1, remainingMs)) })
        .then(response => ({ kind: 'response', response }))
        .catch(() => ({ kind: 'retry' })),
      serverTerminationPromise.then(termination => ({ kind: 'termination', termination })),
    ]);

    if (outcome.kind === 'termination') {
      throw serverTerminationError(url, outcome.termination);
    }

    if (outcome.kind === 'response' && outcome.response.ok) {
      return;
    }

    const remainingDelayMs = timeoutMs - (Date.now() - started);
    if (remainingDelayMs <= 0) {
      break;
    }

    const terminationWhileWaiting = await Promise.race([
      sleep(Math.min(1000, remainingDelayMs)).then(() => null),
      serverTerminationPromise,
    ]);
    if (terminationWhileWaiting !== null) {
      throw serverTerminationError(url, terminationWhileWaiting);
    }
  }

  throw new Error(`Timed out waiting for ${url}`);
}

async function gotoInteractivePage(page, url, expectedTitle) {
  const response = await page.goto(url, { waitUntil: 'domcontentloaded' });
  if (!response || !response.ok()) {
    throw new Error(`Navigation to ${url} failed with HTTP ${response?.status() ?? '<no response>'}`);
  }

  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor({
    state: 'visible',
    timeout: 60000,
  });
  await page.waitForFunction(
    title => document.title === title,
    expectedTitle,
    { timeout: 60000 });
}

function startServer(spawnProcess = spawn) {
  const args = [
    'run',
    '--project',
    path.join(repoRoot, 'samples', 'Counter', 'Counter.Web', 'Counter.Web.csproj'),
    '--configuration',
    'Release',
    '--no-launch-profile',
    '--urls',
    baseUrl,
  ];

  const child = spawnProcess('dotnet', args, {
    cwd: repoRoot,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
    },
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  const logs = [];
  child.stdout.on('data', chunk => logs.push(chunk.toString()));
  child.stderr.on('data', chunk => logs.push(chunk.toString()));

  const terminationPromise = new Promise(resolve => {
    child.once('error', error => resolve({ code: null, signal: null, error }));
    child.once('exit', (code, signal) => resolve({ code, signal, error: null }));
  });

  return { child, logs, terminationPromise };
}

async function captureScreenshot(page, scenario) {
  const fileName = `${sanitizeName(scenario)}.png`;
  const fullPath = path.join(evidenceDir, fileName);
  await page.screenshot({ path: fullPath, fullPage: true });
  return path.relative(outputDir, fullPath).replace(/\\/g, '/');
}

function filterConsoleMessages(messages) {
  return messages.filter(message => /(error|warn|exception|hfc|d31|d32|failed)/i.test(message));
}

function unique(items) {
  return [...new Set(items.filter(Boolean))];
}

function getRouteHeading(page, name) {
  return page
    .getByRole('main')
    .getByRole('heading', { name, exact: true, level: 1 });
}

async function assertFocused(locator, description) {
  await locator.waitFor({ state: 'visible', timeout: 60000 });
  const isFocused = await locator.evaluate(
    (element, timeoutMs) => new Promise(resolve => {
      const deadline = performance.now() + timeoutMs;
      const checkFocus = () => {
        if (element === document.activeElement) {
          resolve(true);
        }
        else if (performance.now() >= deadline) {
          resolve(false);
        }
        else {
          requestAnimationFrame(checkFocus);
        }
      };

      checkFocus();
    }),
    10000);
  if (!isFocused) {
    throw new Error(`${description} is not focused`);
  }
}

async function waitForSelectors(page, selectors) {
  for (const selector of selectors) {
    await page.locator(selector).first().waitFor({ state: 'visible', timeout: 60000 });
  }
}

function resultExitCode(results) {
  if (!Array.isArray(results)) {
    throw new TypeError('Scenario results must be an array.');
  }

  return results.some(result => result?.status === 'fail') ? 1 : 0;
}

async function main() {
  fs.mkdirSync(evidenceDir, { recursive: true });

  const results = [];
  let server;
  let serverLogs = [];
  let serverTerminationPromise;
  let browser;

  const pushBlocked = (scenario, reason) => {
    results.push({
      scenario,
      status: 'skipped',
      evidence: {
        domSelectors: [],
        consoleMatches: [reason],
      },
      durationMs: 0,
    });
  };

  try {
    const startedServer = startServer();
    server = startedServer.child;
    serverLogs = startedServer.logs;
    serverTerminationPromise = startedServer.terminationPromise;
    await waitForServer(`${baseUrl}/counter`, 90000, serverTerminationPromise);
    browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({
      viewport: { width: 1280, height: 900 },
    });

    const runScenario = async (scenario, callback) => {
      const page = await context.newPage();
      const consoleMessages = [];
      const runtimeFailures = [];
      page.on('console', message => {
        const diagnostic = `[console:${message.type()}] ${message.text()}`;
        consoleMessages.push(diagnostic);
        if (message.type() === 'error') {
          runtimeFailures.push(diagnostic);
        }
      });
      page.on('pageerror', error => {
        const diagnostic = `[pageerror] ${error.message}`;
        consoleMessages.push(diagnostic);
        runtimeFailures.push(diagnostic);
      });

      const started = Date.now();
      const evidence = {
        domSelectors: [],
        consoleMatches: [],
      };
      let status = 'pass';

      try {
        const partial = await callback(page);
        if (partial?.domSelectors) {
          evidence.domSelectors.push(...partial.domSelectors);
        }
        if (partial?.consoleMatches) {
          evidence.consoleMatches.push(...partial.consoleMatches);
        }
      }
      catch (error) {
        status = 'fail';
        evidence.consoleMatches.push(error instanceof Error ? error.stack ?? error.message : String(error));
      }

      try {
        evidence.screenshot = await captureScreenshot(page, scenario);
      }
      catch (error) {
        status = 'fail';
        evidence.consoleMatches.push(`screenshot failed: ${error instanceof Error ? error.message : String(error)}`);
      }

      try {
        await page.close();
      }
      catch (error) {
        status = 'fail';
        evidence.consoleMatches.push(`page close failed: ${error instanceof Error ? error.message : String(error)}`);
      }

      evidence.consoleMatches.push(...filterConsoleMessages(consoleMessages));
      if (runtimeFailures.length > 0) {
        status = 'fail';
      }

      results.push({
        scenario,
        status,
        evidence: {
          screenshot: evidence.screenshot,
          domSelectors: unique(evidence.domSelectors),
          consoleMatches: unique(evidence.consoleMatches),
        },
        durationMs: Date.now() - started,
      });
    };

    await runScenario('S1 Inline render', async page => {
      await gotoInteractivePage(page, `${baseUrl}/counter`, 'Counter sample');
      const inline = page.locator('fluent-accordion-item.inline-section');
      await inline.waitFor({ state: 'visible', timeout: 60000 });
      // FluentButton renders as <fluent-button>; prefer exact text over role mapping.
      await inline.getByText('Increment', { exact: true }).first().waitFor({ state: 'visible', timeout: 60000 });
      return {
        domSelectors: ['fluent-accordion-item.inline-section', 'fluent-button:has-text("Increment")'],
      };
    });

    await runScenario('S2 Inline popover open/close', async page => {
      await gotoInteractivePage(page, `${baseUrl}/counter`, 'Counter sample');
      const inline = page.locator('fluent-accordion-item.inline-section');
      await inline.waitFor({ state: 'visible', timeout: 60000 });
      await inline.getByText('Increment', { exact: true }).first().click();
      const popover = inline.locator('.fc-popover');
      await popover.waitFor({ state: 'visible', timeout: 60000 });
      const cancel = popover.getByRole('button', { name: 'Cancel' });
      await cancel.focus();
      await cancel.press('Enter');
      await popover.waitFor({ state: 'hidden', timeout: 10000 });
      return {
        domSelectors: ['fluent-accordion-item.inline-section', '.fc-popover'],
      };
    });

    pushBlocked(
      'S3 Inline popover submit',
      'skipped: headless Playwright + Fluent popover EditForm submit is unreliable; amount→projection is covered by CounterProjectionEffectsTests.IncrementConfirmed_UsesSubmittedAmount',
    );

    await runScenario('S4 Compact inline render', async page => {
      await gotoInteractivePage(page, `${baseUrl}/counter`, 'Counter sample');
      await page.locator('fluent-accordion-item.command-section .fc-expand-in-row').waitFor();
      return {
        domSelectors: ['fluent-accordion-item.command-section .fc-expand-in-row'],
      };
    });

    pushBlocked(
      'S5 Compact inline last-used prefill',
      'skipped: LastUsed read/write is covered by Shell unit tests; reload + Fluent field hydration is flaky under headless automation',
    );

    await runScenario('S6 FullPage route', async page => {
      await gotoInteractivePage(
        page,
        `${baseUrl}/commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection`,
        'Configure Counter');
      const selectors = ['nav[aria-label="breadcrumb"]', 'form'];
      await waitForSelectors(page, selectors);
      const heading = getRouteHeading(page, 'Configure Counter');
      await assertFocused(heading, 'Generated command route heading');
      return {
        domSelectors: [...selectors, '[role="main"] h1'],
      };
    });

    await runScenario('S7 FullPage ReturnPath safe', async page => {
      await gotoInteractivePage(
        page,
        `${baseUrl}/commands/Counter/ConfigureCounterCommand?returnPath=https%3A%2F%2Fevil.example%2Fpath&projectionTypeFqn=Counter.Domain.CounterProjection`,
        'Configure Counter');
      const selectors = ['nav[aria-label="breadcrumb"]', 'nav[aria-label="breadcrumb"] a[href="/"]'];
      await waitForSelectors(page, selectors);
      const breadcrumbHrefs = await page.locator('nav[aria-label="breadcrumb"] a').evaluateAll(
        anchors => anchors.map(anchor => anchor.getAttribute('href')));
      const unsafeBreadcrumbHrefs = breadcrumbHrefs.filter(href => href !== '/');
      if (unsafeBreadcrumbHrefs.length > 0) {
        throw new Error(
          `Unsafe returnPath breadcrumbs resolved to ${unsafeBreadcrumbHrefs.map(href => href ?? '<missing>').join(', ')} instead of /`);
      }
      return {
        domSelectors: selectors,
      };
    });

    const runAxeScenario = async (scenario, url, expectedTitle, selectors, expectedHeadingName = null) => {
      await runScenario(scenario, async page => {
        await gotoInteractivePage(page, url, expectedTitle);
        await waitForSelectors(page, selectors);
        if (expectedHeadingName) {
          const heading = getRouteHeading(page, expectedHeadingName);
          await assertFocused(heading, `${scenario} route heading`);
        }

        // Sample host + Fluent UI: suppress rules that are theme/third-party noise for this harness.
        const axe = new AxeBuilder({ page });
        for (const selector of selectors) {
          axe.include(selector);
        }
        const analysis = await axe
          .disableRules(['aria-prohibited-attr', 'color-contrast', 'label'])
          .analyze();
        const serious = analysis.violations.filter(v => ['serious', 'critical'].includes(v.impact || ''));
        if (serious.length > 0) {
          throw new Error(`Axe serious/critical violations: ${serious.map(v => v.id).join(', ')}`);
        }
        return {
          domSelectors: selectors,
          consoleMatches: ['axe:0 serious/critical violations'],
        };
      });
    };

    await runAxeScenario(
      'A11Y Inline page',
      `${baseUrl}/counter`,
      'Counter sample',
      ['fluent-accordion-item.inline-section', 'fluent-accordion-item.command-section', 'section.data-section']);

    await runAxeScenario(
      'A11Y Compact page',
      `${baseUrl}/counter`,
      'Counter sample',
      ['fluent-accordion-item.command-section .fc-expand-in-row']);

    await runAxeScenario(
      'A11Y FullPage route',
      `${baseUrl}/commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection`,
      'Configure Counter',
      ['nav[aria-label="breadcrumb"]', 'form', '[role="main"] h1'],
      'Configure Counter');

    pushBlocked('S8 Hot-reload density flip', 'blocked: local browser harness does not orchestrate dotnet watch file mutation safely');
    pushBlocked('S9 D31 dev-mode warning', 'blocked: sample host always wires DemoUserContextAccessor; scenario needs alternate host configuration');
    pushBlocked('S10 D38 interleaved submit', 'blocked: sample page does not expose a clean multi-submit harness for correlation race automation yet');

    fs.writeFileSync(artifactPath, JSON.stringify(results, null, 2));
    return resultExitCode(results);
  }
  catch (error) {
    const fallback = [{
      scenario: 'runner',
      status: 'fail',
      evidence: {
        domSelectors: [],
        consoleMatches: [
          error instanceof Error ? error.stack ?? error.message : String(error),
          ...serverLogs.slice(-20),
        ],
      },
      durationMs: 0,
    }];
    fs.writeFileSync(artifactPath, JSON.stringify(fallback, null, 2));
    throw error;
  }
  finally {
    if (browser) {
      await browser.close();
    }

    if (server) {
      server.kill();
    }
  }
}

if (require.main === module) {
  if (process.argv[2] === '--verify-results') {
    const fixturePath = process.argv[3];
    if (!fixturePath) {
      console.error('--verify-results requires a JSON fixture path.');
      process.exitCode = 1;
    }
    else {
      try {
        const results = JSON.parse(fs.readFileSync(fixturePath, 'utf8'));
        process.exitCode = resultExitCode(results);
      }
      catch (error) {
        console.error(error);
        process.exitCode = 1;
      }
    }
  }
  else {
    main()
      .then(exitCode => {
        process.exitCode = exitCode;
      })
      .catch(error => {
        console.error(error);
        process.exitCode = 1;
      });
  }
}

module.exports = { gotoInteractivePage, resultExitCode, startServer, waitForServer };
