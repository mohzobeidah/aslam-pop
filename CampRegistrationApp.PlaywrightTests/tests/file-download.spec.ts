import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('File Download Security', () => {

  test('unauthenticated access to file download redirects', async ({ page }) => {
    const response = await page.request.get('/File/Download?id=1&type=MedicalReport');
    expect(response.status()).toBe(302); // redirects to login
  });

  test('admin can access file download', async ({ page }) => {
    await page.goto('/Admin/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="nationalId"]', 'admin');
    await page.fill('input[name="password"]', 'admin123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Try downloading - might 404 if no file, but should not 401/redirect
    const response = await page.request.get('/File/Download?id=999999&type=MedicalReport');
    expect([200, 404]).toContain(response.status());
  });
});
