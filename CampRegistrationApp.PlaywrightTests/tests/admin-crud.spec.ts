import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Admin CRUD (Super Admin)', () => {

  test('admin list page loads', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Index');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('table').first()).toBeVisible({ timeout: 3000 });
  });

  test('create admin form loads with sector dropdown', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Create');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('[name="NationalId"]')).toBeVisible({ timeout: 3000 });
    await expect(page.locator('#sectorId, [name="SectorId"]')).toBeVisible();
  });

  test('edit admin form loads existing data', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Index');
    await page.waitForLoadState('networkidle');

    const editLink = page.locator('a[href*="Edit"]').first();
    if (await editLink.isVisible()) {
      await editLink.click();
      await page.waitForLoadState('networkidle');
      await expect(page.locator('[name="Name"], [name="NationalId"]').first()).toBeVisible({ timeout: 3000 });
    }
  });
});
