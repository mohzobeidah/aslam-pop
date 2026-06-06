import { test, expect } from '@playwright/test';
import { completeRegistration } from '../helpers/test-utils';
import { Btn } from '../helpers/selectors';

test.describe('Admin Refugee Details', () => {

  test('details page shows registration info', async ({ page }) => {
    const recordId = await completeRegistration(page);

    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto(`/Admin/RefugeeDetails?recordId=${recordId}`);
    await page.waitForLoadState('networkidle');

    // Should show record ID
    await expect(page.locator(`text=${recordId}`).first()).toBeVisible({ timeout: 3000 });
  });

  test('details page shows rejection info when rejected', async ({ page }) => {
    const recordId = await completeRegistration(page);

    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Reject
    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');
    const rejectBtn = page.locator('button:has-text("رفض")').first();
    if (await rejectBtn.isVisible()) {
      await rejectBtn.click();
      await page.waitForSelector('#rejectModal', { state: 'visible' });
      await page.fill('#rejectReason', 'سبب الرفض');
      await page.click('#rejectModal button[type="submit"]');
      await page.waitForLoadState('networkidle');
    }

    // View details
    await page.goto(`/Admin/RefugeeDetails?recordId=${recordId}`);
    await page.waitForLoadState('networkidle');

    // Should show rejection info
    await expect(page.locator('text=سبب الرفض').first()).toBeVisible({ timeout: 3000 });
  });

  test('details page has edit button for pending registration', async ({ page }) => {
    const recordId = await completeRegistration(page);

    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto(`/Admin/RefugeeDetails?recordId=${recordId}`);
    await page.waitForLoadState('networkidle');

    // Edit button should exist
    const editLink = page.locator('a[href*="AdminEditRegistration"]').first();
    await expect(editLink).toBeVisible({ timeout: 3000 });
  });
});
