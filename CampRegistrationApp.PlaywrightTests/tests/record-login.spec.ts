import { test, expect } from '@playwright/test';
import { completeRegistration, loginAsRefugee } from '../helpers/test-utils';
import { Btn, Msg } from '../helpers/selectors';

test.describe('Refugee Login', () => {

  test('approved registration can login and see edit page', async ({ page, request }) => {
    // Register a family
    const recordId = await completeRegistration(page);

    // Approve via API (need admin session)
    // First login as admin
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Navigate to registrations and approve
    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    // Click approve
    const approveBtn = page.locator('button:has-text("موافقة")').first();
    if (await approveBtn.isVisible()) {
      await approveBtn.click();
      await page.waitForLoadState('networkidle');
    }

    // Now login as refugee with the record's head ID
    await page.goto('/Record/Login');
    await page.waitForLoadState('networkidle');

    // We need the head ID - navigate to registration details
    await page.goto('/Admin/RefugeeDetails?recordId=' + recordId);
    await page.waitForLoadState('networkidle');

    // Extract the head ID from the details page
    const headIdEl = page.locator('text=رقم الهوية').first().locator('..');
    const headId = await headIdEl.locator('strong, span').last().textContent();

    // Login as refugee
    await page.fill('input[name="idNumber"]', headId?.trim() || '');
    await page.fill('input[name="password"]', 'test1234');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should redirect to edit page
    await expect(page.locator('[name="Head.FirstName"]')).toBeVisible({ timeout: 5000 });
  });

  test('wrong password shows error message', async ({ page }) => {
    await page.goto('/Record/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="idNumber"]', '999999999');
    await page.fill('input[name="password"]', 'wrongpass');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('empty fields stay on login page', async ({ page }) => {
    await page.goto('/Record/Login');
    await page.waitForLoadState('networkidle');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should stay on login page
    await expect(page.locator('input[name="idNumber"]')).toBeVisible();
  });

  test('rejected registration shows rejection message', async ({ page }) => {
    const recordId = await completeRegistration(page);

    // Login as admin and reject
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    // Click reject
    const rejectBtn = page.locator('button:has-text("رفض")').first();
    if (await rejectBtn.isVisible()) {
      await rejectBtn.click();
      await page.waitForSelector('#rejectModal', { state: 'visible' });
      await page.fill('#rejectReason', 'اختبار رفض');
      await page.click('#rejectModal button[type="submit"]');
      await page.waitForLoadState('networkidle');
    }

    // Now try logging in as refugee
    await page.goto('/Admin/RefugeeDetails?recordId=' + recordId);
    await page.waitForLoadState('networkidle');
    const headIdEl = page.locator('text=رقم الهوية').first().locator('..');
    const headId = await headIdEl.locator('strong, span').last().textContent();

    await page.goto('/Record/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="idNumber"]', headId?.trim() || '');
    await page.fill('input[name="password"]', 'test1234');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should show rejection error
    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });
});
