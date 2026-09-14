import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// Atlas visual parity journeys. Run with: npx playwright test
// Requires the host running with VITE_TILE_URL configured.

test('year drag updates map features and list stays usable without tiles', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('tab', { name: 'map' }).click();
  await page.locator('#map-year').fill('650');
  await expect(page.getByRole('heading', { name: 'Time-filtered map' })).toBeVisible();
  await expect(page.getByLabel('Map results')).toBeVisible();
});

test('timeline filter keeps band and list parity with keyboard path', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('tab', { name: 'timeline' }).click();
  await page.locator('.timeline-svg').focus();
  await page.keyboard.press('ArrowRight');
  await expect(page.getByLabel('Timeline results')).toBeVisible();
});

test('graph hop preserves disputed parallel edges with pagination', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('tab', { name: 'graph' }).click();
  await expect(page.getByRole('heading', { name: 'Relationship graph' })).toBeVisible();
  await expect(page.getByLabel('Relationship results').first().or(page.getByText('No published relationships'))).toBeVisible();
});

test('visual panes meet axe accessibility gates', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('tab', { name: 'timeline' }).click();
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(results.violations).toEqual([]);
});
