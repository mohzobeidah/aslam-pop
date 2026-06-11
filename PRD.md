# Product Requirements Document (PRD): Camp Family Registration Form

## 1. Product Overview
The **Camp Family Registration Form** is a specialized web application built as an **ASP.NET Core MVC** application (`CampRegistrationApp/`) with SQL Server, Entity Framework Core, and an extensive admin/nomination/report/financial/complaint system.

The primary purpose of the application is to collect detailed demographic, health, and socio-economic data from families residing in camp settings. The application provides a digital interface for registering the head of the family and all accompanying family members, ensuring that critical vulnerabilities (health issues, housing conditions, and unique family structures) are documented for aid distribution or administrative planning.

## 2. Target Audience
- **Field Workers/Data Collectors:** Personnel responsible for conducting surveys and registering families in camp environments.
- **Camp Administrators:** Personnel who need a structured database of registered families to manage resources and assistance.

## 3. Goals & Objectives
- **Digitize Registration:** Replace paper-based registration with a digital form to reduce errors and improve data accessibility.
- **Detailed Vulnerability Mapping:** Capture specific health/disability data and housing conditions (e.g., tent types) to identify high-priority cases.
- **Document Verification:** Allow the upload of medical reports and identity images for verification.
- **Simplified Data Management:** Structured storage in SQL Server database for easy analysis and reporting.

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
- **Member Details:** Collect name, ID, date of birth, gender, age (in months for infants), governorate, relationship to head of family, marital status, job, and education. (No phone/wallet — these are head-only.)
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
- **File Storage:** Uploaded files (medical reports, ID images) stored on local filesystem under `wwwroot/uploads/registrations/TEMP/`.
- **Data Storage:** All data stored in SQL Server database via Entity Framework Core.

## 5. Non-Functional Requirements
- **User Interface (UI):** 
    - Full Right-to-Left (RTL) support for Arabic language.
    - Mobile-responsive design for use on tablets and smartphones in the field.
    - High-contrast "Dark Theme" (Gold and Dark Grey) for better visibility.
- **Performance:** Low latency for form submissions with efficient database queries.
- **Accessibility:** Use of the 'Cairo' font for readability in Arabic.

## 6. Technical Architecture

### ASP.NET Core MVC (CampRegistrationApp/)
- **Framework:** .NET 10 MVC with Razor runtime compilation.
- **Frontend:** HTML5, Tailwind CSS (CDN), JavaScript, RTL dark theme.
- **Backend:** ASP.NET Core controllers and services.
- **Database:** SQL Server (LocalDB) via Entity Framework Core 10.
- **File Storage:** Local filesystem under `wwwroot/uploads/`.
- **Authentication:** Session-based (no ASP.NET Identity).
- **Deployment:** GitHub Actions CI/CD → Azure Web App.
- **Key Features:**
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
  - Complaint / ticket management system
  - Soft delete and restore workflow for removed families
  - Unified error pages (404, 403, 500+) with color-coded icons and tailored messages
  - Financial recorder module (planned)

## 7. Data Schema (ASP.NET Core)

### Core Tables

| Table | Description |
| :--- | :--- |
| **Persons** | Family heads and members — name (4 parts), ID, DOB, gender, health/disease/disability, injury, maternity fields, prisoner flags, BathroomStatus, MotherIdNumber |
| **FamilyRegistrations** | Links to head Person, sector, phone, wallet, housing fields (tent/bathroom), approval workflow, rejection tracking, soft delete (`IsDeleted`, `DeletedById`, `DeletedAt`) |
| **FamilyMembers** | Join table linking FamilyRegistration → Person with RelationshipToHead |
| **Attachments** | File metadata linked to Person (MedicalReport or IDImage), stores relative paths |
| **Sectors** | Camp sectors with name, camp, coordinates, area, tent/bathroom counts |
| **Admins** | Admin login with role (Admin/Mandoob), SHA256 password hashing, optional sector assignment |
| **Desires** | Available refugee desire options (aid items) |
| **FamilyDesires** | Join table — ranked desires per FamilyRegistration (Order + DesireId) |
| **Projects** | Aid campaign projects with start/end dates, required count, status, soft delete |
| **Nominations** | Person-project links per sector, approval workflow, soft delete |
| **AuditLogs** | Immutable audit trail with JSON old/new values and source tracking (Web/Mobile) |
| **Notifications** | Per-admin notification with message, link, read status |
| **Assistance** | Aid campaigns with name, type, source, date, sector, status |
| **AssistanceBeneficiaries** | Standalone beneficiary records (FullName, NationalId, Phone, SectorId, BenefitType) |
| **Complaints** | Ticket-based complaint system with status (Pending/InProgress/Resolved/Closed) |

### Key Registration Fields

| Category | Fields |
| :--- | :--- |
| **Family Head** | FirstName, SecondName, ThirdName, LastName, IdNumber, DateOfBirth, Gender, OriginalGovernorate, MaritalStatus, EmploymentStatus, EducationLevel, HealthStatus, ChronicDiseases, DisabilityTypes, Injury fields, Maternity fields, Prisoner flags, BathroomStatus, MotherIdNumber |
| **Registration** | Sector, PhoneNumber, Wallet, WalletType, StatusNotes, LivesInTent, HasBathroom, BathroomType, BathroomStatus, IsChildHeaded, IsFemaleHeaded, IsHusbandAbroad, SupportsOutsidePerson, NeedsDiapers, HasMultipleFamiliesInTent |
| **Members** | Same as Family Head fields + RelationshipToHead. No Sector/PhoneNumber/Wallet (head-only) |
| **Refugee Desires** | Ranked selections from Desires table stored as FamilyDesire join records |

## 8. Future Improvements

### ASP.NET Core Version
- **Document Verification:** Automated ID document verification using OCR.
- **Mobile App:** Native mobile app for field data collection with offline sync.
- **Advanced Analytics:** Power BI integration or built-in dashboards.
- **SMS/Email Notifications:** Automated alerts to refugees about registration status.
- **Multi-language Support:** English/Hebrew alongside Arabic.
- **Biometric Verification:** Fingerprint or facial recognition for duplicate detection.
