import test from 'node:test';
import assert from 'node:assert/strict';
import { applyFeatureCap, clusterGrid, shouldCluster, repPoint } from './geo.js';
import { parseDateBand, toBands, filterBands } from './timelineBands.js';
import { buildNeighborhood, paginateEdges, disputedGroups } from './graph.js';

test('map cap keeps first 500 features with refinement signal', () => {
  const features = Array.from({ length: 502 }, (_, index) => ({ id: `f${index}`, geometry: [{ latitude: index, longitude: index }] }));
  const capped = applyFeatureCap(features);
  assert.equal(capped.visible.length, 500);
  assert.equal(capped.capped, true);
  assert.equal(applyFeatureCap(features.slice(0, 10)).capped, false);
});

test('map clustering groups nearby points and skips geometry-less features', () => {
  const clusters = clusterGrid([
    { id: 'a', geometry: [{ latitude: 10.1, longitude: 20.1 }] },
    { id: 'b', geometry: [{ latitude: 10.2, longitude: 20.2 }] },
    { id: 'c', geometry: [{ latitude: 60, longitude: 60 }] },
    { id: 'd' },
  ], 4);
  assert.equal(clusters.length, 2);
  assert.equal(shouldCluster(61), true);
  assert.equal(shouldCluster(12), false);
  assert.equal(repPoint({}), null);
});

test('interval renders as band while approximate and traditional stay labeled', () => {
  const interval = parseDateBand('250–200 BCE', 'Documented');
  assert.equal(interval.isInterval, true);
  assert.equal(interval.lower, -250);
  assert.equal(interval.upper, -200);
  const approx = parseDateBand('c. 150 CE', 'Probable');
  assert.equal(approx.approximate, true);
  assert.equal(approx.isPoint, true);
  const traditional = parseDateBand('c. 480 BCE', 'Traditional');
  assert.equal(traditional.traditional, true);
  const unknown = parseDateBand('Date not recorded', 'Unknown');
  assert.equal(unknown.isUnknown, true);
});

test('timeline bands preserve titles and filter without losing list parity', () => {
  const bands = toBands([
    { id: 'e1', title: 'Council', displayDate: '400–300 BCE', certainty: 'Disputed', region: 'India', category: 'Council' },
    { id: 'e2', title: 'Pilgrimage', displayDate: '629–645 CE', certainty: 'Probable', region: 'Central Asia', category: 'Travel' },
  ]);
  assert.equal(bands.length, 2);
  assert.equal(filterBands(bands, { region: 'India' }).length, 1);
  assert.equal(filterBands(bands, { category: 'Travel' })[0].title, 'Pilgrimage');
});

test('graph keeps disputed parallel edges split and paginates large neighborhoods', () => {
  const center = 'entity-a';
  const relationships = [
    { id: 'r1', from: 'entity-a', to: 'entity-b', type: 'teacherOf', certainty: 'Probable' },
    { id: 'r2', from: 'entity-a', to: 'entity-b', type: 'teacherOf', certainty: 'Disputed' },
    ...Array.from({ length: 30 }, (_, index) => ({ id: `rx${index}`, from: 'entity-a', to: `entity-x${index}`, type: 'associated', certainty: 'Possible' })),
  ];
  const neighborhood = buildNeighborhood(center, relationships);
  assert.equal(neighborhood.edges.length, 32);
  assert.equal(disputedGroups(neighborhood.edges).length, 1);
  const page = paginateEdges(neighborhood.edges, { pageSize: 25 });
  assert.equal(page.visible.length, 25);
  assert.equal(page.hasMore, true);
  assert.equal(paginateEdges(neighborhood.edges, { pageSize: 25, showAll: true }).visible.length, 32);
});
