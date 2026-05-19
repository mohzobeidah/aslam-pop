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
        private readonly INotificationService _notificationService;

        public RecordController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private async Task PopulateLookupViewBags()
        {
            ViewBag.Sectors = await _context.Sectors.OrderBy(s => s.Name).ToListAsync();
            ViewBag.HealthStatuses = await _context.HealthStatuses.OrderBy(h => h.Name).ToListAsync();
            ViewBag.ChronicDiseases = await _context.ChronicDiseases.OrderBy(c => c.Name).ToListAsync();
            ViewBag.DisabilityTypes = await _context.DisabilityTypes.OrderBy(d => d.Name).ToListAsync();
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
                ModelState.AddModelError("", "يرجى إدخال رقم الهوية وكلمة المرور");
                return View();
            }

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Members)
                    .ThenInclude(m => m.Person)
                .FirstOrDefaultAsync(f => f.FamilyHead.IdNumber == idNumber && f.PasswordHash == hash);

            if (registration == null)
            {
                ModelState.AddModelError("", "رقم الهوية أو كلمة المرور غير صحيحة");
                return View();
            }

            if (registration.ApprovalStatus == RegistrationApprovalStatus.Pending)
            {
                ModelState.AddModelError("", "طلب التسجيل لم يتم الموافقة عليه بعد. يرجى مراجعة المسؤول المختص.");
                return View();
            }

            if (registration.ApprovalStatus == RegistrationApprovalStatus.Rejected)
            {
                ModelState.AddModelError("", "عذراً، تم رفض طلب التسجيل الخاص بك. يرجى التواصل مع المسؤول.");
                return View();
            }

            HttpContext.Session.SetInt32("EditRegistrationId", registration.Id);
            return RedirectToAction("Edit");
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            await PopulateLookupViewBags();
            var regId = HttpContext.Session.GetInt32("EditRegistrationId");
            if (regId == null) return RedirectToAction("Login");

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Members)
                    .ThenInclude(m => m.Person)
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
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(RegistrationViewModel model)
        {
            await PopulateLookupViewBags();
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

            var regId = HttpContext.Session.GetInt32("EditRegistrationId");
            if (regId == null) return RedirectToAction("Login");

            var registration = await _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Members)
                    .ThenInclude(m => m.Person)
                .FirstOrDefaultAsync(f => f.Id == regId);

            if (registration == null) return RedirectToAction("Login");

            if (registration.ApprovalStatus != RegistrationApprovalStatus.Approved)
            {
                HttpContext.Session.Remove("EditRegistrationId");
                return RedirectToAction("Login");
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
                head.Sector = model.Head.Sector;
                head.DateOfBirth = model.Head.DateOfBirth;
                head.Gender = model.Head.Gender;
                head.PhoneNumber = model.Head.PhoneNumber;
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
                head.Wallet = model.Head.Wallet;
                head.BathroomStatus = model.Head.BathroomStatus;
                head.IsPregnant = model.Head.IsPregnant;
                head.PregnancyMonth = model.Head.PregnancyMonth;
                head.IsNursing = model.Head.IsNursing;
                head.NursingInfantName = model.Head.NursingInfantName;
                head.NursingInfantDOB = model.Head.NursingInfantDOB;
                head.NursingInfantID = model.Head.NursingInfantID;

                // Update Registration-level fields
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
                        Sector = mViewModel.Sector,
                        DateOfBirth = mViewModel.DateOfBirth,
                        Gender = mViewModel.Gender,
                        PhoneNumber = mViewModel.PhoneNumber,
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
                        Wallet = mViewModel.Wallet,
                        BathroomStatus = mViewModel.BathroomStatus,
                        IsPregnant = mViewModel.IsPregnant,
                        PregnancyMonth = mViewModel.PregnancyMonth,
                        IsNursing = mViewModel.IsNursing,
                        NursingInfantName = mViewModel.NursingInfantName,
                        NursingInfantDOB = mViewModel.NursingInfantDOB,
                        NursingInfantID = mViewModel.NursingInfantID
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

                await _notificationService.NotifyMandoobsAsync(
                    head.Sector,
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
                    Sector = registration.FamilyHead.Sector,
                    DateOfBirth = registration.FamilyHead.DateOfBirth,
                    Gender = registration.FamilyHead.Gender,
                    PhoneNumber = registration.FamilyHead.PhoneNumber,
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
                    Wallet = registration.FamilyHead.Wallet,
                    BathroomStatus = registration.FamilyHead.BathroomStatus,
                    IsHouseDestroyed = registration.FamilyHead.IsHouseDestroyed,
                    IsPregnant = registration.FamilyHead.IsPregnant,
                    PregnancyMonth = registration.FamilyHead.PregnancyMonth,
                    IsNursing = registration.FamilyHead.IsNursing,
                    NursingInfantName = registration.FamilyHead.NursingInfantName,
                    NursingInfantDOB = registration.FamilyHead.NursingInfantDOB,
                    NursingInfantID = registration.FamilyHead.NursingInfantID
                },
                Members = registration.Members.Select(m => new MemberViewModel
                {
                    FirstName = m.Person.FirstName,
                    SecondName = m.Person.SecondName,
                    ThirdName = m.Person.ThirdName,
                    LastName = m.Person.LastName,
                    IdNumber = m.Person.IdNumber,
                    Sector = m.Person.Sector,
                    DateOfBirth = m.Person.DateOfBirth,
                    Gender = m.Person.Gender,
                    PhoneNumber = m.Person.PhoneNumber,
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
                    IsPregnant = m.Person.IsPregnant,
                    PregnancyMonth = m.Person.PregnancyMonth,
                    IsNursing = m.Person.IsNursing,
                    NursingInfantName = m.Person.NursingInfantName,
                    NursingInfantDOB = m.Person.NursingInfantDOB,
                    NursingInfantID = m.Person.NursingInfantID,
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
                StatusNotes = registration.StatusNotes
            };
        }
    }
}
