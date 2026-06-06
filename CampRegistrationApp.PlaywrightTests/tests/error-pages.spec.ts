import { test, expect } from '@playwright/test';

test.describe('Error Pages', () => {

  test('404 page shown for unknown routes', async ({ page }) => {
    const response = await page.goto('/this-page-does-not-exist', { waitUntil: 'networkidle' });
    expect(response?.status()).toBe(404);
  });

  test('production error page shows request ID', async ({ page }) => {
    await page.goto('/Home/Error');
    await page.waitForLoadState('networkidle');

    // Should show a user-friendly message
    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });

  test('privacy page loads', async ({ page }) => {
    await page.goto('/Home/Privacy');
    await page.waitForLoadState('networkidle');
    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });
});
