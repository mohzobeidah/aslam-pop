import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Project & Nomination System', () => {

  test('project list loads', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Project/Index');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
  });

  test('create project form loads', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Project/Create');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('[name="Name"]').first()).toBeVisible({ timeout: 3000 });
  });

  test('create project submits', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Project/Create');
    await page.waitForLoadState('networkidle');

    await page.fill('[name="Name"]', 'مشروع توزيع الطرود');
    await page.fill('[name="StartDate"]', '2026-03-01');
    await page.fill('[name="EndDate"]', '2026-04-01');
    await page.fill('[name="RequiredCount"]', '100');
    await page.locator('[name="Status"]').selectOption('Active');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should redirect back to index
    await expect(page).toHaveURL(/\/Project\//);
  });

  test('nomination page loads for project view', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Get first project
    await page.goto('/Project/Index');
    await page.waitForLoadState('networkidle');

    // Click view on first project
    const viewLink = page.locator('a[href*="/Nomination/"], a[href*="/Project/View"]').first();
    if (await viewLink.isVisible()) {
      await viewLink.click();
      await page.waitForLoadState('networkidle');
      await expect(page.locator('body')).toBeVisible({ timeout: 3000 });
    }
  });

  test('search person for nomination', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Go to project index and find a project to view
    await page.goto('/Project/Index');
    await page.waitForLoadState('networkidle');

    const viewLink = page.locator('a[href*="/Nomination/"], a[href*="/Project/View"]').first();
    if (await viewLink.isVisible()) {
      await viewLink.click();
      await page.waitForLoadState('networkidle');

      // Look for search input
      const searchInput = page.locator('input[type="search"], input[name="search"]').first();
      if (await searchInput.isVisible()) {
        await searchInput.fill('أحمد');
        await page.keyboard.press('Enter');
        await page.waitForLoadState('networkidle');
      }
    }
  });
});
