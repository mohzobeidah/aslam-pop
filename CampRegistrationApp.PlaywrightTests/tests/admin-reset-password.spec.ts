import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Admin Reset Password', () => {

  test('reset password returns new password', async ({ page }) => {
    // Login
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Go to refugee details of a registration
    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');

    // Click reset password on first row
    const resetBtn = page.locator('button:has-text("إعادة تعيين"), a:has-text("إعادة تعيين")').first();
    if (await resetBtn.isVisible()) {
      const [response] = await Promise.all([
        page.waitForResponse(resp => resp.url().includes('ResetPassword')),
        resetBtn.click(),
      ]);
      const data = await response.json();
      expect(data.password).toBeTruthy();
      expect(data.password.length).toBe(4);
    }
  });
});
