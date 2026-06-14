import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Assistance Details - New Features', () => {

  async function login(page: import('@playwright/test').Page) {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.locator('button[type="submit"]').click();
    await page.waitForURL('**/Admin/**', { timeout: 10000 });
  }

  test('details page has all new buttons', async ({ page }) => {
    await login(page);

    // Navigate to assistance list
    await page.goto('/Assistance/Index');
    await page.waitForLoadState('networkidle');

    // Try to find an existing assistance to view
    const viewLink = page.locator('a:has-text("عرض")').first();
    if (await viewLink.isVisible({ timeout: 3000 }).catch(() => false)) {
      await viewLink.click();
      await page.waitForLoadState('networkidle');
    } else {
      // Create one
      await page.goto('/Assistance/Create');
      await page.waitForLoadState('networkidle');
      await page.fill('[name="Name"]', `اختبار تفاصيل ${Date.now()}`);
      await page.selectOption('[name="AssistanceType"]', 'غذائية');
      await page.fill('[name="Source"]', 'مصدر اختبار');
      await page.fill('[name="AssistanceDate"]', '2026-06-15');
      await page.selectOption('[name="SectorId"]', '1');
      await page.click('button:has-text("حفظ")');
      await page.waitForLoadState('networkidle');

      if (page.url().includes('/Assistance/Index')) {
        await page.locator('a:has-text("عرض")').first().click();
        await page.waitForLoadState('networkidle');
      }
    }

    // Verify all new buttons are present
    await expect(page.locator('h1')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('button:has-text("إضافة متعددة")')).toBeVisible({ timeout: 3000 });
    await expect(page.locator('button:has-text("رفع Excel")')).toBeVisible({ timeout: 3000 });
    await expect(page.getByRole('link', { name: 'تحميل قالب', exact: true })).toBeVisible({ timeout: 3000 });
    await expect(page.locator('a:has-text("تصدير Excel")')).toBeVisible({ timeout: 3000 });
  });

  test('bulk add modal opens and shows family heads', async ({ page }) => {
    await login(page);

    await page.goto('/Assistance/Index');
    await page.waitForLoadState('networkidle');

    const viewLink = page.locator('a:has-text("عرض")').first();
    if (await viewLink.isVisible({ timeout: 3000 }).catch(() => false)) {
      await viewLink.click();
      await page.waitForLoadState('networkidle');
    } else {
      await page.goto('/Assistance/Create');
      await page.waitForLoadState('networkidle');
      await page.fill('[name="Name"]', `اختبار متعدد ${Date.now()}`);
      await page.selectOption('[name="AssistanceType"]', 'غذائية');
      await page.fill('[name="Source"]', 'اختبار');
      await page.fill('[name="AssistanceDate"]', '2026-06-15');
      await page.selectOption('[name="SectorId"]', '1');
      await page.click('button:has-text("حفظ")');
      await page.waitForLoadState('networkidle');
      if (page.url().includes('/Assistance/Index')) {
        await page.locator('a:has-text("عرض")').first().click();
        await page.waitForLoadState('networkidle');
      }
    }

    await expect(page.locator('h1')).toBeVisible({ timeout: 5000 });

    // Open bulk modal
    await page.locator('button:has-text("إضافة متعددة")').click();
    await page.waitForTimeout(1000);

    const modal = page.locator('#bulkModal');
    await expect(modal).toBeVisible({ timeout: 5000 });

    // Verify modal UI
    await expect(page.locator('#bulkSectorFilter')).toBeVisible();
    await expect(page.locator('#bulkSearch')).toBeVisible();
    await expect(page.locator('#selectAllCheck')).toBeVisible();
    await expect(page.locator('#selectedCount')).toBeVisible();
    await expect(page.locator('#confirmBulkBtn')).toBeVisible();

    // Wait for family heads to load
    await page.waitForTimeout(3000);
    const listLoaded = await page.locator('#bulkLoading').isHidden().catch(() => false);
    expect(listLoaded).toBe(true);

    // Close modal
    await page.locator('#bulkModal').getByRole('button', { name: 'إلغاء', exact: true }).click();
    await page.waitForTimeout(500);
    await expect(modal).not.toBeVisible({ timeout: 3000 });
  });

  test('excel import modal opens', async ({ page }) => {
    await login(page);

    await page.goto('/Assistance/Index');
    await page.waitForLoadState('networkidle');

    const viewLink = page.locator('a:has-text("عرض")').first();
    if (await viewLink.isVisible({ timeout: 3000 }).catch(() => false)) {
      await viewLink.click();
      await page.waitForLoadState('networkidle');
    } else {
      await page.goto('/Assistance/Create');
      await page.waitForLoadState('networkidle');
      await page.fill('[name="Name"]', `اختبار Excel ${Date.now()}`);
      await page.selectOption('[name="AssistanceType"]', 'غذائية');
      await page.fill('[name="Source"]', 'اختبار');
      await page.fill('[name="AssistanceDate"]', '2026-06-15');
      await page.selectOption('[name="SectorId"]', '1');
      await page.click('button:has-text("حفظ")');
      await page.waitForLoadState('networkidle');
      if (page.url().includes('/Assistance/Index')) {
        await page.locator('a:has-text("عرض")').first().click();
        await page.waitForLoadState('networkidle');
      }
    }

    await expect(page.locator('h1')).toBeVisible({ timeout: 5000 });

    // Open excel modal
    await page.locator('button:has-text("رفع Excel")').click();
    await page.waitForTimeout(1000);

    const modal = page.locator('#excelModal');
    await expect(modal).toBeVisible({ timeout: 5000 });
    await expect(page.locator('#excelForm')).toBeVisible();
    await expect(page.locator('input[type="file"]')).toBeVisible();
    await expect(page.locator('button:has-text("رفع واستيراد")')).toBeVisible();

    // Close
    await page.getByRole('button', { name: 'إغلاق' }).click();
    await page.waitForTimeout(500);
    await expect(modal).not.toBeVisible({ timeout: 3000 });
  });

  test('download template from details page', async ({ page }) => {
    await login(page);

    const response = await page.request.get('/Assistance/DownloadTemplate');
    expect(response.ok()).toBe(true);
    expect(response.headers()['content-type']).toContain('spreadsheet');
  });

  test('search person on details page', async ({ page }) => {
    await login(page);

    await page.goto('/Assistance/Index');
    await page.waitForLoadState('networkidle');

    const viewLink = page.locator('a:has-text("عرض")').first();
    if (await viewLink.isVisible({ timeout: 3000 }).catch(() => false)) {
      await viewLink.click();
      await page.waitForLoadState('networkidle');
    } else {
      await page.goto('/Assistance/Create');
      await page.waitForLoadState('networkidle');
      await page.fill('[name="Name"]', `اختبار بحث ${Date.now()}`);
      await page.selectOption('[name="AssistanceType"]', 'غذائية');
      await page.fill('[name="Source"]', 'اختبار');
      await page.fill('[name="AssistanceDate"]', '2026-06-15');
      await page.selectOption('[name="SectorId"]', '1');
      await page.click('button:has-text("حفظ")');
      await page.waitForLoadState('networkidle');
      if (page.url().includes('/Assistance/Index')) {
        await page.locator('a:has-text("عرض")').first().click();
        await page.waitForLoadState('networkidle');
      }
    }

    await expect(page.locator('h1')).toBeVisible({ timeout: 5000 });

    // Search for a person
    const searchInput = page.locator('#personSearch');
    await expect(searchInput).toBeVisible({ timeout: 3000 });
    await searchInput.fill('test');
    await page.locator('button:has-text("بحث")').click();
    await page.waitForTimeout(2000);

    // Either results or not-found should be visible
    const hasResults = await page.locator('#searchResults').isVisible().catch(() => false);
    const hasNotFound = await page.locator('#notFound').isVisible().catch(() => false);
    expect(hasResults || hasNotFound).toBe(true);
  });
});
