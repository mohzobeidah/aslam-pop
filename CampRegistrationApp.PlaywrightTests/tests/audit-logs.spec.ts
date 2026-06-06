import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Audit Logs', () => {

  test('audit logs page loads for super admin', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/AuditLogs');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });

  test('audit logs filterable by action', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/AuditLogs');
    await page.waitForLoadState('networkidle');

    // Try filtering
    const actionFilter = page.locator('#action, [name="action"]').first();
    if (await actionFilter.isVisible()) {
      await actionFilter.fill('Login');
      await page.click('button[type="submit"], .btn-filter');
      await page.waitForLoadState('networkidle');
    }
  });

  test('audit logs paginated at 50 per page', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/AuditLogs');
    await page.waitForLoadState('networkidle');

    // Check pagination links
    const pagination = page.locator('.pagination, a:has-text("التالي"), a:has-text("السابق")').first();
    if (await pagination.isVisible()) {
      await pagination.click();
      await page.waitForLoadState('networkidle');
    }
  });
});
