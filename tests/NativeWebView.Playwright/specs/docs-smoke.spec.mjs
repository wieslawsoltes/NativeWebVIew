import { test, expect } from '@playwright/test';

test('docs home page renders key content', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveTitle(/NativeWebView/i);
  await expect(page.getByRole('heading', { name: 'NativeWebView' }).first()).toBeVisible();
  await expect(page.getByText('native-webview-first control stack for Avalonia', { exact: false })).toBeVisible();
});

test('quickstart and release docs are reachable', async ({ page }) => {
  await page.goto('/');

  await page.getByRole('link', { name: 'Quickstart' }).first().click();
  await expect(page).toHaveURL(/quickstart/);
  await expect(page.getByRole('heading', { name: 'Quickstart' })).toBeVisible();

  await page.getByRole('link', { name: 'CI and Release' }).first().click();
  await expect(page).toHaveURL(/ci-and-release/);
  await expect(page.getByRole('heading', { name: 'CI and Release' })).toBeVisible();
  await expect(page.locator('code', { hasText: '.github/workflows/release.yml' }).first()).toBeVisible();
});
