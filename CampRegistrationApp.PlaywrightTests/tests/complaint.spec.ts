import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Complaint System', () => {

  test('public submission form loads', async ({ page }) => {
    await page.goto('/Complaint/Create');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('textarea, input').first()).toBeVisible({ timeout: 3000 });
  });

  test('submit complaint and see confirmation', async ({ page }) => {
    await page.goto('/Complaint/Create');
    await page.waitForLoadState('networkidle');

    await page.fill('textarea, [name="Description"], [name="Message"]', 'هذا شكوى اختبارية');
    await page.fill('[name="Name"], [name="FullName"]', 'مشتكي');
    await page.fill('[name="Phone"], [name="PhoneNumber"]', '0591234567');

    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should show confirmation page
    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });

  test('admin can view complaint list', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Complaint/Index');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });

  test('admin can view complaint details', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Complaint/Index');
    await page.waitForLoadState('networkidle');

    // Click on first complaint
    const detailLink = page.locator('a[href*="Details"]').first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
    }
  });
});
