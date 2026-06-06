import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Authorization & Role-Based Access', () => {

  test('anonymous user redirected from admin pages', async ({ page }) => {
    await page.goto('/Admin/Dashboard');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/Admin\/Login/);
  });

  test('anonymous user redirected from report pages', async ({ page }) => {
    await page.goto('/Report/Index');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/Admin\/Login/);
  });

  test('anonymous user redirected from assistance pages', async ({ page }) => {
    await page.goto('/Assistance/Index');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/Admin\/Login/);
  });

  test('anonymous user redirected from complaint admin pages', async ({ page }) => {
    await page.goto('/Complaint/Index');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/Admin\/Login/);
  });

  test('anonymous user can access complaint create page', async ({ page }) => {
    await page.goto('/Complaint/Create');
    await page.waitForLoadState('networkidle');
    await expect(page.locator('textarea, input').first()).toBeVisible({ timeout: 3000 });
  });

  test('anonymous user can access registration page', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');
    await expect(page.locator('[name="Head.FirstName"]')).toBeVisible({ timeout: 3000 });
  });

  test('admin can access admin-only pages', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Navigate to multiple admin pages
    const adminPages = [
      '/Admin/Dashboard',
      '/Admin/Registrations',
      '/Admin/Refugees',
      '/Admin/Index',
      '/Admin/Sectors',
    ];

    for (const url of adminPages) {
      await page.goto(url);
      await page.waitForLoadState('networkidle');
      expect(page.url()).toContain(url);
    }
  });
});
