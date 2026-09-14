import test from 'node:test';
import assert from 'node:assert/strict';
import { parseRoute, buildRoute, detailRouteToApi } from './routes.js';
import { t, resolveLocale } from './i18n.js';
import { buildEvent, validateTelemetry } from './telemetry.js';
import { buildMeta } from './meta.js';
import { saveVisit, loadVisit, visitKey } from './offline.js';
import { TOUR_STEPS, nextTourStep } from './onboarding.js';

test('router keeps API detailRoute parity and flags unknown ids as notfound-safe', () => {
  assert.equal(buildRoute('Person', 'abc-123'), '/persons/abc-123');
  assert.equal(buildRoute('Place', 'bodh-gaya'), '/places/bodh-gaya');
  const parsed = parseRoute('/places/bodh-gaya');
  assert.equal(parsed.kind, 'entity');
  assert.equal(parsed.type, 'place');
  assert.equal(detailRouteToApi('/places/bodh-gaya'), 'places/bodh-gaya');
  assert.equal(parseRoute('/').kind, 'home');
  assert.equal(parseRoute('/timeline').kind, 'timeline');
  assert.equal(parseRoute('/nope/xyz').kind, 'notfound');
});

test('i18n falls back to English and never blanks', () => {
  assert.equal(resolveLocale('zh-TW'), 'zh');
  assert.equal(resolveLocale('xx'), 'en');
  assert.ok(t('en', 'search.label').length > 0);
  assert.equal(t('zh', 'detail.sources'), t('en', 'detail.sources'));
  assert.equal(t('ja', 'detail.sources'), t('en', 'detail.sources'));
  assert.ok(t('zh', 'app.tagline').length > 0);
  assert.equal(t('en', 'missing.key.stays-key'), 'missing.key.stays-key');
});

test('telemetry accepts anonymous counters and rejects PII', () => {
  const ok = buildEvent('page_view_by_type', { entityType: 'Place' });
  assert.equal(validateTelemetry(ok).length, 0);
  const noHit = buildEvent('search_no_hit', {});
  assert.equal(validateTelemetry(noHit).length, 0);
  assert.ok(validateTelemetry({ kind: 'page_view_by_type', email: 'a@b.com' }).length > 0);
  assert.ok(validateTelemetry({ kind: 'page_view_by_type', query: 'secret' }).length > 0);
  assert.ok(validateTelemetry({ kind: 'page_view_by_type', claimText: 'long statement' }).length > 0);
  assert.ok(validateTelemetry({ kind: 'tracking_cookie', entityType: 'Place' }).length > 0);
});

test('entity meta exposes title, description, and canonical without drafts', () => {
  const meta = buildMeta({ canonicalName: 'Ashoka', type: 'Person', certainty: 'Documented', summary: 'Mauryan emperor.' }, '/persons/ashoka', 'https://atlas.example');
  assert.ok(meta.title.includes('Ashoka'));
  assert.ok(meta.description.includes('Mauryan'));
  assert.equal(meta.canonical, 'https://atlas.example/persons/ashoka');
});

test('visited reads cache with bounded TTL and stale signal', () => {
  const mem = new Map();
  globalThis.localStorage = {
    getItem: (k) => (mem.has(k) ? mem.get(k) : null),
    setItem: (k, v) => { mem.set(k, String(v)); },
    removeItem: (k) => { mem.delete(k); },
    key: (i) => [...mem.keys()][i] ?? null,
    get length() { return mem.size; },
  };
  assert.ok(visitKey('/places/x').includes('/places/x'));
  assert.equal(loadVisit('/places/x', 1000).status, 'miss');
  assert.equal(saveVisit('/places/x', { id: 'x' }, 1000), true);
  const hit = loadVisit('/places/x', 2000);
  assert.equal(hit.status, 'stale-ok');
  assert.equal(hit.data.id, 'x');
  assert.equal(loadVisit('/places/x', 1000 + 8 * 24 * 60 * 60 * 1000).status, 'expired');
  delete globalThis.localStorage;
});

test('onboarding tour advances and terminates', () => {
  assert.equal(TOUR_STEPS.length, 3);
  assert.equal(nextTourStep(null), 0);
  assert.equal(nextTourStep(0), 1);
  assert.equal(nextTourStep(2), null);
});
