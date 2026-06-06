import { test, expect } from '@playwright/test';
import { Btn, Report } from '../helpers/selectors';

test.describe('Report System - Excel Export', () => {

  test('export excel returns xlsx file', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    await page.goto('/Report/Index');
    await page.waitForLoadState('networkidle');

    // Click export
    const response = await page.request.post('/Report/ExportExcel', {
      form: {
        ReportType: 'Normal',
        Status: 'Pending',
      },
    });

    expect(response.ok()).toBe(true);
    expect(response.headers()['content-type']).toContain('spreadsheet');
  });

  test('export for disabled report type', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    const response = await page.request.post('/Report/ExportExcel', {
      form: {
        ReportType: 'Disabled',
        Status: 'Approved',
      },
    });

    expect(response.ok()).toBe(true);
    expect(response.headers()['content-type']).toContain('spreadsheet');
  });
});
