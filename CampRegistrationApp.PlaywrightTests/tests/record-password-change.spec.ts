import { test, expect } from '@playwright/test';
import { Btn } from '../helpers/selectors';

test.describe('Record Password Change', () => {

  test('change password page shows force change banner when password equals ID', async ({ page }) => {
    // First register a family
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    // Use a specific head ID that we know
    await page.fill('[name="Head.FirstName"]', 'قسر');
    await page.fill('[name="Head.SecondName"]', 'تغيير');
    await page.fill('[name="Head.ThirdName"]', 'كلمة');
    await page.fill('[name="Head.LastName"]', 'المرور');
    await page.fill('[name="Head.IdNumber"]', '123456789');  // 9 digits, will also be the password
    await page.locator('[name="Head.SectorId"]').selectOption('1');
    await page.fill('[name="Head.DateOfBirth"]', '1990-01-15');
    await page.fill('[name="Head.PhoneNumber"]', '0591111111');
    await page.locator('[name="Head.Gender"]').selectOption('ذكر');
    await page.locator('[name="Head.HealthStatus"]').selectOption('سليم');
    await page.locator('[name="Head.OriginalGovernorate"]').selectOption('غزة');
    await page.locator('[name="Head.MaritalStatus"]').selectOption('أعزب');
    await page.locator('[name="Head.EmploymentStatus"]').selectOption('موظف');
    await page.locator('[name="Head.EducationLevel"]').selectOption('جامعي');

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Step 3 desires
    await page.locator('[name="LivesInTent"]').selectOption('false');
    for (let i = 0; i < 3; i++) {
      const el = page.locator(`[name="DesireIds[${i}]"]`);
      if (await el.count()) await el.selectOption(String(i + 1));
    }
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Step 4: use same password as ID number
    await page.fill('#regPassword', '123456789');
    await page.fill('#regConfirmPassword', '123456789');
    await page.check('#acceptResponsibility');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Login with the same password == ID
    await page.goto('/Record/Login');
    await page.waitForLoadState('networkidle');
    await page.fill('input[name="idNumber"]', '123456789');
    await page.fill('input[name="password"]', '123456789');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should be on edit page with force change banner
    const body = page.locator('body');
    await expect(body).toContainText(/تغيير كلمة المرور|يجب تغيير/i, { timeout: 5000 });

    // Change password
    await page.fill('input[name="newPassword"]', 'newpass123');
    await page.fill('input[name="confirmPassword"]', 'newpass123');
    await page.click(Btn.submit);
    await page.waitForLoadState('networkidle');

    // Should succeed
    await expect(page.locator('.alert-success, .bg-green')).toBeVisible({ timeout: 3000 });
  });
});
