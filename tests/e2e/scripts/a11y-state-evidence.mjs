import { basename } from 'node:path';
import { readFileSync } from 'node:fs';

const outcomes = ['validation', 'returned-rejection', 'exception-rejection', 'forbidden', 'unavailable', 'blank-unavailable', 'rate-limited', 'blank-rate-limited', 'transport', 'admission', 'policy'];
const lifecycle = ['idle', 'submitting', 'acknowledged', 'syncing', 'confirmed', 'rejected', 'idempotent-confirmed', 'needs-review', 'warning', 'degraded', 'degraded-exhausted'];
export const requiredStates = ['light', 'dark'].flatMap(theme => [
  ...outcomes.map(outcome => `zero-${outcome}-${theme}`),
  `zero-accepted-${theme}`,
  ...lifecycle.map(state => `lifecycle-${state}-${theme}`),
  ...['generated-missing-external', 'generated-missing-lost-focus', 'projection-two', 'projection-loading', 'projection-empty', 'projection-error', 'home', 'navigation-tabs', 'toolbar', 'generated-forms', 'filter-popover', 'view-menu', 'tabs-active', 'palette-default', 'palette-selected', 'palette-empty', 'settings', 'generated-validation', 'policy-denied', 'destructive-dialog', 'abandonment', 'connection-reconnecting', 'connection-disconnected', 'connection-connected', 'stale-reconnecting', 'stale-disconnected', 'slow-query', 'result-limit', 'filter-empty', 'filter-hidden-detail', 'fresh-presentation'].map(state => `${state}-${theme}`),
]);

export function validateStateEvidence(files, { unsupportedForcedColorsCapability = null } = {}) {
  const errors = [];
  for (const state of requiredStates) {
    const candidates = files.filter(file => basename(file) === `ae-state-${state}.json`);
    if (candidates.length === 0) { errors.push(`Missing state evidence: ${state}`); continue; }
    for (const file of candidates) {
      let evidence;
      try { evidence = JSON.parse(readFileSync(file, 'utf8')); } catch { errors.push(`Invalid JSON: ${file}`); continue; }
      if (evidence.theme !== (state.endsWith('-dark') ? 'Dark' : 'Light') || !Array.isArray(evidence.themePaint?.rgb) || evidence.themePaint.rgb.length !== 3 || (evidence.themePaint.rgb.reduce((sum, channel) => sum + channel, 0) < 384) !== state.endsWith('-dark')) errors.push(`Mismatched actual theme paint: ${file}`);
      if (evidence.state !== state) errors.push(`Mismatched state identity: ${file}`);
      const mediaOnly = evidence.engine === 'webkit' && unsupportedForcedColorsCapability?.engine === 'webkit' && unsupportedForcedColorsCapability.authoredColorReplacement === false && unsupportedForcedColorsCapability.forced?.forcedColors === true && evidence.forcedColorsPaint === false && evidence.forcedColorsStatus === 'unsupported authored-color replacement; media rules only';
      const keys = mediaOnly ? ['text'] : ['text', 'systemText'];
      if (!mediaOnly && evidence.forcedColorsPaint !== true) errors.push(`Missing actual forced palette paint: ${file}`);
      for (const key of keys) {
        if (!Array.isArray(evidence[key]) || evidence[key].length === 0) errors.push(`Empty ${key} samples: ${file}`);
        else if (evidence[key].some(sample => !Number.isFinite(sample.ratio) || ![3, 4.5].includes(sample.minimum) || sample.ratio < sample.minimum || !sample.paint?.trim())) errors.push(`Failed ${key} contrast: ${file}`);
      }
      if (evidence.forcedColors !== true || evidence.reducedMotion !== true) errors.push(`Missing actual media-mode evidence: ${file}`);
      if (!Array.isArray(evidence.targets) || evidence.targets.length === 0 || evidence.targets.some(target => target.failure)) errors.push(`Invalid target inventory: ${file}`);
      if (!Array.isArray(evidence.boundaries) || evidence.boundaries.some(group => !Array.isArray(group) || group.length === 0 || !group.some(sample => Number.isFinite(sample.ratio) && sample.ratio >= 3))) errors.push(`Invalid boundary evidence: ${file}`);
      const needsIcon = !state.startsWith('zero-accepted-') && /^(zero-|lifecycle-(acknowledged|confirmed|rejected|idempotent-confirmed|needs-review|warning|degraded|degraded-exhausted)-|connection-|generated-forms-|settings-|slow-query-|result-limit-)/u.test(state);
      if (needsIcon && !evidence.icon?.length) errors.push(`Empty required icon samples: ${file}`);
      if (/^(generated-forms-|toolbar-|palette-|policy-denied-)/u.test(state) && !evidence.boundaries?.length) errors.push(`Empty required control-boundary samples: ${file}`);
      if (/^(generated-validation-|fresh-presentation-|generated-missing-lost-focus-|tabs-active-|palette-selected-|destructive-dialog-|abandonment-)/u.test(state) && (!Array.isArray(evidence.cues) || evidence.cues.length === 0 || evidence.cues.some(group => !Array.isArray(group) || !group.some(sample => Number.isFinite(sample.ratio) && sample.ratio >= 3)))) errors.push(`Missing meaningful state cue: ${file}`);
      if (!Array.isArray(evidence.icon) || evidence.icon.some(sample => !Number.isFinite(sample.ratio) || sample.ratio < 3)) errors.push(`Invalid icon evidence: ${file}`);
    }
  }
  return errors;
}
