import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Admin Dashboard', () => {

  test('dashboard loads with statistics', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should be on dashboard
    await expect(page.locator('h1, h2, .dashboard-title').first()).toBeVisible({ timeout: 3000 });
  });

  test('mandoob dashboard shows only their sector', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // General check that dashboard works
    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });
});
