// Form field names for the registration wizard
export const RegistrationStep1 = {
  firstName: 'Head.FirstName',
  secondName: 'Head.SecondName',
  thirdName: 'Head.ThirdName',
  lastName: 'Head.LastName',
  idNumber: 'Head.IdNumber',
  sector: 'SectorId',
  dateOfBirth: 'Head.DateOfBirth',
  phoneNumber: 'PhoneNumber',
  wallet: 'Wallet',
  walletType: 'WalletType',
  originalGovernorate: 'Head.OriginalGovernorate',
  maritalStatus: 'Head.MaritalStatus',
  employmentStatus: 'Head.EmploymentStatus',
  educationLevel: 'Head.EducationLevel',
  gender: 'Head.Gender',
  healthStatus: 'Head.HealthStatus',
  hasChronicDiseases: 'Head.HasChronicDiseases',
  chronicDiseases: 'Head.ChronicDiseases',
  hasDisability: 'Head.HasDisability',
  disabilityTypes: 'Head.DisabilityTypes',
  needsDiapers: 'Head.NeedsDiapers',
  hasMaternity: 'Head.HasMaternity',
  isPregnant: 'Head.IsPregnant',
  isNursing: 'Head.IsNursing',
  hasPrisoner: 'Head.HasPrisoner',
  prisonerCount: 'Head.PrisonerCount',
  needsMedicalReport: 'Head.NeedsMedicalReport',
};

export const RegistrationStep3 = {
  livesInTent: 'LivesInTent',
  tentType: 'TentType',
  hasBathroom: 'HasBathroom',
  bathroomType: 'BathroomType',
  bathroomStatus: 'Head.BathroomStatus',
  hasInjury: 'HasInjury',
  specialNeeds: 'SpecialNeeds',
  desireIds: '#desireIdsInput',
  statusNotes: 'StatusNotes',
};

export const MemberField = (index: number, field: string) =>
  `Members[${index}].${field}`;

export const MemberFields = {
  firstName: (i: number) => MemberField(i, 'FirstName'),
  secondName: (i: number) => MemberField(i, 'SecondName'),
  thirdName: (i: number) => MemberField(i, 'ThirdName'),
  lastName: (i: number) => MemberField(i, 'LastName'),
  idNumber: (i: number) => MemberField(i, 'IdNumber'),
  relationship: (i: number) => MemberField(i, 'RelationshipToHead'),
  gender: (i: number) => MemberField(i, 'Gender'),
  dateOfBirth: (i: number) => MemberField(i, 'DateOfBirth'),
  maritalStatus: (i: number) => MemberField(i, 'MaritalStatus'),
  healthStatus: (i: number) => MemberField(i, 'HealthStatus'),
  motherId: (i: number) => MemberField(i, 'MotherIdNumber'),
  maritalStatus: (i: number) => MemberField(i, 'MaritalStatus'),
};

// Navigation
export const Nav = {
  registrationsLink: 'a[href*="/Admin/Registrations"]',
  changePasswordLink: "a:has-text('تغيير كلمة المرور')",
  dashboardLink: "a:has-text('لوحة التحكم')",
  reportsLink: "a:has-text('التقارير')",
  auditLogsLink: "a:has-text('سجل التدقيق')",
  assistanceLink: "a:has-text('المساعدات')",
  logoutButton: "button:has-text('تسجيل الخروج')",
};

// Buttons
export const Btn = {
  nextStep: "button:has-text('التالي')",
  previousStep: "button:has-text('السابق')",
  submit: "button[type='submit']",
  addMember: "button:has-text('إضافة فرد')",
  removeMember: (i: number) => `button.remove-member-${i}`,
  approve: "button:has-text('موافقة')",
  reject: "button:has-text('رفض')",
  confirmReject: "#rejectModal button[type='submit']",
  edit: (i: number) => `.edit-btn-${i}`,
  save: "button[type='submit']",
  resetPassword: "button:has-text('إعادة تعيين كلمة المرور')",
  submitStep4: "button:has-text('تأكيد وإرسال')",
};

// Modal / Alerts
export const Modal = {
  rejectModal: '#rejectModal',
  rejectReasonInput: '#rejectReason',
};

// Messages
export const Msg = {
  errorSummary: '.validation-summary-errors, .text-red, .alert-danger, .bg-red-900',
  successAlert: "div:has-text('تم تسجيل'), h1:has-text('تم التسجيل')",
  successRecordId: '.font-mono.font-bold.tracking-widest',
  successBanner: '.bg-green-900\\/50, .bg-green-900',
  successRegisterAnother: "a:has-text('تسجيل عائلة أخرى')",
  emptyMessage: '.empty-message, .no-data, .alert-info',
  forceChangeBanner: '.text-gray-500, .bg-yellow-100',
};

// Report
export const Report = {
  columnCheckbox: (group: string) => `input[name='SelectedColumns'][value^='${group}_']`,
  previewButton: "button:has-text('عرض')",
  exportButton: "button:has-text('تصدير')",
  previewTable: 'table.table',
};
