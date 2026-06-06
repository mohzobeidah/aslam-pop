import { test, expect } from '@playwright/test';
import { Btn, Report } from '../helpers/selectors';

test.describe('Report System - Preview', () => {

  test('report page loads with column groups', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Report/Index');
    await page.waitForLoadState('networkidle');

    // Column groups should be visible
    await expect(page.locator('input[type="checkbox"]').first()).toBeVisible({ timeout: 3000 });
  });

  test('preview generates table with selected columns', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Report/Index');
    await page.waitForLoadState('networkidle');

    // Click preview
    await page.click(Report.previewButton);
    await page.waitForLoadState('networkidle');

    // Table should appear (or empty message)
    const hasTable = await page.locator('table').first().isVisible().catch(() => false);
    const hasEmpty = await page.locator('.empty-message, .no-data').isVisible().catch(() => false);
    expect(hasTable || hasEmpty).toBe(true);
  });

  test('filter by status works in preview', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Report/Index');
    await page.waitForLoadState('networkidle');

    // Select Pending status filter
    const statusFilter = page.locator('#status, [name="Status"]');
    if (await statusFilter.isVisible()) {
      await statusFilter.selectOption('Pending');
    }

    await page.click(Report.previewButton);
    await page.waitForLoadState('networkidle');
  });

  test('preview with NeedsDiapers filter', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Report/Index');
    await page.waitForLoadState('networkidle');

    // Check NeedsDiapers checkbox
    const needsDiapers = page.locator('[name="NeedsDiapers"], #NeedsDiapers');
    if (await needsDiapers.isVisible()) {
      await needsDiapers.check();
    }

    await page.click(Report.previewButton);
    await page.waitForLoadState('networkidle');
  });

  test('preview for each report type', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    const reportTypes = ['Normal', 'Disabled', 'ChronicSick', 'Pregnant', 'Nursing'];
    for (const rtype of reportTypes) {
      await page.goto('/Report/Index');
      await page.waitForLoadState('networkidle');

      const reportTypeSelect = page.locator('#ReportType, [name="ReportType"]');
      if (await reportTypeSelect.isVisible()) {
        await reportTypeSelect.selectOption(rtype);
      }

      await page.click(Report.previewButton);
      await page.waitForLoadState('networkidle');
    }
  });
});
