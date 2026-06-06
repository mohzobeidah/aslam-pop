# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: registration-flow.spec.ts >> Registration Wizard - Full Flow >> generates valid 8-char record ID
- Location: tests\registration-flow.spec.ts:29:7

# Error details

```
Error: expect(received).toBe(expected) // Object.is equality

Expected: true
Received: false
```

# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - banner [ref=e2]:
    - navigation [ref=e3]:
      - generic [ref=e4]:
        - link "نظام تسجيل العائلات" [ref=e5] [cursor=pointer]:
          - /url: /
        - generic [ref=e6]:
          - button "☀️" [ref=e7] [cursor=pointer]
          - link "تسجيل جديد" [ref=e8] [cursor=pointer]:
            - /url: /Registration
          - link "تسجيل الدخول" [ref=e9] [cursor=pointer]:
            - /url: /Record/Login
          - link "تقديم شكوى" [ref=e10] [cursor=pointer]:
            - /url: /Complaint/Create
          - link "دخول المشرفين" [ref=e11] [cursor=pointer]:
            - /url: /Admin/Login
  - main [ref=e13]:
    - generic [ref=e15]:
      - generic [ref=e16]:
        - heading "نموذج تسجيل العائلات" [level=1] [ref=e17]
        - paragraph [ref=e18]: يتم حفظ البيانات تلقائياً في المتصفح
      - generic [ref=e19]:
        - list [ref=e21]:
          - listitem [ref=e22]: The EducationLevel field is required.
        - generic [ref=e23]:
          - generic [ref=e24] [cursor=pointer]:
            - generic [ref=e25]: "1"
            - generic [ref=e26]: رب الأسرة
          - generic [ref=e27] [cursor=pointer]:
            - generic [ref=e28]: "2"
            - generic [ref=e29]: أفراد الأسرة
          - generic [ref=e30] [cursor=pointer]:
            - generic [ref=e31]: "3"
            - generic [ref=e32]: السكن
          - generic [ref=e33] [cursor=pointer]:
            - generic [ref=e34]: "4"
            - generic [ref=e35]: المراجعة
        - generic [ref=e36]:
          - generic [ref=e37]:
            - heading "بيانات رب الأسرة" [level=2] [ref=e38]
            - generic [ref=e39]:
              - generic [ref=e40]:
                - generic [ref=e41]: الاسم الأول
                - textbox [ref=e42]: أحمد
              - generic [ref=e43]:
                - generic [ref=e44]: الاسم الثاني
                - textbox [ref=e45]: محمد
              - generic [ref=e46]:
                - generic [ref=e47]: الاسم الثالث
                - textbox [ref=e48]: سالم
              - generic [ref=e49]:
                - generic [ref=e50]: اسم العائلة
                - textbox [ref=e51]: حسن
            - generic [ref=e52]:
              - generic [ref=e53]:
                - generic [ref=e54]: رقم الهوية
                - textbox [ref=e55]: "603803073"
              - generic [ref=e56]:
                - generic [ref=e57]: القاطع
                - combobox [ref=e58]:
                  - option "اختر القاطع..."
                  - option "A" [selected]
                  - option "B"
                  - option "C"
                  - option "D"
                  - option "أكرم عابدين"
                  - option "ابو جهاد الاخرس"
                  - option "ابو حمزة عيد"
                  - option "جبر الدربى"
              - generic [ref=e59]:
                - generic [ref=e60]: تاريخ الميلاد
                - textbox [ref=e61]: 1990-01-15
              - generic [ref=e62]:
                - generic [ref=e63]: الجنس
                - generic [ref=e64]:
                  - generic [ref=e65] [cursor=pointer]:
                    - radio "ذكر" [checked] [ref=e66]
                    - text: ذكر
                  - generic [ref=e67] [cursor=pointer]:
                    - radio "أنثى" [ref=e68]
                    - text: أنثى
              - generic [ref=e69]:
                - generic [ref=e70]: رقم الهاتف
                - textbox [ref=e71]: "0594300592"
              - generic [ref=e72]:
                - generic [ref=e73]: المحفظة
                - textbox [ref=e74]: "0598457059"
              - generic [ref=e75]:
                - generic [ref=e76]: نوع المحفظة
                - combobox [ref=e77]:
                  - option "اختر نوع المحفظة..."
                  - option "بنك" [selected]
                  - option "بال بي"
                  - option "جوال بي"
              - generic [ref=e78]:
                - generic [ref=e79]: مكان السكن الأصلي
                - combobox [ref=e80]:
                  - option "اختر المنطقة..."
                  - option "غزة" [selected]
                  - option "خانيونيس"
                  - option "شمال غزة"
                  - option "رفح"
                  - option "دير البلح"
                  - option "النصيرات"
                  - option "المغازي"
              - generic [ref=e81]:
                - generic [ref=e82]: الحالة الاجتماعية
                - combobox [ref=e83]:
                  - option "اختر الحالة..."
                  - option "متزوج" [selected]
                  - option "أعزب"
                  - option "أرمل"
                  - option "مطلق"
                  - option "منفصل"
              - generic [ref=e84]:
                - generic [ref=e85]: الوظيفة
                - textbox [ref=e86]: موظف
              - generic [ref=e87]:
                - generic [ref=e88]: المستوى التعليمي
                - combobox [ref=e89]:
                  - option "اختر..."
                  - option "بدون"
                  - option "ابتدائي"
                  - option "إعدادي"
                  - option "ثانوي"
                  - option "جامعي" [selected]
                  - option "دراسات عليا"
              - generic [ref=e90]:
                - checkbox [ref=e91]
                - generic [ref=e92]: المنزل مدمر
              - generic [ref=e93]:
                - checkbox [ref=e94]
                - generic [ref=e95]: أسير
            - generic [ref=e96]:
              - generic [ref=e97]: صورة الهوية
              - button "Choose File" [ref=e98]
            - generic [ref=e99]:
              - heading "الحالة الصحية" [level=3] [ref=e100]
              - generic [ref=e101]:
                - generic [ref=e102] [cursor=pointer]:
                  - radio "سليم" [checked] [ref=e103]
                  - text: سليم
                - generic [ref=e104] [cursor=pointer]:
                  - radio "مريض" [ref=e105]
                  - text: مريض
              - generic [ref=e107]:
                - checkbox [ref=e108]
                - generic [ref=e109]: هل تعرض لإصابة؟
              - generic [ref=e110]:
                - checkbox [ref=e111]
                - generic [ref=e112]: حامل
              - generic [ref=e113]:
                - checkbox [ref=e114]
                - generic [ref=e115]: مرضع
          - generic [ref=e117]:
            - button "✕ مسح الكل" [ref=e118] [cursor=pointer]
            - button "التالي" [ref=e119] [cursor=pointer]
  - contentinfo [ref=e120]:
    - generic [ref=e121]: © 2026 - نظام تسجيل العائلات - جميع الحقوق محفوظة
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
> 32 |     expect(validChars.test(recordId!)).toBe(true);
     |                                        ^ Error: expect(received).toBe(expected) // Object.is equality
  33 |   });
  34 | 
  35 |   test('success page shows record ID to user', async ({ page }) => {
  36 |     await completeRegistration(page);
  37 | 
  38 |     // Success page visible with record ID
  39 |     await expect(page.locator('h1:has-text("تم التسجيل")')).toBeVisible();
  40 |   });
  41 | });
  42 | 
```