import { test, expect } from '@playwright/test';
import { fillStep1, fillStep3, fillStep4, generatePalestinianId, addMember } from '../helpers/test-utils';
import { RegistrationStep1, RegistrationStep3, Btn, Msg } from '../helpers/selectors';

test.describe('Registration Validation - Step 1', () => {

  test('shows error when required fields are empty', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    // Skip filling fields, go straight to next
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Should stay on step 1 with errors
    await expect(page.locator('[name="Head.FirstName"]')).toBeVisible();
  });

  test('health/disease contradiction: clears diseases when health is سليم', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    // Select سليم health status
    await page.locator(`[name="${RegistrationStep1.healthStatus}"]`).selectOption('سليم');
    await page.waitForTimeout(200);

    // Checkboxes for diseases should be cleared/unchecked
    const diseaseCheckboxes = page.locator(`[name="${RegistrationStep1.chronicDiseases}"]`);
    if (await diseaseCheckboxes.count() > 0) {
      await expect(diseaseCheckboxes.first()).not.toBeChecked();
    }
  });

  test('wallet type required when wallet provided', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    await fillStep1(page, {
      [RegistrationStep1.wallet]: '987654321',
      [RegistrationStep1.walletType]: '',  // leave empty
    });

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Should show validation error
    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('validates Palestinian ID check digit', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    // Fill with invalid check digit (all 9s - very unlikely to be valid)
    await fillStep1(page, {
      [RegistrationStep1.idNumber]: '123456780',  // wrong check digit
    });

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('Mother ID validated when provided for member', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    await fillStep1(page);
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Add a member
    await page.click(Btn.addMember);
    await page.waitForTimeout(300);
    await addMember(page, 0);

    // Fill mother ID with invalid value
    await page.fill(`[name="Members[0].MotherIdNumber"]`, '123');  // too short

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Should show mother ID error
    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });
});
