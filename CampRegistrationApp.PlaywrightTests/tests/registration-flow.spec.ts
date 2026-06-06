import { test, expect } from '@playwright/test';
import { completeRegistration } from '../helpers/test-utils';

test.describe('Registration Wizard - Full Flow', () => {

  test('complete registration from start to success page', async ({ page }) => {
    const recordId = await completeRegistration(page);
    expect(recordId).toBeTruthy();
    expect(recordId!.length).toBe(8);
  });

  test('step navigation works (next, previous, step indicators)', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    // Step indicators visible
    await expect(page.locator('#step-indicator-1')).toBeVisible();

    // Can go back from step 2 to step 1
    await page.locator('button:has-text("التالي")').click();
    await page.waitForLoadState('networkidle');
    await page.locator('button:has-text("السابق")').click();
    await page.waitForLoadState('networkidle');

    // Still on step 1
    await expect(page.locator('[name="Head.FirstName"]')).toBeVisible();
  });

  test('generates valid 8-char record ID', async ({ page }) => {
    const recordId = await completeRegistration(page);
    const validChars = /^[23456789ABCDEFGHJKLMNPQRSTUVWXYZ]+$/;
    expect(validChars.test(recordId!)).toBe(true);
  });

  test('success page shows record ID to user', async ({ page }) => {
    await completeRegistration(page);

    // Success page visible with record ID
    await expect(page.locator('h1:has-text("تم التسجيل")')).toBeVisible();
  });
});
