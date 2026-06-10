# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: admin-approve.spec.ts >> Admin Approval >> approves a pending registration
- Location: tests\admin-approve.spec.ts:7:7

# Error details

```
Test timeout of 60000ms exceeded.
```

```
Error: locator.selectOption: Test timeout of 60000ms exceeded.
Call log:
  - waiting for locator('[name="SectorId"]')
    - locator resolved to <select required="" name="SectorId" data-ls="SectorId" class="w-full p-2 rounded bg-camp-dark-grey border border-gray-600 focus:border-camp-gold outline-none">…</select>
  - attempting select option action
    2 × waiting for element to be visible and enabled
      - did not find some options
    - retrying select option action
    - waiting 20ms
    2 × waiting for element to be visible and enabled
      - did not find some options
    - retrying select option action
      - waiting 100ms
    110 × waiting for element to be visible and enabled
        - did not find some options
      - retrying select option action
        - waiting 500ms

```

# Page snapshot

```yaml
- generic [ref=e1]:
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
        - generic [ref=e20]:
          - generic [ref=e21] [cursor=pointer]:
            - generic [ref=e22]: "1"
            - generic [ref=e23]: رب الأسرة
          - generic [ref=e24] [cursor=pointer]:
            - generic [ref=e25]: "2"
            - generic [ref=e26]: أفراد الأسرة
          - generic [ref=e27] [cursor=pointer]:
            - generic [ref=e28]: "3"
            - generic [ref=e29]: السكن
          - generic [ref=e30] [cursor=pointer]:
            - generic [ref=e31]: "4"
            - generic [ref=e32]: المراجعة
        - generic [ref=e33]:
          - generic [ref=e34]:
            - heading "بيانات رب الأسرة" [level=2] [ref=e35]
            - generic [ref=e36]:
              - generic [ref=e37]:
                - generic [ref=e38]: الاسم الأول
                - textbox [ref=e39]: أحمد
              - generic [ref=e40]:
                - generic [ref=e41]: الاسم الثاني
                - textbox [ref=e42]: محمد
              - generic [ref=e43]:
                - generic [ref=e44]: الاسم الثالث
                - textbox [ref=e45]: سالم
              - generic [ref=e46]:
                - generic [ref=e47]: اسم العائلة
                - textbox [ref=e48]: حسن
            - generic [ref=e49]:
              - generic [ref=e50]:
                - generic [ref=e51]: رقم الهوية
                - textbox [active] [ref=e52]: "033430729"
              - generic [ref=e53]:
                - generic [ref=e54]: القاطع
                - combobox [ref=e55]:
                  - option "اختر القاطع..." [selected]
                  - option "مخيم السلام قاطع A-جبر الدربى"
                  - option "مخيم السلام قاطع B-ابو جهاد الاخرس"
                  - option "مخيم السلام قاطع C-أكرم عابدين"
                  - option "مخيم السلام قاطع D-اياد فايق عبد الحليم جودة"
              - generic [ref=e56]:
                - generic [ref=e57]: تاريخ الميلاد
                - textbox [ref=e58]
              - generic [ref=e59]:
                - generic [ref=e60]: الجنس
                - generic [ref=e61]:
                  - generic [ref=e62] [cursor=pointer]:
                    - radio "ذكر" [checked] [ref=e63]
                    - text: ذكر
                  - generic [ref=e64] [cursor=pointer]:
                    - radio "أنثى" [ref=e65]
                    - text: أنثى
              - generic [ref=e66]:
                - generic [ref=e67]: رقم الهاتف
                - textbox [ref=e68]
              - generic [ref=e69]:
                - generic [ref=e70]: المحفظة
                - textbox [ref=e71]
              - generic [ref=e72]:
                - generic [ref=e73]: نوع المحفظة
                - combobox [ref=e74]:
                  - option "اختر نوع المحفظة..." [selected]
                  - option "بنك"
                  - option "بال بي"
                  - option "جوال بي"
              - generic [ref=e75]:
                - generic [ref=e76]: مكان السكن الأصلي
                - combobox [ref=e77]:
                  - option "اختر المنطقة..." [selected]
                  - option "غزة"
                  - option "خانيونيس"
                  - option "شمال غزة"
                  - option "رفح"
                  - option "دير البلح"
                  - option "النصيرات"
                  - option "المغازي"
              - generic [ref=e78]:
                - generic [ref=e79]: الحالة الاجتماعية
                - combobox [ref=e80]:
                  - option "اختر الحالة..." [selected]
                  - option "متزوج"
                  - option "أعزب"
                  - option "أرمل"
                  - option "مطلق"
                  - option "منفصل"
              - generic [ref=e81]:
                - generic [ref=e82]: الوظيفة
                - textbox [ref=e83]
              - generic [ref=e84]:
                - generic [ref=e85]: المستوى التعليمي
                - combobox [ref=e86]:
                  - option "اختر..." [selected]
                  - option "بدون"
                  - option "ابتدائي"
                  - option "إعدادي"
                  - option "ثانوي"
                  - option "جامعي"
                  - option "دراسات عليا"
              - generic [ref=e87]:
                - checkbox [ref=e88]
                - generic [ref=e89]: المنزل مدمر
              - generic [ref=e90]:
                - checkbox [ref=e91]
                - generic [ref=e92]: أسير
            - generic [ref=e93]:
              - generic [ref=e94]: صورة الهوية
              - button "Choose File" [ref=e95]
            - generic [ref=e96]:
              - heading "الحالة الصحية" [level=3] [ref=e97]
              - generic [ref=e98]:
                - generic [ref=e99] [cursor=pointer]:
                  - radio "سليم" [checked] [ref=e100]
                  - text: سليم
                - generic [ref=e101] [cursor=pointer]:
                  - radio "مريض" [ref=e102]
                  - text: مريض
              - generic [ref=e104]:
                - checkbox [ref=e105]
                - generic [ref=e106]: هل تعرض لإصابة؟
              - generic [ref=e107]:
                - checkbox [ref=e108]
                - generic [ref=e109]: حامل
              - generic [ref=e110]:
                - checkbox [ref=e111]
                - generic [ref=e112]: مرضع
          - generic [ref=e114]:
            - button "✕ مسح الكل" [ref=e115] [cursor=pointer]
            - button "التالي" [ref=e116] [cursor=pointer]
  - contentinfo [ref=e117]:
    - generic [ref=e118]: © 2026 - نظام تسجيل العائلات - جميع الحقوق محفوظة
```

# Test source

```ts
  1   | import { Page, expect } from '@playwright/test';
  2   | import { RegistrationStep1, RegistrationStep3, MemberFields, Btn, Modal, Msg } from './selectors';
  3   | 
  4   | /** Generate a valid 9-digit Palestinian ID with correct check digit */
  5   | export function generatePalestinianId(): string {
  6   |   const base = Array.from({ length: 8 }, () => Math.floor(Math.random() * 10)).join('');
  7   |   const weights = [1, 2, 1, 2, 1, 2, 1, 2];
  8   |   let sum = 0;
  9   |   for (let i = 0; i < 8; i++) {
  10  |     const product = parseInt(base[i]) * weights[i];
  11  |     sum += product >= 10 ? (product % 10) + 1 : product;
  12  |   }
  13  |   const checkDigit = (10 - (sum % 10)) % 10;
  14  |   return base + checkDigit;
  15  | }
  16  | 
  17  | /** Generate unique ID for test records */
  18  | let counter = Date.now();
  19  | export function uniqueId(prefix = 'TEST'): string {
  20  |   return `${prefix}${counter++}`.slice(0, 20);
  21  | }
  22  | 
  23  | /** Fill Step 1 (Head Info) of the registration wizard */
  24  | export async function fillStep1(page: Page, overrides: Record<string, string> = {}) {
  25  |   const fields: Record<string, string> = {
  26  |     [RegistrationStep1.firstName]: 'أحمد',
  27  |     [RegistrationStep1.secondName]: 'محمد',
  28  |     [RegistrationStep1.thirdName]: 'سالم',
  29  |     [RegistrationStep1.lastName]: 'حسن',
  30  |     [RegistrationStep1.idNumber]: generatePalestinianId(),
  31  |     [RegistrationStep1.sector]: '1',
  32  |     [RegistrationStep1.dateOfBirth]: '1990-01-15',
  33  |     [RegistrationStep1.phoneNumber]: '059' + Math.floor(1000000 + Math.random() * 9000000).toString(),
  34  |     [RegistrationStep1.wallet]: '059' + Math.floor(1000000 + Math.random() * 9000000).toString(),
  35  |     [RegistrationStep1.walletType]: 'بنك',
  36  |     [RegistrationStep1.originalGovernorate]: 'غزة',
  37  |     [RegistrationStep1.maritalStatus]: 'متزوج',
  38  |     [RegistrationStep1.employmentStatus]: 'موظف',
  39  |     [RegistrationStep1.educationLevel]: 'جامعي',
  40  |     [RegistrationStep1.gender]: 'ذكر',
  41  |     [RegistrationStep1.healthStatus]: 'سليم',
  42  |     ...overrides,
  43  |   };
  44  | 
  45  |   const selectFields = new Set([
  46  |     RegistrationStep1.sector,
  47  |     RegistrationStep1.walletType,
  48  |     RegistrationStep1.originalGovernorate,
  49  |     RegistrationStep1.maritalStatus,
  50  |     RegistrationStep1.educationLevel,
  51  |   ]);
  52  | 
  53  |   const radioFields = new Set([
  54  |     RegistrationStep1.gender,
  55  |     RegistrationStep1.healthStatus,
  56  |   ]);
  57  | 
  58  |   for (const [name, value] of Object.entries(fields)) {
  59  |     if (selectFields.has(name)) {
  60  |       const el = page.locator(`[name="${name}"]`);
> 61  |       if (await el.count()) await el.selectOption(value);
      |                                      ^ Error: locator.selectOption: Test timeout of 60000ms exceeded.
  62  |     } else if (radioFields.has(name)) {
  63  |       const el = page.locator(`[name="${name}"][value="${value}"]`);
  64  |       if (await el.count()) await el.check();
  65  |     } else {
  66  |       const el = page.locator(`[name="${name}"]`);
  67  |       if (await el.count()) {
  68  |         await el.fill('');
  69  |         await el.fill(value);
  70  |       }
  71  |     }
  72  |   }
  73  | }
  74  | 
  75  | /** Fill Step 2: add a family member */
  76  | const memberRadioFields = new Set(['Gender', 'HealthStatus']);
  77  | 
  78  | function isMemberRadioField(name: string): boolean {
  79  |   return memberRadioFields.has(name.split('.').pop() || '');
  80  | }
  81  | 
  82  | export async function addMember(page: Page, index: number, overrides: Record<string, string> = {}) {
  83  |   const fields: Record<string, string> = {
  84  |     [MemberFields.firstName(index)]: 'فاطمة',
  85  |     [MemberFields.secondName(index)]: 'أحمد',
  86  |     [MemberFields.thirdName(index)]: 'خالد',
  87  |     [MemberFields.lastName(index)]: 'حسن',
  88  |     [MemberFields.idNumber(index)]: generatePalestinianId(),
  89  |     [MemberFields.relationship(index)]: 'زوجة',
  90  |     [MemberFields.gender(index)]: 'أنثى',
  91  |     [MemberFields.dateOfBirth(index)]: '1995-06-20',
  92  |     [MemberFields.maritalStatus(index)]: 'متزوج',
  93  |     [MemberFields.healthStatus(index)]: 'سليم',
  94  |     ...overrides,
  95  |   };
  96  | 
  97  |   for (const [name, value] of Object.entries(fields)) {
  98  |     const el = page.locator(`[name="${name}"]`);
  99  |     if (await el.count()) {
  100 |       if (isMemberRadioField(name)) {
  101 |         const radio = page.locator(`[name="${name}"][value="${value}"]`);
  102 |         if (await radio.count()) await radio.check();
  103 |       } else {
  104 |         const tag = await el.first().evaluate(e => e.tagName);
  105 |         if (tag === 'SELECT' || tag === 'select') await el.first().selectOption(value);
  106 |         else {
  107 |           await el.first().fill('');
  108 |           await el.first().fill(value);
  109 |         }
  110 |       }
  111 |     }
  112 |   }
  113 | }
  114 | 
  115 | /** Fill Step 3: housing, bathroom, and desires */
  116 | export async function fillStep3(page: Page, overrides: Record<string, string> = {}) {
  117 |   const fields: Record<string, string> = {
  118 |     [RegistrationStep3.livesInTent]: 'true',
  119 |     [RegistrationStep3.tentType]: 'خيمة صينية',
  120 |     [RegistrationStep3.hasBathroom]: 'true',
  121 |     [RegistrationStep3.bathroomType]: 'Private',
  122 |     [RegistrationStep3.bathroomStatus]: 'جيد',
  123 |     ...overrides,
  124 |   };
  125 | 
  126 |   const selectFields = new Set([
  127 |     RegistrationStep3.tentType,
  128 |     RegistrationStep3.bathroomStatus,
  129 |   ]);
  130 | 
  131 |   const radioFields = new Set([
  132 |     RegistrationStep3.livesInTent,
  133 |     RegistrationStep3.hasBathroom,
  134 |     RegistrationStep3.bathroomType,
  135 |   ]);
  136 | 
  137 |   for (const [name, value] of Object.entries(fields)) {
  138 |     if (selectFields.has(name)) {
  139 |       const el = page.locator(`[name="${name}"]`);
  140 |       if (await el.first().count()) await el.first().selectOption(value);
  141 |     } else if (radioFields.has(name)) {
  142 |       const el = page.locator(`[name="${name}"][value="${value}"]`);
  143 |       if (await el.first().count()) await el.first().check();
  144 |     } else {
  145 |       const el = page.locator(`[name="${name}"]`);
  146 |       if (await el.first().count()) {
  147 |         await el.first().fill('');
  148 |         await el.first().fill(value);
  149 |       }
  150 |     }
  151 |   }
  152 | 
  153 |   // Set desires as individual selects (5 ranked dropdowns)
  154 |   const desireValues = [1, 2, 3, 4, 5];
  155 |   for (let i = 0; i < desireValues.length; i++) {
  156 |     const el = page.locator(`[name="Desires[${i}]"]`);
  157 |     if (await el.count()) {
  158 |       await el.evaluate((sel: Element, val: number) => {
  159 |         const select = sel as HTMLSelectElement;
  160 |         const option = Array.from(select.options).find(o => parseInt(o.value) === val);
  161 |         if (option) select.value = option.value;
```