# Product Requirements Document (PRD): Camp Family Registration Form

## 1. Product Overview
The **Camp Family Registration Form** is a specialized web application available in two versions. The **original version** was developed using Google Apps Script for rapid deployment in camp settings. The system has since been expanded into a full **ASP.NET Core MVC** application (`CampRegistrationApp/`) with SQL Server, Entity Framework Core, and an extensive admin/nomination/report/financial system.

The primary purpose of both versions is to collect detailed demographic, health, and socio-economic data from families residing in camp settings. The application provides a digital interface for registering the head of the family and all accompanying family members, ensuring that critical vulnerabilities (health issues, housing conditions, and unique family structures) are documented for aid distribution or administrative planning.

## 2. Target Audience
- **Field Workers/Data Collectors:** Personnel responsible for conducting surveys and registering families in camp environments.
- **Camp Administrators:** Personnel who need a structured database of registered families to manage resources and assistance.

## 3. Goals & Objectives
- **Digitize Registration:** Replace paper-based registration with a digital form to reduce errors and improve data accessibility.
- **Detailed Vulnerability Mapping:** Capture specific health/disability data and housing conditions (e.g., tent types) to identify high-priority cases.
- **Document Verification:** Allow the upload of medical reports and identity documents directly to a centralized cloud storage (Google Drive).
- **Simplified Data Management:** Automate the storage of registration data into a Google Sheet for easy analysis and reporting.

## 4. Functional Requirements

### 4.1 Family Head Registration
- **Personal Information:** Collect full name (four parts), ID number, sector (A, B, C, D), date of birth, gender, phone number, and original governorate.
- **Socio-Economic Data:** Capture marital status, employment status, and educational qualification.
- **Health Profile:**
    - Toggle between "Healthy" and "Sick".
    - For "Sick" profiles: Collect chronic diseases (Diabetes, Hypertension, Heart, Asthma, Kidney Failure, Cancer, Thalassemia, Other) and disability types (Motor, Hearing, Visual, War Injury).
    - Injury Tracking: If an injury occurred, collect the date and detailed description.
- **Document Upload:** Support uploading a medical report and an ID image.

### 4.2 Family Member Registration
- **Dynamic Addition:** Ability to add multiple family members to a single registration.
- **Member Details:** Collect name, ID, date of birth, gender, age (in months for infants), phone, governorate, relationship to head of family, marital status, job, and education.
- **Member Health Profile:** Same health tracking capabilities as the family head (diseases, disabilities, injuries).
- **Gender-Specific Data:** 
    - For female members: Track pregnancy (with month) and nursing status.
    - For nursing mothers: Collect the infant's name, date of birth (with auto-calculated age in months), and ID.
- **Member Documents:** Support uploading medical reports and ID images per member.

### 4.3 Housing & Special Cases
- **Housing Conditions:** 
    - Track if the family lives in a tent.
    - If yes, specify tent type (Installation, Dome, Emirati, Qatari, Other) and the presence/type of bathroom (Private, Shared).
- **Special Household Structures:**
    - **Child-Headed Household:** Flag and document details if a child is the head of the family.
    - **Female-Headed Household:** Flag and document details if a woman is the head of the family.
    - **External Support:** Flag and document if the family head supports a person outside the immediate family.

### 4.4 Data Processing & Storage
- **Unique Identification:** Generate a unique 8-character Record ID for every successful registration.
- **Cloud Storage:** Upload all files to a dedicated Google Drive folder named "التقارير الطبية".
- **Spreadsheet Integration:** Append all collected data as a single row in a Google Sheet named "البيانات".

## 5. Non-Functional Requirements
- **User Interface (UI):** 
    - Full Right-to-Left (RTL) support for Arabic language.
    - Mobile-responsive design for use on tablets and smartphones in the field.
    - High-contrast "Dark Theme" (Gold and Dark Grey) for better visibility.
- **Performance:** Low latency for form submissions, leveraging Google's infrastructure.
- **Accessibility:** Use of the 'Cairo' font for readability in Arabic.

## 6. Technical Architecture

### Version 1: Google Apps Script (GAS)
- **Frontend:** HTML5, CSS3 (Tailwind CSS), JavaScript (Client-side).
- **Backend:** Google Apps Script (GAS).
- **Database:** Google Sheets (as a flat-file database).
- **File Storage:** Google Drive.
- **Deployment:** Google Apps Script Web App.

### Version 2: ASP.NET Core MVC (CampRegistrationApp/)
- **Framework:** .NET 10 MVC with Razor runtime compilation.
- **Frontend:** HTML5, Tailwind CSS (CDN), JavaScript, RTL dark theme.
- **Backend:** ASP.NET Core controllers and services.
- **Database:** SQL Server (LocalDB) via Entity Framework Core 10.
- **File Storage:** Local filesystem under `wwwroot/uploads/`.
- **Authentication:** Session-based (no ASP.NET Identity).
- **Deployment:** GitHub Actions CI/CD → Azure Web App.
- **Key Features Added Beyond GAS:**
  - Admin panel with role-based access (Admin, Mandoob)
  - Registration approval workflow (Pending/Approved/Rejected) with rejection reasons
  - Family member management, dynamic add/remove
  - Refugee desires (ranked dropdowns)
  - Project & nomination system for aid distribution
  - Report system with dynamic Excel export (ClosedXML)
  - Detailed audit logging (JSON diffs via RegistrationChangeTracker)
  - Notification system (bell icon polling)
  - Assistance/beneficiary management with Excel import
  - Force password change (if password matches ID)
  - Dashboard with CTE-based demographic statistics
  - Complaint management system
  - Financial recorder module

## 7. Data Schema
| Column Name | Description |
| :--- | :--- |
| وقت التسجيل | Registration Timestamp |
| الاسم الأول...العائلة | Head of Family Full Name |
| رقم الهوية | ID Number |
| القاطع | Sector (A/B/C/D) |
| تاريخ الميلاد | Date of Birth |
| الجنس | Gender |
| رقم الجوال | Phone Number |
| المحافظة الأصلية | Original Governorate |
| الحالة الاجتماعية | Marital Status |
| الحالة الوظيفية | Job Status |
| المؤهل العلمي | Education Level |
| الحالة الصحية | Health Status (Healthy/Sick) |
| الأمراض المزمنة | List of chronic diseases |
| أنواع الإعاقة | Types of disabilities |
| هل تعرض لإصابة | Injury history (Yes/No) |
| تاريخ الإصابة | Date of injury |
| تفاصيل الإصابة | Injury details |
| رابط التقرير الطبي | URL to medical report in Drive |
| رابط صورة الهوية | URL to ID image in Drive |
| يعيل أسرتك طفل | Child-headed flag (Yes/No) |
| تفاصيل الطفل | Child-head details |
| تعيل أسرتك امرأة | Female-headed flag (Yes/No) |
| تفاصيل المرأة | Female-head details |
| يعيل شخص خارج العائلة | Supports outside person (Yes/No) |
| اسم الشخص | Outside person name |
| صلة القرابة | Relationship to outside person |
| يسكن خيمة | Lives in tent (Yes/No) |
| نوع الخيمة | Tent type |
| نوع الخيمة أخرى | Other tent type description |
| يوجد حمام | Bathroom exists (Yes/No) |
| نوع الحمام | Bathroom type (Private/Shared) |
| أفراد الأسرة (JSON) | JSON string containing all members' data |
| معرف التسجيل | Unique Record ID |

## 8. Future Improvements

### GAS Version
- **Input Validation:** Implement more robust server-side validation to prevent data corruption.
- **Data Sanitization:** Ensure user inputs are sanitized before being written to the spreadsheet.
- **File Type Handling:** Improve `uploadFileToDrive` to detect and set the correct MimeType for images vs PDFs.
- **Dynamic Member IDs:** Replace index-based naming for member health fields to allow safe deletion of members during registration.
- **Offline Support:** Implement local storage caching to allow data collection in areas with poor internet connectivity, syncing when online.

### ASP.NET Core Version
- **Document Verification:** Automated ID document verification using OCR.
- **Mobile App:** Native mobile app for field data collection with offline sync.
- **Advanced Analytics:** Power BI integration or built-in dashboards.
- **SMS/Email Notifications:** Automated alerts to refugees about registration status.
- **Multi-language Support:** English/Hebrew alongside Arabic.
- **Biometric Verification:** Fingerprint or facial recognition for duplicate detection.
