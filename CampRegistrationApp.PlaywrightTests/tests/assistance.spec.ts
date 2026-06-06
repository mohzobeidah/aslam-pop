import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Assistance System', () => {

  test('assistance list loads', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Assistance/Index');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });

  test('create assistance form loads', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Assistance/Create');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('[name="Name"]').first()).toBeVisible({ timeout: 3000 });
  });

  test('create assistance submits successfully', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Assistance/Create');
    await page.waitForLoadState('networkidle');

    await page.fill('[name="Name"]', 'مساعدات الشتاء');
    await page.locator('[name="Type"]').selectOption('1');
    await page.locator('[name="Source"]').selectOption('1');
    await page.fill('[name="Date"]', '2026-01-15');
    await page.locator('[name="SectorId"]').selectOption('1');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should redirect to index
    await expect(page).toHaveURL(/\/Assistance\/Index|\/Assistance\/Details/);
  });

  test('search persons for beneficiary add', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // First create an assistance
    await page.goto('/Assistance/Create');
    await page.waitForLoadState('networkidle');
    await page.fill('[name="Name"]', 'مساعدات طبية');
    await page.locator('[name="Type"]').selectOption('1');
    await page.locator('[name="Source"]').selectOption('1');
    await page.fill('[name="Date"]', '2026-02-01');
    await page.locator('[name="SectorId"]').selectOption('1');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Navigate to details page (should have id in URL)
    const currentUrl = page.url();
    expect(currentUrl).toContain('/Assistance/Details');

    // Check that search form exists
    const searchInput = page.locator('input[type="search"], input[name="search"]').first();
    if (await searchInput.isVisible()) {
      await searchInput.fill('admin');
      await page.keyboard.press('Enter');
      await page.waitForLoadState('networkidle');
    }
  });

  test('import page loads', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Assistance/Import');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });

  test('download import template', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    const response = await page.request.get('/Assistance/DownloadTemplate');
    expect(response.ok()).toBe(true);
    expect(response.headers()['content-type']).toContain('spreadsheet');
  });
});
