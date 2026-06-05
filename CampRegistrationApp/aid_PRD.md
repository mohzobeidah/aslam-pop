# نظام إدارة المساعدات والمستفيدين
## الحالة: ✅ منفذ بالكامل (مدمج في ASP.NET Core MVC)

> **ملاحظة:** هذا المستند هو الـ PRD الأصلي. تم تنفيذ الميزة بالكامل في `CampRegistrationApp/` مع
> نماذج `Assistance` و `AssistanceBeneficiary` و `AssistanceImport`،
> وخدمات `AssistanceService` و `ImportService`،
> ووحدة تحكم `AssistanceController` مع CRUD كامل واستيراد Excel وتصدير.

– نظام إدارة المساعدات والمستفيدين
اسم الميزة

نظام إدارة المساعدات والمستفيدين مع دعم الإدخال اليدوي والاستيراد عبر Excel والتدقيق الكامل (Auditing)

1. الهدف من النظام

إنشاء نظام لإدارة المساعدات والمستفيدين بحيث يمكن:

إضافة المساعدات وتحديد مصدرها وتاريخها.
إضافة المستفيدين وربطهم بالمساعدة.
رفع المستفيدين عبر ملفات Excel.
عرض وإدارة البيانات داخل النظام.
تطبيق صلاحيات حسب القاطع (Sector).
تسجيل جميع العمليات داخل Audit Log بشكل كامل.
2. أنواع المستخدمين والصلاحيات
الدور	الصلاحيات
Admin	إدارة كاملة لجميع البيانات في كل القواطع
Mandoub / Data Entry	إدارة البيانات الخاصة بقاطعه فقط
Viewer	مشاهدة البيانات فقط
3. صلاحيات Admin

يمكن للأدمن:

إضافة وتعديل وحذف أي مساعدة.
إضافة وتعديل وحذف أي مستفيد.
مشاهدة جميع القواطع.
رفع ملفات Excel لأي قاطع.
اعتماد وإلغاء اعتماد السجلات.
مشاهدة جميع الـ Audit Logs.
تصدير البيانات والتقارير.
إدارة المستخدمين والصلاحيات.
4. صلاحيات المندوب (Mandoub / Data Entry)

المندوب يكون مرتبط بقاطع محدد فقط (Sector).

يمكنه:
إضافة مساعدات ضمن القاطع الخاص به.
إضافة مستفيدين ضمن قطاعه فقط.
تعديل البيانات الخاصة بقاطعه.
حذف السجلات الخاصة بقاطعه.
رفع ملفات Excel لقاطعه فقط.
مشاهدة بيانات القاطع الخاص به فقط.
لا يمكنه:
مشاهدة بيانات القواطع الأخرى.
تعديل أو حذف بيانات قاطع آخر.
اعتماد السجلات.
إلغاء الاعتماد.
مشاهدة Audit Logs الكاملة.
5. إدارة المساعدات
صفحة إضافة مساعدة

يقوم المستخدم بإضافة مساعدة جديدة تحتوي على:

الحقل	النوع
اسم المساعدة	Text
نوع المساعدة	Dropdown
مصدر المساعدة	Text
تاريخ المساعدة	Date
وصف المساعدة	TextArea
القاطع	Auto حسب المستخدم
حالة المساعدة	Draft / Approved / Cancelled
مرفقات	File Upload
6. إدارة المستفيدين

يمكن إضافة المستفيدين بطريقتين:

1. الإضافة اليدوية

عبر نموذج داخل النظام.

2. الاستيراد عبر Excel

رفع ملف Excel يحتوي على بيانات المستفيدين.

7. نموذج إضافة مستفيد
الحقل	النوع
الاسم الكامل	Text
رقم الهوية	Text
رقم الجوال	Text
رقم الملف	Text
اسم العائلة	Text
المحافظة	Dropdown
القاطع	Auto حسب المستخدم
عدد أفراد الأسرة	Integer
نوع الاستفادة	Dropdown
ملاحظات	TextArea
8. رفع ملف Excel
صفحة الاستيراد

يمكن للمستخدم رفع ملف Excel لإضافة المستفيدين دفعة واحدة.

الأعمدة المطلوبة داخل الملف
العمود
FullName
NationalId
Phone
FileNumber
FamilyName
Sector
City
FamilyCount
9. التحقق من البيانات أثناء الاستيراد

يجب التحقق من:

عدم تكرار رقم الهوية.
صحة رقم الجوال.
عدم وجود حقول فارغة.
صحة تنسيق Excel.
عدم إضافة بيانات لقاطع آخر بواسطة المندوب.
منع التكرار داخل نفس المساعدة.
10. عرض البيانات
صفحة المستفيدين

يتم عرض البيانات داخل جدول يحتوي على:

العمود
الاسم
رقم الهوية
الجوال
اسم المساعدة
مصدر المساعدة
تاريخ المساعدة
المحافظة
القاطع
المستخدم الذي أضاف السجل
تاريخ الإضافة
حالة السجل
11. الفلاتر والبحث

يدعم النظام:

البحث بالاسم.
البحث برقم الهوية.
البحث بالجوال.
البحث بالقاطع.
البحث بالمحافظة.
البحث بنوع المساعدة.
البحث بتاريخ المساعدة.
12. حالات السجل
الحالة	الوصف
Draft	مسودة
Submitted	تم الإدخال
Approved	معتمد
Rejected	مرفوض
Cancelled	ملغي
13. قواعد العمل (Business Rules)
للمندوب
جميع العمليات مرتبطة بقاطعه فقط.
لا يمكنه الوصول لبيانات قاطع آخر.
لا يمكنه تعديل سجل معتمد.
الحذف يكون Soft Delete فقط.
للأدمن
يمكنه إدارة جميع القواطع.
يمكنه تعديل السجلات المعتمدة.
يمكنه استرجاع السجلات المحذوفة.
يمكنه تجاوز القيود مع تسجيل Audit كامل.
14. الـ Auditing المطلوب

يجب تسجيل جميع العمليات التالية:

العملية
إنشاء مساعدة
تعديل مساعدة
حذف مساعدة
إضافة مستفيد
تعديل مستفيد
حذف مستفيد
رفع Excel
فشل الاستيراد
تنزيل التقارير
اعتماد السجلات
إلغاء الاعتماد
تسجيل الدخول والخروج
تجاوز القيود
15. تفاصيل الـ Audit Log
الحقل	الوصف
المستخدم	User
الدور	Role
القاطع	Sector
العملية	Action
اسم الجدول	TableName
رقم السجل	RecordId
القيم القديمة	OldValues
القيم الجديدة	NewValues
عنوان IP	IPAddress
نوع العملية	Web / Excel / API
وقت العملية	Timestamp
16. قاعدة البيانات المقترحة
جدول المستخدمين
Users
Column
Id
FullName
Role
SectorId
IsActive
جدول المساعدات
Assistance
Column
Id
Name
AssistanceType
Source
AssistanceDate
Description
SectorId
Status
CreatedBy
CreatedAt
جدول المستفيدين
AssistanceBeneficiaries
Column
Id
AssistanceId
FullName
NationalId
Phone
FileNumber
FamilyName
City
SectorId
FamilyCount
BenefitType
Status
Notes
CreatedBy
CreatedAt
جدول الاستيراد
AssistanceImports
Column
Id
FileName
ImportedBy
SectorId
ImportedAt
TotalRows
SuccessRows
FailedRows
DuplicateRows
جدول التدقيق
AuditLogs
Column
Id
UserId
Role
SectorId
Action
TableName
RecordId
OldValues
NewValues
IPAddress
Source
CreatedAt
17. الـ APIs المطلوبة
Assistance APIs
Create Assistance
Update Assistance
Delete Assistance
Get Assistance
Get Assistance Details
Beneficiaries APIs
Add Beneficiary
Update Beneficiary
Delete Beneficiary
Get Beneficiaries
Search Beneficiaries
Excel APIs
Upload Excel File
Validate Excel
Import Beneficiaries
Download Template
Download Error Report
18. المتطلبات التقنية
Frontend
DataTables
Select2
Excel Upload Component
Validation Messages
Pagination
Advanced Filters
Export Excel / PDF
Backend
ASP.NET Core / MVC
SQL Server
Repository Pattern
Role-Based Authorization
Auditing Middleware
Background Jobs للاستيراد الكبير
19. التقارير المطلوبة
تقرير عدد المستفيدين لكل مساعدة.
تقرير حسب القاطع.
تقرير حسب المحافظة.
تقرير التكرار.
تقرير التعديلات والحذف.
تقرير عمليات الاستيراد.
تقرير Audit كامل.
تقرير نشاط المستخدمين.
20. ملاحظات مهمة للتنفيذ
جميع العمليات يجب أن تكون Transactional.
استخدام Soft Delete للحذف.
منع التكرار حسب رقم الهوية.
حفظ ملفات Excel المرفوعة.
جميع العمليات تسجل داخل Audit Log.
تطبيق Sector Filtering تلقائ