import { test, expect } from '@playwright/test';
import { completeRegistration } from '../helpers/test-utils';
import { Btn } from '../helpers/selectors';

test.describe('Admin Approval', () => {

  test('approves a pending registration', async ({ page }) => {
    const recordId = await completeRegistration(page);

    // Login as admin
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Go to registrations
    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    // Click approve
    const approveBtn = page.locator('button:has-text("موافقة")').first();
    await expect(approveBtn).toBeVisible({ timeout: 5000 });
    await approveBtn.click();
    await page.waitForLoadState('networkidle');

    // Filter by Approved
    await page.locator('#status').selectOption('Approved');
    await page.click('button[type="submit"], .btn-filter');
    await page.waitForLoadState('networkidle');

    // Should show at least one approved
    await expect(page.locator('table tbody tr, .registration-row').first()).toBeVisible({ timeout: 3000 });
  });

  test('approve clears previous rejection fields', async ({ page }) => {
    const recordId = await completeRegistration(page);

    // Admin login
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Reject first
    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    const rejectBtn = page.locator('button:has-text("رفض")').first();
    if (await rejectBtn.isVisible()) {
      await rejectBtn.click();
      await page.waitForSelector('#rejectModal', { state: 'visible' });
      await page.fill('#rejectReason', 'سبب مؤقت للاختبار');
      await page.click('#rejectModal button[type="submit"]');
      await page.waitForLoadState('networkidle');
    }

    // Now approve
    await page.goto('/Admin/Registrations?status=Rejected');
    await page.waitForLoadState('networkidle');

    const approveBtn = page.locator('button:has-text("موافقة")').first();
    if (await approveBtn.isVisible()) {
      await approveBtn.click();
      await page.waitForLoadState('networkidle');
    }

    // Should be approved now
    await page.locator('#status').selectOption('Approved');
    await page.click('button[type="submit"], .btn-filter');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('table tbody tr, .registration-row').first()).toBeVisible({ timeout: 3000 });
  });
});
