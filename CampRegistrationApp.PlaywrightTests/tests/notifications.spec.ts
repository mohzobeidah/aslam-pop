import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Notification System', () => {

  test('notification bell visible on admin pages', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Bell icon should exist in nav
    const bell = page.locator('.bell-icon, .notification-bell, [data-badge], .fa-bell').first();
    await expect(bell).toBeVisible({ timeout: 3000 });
  });

  test('notifications page loads', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Notifications');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });
});
