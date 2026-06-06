import { Page, expect } from '@playwright/test';
import { RegistrationStep1, RegistrationStep3, MemberFields, Btn, Modal, Msg } from './selectors';

/** Generate a valid 9-digit Palestinian ID with correct check digit */
export function generatePalestinianId(): string {
  const base = Array.from({ length: 8 }, () => Math.floor(Math.random() * 10)).join('');
  const weights = [1, 2, 1, 2, 1, 2, 1, 2];
  let sum = 0;
  for (let i = 0; i < 8; i++) {
    const product = parseInt(base[i]) * weights[i];
    sum += product >= 10 ? (product % 10) + 1 : product;
  }
  const checkDigit = (10 - (sum % 10)) % 10;
  return base + checkDigit;
}

/** Generate unique ID for test records */
let counter = Date.now();
export function uniqueId(prefix = 'TEST'): string {
  return `${prefix}${counter++}`.slice(0, 20);
}

/** Fill Step 1 (Head Info) of the registration wizard */
export async function fillStep1(page: Page, overrides: Record<string, string> = {}) {
  const fields: Record<string, string> = {
    [RegistrationStep1.firstName]: 'أحمد',
    [RegistrationStep1.secondName]: 'محمد',
    [RegistrationStep1.thirdName]: 'سالم',
    [RegistrationStep1.lastName]: 'حسن',
    [RegistrationStep1.idNumber]: generatePalestinianId(),
    [RegistrationStep1.sector]: '1',
    [RegistrationStep1.dateOfBirth]: '1990-01-15',
    [RegistrationStep1.phoneNumber]: '059' + Math.floor(1000000 + Math.random() * 9000000).toString(),
    [RegistrationStep1.wallet]: '059' + Math.floor(1000000 + Math.random() * 9000000).toString(),
    [RegistrationStep1.walletType]: 'بنك',
    [RegistrationStep1.originalGovernorate]: 'غزة',
    [RegistrationStep1.maritalStatus]: 'متزوج',
    [RegistrationStep1.employmentStatus]: 'موظف',
    [RegistrationStep1.educationLevel]: 'جامعي',
    [RegistrationStep1.gender]: 'ذكر',
    [RegistrationStep1.healthStatus]: 'سليم',
    ...overrides,
  };

  const selectFields = new Set([
    RegistrationStep1.sector,
    RegistrationStep1.walletType,
    RegistrationStep1.originalGovernorate,
    RegistrationStep1.maritalStatus,
    RegistrationStep1.educationLevel,
  ]);

  const radioFields = new Set([
    RegistrationStep1.gender,
    RegistrationStep1.healthStatus,
  ]);

  for (const [name, value] of Object.entries(fields)) {
    if (selectFields.has(name)) {
      const el = page.locator(`[name="${name}"]`);
      if (await el.count()) await el.selectOption(value);
    } else if (radioFields.has(name)) {
      const el = page.locator(`[name="${name}"][value="${value}"]`);
      if (await el.count()) await el.check();
    } else {
      const el = page.locator(`[name="${name}"]`);
      if (await el.count()) {
        await el.fill('');
        await el.fill(value);
      }
    }
  }
}

/** Fill Step 2: add a family member */
const memberRadioFields = new Set(['Gender', 'HealthStatus']);

function isMemberRadioField(name: string): boolean {
  return memberRadioFields.has(name.split('.').pop() || '');
}

export async function addMember(page: Page, index: number, overrides: Record<string, string> = {}) {
  const fields: Record<string, string> = {
    [MemberFields.firstName(index)]: 'فاطمة',
    [MemberFields.secondName(index)]: 'أحمد',
    [MemberFields.thirdName(index)]: 'خالد',
    [MemberFields.lastName(index)]: 'حسن',
    [MemberFields.idNumber(index)]: generatePalestinianId(),
    [MemberFields.relationship(index)]: 'زوجة',
    [MemberFields.gender(index)]: 'أنثى',
    [MemberFields.dateOfBirth(index)]: '1995-06-20',
    [MemberFields.maritalStatus(index)]: 'متزوج',
    [MemberFields.healthStatus(index)]: 'سليم',
    ...overrides,
  };

  for (const [name, value] of Object.entries(fields)) {
    const el = page.locator(`[name="${name}"]`);
    if (await el.count()) {
      if (isMemberRadioField(name)) {
        const radio = page.locator(`[name="${name}"][value="${value}"]`);
        if (await radio.count()) await radio.check();
      } else {
        const tag = await el.first().evaluate(e => e.tagName);
        if (tag === 'SELECT' || tag === 'select') await el.first().selectOption(value);
        else {
          await el.first().fill('');
          await el.first().fill(value);
        }
      }
    }
  }
}

/** Fill Step 3: housing, bathroom, and desires */
export async function fillStep3(page: Page, overrides: Record<string, string> = {}) {
  const fields: Record<string, string> = {
    [RegistrationStep3.livesInTent]: 'true',
    [RegistrationStep3.tentType]: 'خيمة صينية',
    [RegistrationStep3.hasBathroom]: 'true',
    [RegistrationStep3.bathroomType]: 'Private',
    [RegistrationStep3.bathroomStatus]: 'جيد',
    ...overrides,
  };

  const selectFields = new Set([
    RegistrationStep3.tentType,
    RegistrationStep3.bathroomStatus,
  ]);

  const radioFields = new Set([
    RegistrationStep3.livesInTent,
    RegistrationStep3.hasBathroom,
    RegistrationStep3.bathroomType,
  ]);

  for (const [name, value] of Object.entries(fields)) {
    if (selectFields.has(name)) {
      const el = page.locator(`[name="${name}"]`);
      if (await el.first().count()) await el.first().selectOption(value);
    } else if (radioFields.has(name)) {
      const el = page.locator(`[name="${name}"][value="${value}"]`);
      if (await el.first().count()) await el.first().check();
    } else {
      const el = page.locator(`[name="${name}"]`);
      if (await el.first().count()) {
        await el.first().fill('');
        await el.first().fill(value);
      }
    }
  }

  // Set desires as individual selects (5 ranked dropdowns)
  const desireValues = [1, 2, 3, 4, 5];
  for (let i = 0; i < desireValues.length; i++) {
    const el = page.locator(`[name="Desires[${i}]"]`);
    if (await el.count()) {
      await el.evaluate((sel: Element, val: number) => {
        const select = sel as HTMLSelectElement;
        const option = Array.from(select.options).find(o => parseInt(o.value) === val);
        if (option) select.value = option.value;
        select.dispatchEvent(new Event('change', { bubbles: true }));
      }, desireValues[i]);
    }
  }
}

/** Fill Step 4: password and submit */
export async function fillStep4(page: Page, password = 'test1234') {
  await page.fill('#regPassword', password);
  await page.fill('#regConfirmPassword', password);
  await page.check('#acceptResponsibility');
}

/** Handle an alert dialog if present (dismiss it) */
async function handleAlert(page: Page): Promise<boolean> {
  try {
    const dialog = await page.waitForEvent('dialog', { timeout: 500 });
    await dialog.dismiss();
    return true;
  } catch {
    return false;
  }
}

/** Complete a full registration from start to finish */
export async function completeRegistration(page: Page) {
  await page.goto('/Registration/Index');
  await page.waitForLoadState('networkidle');

  // Clear any stale localStorage from previous tests
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await page.waitForLoadState('networkidle');

  // Step 1
  await fillStep1(page);
  await page.click(Btn.nextStep);
  await handleAlert(page);

  // Step 2: add a wife
  await page.click(Btn.addMember);
  await page.waitForTimeout(300);
  await addMember(page, 0);
  await page.click(Btn.nextStep);
  await handleAlert(page);

  // Step 3
  await fillStep3(page);
  await page.click(Btn.nextStep);
  await handleAlert(page);

  // Step 4
  await fillStep4(page);

  // Click submit button
  await page.locator(Btn.submitStep4).click();

  // Wait for either success or validation errors
  try {
    await expect(page.locator(Msg.successAlert).or(page.locator(Msg.errorSummary))).toBeVisible({ timeout: 15000 });
  } catch {
    await page.waitForTimeout(2000);
  }

  // Check if we landed on success page (separate page or in-page banner)
  const isSuccess = await page.locator(Msg.successAlert).isVisible().catch(() => false);
  if (!isSuccess) return '';

  // Extract RecordId - try multiple approaches
  // 1: In-page banner with "رقم القيد: XXXXXXXX" text
  const bannerText = await page.locator(Msg.successAlert).first().textContent().catch(() => '');
  const idMatch = bannerText?.match(/[23456789ABCDEFGHJKLMNPQRSTUVWXYZ]{8}/);
  if (idMatch) return idMatch[0];

  // 2: Dedicated success page with font-mono record ID
  const recordId = await page.locator(Msg.successRecordId).first().textContent().catch(() => '');
  return recordId?.trim() || '';
}

/** Login as admin */
export async function loginAsAdmin(page: Page, nationalId = 'admin', password = 'admin123') {
  await page.goto('/Admin/Login');
  await page.waitForLoadState('networkidle');
  await page.fill('input[name="nationalId"]', nationalId);
  await page.fill('input[name="password"]', password);
  await page.click(Btn.submit);
  await page.waitForLoadState('networkidle');
}

/** Login as refugee */
export async function loginAsRefugee(page: Page, idNumber: string, password: string) {
  await page.goto('/Record/Login');
  await page.waitForLoadState('networkidle');
  await page.fill('input[name="idNumber"]', idNumber);
  await page.fill('input[name="password"]', password);
  await page.click(Btn.submit);
  await page.waitForLoadState('networkidle');
}

/** Wait for any success/error alert to appear */
export async function waitForAlert(page: Page) {
  await page.waitForSelector(Msg.errorSummary, { timeout: 5000 }).catch(() => {});
  await page.waitForSelector(Msg.successAlert, { timeout: 5000 }).catch(() => {});
}
