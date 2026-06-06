import { chromium } from '@playwright/test';

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();

  await page.goto('http://localhost:5392/Registration/Index');
  await page.waitForLoadState('networkidle');
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await page.waitForLoadState('networkidle');

  // Fill step 1
  await page.fill('[name="Head.FirstName"]', 'أحمد');
  await page.fill('[name="Head.SecondName"]', 'محمد');
  await page.fill('[name="Head.ThirdName"]', 'سالم');
  await page.fill('[name="Head.LastName"]', 'حسن');
  await page.fill('[name="Head.IdNumber"]', '123456789');
  await page.selectOption('[name="SectorId"]', '1');
  await page.fill('[name="Head.DateOfBirth"]', '1990-01-15');
  await page.check('[name="Head.Gender"][value="ذكر"]');
  await page.fill('[name="PhoneNumber"]', '0591234567');
  await page.fill('[name="Wallet"]', '0591234567');
  await page.selectOption('[name="WalletType"]', 'بنك');
  await page.selectOption('[name="Head.OriginalGovernorate"]', 'غزة');
  await page.selectOption('[name="Head.MaritalStatus"]', 'متزوج');
  await page.fill('[name="Head.EmploymentStatus"]', 'موظف');
  await page.selectOption('[name="Head.EducationLevel"]', 'جامعي');
  await page.check('[name="Head.HealthStatus"][value="سليم"]');

  // Go to step 2
  await page.click('button:has-text("التالي")');
  await page.waitForTimeout(1000);

  // Add member
  await page.click('button:has-text("إضافة فرد")');
  await page.waitForTimeout(500);

  // Fill member
  await page.fill('[name="Members[0].FirstName"]', 'فاطمة');
  await page.fill('[name="Members[0].SecondName"]', 'أحمد');
  await page.fill('[name="Members[0].ThirdName"]', 'خالد');
  await page.fill('[name="Members[0].LastName"]', 'حسن');
  const memberId = '987654321';
  await page.fill('[name="Members[0].IdNumber"]', memberId);
  await page.selectOption('[name="Members[0].Gender"]', 'أنثى');
  await page.fill('[name="Members[0].DateOfBirth"]', '1995-06-20');
  await page.selectOption('[name="Members[0].RelationshipToHead"]', 'زوجة');
  await page.selectOption('[name="Members[0].MaritalStatus"]', 'متزوج');
  await page.check('[name="Members[0].HealthStatus"][value="سليم"]');

  // Go to step 3
  await page.click('button:has-text("التالي")');
  await page.waitForTimeout(1000);

  // Fill step 3
  await page.check('[name="LivesInTent"][value="true"]');
  await page.waitForTimeout(300);
  await page.selectOption('[name="TentType"]', 'خيمة صينية');
  await page.check('[name="HasBathroom"][value="true"]');
  await page.waitForTimeout(300);
  await page.check('[name="BathroomType"][value="Private"]');
  await page.selectOption('[name="Head.BathroomStatus"]', 'جيد');

  // Set desires
  const desireValues = [1, 2, 3, 4, 5];
  for (let i = 0; i < desireValues.length; i++) {
    const el = page.locator(`[name="Desires[${i}]"]`);
    if (await el.count()) {
      await el.evaluate((sel: Element, val: number) => {
        const select = sel as HTMLSelectElement;
        select.value = String(val);
        select.dispatchEvent(new Event('change', { bubbles: true }));
      }, desireValues[i]);
      await page.waitForTimeout(200);
    }
  }

  // Go to step 4
  await page.click('button:has-text("التالي")');
  await page.waitForTimeout(1000);

  // Fill password
  await page.fill('#regPassword', 'test1234');
  await page.fill('#regConfirmPassword', 'test1234');
  await page.check('#acceptResponsibility');

  // Capture form HTML before submit
  const formHtml = await page.evaluate(() => {
    const form = document.getElementById('registrationForm');
    return form ? form.innerHTML.substring(0, 5000) : 'form not found';
  });
  console.log('FORM HTML (first 5000 chars):');
  console.log(formHtml);

  // Click submit
  console.log('Clicking submit...');
  await page.click('button:has-text("تأكيد وإرسال")');
  await page.waitForTimeout(5000);

  // Check page content
  const pageContent = await page.evaluate(() => {
    const errors = document.querySelectorAll('li');
    const errorTexts = Array.from(errors).map(e => e.textContent);
    const body = document.body?.innerText?.substring(0, 2000) || 'no body';
    return { errorTexts, body };
  });

  console.log('\n\nPAGE ERRORS:');
  console.log(JSON.stringify(pageContent.errorTexts, null, 2));
  console.log('\n\nPAGE BODY (first 2000 chars):');
  console.log(pageContent.body);

  await page.waitForTimeout(5000);
  await browser.close();
})();
