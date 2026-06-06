import { test, expect } from '@playwright/test';
import { completeRegistration, loginAsRefugee } from '../helpers/test-utils';
import { Btn, Msg } from '../helpers/selectors';

test.describe('Refugee Edit', () => {

  test('edit page loads with previous data', async ({ page }) => {
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
    const approveBtn = page.locator('button:has-text("موافقة")').first();
    if (await approveBtn.isVisible()) {
      await approveBtn.click();
      await page.waitForLoadState('networkidle');
    }

    // Search for this registration
    await page.goto('/Record/Search');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="recordId"]', recordId || '');

    // Get head ID from details
    await page.goto('/Admin/RefugeeDetails?recordId=' + recordId);
    await page.waitForLoadState('networkidle');
    const headId = await page.locator('text=/رقم الهوية/').locator('..').locator('strong, span').last().textContent();

    // Login as refugee
    await page.goto('/Record/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="idNumber"]', headId?.trim() || '');
    await page.fill('input[name="password"]', 'test1234');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Edit page loaded with data
    await expect(page.locator('[name="Head.FirstName"]')).toHaveValue(/./);
  });
});
