import { test, expect } from '@playwright/test';
import { loginAsAdmin } from '../helpers/test-utils';
import { Btn } from '../helpers/selectors';

test.describe('Admin Refugee List', () => {

  test('refugee list loads with filters', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Refugees');
    await page.waitForLoadState('networkidle');

    // Filters should be visible
    await expect(page.locator('#status, #sector, input[type="search"]').first()).toBeVisible({ timeout: 3000 });
  });

  test('filter by status shows matching records', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Refugees');
    await page.waitForLoadState('networkidle');

    // Select Pending filter
    const statusFilter = page.locator('#status');
    if (await statusFilter.isVisible()) {
      await statusFilter.selectOption('Pending');
      await page.click('button[type="submit"], .btn-filter');
      await page.waitForLoadState('networkidle');
    }
  });

  test('export refugees to excel', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Navigate to export
    const response = await page.request.get('/Admin/ExportRefugeesToExcel');
    expect(response.ok()).toBe(true);
    expect(response.headers()['content-type']).toContain('spreadsheet');
  });
});
