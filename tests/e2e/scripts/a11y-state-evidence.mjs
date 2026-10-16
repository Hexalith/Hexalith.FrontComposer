import { basename, dirname, join, resolve } from 'node:path';
import { readFileSync, statSync } from 'node:fs';

const outcomes = ['validation', 'returned-rejection', 'exception-rejection', 'forbidden', 'unavailable', 'blank-unavailable', 'rate-limited', 'blank-rate-limited', 'transport', 'admission', 'policy'];
const lifecycle = ['idle', 'submitting', 'acknowledged', 'syncing', 'confirmed', 'rejected', 'idempotent-confirmed', 'needs-review', 'warning', 'degraded', 'degraded-exhausted'];
export const requiredStates = ['light', 'dark'].flatMap(theme => [
  ...outcomes.map(outcome => `zero-${outcome}-${theme}`),
  `zero-accepted-${theme}`,
  ...['shell-chrome', 'theme-menu', 'rail-labeled', 'rail-icons'].map(state => `${state}-${theme}`),
  ...lifecycle.map(state => `lifecycle-${state}-${theme}`),
  ...['generated-missing-external', 'generated-missing-lost-focus', 'projection-two', 'projection-loading', 'projection-empty', 'projection-error', 'home', 'navigation-tabs', 'toolbar', 'generated-forms', 'filter-popover', 'view-menu', 'tabs-active', 'palette-default', 'palette-selected', 'palette-empty', 'settings', 'generated-validation', 'policy-denied', 'destructive-dialog', 'abandonment', 'connection-reconnecting', 'connection-disconnected', 'connection-connected', 'stale-reconnecting', 'stale-disconnected', 'slow-query', 'result-limit', 'filter-empty', 'filter-hidden-detail', 'fresh-presentation'].map(state => `${state}-${theme}`),
]);

export function validateStateEvidence(files, { unsupportedForcedColorsCapability = null, expectedEngine = null, expectedRun = null } = {}) {
  const errors = [];
  const validEdges = group => Array.isArray(group) && group.length > 0
    && group.every(sample => Number.isInteger(sample.owner) && sample.owner >= 0 && Number.isFinite(sample.ratio) && sample.minimum === 3)
    && [...new Set(group.map(sample => sample.owner))].every(owner => group.some(sample => sample.owner === owner && sample.ratio >= 3));
  const groups = new Map();
  for (const file of files.filter(file => /^ae-state-.+\.json$/u.test(basename(file)))) {
    let evidence;
    try { evidence = JSON.parse(readFileSync(file, 'utf8')); } catch { errors.push(`Invalid JSON: ${file}`); continue; }
    if (!['chromium','firefox','webkit'].includes(evidence.engine) || typeof evidence.runId !== 'string' || !evidence.runId.trim()) { errors.push(`Missing engine/run ownership: ${file}`); continue; }
    if (expectedEngine && evidence.engine !== expectedEngine || expectedRun && evidence.runId !== expectedRun) { errors.push(`Unexpected engine/run ownership: ${file}`); continue; }
    const key = JSON.stringify([evidence.engine, evidence.runId]);
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push({ file, evidence });
  }
  if (!groups.size) errors.push('Missing state evidence: no owned engine/run matrix');
  for (const [owner, rows] of groups) for (const state of requiredStates) {
    const candidates = rows.filter(({ file, evidence }) => basename(file) === `ae-state-${state}.json` && evidence.state === state);
    if (candidates.length !== 1) { errors.push(`${candidates.length ? 'Duplicate' : 'Missing'} state evidence: ${state} in ${owner}`); continue; }
    for (const { file, evidence } of candidates) {
      if (evidence.theme !== (state.endsWith('-dark') ? 'Dark' : 'Light') || !Array.isArray(evidence.themePaint?.rgb) || evidence.themePaint.rgb.length !== 3 || (evidence.themePaint.rgb.reduce((sum, channel) => sum + channel, 0) < 384) !== state.endsWith('-dark')) errors.push(`Mismatched actual theme paint: ${file}`);
      if (evidence.state !== state) errors.push(`Mismatched state identity: ${file}`);
      if (evidence.viewport?.width !== (state.startsWith('rail-') ? 1600 : 320) || evidence.viewport?.height !== 900) errors.push(`Missing exact viewport evidence: ${file}`);
      const mediaOnly = evidence.engine === 'webkit' && unsupportedForcedColorsCapability?.engine === 'webkit' && unsupportedForcedColorsCapability.authoredColorReplacement === false && unsupportedForcedColorsCapability.forced?.forcedColors === true && evidence.forcedColorsPaint === false && evidence.forcedColorsStatus === 'unsupported authored-color replacement; media rules only';
      const keys = mediaOnly ? ['text'] : ['text', 'systemText'];
      if (!mediaOnly && evidence.forcedColorsPaint !== true) errors.push(`Missing actual forced palette paint: ${file}`);
      for (const key of keys) {
        if (!Array.isArray(evidence[key]) || evidence[key].length === 0) errors.push(`Empty ${key} samples: ${file}`);
        else if (evidence[key].some(sample => !Number.isFinite(sample.ratio) || ![3, 4.5].includes(sample.minimum) || sample.ratio < sample.minimum || !sample.paint?.trim())) errors.push(`Failed ${key} contrast: ${file}`);
      }
      if (evidence.forcedColors !== true || evidence.reducedMotion !== true) errors.push(`Missing actual media-mode evidence: ${file}`);
      if (!Array.isArray(evidence.targets) || evidence.targets.length === 0 || evidence.targets.some(target => !target || typeof target !== 'object' || target.failure !== null
        || typeof target.target !== 'string' || !target.target.trim()
        || !Number.isFinite(target.width) || !Number.isFinite(target.height) || target.width <= 0 || target.height <= 0
        || target.conforming !== (target.width >= 24 && target.height >= 24)
        || !target.conforming && (!target.exception || !['Inline','Spacing','Equivalent','User Agent Control','Essential'].includes(target.exception.kind)
          || ['selector','rationale','keyboardPath','evidence'].some(key => typeof target.exception[key] !== 'string' || !target.exception[key].trim())
          || target.exception.kind === 'Equivalent' && (typeof target.exception.equivalentSelector !== 'string' || !target.exception.equivalentSelector.trim())))) errors.push(`Invalid target inventory: ${file}`);
      if (!Array.isArray(evidence.boundaries) || evidence.boundaries.some(group => !validEdges(group))) errors.push(`Invalid boundary evidence: ${file}`);
      const needsIcon = !state.startsWith('zero-accepted-') && /^(zero-|shell-chrome-|rail-|lifecycle-(acknowledged|confirmed|rejected|idempotent-confirmed|needs-review|warning|degraded|degraded-exhausted)-|connection-|generated-forms-|settings-|slow-query-|result-limit-)/u.test(state);
      if (needsIcon && !evidence.icon?.length) errors.push(`Empty required icon samples: ${file}`);
      if (/^(generated-forms-|toolbar-|palette-|policy-denied-)/u.test(state) && !evidence.boundaries?.length) errors.push(`Empty required control-boundary samples: ${file}`);
      if (/^(settings-|theme-menu-|rail-|generated-validation-|fresh-presentation-|generated-missing-lost-focus-|tabs-active-|palette-selected-|destructive-dialog-|abandonment-)/u.test(state) && (!Array.isArray(evidence.cues) || evidence.cues.length === 0 || evidence.cues.some(group => !validEdges(group)))) errors.push(`Missing meaningful state cue: ${file}`);
      if (!Array.isArray(evidence.icon) || evidence.icon.some(sample => !Number.isFinite(sample.ratio) || sample.ratio < 3)) errors.push(`Invalid icon evidence: ${file}`);
      const focus = evidence.systemFocus;
      const needsFocus = /^(settings-|theme-menu-|generated-validation-|generated-missing-lost-focus-|tabs-active-|destructive-dialog-|abandonment-)/u.test(state);
      if (!focus || focus.mode !== 'forced-colors' || focus.status === 'none' && (needsFocus || !['BODY','HTML'].includes(focus.target))
        || focus.status === 'measured' && (typeof focus.target !== 'string' || !focus.target.trim() || typeof focus.requireIndicator !== 'boolean'
          || !Array.isArray(focus.failures) || focus.failures.length || !focus.box || ['left','right','top','bottom'].some(key => !Number.isFinite(focus.box[key])))
        || !['none','measured'].includes(focus.status)) errors.push(`Invalid forced-color focus geometry: ${file}`);
      if (!mediaOnly) {
        if (!Array.isArray(evidence.systemIcon) || evidence.systemIcon.length !== evidence.icon?.length || evidence.systemIcon.some(sample => !Number.isFinite(sample.ratio) || sample.ratio < 3)) errors.push(`Invalid system icon evidence: ${file}`);
        if (!Array.isArray(evidence.systemBoundaries) || evidence.systemBoundaries.length !== evidence.boundaries?.length || evidence.systemBoundaries.some(group => !validEdges(group))) errors.push(`Invalid system boundary evidence: ${file}`);
        if (!Array.isArray(evidence.systemCues) || evidence.systemCues.length !== evidence.cues?.length || evidence.systemCues.some(group => !validEdges(group))) errors.push(`Invalid system state cue: ${file}`);
        const snapshot = files.find(path => resolve(path) === resolve(join(dirname(file), evidence.systemSnapshot ?? '')));
        if (evidence.systemSnapshot !== `system-state-${state}.png` || !snapshot || statSync(snapshot).size === 0) errors.push(`Missing system-state snapshot: ${file}`);
      }
    }
  }
  return errors;
}
