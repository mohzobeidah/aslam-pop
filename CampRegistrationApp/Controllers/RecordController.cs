using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;
using System.Security.Cryptography;
using System.Text;

namespace CampRegistrationApp.Controllers
{
    public class RecordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _env;

        public RecordController(ApplicationDbContext context, IAuditService audit, INotificationService notificationService, IWebHostEnvironment env)
        {
            _context = context;
            _audit = audit;
            _notificationService = notificationService;
            _env = env;
        }

        private async Task PopulateLookupViewBags()
        {
            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            ViewBag.HealthStatuses = await _context.HealthStatuses.OrderBy(h => h.Name).ToListAsync();
            ViewBag.ChronicDiseases = await _context.ChronicDiseases.OrderBy(c => c.Name).ToListAsync();
            ViewBag.DisabilityTypes = await _context.DisabilityTypes.OrderBy(d => d.Name).ToListAsync();
            ViewBag.Desires = await _context.Desires.OrderBy(d => d.Id).ToListAsync();
        }

        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Find(string recordId, string headIdNumber)
        {
            if (string.IsNullOrEmpty(recordId) || string.IsNullOrEmpty(headIdNumber))
            {
                ModelState.AddModelError("", "يرجى إدخال معرف التسجيل ورقم هوية رب الأسرة");
                return View("Search");
            }

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Members)
                    .ThenInclude(m => m.Person)
                .FirstOrDefaultAsync(f => f.RecordId == recordId && f.FamilyHead.IdNumber == headIdNumber);

            if (registration == null)
            {
                ModelState.AddModelError("", "لم يتم العثور على سجل مطابق للبيانات المدخلة");
                return View("Search");
            }

            var model = MapToViewModel(registration);
            HttpContext.Session.SetInt32("EditRegistrationId", registration.Id);
            return RedirectToAction("Edit");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string idNumber, string password)
        {
            if (string.IsNullOrEmpty(idNumber) || string.IsNullOrEmpty(password))
            {
                await _audit.LogAsync(0, "LoginFailed", "FamilyRegistrations", null,
                    new { idNumber, reason = "حقول فارغة" },
                    null);
                ModelState.AddModelError("", "يرجى إدخال رقم الهوية وكلمة المرور");
                return View();
            }

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Members)
                    .ThenInclude(m => m.Person)
                .Include(f => f.Sector)
                .FirstOrDefaultAsync(f => f.FamilyHead.IdNumber == idNumber && f.PasswordHash == hash);

            if (registration == null)
            {
                await _audit.LogAsync(0, "LoginFailed", "FamilyRegistrations", null,
                    new { idNumber, reason = "رقم الهوية أو كلمة المرور غير صحيحة" },
                    null);
                ModelState.AddModelError("", "رقم الهوية أو كلمة المرور غير صحيحة");
                return View();
            }

            if (registration.ApprovalStatus == RegistrationApprovalStatus.Pending)
            {
                await _audit.LogAsync(0, "LoginFailed", "FamilyRegistrations", registration.RecordId,
                    new { idNumber, reason = "طلب التسجيل لم يتم الموافقة عليه بعد" },
                    null);
                ModelState.AddModelError("", "طلب التسجيل لم يتم الموافقة عليه بعد. يرجى مراجعة المسؤول المختص.");
                return View();
            }

            if (registration.ApprovalStatus == RegistrationApprovalStatus.Rejected)
            {
                await _audit.LogAsync(0, "LoginFailed", "FamilyRegistrations", registration.RecordId,
                    new { idNumber, reason = "تم رفض طلب التسجيل" },
                    null);
                ModelState.AddModelError("", "عذراً، تم رفض طلب التسجيل الخاص بك. يرجى التواصل مع المسؤول.");
                return View();
            }

            HttpContext.Session.SetInt32("EditRegistrationId", registration.Id);

            await _audit.LogAsync(0, "Login", "FamilyRegistrations", registration.RecordId,
                null,
                new { headName = registration.FamilyHead.FullName, idNumber, sector = registration.Sector?.Name });

            return RedirectToAction("Edit");
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            await PopulateLookupViewBags();
            var regId = HttpContext.Session.GetInt32("EditRegistrationId");
            if (regId == null) return RedirectToAction("Login");

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead).ThenInclude(h => h.Attachments)
                .Include(f => f.Members)
                    .ThenInclude(m => m.Person)
                .Include(f => f.FamilyDesires)
                .FirstOrDefaultAsync(f => f.Id == regId);

            if (registration == null)
            {
                HttpContext.Session.Remove("EditRegistrationId");
                return RedirectToAction("Login");
            }

            if (registration.ApprovalStatus != RegistrationApprovalStatus.Approved)
            {
                HttpContext.Session.Remove("EditRegistrationId");
                TempData["Error"] = "لم يتم الموافقة على طلب التسجيل بعد";
                return RedirectToAction("Login");
            }

            var model = MapToViewModel(registration);
            ViewBag.HeadAttachments = registration.FamilyHead.Attachments.ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(int personId, string fileType, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            var regId = HttpContext.Session.GetInt32("EditRegistrationId");
            var recordId = regId?.ToString() ?? "TEMP";
            var fileName = $"{personId}_{fileType}_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetExtension(file.FileName)}";
            var folderPath = Path.Combine(_env.WebRootPath, "uploads", "registrations", recordId);

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { path = $"/uploads/registrations/{recordId}/{fileName}" });
        }

        [HttpPost]
        public async Task<IActionResult> Update(RegistrationViewModel model)
        {
            await PopulateLookupViewBags();
            var regId = HttpContext.Session.GetInt32("EditRegistrationId");
            if (regId == null) return RedirectToAction("Login");
            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead).ThenInclude(h => h.Attachments)
                .Include(f => f.Members).ThenInclude(m => m.Person)
                .Include(f => f.FamilyDesires)
                .FirstOrDefaultAsync(f => f.Id == regId);
            if (registration == null) return RedirectToAction("Login");
            if (registration.ApprovalStatus != RegistrationApprovalStatus.Approved)
            {
                HttpContext.Session.Remove("EditRegistrationId");
                return RedirectToAction("Login");
            }
            if (model == null) model = MapToViewModel(registration);
            ViewBag.HeadAttachments = registration.FamilyHead.Attachments.ToList();
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "يرجى تصحيح الأخطاء في البيانات");
                return View("Edit", model);
            }

            // Server-side validation: married requires wife member
            if (model.Head.MaritalStatus == "متزوج" && !model.Members.Any(m => m.RelationshipToHead == "زوجة"))
            {
                ModelState.AddModelError("", "بما أن الحالة الاجتماعية متزوج، يجب إضافة فرد بصفة زوجة");
                return View("Edit", model);
            }

            // Server-side validation: sick requires disease or disability for head
            if (model.Head.HealthStatus == "مريض" && string.IsNullOrEmpty(model.Head.ChronicDiseases) && string.IsNullOrEmpty(model.Head.DisabilityTypes))
            {
                ModelState.AddModelError("", "بما أن الحالة الصحية مريض، يجب اختيار مرض مزمن أو نوع إعاقة على الأقل لرب الأسرة");
                return View("Edit", model);
            }

            // Server-side validation: sick requires disease or disability for each member
            for (int i = 0; i < model.Members.Count; i++)
            {
                var m = model.Members[i];
                if (m.HealthStatus == "مريض" && string.IsNullOrEmpty(m.ChronicDiseases) && string.IsNullOrEmpty(m.DisabilityTypes))
                {
                    ModelState.AddModelError("", $"الفرد رقم {i + 1}: بما أن الحالة الصحية مريض، يجب اختيار مرض مزمن أو نوع إعاقة على الأقل");
                    return View("Edit", model);
                }
            }

            // Check for duplicate IDs
            var currentHeadId = registration.FamilyHead.IdNumber;
            var currentMemberIds = registration.Members.Select(m => m.Person.IdNumber).ToHashSet();
            var allIds = new List<string> { model.Head.IdNumber };
            allIds.AddRange(model.Members.Select(m => m.IdNumber));

            var duplicateInForm = allIds.GroupBy(id => id).Any(g => g.Count() > 1);
            if (duplicateInForm)
            {
                ModelState.AddModelError("", "يوجد تكرار في أرقام الهوية داخل نفس الطلب. يجب أن يكون لكل شخص رقم هوية فريد.");
                return View("Edit", model);
            }

            var existingIds = await _context.Persons
                .Where(p => allIds.Contains(p.IdNumber) && p.IdNumber != currentHeadId && !currentMemberIds.Contains(p.IdNumber))
                .Select(p => p.IdNumber)
                .ToListAsync();

            if (existingIds.Any())
            {
                if (existingIds.Contains(model.Head.IdNumber) && model.Head.IdNumber != currentHeadId)
                {
                    ModelState.AddModelError("", "رقم الهوية هذا مسجل مسبقاً لرب أسرة آخر.");
                }
                else
                {
                    var duplicateMembers = model.Members
                        .Where(m => existingIds.Contains(m.IdNumber))
                        .Select(m => $"{m.FirstName} {m.LastName} (رقم: {m.IdNumber})");
                    ModelState.AddModelError("", $"أرقام الهوية التالية مسجلة مسبقاً لأفراد آخرين: {string.Join("، ", duplicateMembers)}");
                }
                return View("Edit", model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update Family Head
                var head = registration.FamilyHead;
                head.FirstName = model.Head.FirstName;
                head.SecondName = model.Head.SecondName;
                head.ThirdName = model.Head.ThirdName;
                head.LastName = model.Head.LastName;
                head.IdNumber = model.Head.IdNumber;
                head.DateOfBirth = model.Head.DateOfBirth;
                head.Gender = model.Head.Gender;
                head.OriginalGovernorate = model.Head.OriginalGovernorate;
                head.MaritalStatus = model.Head.MaritalStatus;
                head.EmploymentStatus = model.Head.EmploymentStatus;
                head.EducationLevel = model.Head.EducationLevel;
                head.HealthStatus = model.Head.HealthStatus;
                head.ChronicDiseases = model.Head.ChronicDiseases;
                head.DisabilityTypes = model.Head.DisabilityTypes;
                head.HasInjury = model.Head.HasInjury;
                head.InjuryDate = model.Head.InjuryDate;
                head.InjuryDetails = model.Head.InjuryDetails;
                head.IsHouseDestroyed = model.Head.IsHouseDestroyed;
                head.IsPrisoner = model.Head.IsPrisoner;
                head.BathroomStatus = model.Head.BathroomStatus;
                head.IsPregnant = model.Head.IsPregnant;
                head.PregnancyMonth = model.Head.PregnancyMonth;
                head.IsNursing = model.Head.IsNursing;
                head.NursingInfantName = model.Head.NursingInfantName;
                head.NursingInfantDOB = model.Head.NursingInfantDOB;
                head.NursingInfantID = model.Head.NursingInfantID;
                head.MotherIdNumber = model.Head.MotherIdNumber;

                await _context.SaveChangesAsync();

                // Save new attachments
                if (!string.IsNullOrEmpty(model.Head.HeadIdImagePath))
                {
                    _context.Attachments.Add(new Attachment
                    {
                        PersonId = head.Id,
                        FileType = "IDImage",
                        FilePath = model.Head.HeadIdImagePath
                    });
                }

                if (!string.IsNullOrEmpty(model.Head.MedicalImagePath))
                {
                    _context.Attachments.Add(new Attachment
                    {
                        PersonId = head.Id,
                        FileType = "MedicalReport",
                        FilePath = model.Head.MedicalImagePath
                    });
                }

                // Update Registration-level fields
                registration.SectorId = model.SectorId ?? 0;
                registration.PhoneNumber = model.PhoneNumber;
                registration.Wallet = model.Wallet;
                registration.WalletType = model.WalletType;
                registration.IsChildHeaded = model.IsChildHeaded;
                registration.ChildHeadedDetails = model.ChildHeadedDetails;
                registration.IsFemaleHeaded = model.IsFemaleHeaded;
                registration.FemaleHeadedDetails = model.FemaleHeadedDetails;
                registration.SupportsOutsidePerson = model.SupportsOutsidePerson;
                registration.OutsidePersonName = model.OutsidePersonName;
                registration.OutsidePersonRelation = model.OutsidePersonRelation;
                registration.LivesInTent = model.LivesInTent;
                registration.TentType = model.TentType;
                registration.OtherTentType = model.OtherTentType;
                registration.HasBathroom = model.HasBathroom;
                registration.BathroomType = model.BathroomType;
                registration.NeedsDiapers = model.NeedsDiapers;
                registration.DiaperDetails = model.DiaperDetails;
                registration.HasMultipleFamiliesInTent = model.HasMultipleFamiliesInTent;
                registration.AdditionalFamiliesCount = model.AdditionalFamiliesCount;
                registration.StatusNotes = model.StatusNotes;
                registration.IsHusbandAbroad = model.IsHusbandAbroad;

                // Update family desires
                _context.FamilyDesires.RemoveRange(registration.FamilyDesires);
                if (model.DesireIds != null)
                {
                    for (int i = 0; i < model.DesireIds.Count; i++)
                    {
                        if (model.DesireIds[i] > 0)
                        {
                            _context.FamilyDesires.Add(new FamilyDesire
                            {
                                FamilyRegistrationId = registration.Id,
                                DesireId = model.DesireIds[i],
                                Order = i + 1
                            });
                        }
                    }
                }

                // Get old member person IDs BEFORE removing
                var oldPersonIds = registration.Members.Select(m => m.PersonId).ToList();

                // Remove existing members
                _context.FamilyMembers.RemoveRange(registration.Members);

                // Remove old member persons (not the head)
                var oldPersons = await _context.Persons
                    .Where(p => oldPersonIds.Contains(p.Id) && p.Id != registration.FamilyHeadId)
                    .ToListAsync();
                _context.Persons.RemoveRange(oldPersons);
                await _context.SaveChangesAsync();

                // Add new members
                foreach (var mViewModel in model.Members)
                {
                    var memberPerson = new Person
                    {
                        FirstName = mViewModel.FirstName,
                        SecondName = mViewModel.SecondName,
                        ThirdName = mViewModel.ThirdName,
                        LastName = mViewModel.LastName,
                        IdNumber = mViewModel.IdNumber,
                        DateOfBirth = mViewModel.DateOfBirth,
                        Gender = mViewModel.Gender,
                        OriginalGovernorate = mViewModel.OriginalGovernorate,
                        MaritalStatus = mViewModel.MaritalStatus,
                        EmploymentStatus = mViewModel.EmploymentStatus,
                        EducationLevel = mViewModel.EducationLevel,
                        HealthStatus = mViewModel.HealthStatus,
                        ChronicDiseases = mViewModel.ChronicDiseases,
                        DisabilityTypes = mViewModel.DisabilityTypes,
                        HasInjury = mViewModel.HasInjury,
                        InjuryDate = mViewModel.InjuryDate,
                        InjuryDetails = mViewModel.InjuryDetails,
                        IsPrisoner = mViewModel.IsPrisoner,
                        IsHusbandPrisoner = mViewModel.IsHusbandPrisoner,
                        IsPregnant = mViewModel.IsPregnant,
                        PregnancyMonth = mViewModel.PregnancyMonth,
                        IsNursing = mViewModel.IsNursing,
                        NursingInfantName = mViewModel.NursingInfantName,
                        NursingInfantDOB = mViewModel.NursingInfantDOB,
                        NursingInfantID = mViewModel.NursingInfantID,
                        MotherIdNumber = mViewModel.MotherIdNumber,
                        BathroomStatus = mViewModel.BathroomStatus
                    };
                    _context.Persons.Add(memberPerson);
                    await _context.SaveChangesAsync();

                    _context.FamilyMembers.Add(new FamilyMember
                    {
                        RegistrationId = registration.Id,
                        PersonId = memberPerson.Id,
                        RelationshipToHead = mViewModel.RelationshipToHead
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var sectorName = await _context.Sectors
                    .Where(s => s.Id == model.SectorId)
                    .Select(s => s.Name)
                    .FirstAsync();
                await _audit.LogAsync(0, "RecordEdit", "FamilyRegistrations",
                    registration.RecordId,
                    new { action = "تم تعديل بيانات العائلة بواسطة رب الأسرة" },
                    new { headName = head.FullName, sector = sectorName });
                await _notificationService.NotifyMandoobsAsync(
                    sectorName,
                    $"تعديل بيانات: {head.FullName} - رقم القيد: {registration.RecordId}",
                    $"/Admin/RefugeeDetails/{registration.Id}");

                TempData["Success"] = "تم تعديل البيانات بنجاح";
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "حدث خطأ أثناء حفظ التعديلات: " + ex.Message;
                return View("Edit", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var regId = HttpContext.Session.GetInt32("EditRegistrationId");
            if (regId == null) return RedirectToAction("Login");

            var registration = await _context.FamilyRegistrations
                .FirstOrDefaultAsync(f => f.Id == regId);
            if (registration == null) return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var regId = HttpContext.Session.GetInt32("EditRegistrationId");
            if (regId == null) return RedirectToAction("Login");

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .FirstOrDefaultAsync(f => f.Id == regId);
            if (registration == null) return RedirectToAction("Login");

            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError("", "جميع الحقول مطلوبة");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "كلمة المرور الجديدة وتأكيدها غير متطابقين");
                return View();
            }

            if (newPassword.Length < 4)
            {
                ModelState.AddModelError("", "كلمة المرور يجب أن تكون 4 أحرف على الأقل");
                return View();
            }

            var oldHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(oldPassword)));
            if (registration.PasswordHash != oldHash)
            {
                ModelState.AddModelError("", "كلمة المرور القديمة غير صحيحة");
                return View();
            }

            var newHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(newPassword)));
            registration.PasswordHash = newHash;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(0, "ChangePassword", "FamilyRegistrations", registration.RecordId,
                new { action = "تم تغيير كلمة المرور بواسطة رب الأسرة" },
                new { headName = registration.FamilyHead.FullName },
                source: "Web");

            TempData["Success"] = "تم تغيير كلمة المرور بنجاح";
            return RedirectToAction("Edit");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("EditRegistrationId");
            return RedirectToAction("Login");
        }

        private RegistrationViewModel MapToViewModel(FamilyRegistration registration)
        {
            return new RegistrationViewModel
            {
                Id = registration.Id,
                RecordId = registration.RecordId,
                CurrentStep = 1,
                Head = new PersonViewModel
                {
                    FirstName = registration.FamilyHead.FirstName,
                    SecondName = registration.FamilyHead.SecondName,
                    ThirdName = registration.FamilyHead.ThirdName,
                    LastName = registration.FamilyHead.LastName,
                    IdNumber = registration.FamilyHead.IdNumber,
                    DateOfBirth = registration.FamilyHead.DateOfBirth,
                    Gender = registration.FamilyHead.Gender,
                    OriginalGovernorate = registration.FamilyHead.OriginalGovernorate,
                    MaritalStatus = registration.FamilyHead.MaritalStatus,
                    EmploymentStatus = registration.FamilyHead.EmploymentStatus,
                    EducationLevel = registration.FamilyHead.EducationLevel,
                    HealthStatus = registration.FamilyHead.HealthStatus,
                    ChronicDiseases = registration.FamilyHead.ChronicDiseases,
                    DisabilityTypes = registration.FamilyHead.DisabilityTypes,
                    HasInjury = registration.FamilyHead.HasInjury,
                    InjuryDate = registration.FamilyHead.InjuryDate,
                    InjuryDetails = registration.FamilyHead.InjuryDetails,
                    IsPrisoner = registration.FamilyHead.IsPrisoner,
                    BathroomStatus = registration.FamilyHead.BathroomStatus,
                    IsHouseDestroyed = registration.FamilyHead.IsHouseDestroyed,
                    IsPregnant = registration.FamilyHead.IsPregnant,
                    PregnancyMonth = registration.FamilyHead.PregnancyMonth,
                    IsNursing = registration.FamilyHead.IsNursing,
                    NursingInfantName = registration.FamilyHead.NursingInfantName,
                    NursingInfantDOB = registration.FamilyHead.NursingInfantDOB,
                    NursingInfantID = registration.FamilyHead.NursingInfantID,
                    MotherIdNumber = registration.FamilyHead.MotherIdNumber
                },
                Members = registration.Members.Select(m => new MemberViewModel
                {
                    FirstName = m.Person.FirstName,
                    SecondName = m.Person.SecondName,
                    ThirdName = m.Person.ThirdName,
                    LastName = m.Person.LastName,
                    IdNumber = m.Person.IdNumber,
                    DateOfBirth = m.Person.DateOfBirth,
                    Gender = m.Person.Gender,
                    OriginalGovernorate = m.Person.OriginalGovernorate,
                    MaritalStatus = m.Person.MaritalStatus,
                    EmploymentStatus = m.Person.EmploymentStatus,
                    EducationLevel = m.Person.EducationLevel,
                    HealthStatus = m.Person.HealthStatus,
                    ChronicDiseases = m.Person.ChronicDiseases,
                    DisabilityTypes = m.Person.DisabilityTypes,
                    HasInjury = m.Person.HasInjury,
                    InjuryDate = m.Person.InjuryDate,
                    InjuryDetails = m.Person.InjuryDetails,
                    IsPrisoner = m.Person.IsPrisoner,
                    IsHusbandPrisoner = m.Person.IsHusbandPrisoner,
                    IsPregnant = m.Person.IsPregnant,
                    PregnancyMonth = m.Person.PregnancyMonth,
                    IsNursing = m.Person.IsNursing,
                    NursingInfantName = m.Person.NursingInfantName,
                    NursingInfantDOB = m.Person.NursingInfantDOB,
                    NursingInfantID = m.Person.NursingInfantID,
                    MotherIdNumber = m.Person.MotherIdNumber,
                    BathroomStatus = m.Person.BathroomStatus,
                    RelationshipToHead = m.RelationshipToHead
                }).ToList(),
                IsChildHeaded = registration.IsChildHeaded,
                ChildHeadedDetails = registration.ChildHeadedDetails,
                IsFemaleHeaded = registration.IsFemaleHeaded,
                FemaleHeadedDetails = registration.FemaleHeadedDetails,
                SupportsOutsidePerson = registration.SupportsOutsidePerson,
                OutsidePersonName = registration.OutsidePersonName,
                OutsidePersonRelation = registration.OutsidePersonRelation,
                LivesInTent = registration.LivesInTent,
                TentType = registration.TentType,
                OtherTentType = registration.OtherTentType,
                HasBathroom = registration.HasBathroom,
                BathroomType = registration.BathroomType,
                NeedsDiapers = registration.NeedsDiapers,
                DiaperDetails = registration.DiaperDetails,
                HasMultipleFamiliesInTent = registration.HasMultipleFamiliesInTent,
                AdditionalFamiliesCount = registration.AdditionalFamiliesCount,
                StatusNotes = registration.StatusNotes,
                SectorId = registration.SectorId,
                PhoneNumber = registration.PhoneNumber,
                Wallet = registration.Wallet,
                WalletType = registration.WalletType,
                IsHusbandAbroad = registration.IsHusbandAbroad,
                DesireIds = registration.FamilyDesires
                    .OrderBy(fd => fd.Order)
                    .Select(fd => fd.DesireId)
                    .ToList()
            };
        }
    }
}
