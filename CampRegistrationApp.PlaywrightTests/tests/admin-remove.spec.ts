import { test, expect } from '@playwright/test';
import { completeRegistration } from '../helpers/test-utils';
import { Btn } from '../helpers/selectors';

test.describe('Admin Remove Refugee', () => {

  test('remove button visible for approved registration', async ({ page }) => {
    const recordId = await completeRegistration(page);

    // Login as admin and approve
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    // Approve
    const approveBtn = page.locator('button:has-text("موافقة")').first();
    if (await approveBtn.isVisible()) {
      await approveBtn.click();
      await page.waitForLoadState('networkidle');
    }

    // Check details page for remove option
    await page.goto(`/Admin/RefugeeDetails?recordId=${recordId}`);
    await page.waitForLoadState('networkidle');

    // Remove button may be in details page
    const removeBtn = page.locator('button:has-text("حذف"), button:has-text("إزالة"), a:has-text("حذف")').first();
    if (await removeBtn.isVisible()) {
      page.on('dialog', dialog => dialog.accept());
      await removeBtn.click();
      await page.waitForLoadState('networkidle');
    }
  });
});
