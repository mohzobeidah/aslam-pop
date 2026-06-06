import { test, expect } from '@playwright/test';
import { Btn, Msg } from '../helpers/selectors';

test.describe('Admin Authentication', () => {

  test('login with correct credentials redirects to dashboard', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should be on dashboard
    await expect(page).toHaveURL(/\/Admin\/Dashboard/);
  });

  test('login with wrong credentials shows error', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'wrongpass');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('empty fields stay on login page', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await expect(page.locator('input[name="nationalId"]')).toBeVisible();
  });

  test('logout redirects to login', async ({ page }) => {
    // Login first
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Click logout
    await page.goto('/Admin/Logout');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/Admin\/Login/);
  });
});
