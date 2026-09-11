import test from 'node:test';
import assert from 'node:assert/strict';
import { apiUrl, displayDate, uncertaintyLabel } from './formatters.js';

test('preserves explicit uncertainty vocabulary', () => {
  assert.equal(uncertaintyLabel('Traditional'), 'traditional account');
  assert.equal(uncertaintyLabel('Disputed'), 'disputed');
  assert.equal(uncertaintyLabel('Unknown'), 'date or evidence unknown');
});

test('does not replace interval or unknown dates with precision', () => {
  assert.equal(displayDate({ displayExpression: 'c. 250–200 BCE' }), 'c. 250–200 BCE');
  assert.equal(displayDate(null), 'Date not recorded');
});

test('builds encoded API query URLs', () => {
  assert.equal(apiUrl('search', { q: 'Nālandā', limit: 8 }), '/api/v1/search?q=N%C4%81land%C4%81&limit=8');
  assert.equal(apiUrl('map', { year: 500, region: '' }), '/api/v1/map?year=500');
});
