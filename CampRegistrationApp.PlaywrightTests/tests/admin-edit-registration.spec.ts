import { test, expect } from '@playwright/test';
import { completeRegistration } from '../helpers/test-utils';
import { Btn } from '../helpers/selectors';

test.describe('Admin Edit Registration', () => {

  test('admin can edit pending registration', async ({ page }) => {
    const recordId = await completeRegistration(page);

    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Navigate to admin edit
    await page.goto(`/Admin/AdminEditRegistration?recordId=${recordId}`);
    await page.waitForLoadState('networkidle');

    // Handle possible alert
    try { await page.locator('dialog, .alert').first().waitFor({ timeout: 2000 }); } catch { }

    // Change head first name
    const firstNameInput = page.locator('[name="Head.FirstName"]');
    await expect(firstNameInput).toBeVisible({ timeout: 5000 });
    await firstNameInput.fill('');
    await firstNameInput.fill('Admin Edited');

    // Submit
    await page.click('button[type="submit"]');
    await page.waitForLoadState('networkidle');

    // Should succeed
    await expect(page.locator('.alert-success, .bg-green').first()).toBeVisible({ timeout: 5000 });
  });

  test('admin edit for approved registration loads correctly', async ({ page }) => {
    const recordId = await completeRegistration(page);

    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Approve first
    await page.goto('/Admin/Registrations');
    await page.waitForLoadState('networkidle');
    const approveBtn = page.locator('button:has-text("موافقة")').first();
    if (await approveBtn.isVisible()) {
      await approveBtn.click();
      await page.waitForLoadState('networkidle');
    }

    // Now edit
    await page.goto(`/Admin/AdminEditRegistration?recordId=${recordId}`);
    await page.waitForLoadState('networkidle');

    // The edit page should load
    await expect(page.locator('[name="Head.FirstName"]').first()).toBeVisible({ timeout: 5000 });
  });
});
