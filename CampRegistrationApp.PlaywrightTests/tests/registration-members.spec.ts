import { test, expect } from '@playwright/test';
import { fillStep1, addMember, generatePalestinianId } from '../helpers/test-utils';
import { RegistrationStep1, Btn, Msg } from '../helpers/selectors';

test.describe('Registration Validation - Step 2 (Members)', () => {

  test('requires at least one wife when head is married', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    await fillStep1(page, {
      [RegistrationStep1.maritalStatus]: 'متزوج',
    });

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Try going to step 3 without adding any members
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Should show error about wife requirement
    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('rejects duplicate IDs between head and member', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    const sharedId = generatePalestinianId();

    await fillStep1(page, {
      [RegistrationStep1.idNumber]: sharedId,
    });

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Add a member with the same ID as head
    await page.click(Btn.addMember);
    await page.waitForTimeout(300);
    await addMember(page, 0, {
      'Members[0].IdNumber': sharedId,
    });

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Should show duplicate ID error
    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('rejects duplicate IDs between two members', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    await fillStep1(page);

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    const dupId = generatePalestinianId();

    // Add first member
    await page.click(Btn.addMember);
    await page.waitForTimeout(300);
    await addMember(page, 0);

    // Add second member with same ID
    await page.click(Btn.addMember);
    await page.waitForTimeout(300);
    await addMember(page, 1, {
      'Members[1].IdNumber': dupId,
      'Members[1].RelationshipToHead': 'ابن',
      'Members[1].Gender': 'ذكر',
    });

    // Set first member's ID to match second
    await page.fill('[name="Members[0].IdNumber"]', dupId);

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });

  test('dynamic add/remove member rows', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    await fillStep1(page);
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    // Add 3 members
    for (let i = 0; i < 3; i++) {
      await page.click(Btn.addMember);
      await page.waitForTimeout(300);
    }

    // Should see 3 member sections
    await expect(page.locator('.member-section, .member-card')).toHaveCount(3);
  });

  test('sick members require at least one disease selected', async ({ page }) => {
    await page.goto('/Registration/Index');
    await page.waitForLoadState('networkidle');

    await fillStep1(page);
    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    await page.click(Btn.addMember);
    await page.waitForTimeout(300);
    await addMember(page, 0, {
      'Members[0].HealthStatus': 'مريض',
    });

    await page.click(Btn.nextStep);
    await page.waitForLoadState('networkidle');

    const error = page.locator(Msg.errorSummary);
    await expect(error).toBeVisible({ timeout: 3000 });
  });
});
