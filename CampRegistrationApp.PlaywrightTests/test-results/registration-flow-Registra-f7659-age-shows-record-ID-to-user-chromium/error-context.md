# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: registration-flow.spec.ts >> Registration Wizard - Full Flow >> success page shows record ID to user
- Location: tests\registration-flow.spec.ts:35:7

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: expect(locator).toBeVisible() failed

Locator: locator('h1:has-text("تم التسجيل")')
Expected: visible
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for locator('h1:has-text("تم التسجيل")')

```

```yaml
- banner:
  - navigation:
    - link "نظام تسجيل العائلات":
      - /url: /
    - button "☀️"
    - link "تسجيل جديد":
      - /url: /Registration
    - link "تسجيل الدخول":
      - /url: /Record/Login
    - link "تقديم شكوى":
      - /url: /Complaint/Create
    - link "دخول المشرفين":
      - /url: /Admin/Login
- main:
  - heading "نموذج تسجيل العائلات" [level=1]
  - paragraph: يتم حفظ البيانات تلقائياً في المتصفح
  - list:
    - listitem: The EducationLevel field is required.
  - text: 1 رب الأسرة 2 أفراد الأسرة 3 السكن 4 المراجعة
  - heading "بيانات رب الأسرة" [level=2]
  - text: الاسم الأول
  - textbox: أحمد
  - text: الاسم الثاني
  - textbox: محمد
  - text: الاسم الثالث
  - textbox: سالم
  - text: اسم العائلة
  - textbox: حسن
  - text: رقم الهوية
  - textbox: "859168411"
  - text: القاطع
  - combobox:
    - option "اختر القاطع..."
    - option "A" [selected]
    - option "B"
    - option "C"
    - option "D"
    - option "أكرم عابدين"
    - option "ابو جهاد الاخرس"
    - option "ابو حمزة عيد"
    - option "جبر الدربى"
  - text: تاريخ الميلاد
  - textbox: 1990-01-15
  - text: الجنس
  - radio "ذكر" [checked]
  - text: ذكر
  - radio "أنثى"
  - text: أنثى رقم الهاتف
  - textbox: "0596176899"
  - text: المحفظة
  - textbox: "0593416445"
  - text: نوع المحفظة
  - combobox:
    - option "اختر نوع المحفظة..."
    - option "بنك" [selected]
    - option "بال بي"
    - option "جوال بي"
  - text: مكان السكن الأصلي
  - combobox:
    - option "اختر المنطقة..."
    - option "غزة" [selected]
    - option "خانيونيس"
    - option "شمال غزة"
    - option "رفح"
    - option "دير البلح"
    - option "النصيرات"
    - option "المغازي"
  - text: الحالة الاجتماعية
  - combobox:
    - option "اختر الحالة..."
    - option "متزوج" [selected]
    - option "أعزب"
    - option "أرمل"
    - option "مطلق"
    - option "منفصل"
  - text: الوظيفة
  - textbox: موظف
  - text: المستوى التعليمي
  - combobox:
    - option "اختر..."
    - option "بدون"
    - option "ابتدائي"
    - option "إعدادي"
    - option "ثانوي"
    - option "جامعي" [selected]
    - option "دراسات عليا"
  - checkbox
  - text: المنزل مدمر
  - checkbox
  - text: أسير صورة الهوية
  - button "Choose File"
  - heading "الحالة الصحية" [level=3]
  - radio "سليم" [checked]
  - text: سليم
  - radio "مريض"
  - text: مريض
  - checkbox
  - text: هل تعرض لإصابة؟
  - checkbox
  - text: حامل
  - checkbox
  - text: مرضع
  - button "✕ مسح الكل"
  - button "التالي"
- contentinfo: © 2026 - نظام تسجيل العائلات - جميع الحقوق محفوظة
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test';
  2  | import { completeRegistration } from '../helpers/test-utils';
  3  | 
  4  | test.describe('Registration Wizard - Full Flow', () => {
  5  | 
  6  |   test('complete registration from start to success page', async ({ page }) => {
  7  |     const recordId = await completeRegistration(page);
  8  |     expect(recordId).toBeTruthy();
  9  |     expect(recordId!.length).toBe(8);
  10 |   });
  11 | 
  12 |   test('step navigation works (next, previous, step indicators)', async ({ page }) => {
  13 |     await page.goto('/Registration/Index');
  14 |     await page.waitForLoadState('networkidle');
  15 | 
  16 |     // Step indicators visible
  17 |     await expect(page.locator('#step-indicator-1')).toBeVisible();
  18 | 
  19 |     // Can go back from step 2 to step 1
  20 |     await page.locator('button:has-text("التالي")').click();
  21 |     await page.waitForLoadState('networkidle');
  22 |     await page.locator('button:has-text("السابق")').click();
  23 |     await page.waitForLoadState('networkidle');
  24 | 
  25 |     // Still on step 1
  26 |     await expect(page.locator('[name="Head.FirstName"]')).toBeVisible();
  27 |   });
  28 | 
  29 |   test('generates valid 8-char record ID', async ({ page }) => {
  30 |     const recordId = await completeRegistration(page);
  31 |     const validChars = /^[23456789ABCDEFGHJKLMNPQRSTUVWXYZ]+$/;
  32 |     expect(validChars.test(recordId!)).toBe(true);
  33 |   });
  34 | 
  35 |   test('success page shows record ID to user', async ({ page }) => {
  36 |     await completeRegistration(page);
  37 | 
  38 |     // Success page visible with record ID
> 39 |     await expect(page.locator('h1:has-text("تم التسجيل")')).toBeVisible();
     |                                                             ^ Error: expect(locator).toBeVisible() failed
  40 |   });
  41 | });
  42 | 
```