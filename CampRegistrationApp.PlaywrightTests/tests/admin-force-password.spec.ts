import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Admin Force Password Change', () => {

  test('redirects to change password when password matches national ID', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin');  // same as national ID
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should be on change password page, not dashboard
    await expect(page).toHaveURL(/\/Admin\/ChangePassword/);
  });

  test('force change page hides old password field', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Old password field should not be visible
    await expect(page.locator('input[name="oldPassword"]')).not.toBeVisible();
  });

  test('prevents using same password as national ID', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Try to set same password
    await page.fill('input[name="newPassword"]', 'admin');
    await page.fill('input[name="confirmPassword"]', 'admin');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should show error
    await expect(page.locator('.validation-summary-errors, .text-red')).toBeVisible({ timeout: 3000 });
  });

  test('successful force change redirects to dashboard', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Change to new password
    await page.fill('input[name="newPassword"]', 'newadminpass123');
    await page.fill('input[name="confirmPassword"]', 'newadminpass123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should redirect to dashboard on success
    await expect(page).toHaveURL(/\/Admin\/Dashboard/, { timeout: 5000 });
  });
});
