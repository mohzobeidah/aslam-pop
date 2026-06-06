import { test, expect } from '@playwright/test';
import { completeRegistration } from '../helpers/test-utils';
import { Btn, Modal, Msg } from '../helpers/selectors';

test.describe('Admin Rejection', () => {

  test('reject modal opens when clicking reject', async ({ page }) => {
    await completeRegistration(page);

    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    const rejectBtn = page.locator('button:has-text("رفض")').first();
    await expect(rejectBtn).toBeVisible({ timeout: 5000 });
    await rejectBtn.click();

    // Modal should appear
    await expect(page.locator(Modal.rejectModal)).toBeVisible({ timeout: 3000 });
  });

  test('rejection with valid reason changes status to rejected', async ({ page }) => {
    await completeRegistration(page);

    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    await page.locator('button:has-text("رفض")').first().click();
    await page.waitForSelector(Modal.rejectModal, { state: 'visible' });
    await page.fill(Modal.rejectReasonInput, 'مستندات غير مكتملة');
    await page.click(Modal.rejectConfirmButton || '#rejectModal button[type="submit"]');
    await page.waitForLoadState('networkidle');

    // Filter by Rejected
    await page.locator('#status').selectOption('Rejected');
    await page.click('button[type="submit"], .btn-filter');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('table tbody tr, .registration-row').first()).toBeVisible({ timeout: 3000 });
  });

  test('empty reason keeps modal open', async ({ page }) => {
    await completeRegistration(page);

    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    await page.locator('button:has-text("رفض")').first().click();
    await page.waitForSelector(Modal.rejectModal, { state: 'visible' });
    // Submit with empty reason
    await page.click('#rejectModal button[type="submit"]');
    await page.waitForLoadState('networkidle');

    // The page reloads with TempData error; registration stays Pending
    await expect(page.locator('.alert-danger, .bg-red')).toBeVisible({ timeout: 3000 });
  });
});
