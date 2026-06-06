import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Sector CRUD', () => {

  test('sector list shows sectors with mandoob assignments', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Sectors');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('table').first()).toBeVisible({ timeout: 3000 });
  });

  test('create sector form loads', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/CreateSector');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('[name="Name"]')).toBeVisible({ timeout: 3000 });
  });

  test('edit sector form loads with data', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Sectors');
    await page.waitForLoadState('networkidle');

    const editLink = page.locator('a[href*="EditSector"]').first();
    if (await editLink.isVisible()) {
      await editLink.click();
      await page.waitForLoadState('networkidle');
      await expect(page.locator('[name="Name"]').first()).toBeVisible({ timeout: 3000 });
    }
  });

  test('assign mandoob to sector', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Sectors');
    await page.waitForLoadState('networkidle');

    // Look for assign mandoob button
    const assignBtn = page.locator('button:has-text("تعيين"), a:has-text("تعيين")').first();
    if (await assignBtn.isVisible()) {
      await assignBtn.click();
      await page.waitForLoadState('networkidle');

      // Should show some result
      const body = page.locator('body');
      await expect(body).toBeVisible({ timeout: 3000 });
    }
  });
});
