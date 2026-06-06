import { test, expect } from '@playwright/test';
import { fillStep1, addMember } from '../helpers/test-utils';
import { RegistrationStep3, Btn, Msg } from '../helpers/selectors';

test.describe('Registration Validation - Step 3 (Housing & Desires)', () => {

  async function goToStep3(page: import('@playwright/test').Page) {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');
    await fillStep1(page);
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');
    await page.click(Btn.addMember);
    await page.waitForTimeout(300);
    await addMember(page, 0);
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');
  }

  test('all desire selects are required', async ({ page }) => {
    await goToStep3(page);

    // Submit without selecting desires
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('conditional: TentType required when LivesInTent=true', async ({ page }) => {
    await goToStep3(page);

    // Select LivesInTent=true but leave TentType empty
    await page.locator(`[name="${RegistrationStep3.livesInTent}"]`).selectOption('true');

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('conditional: BathroomType and BathroomStatus required when HasBathroom=true', async ({ page }) => {
    await goToStep3(page);

    // Select HasBathroom=true but leave others empty
    await page.locator(`[name="${RegistrationStep3.hasBathroom}"]`).selectOption('true');

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('all fields satisfied allows proceeding to step 4', async ({ page }) => {
    await goToStep3(page);

    // Fill all required fields
    await page.locator(`[name="${RegistrationStep3.livesInTent}"]`).selectOption('true');
    await page.locator(`[name="${RegistrationStep3.tentType}"]`).selectOption('خيمة');
    await page.locator(`[name="${RegistrationStep3.hasBathroom}"]`).selectOption('true');
    await page.locator(`[name="${RegistrationStep3.bathroomType}"]`).selectOption('خاص');
    await page.locator(`[name="${RegistrationStep3.bathroomStatus}"]`).selectOption('جيد');

    // Select 3 desires
    for (let i = 0; i < 3; i++) {
      const el = page.locator(`[name="DesireIds[${i}]"]`);
      if (await el.count()) await el.selectOption(String(i + 1));
    }

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Should reach step 4 (password fields)
    await expect(page.locator('#regPassword')).toBeVisible({ timeout: 3000 });
  });
});
