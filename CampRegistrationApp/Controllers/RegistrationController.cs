using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using CampRegistrationApp.Services;
using System.Diagnostics;

namespace CampRegistrationApp.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecordIdGenerator _idGenerator;
        private readonly IWebHostEnvironment _env;
        private readonly INotificationService _notificationService;

        public RegistrationController(ApplicationDbContext context, IRecordIdGenerator idGenerator, IWebHostEnvironment env, INotificationService notificationService)
        {
            _context = context;
            _idGenerator = idGenerator;
            _env = env;
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
        public async Task<IActionResult> Index()
        {
            await PopulateLookupViewBags();
            return View(new RegistrationViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ProcessStep(RegistrationViewModel model)
        {
            await PopulateLookupViewBags();
            if (model.CurrentStep == 1 && !ModelState.IsValid)
            {
                return View("Index", model);
            }

            model.CurrentStep++;
            if (model.CurrentStep > 4)
            {
                return RedirectToAction("Review", model);
            }

            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> CheckId(string idNumber)
        {
            if (string.IsNullOrEmpty(idNumber)) return Ok(new { exists = false });

            var exists = await _context.Persons.AnyAsync(p => p.IdNumber == idNumber);
            return Ok(new { exists });
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(int personId, string fileType, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            var recordId = "TEMP"; // In a real scenario, we'd manage this better in the session
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
        public async Task<IActionResult> Submit(RegistrationViewModel model)
        {
            await PopulateLookupViewBags();
            if (!ModelState.IsValid) return View("Index", model);

            // Check for duplicate ID
            if (await _context.Persons.AnyAsync(p => p.IdNumber == model.Head.IdNumber))
            {
                ModelState.AddModelError("", "هذا الرقم مسجل مسبقاً. يمكنك <a href='/Record/Login' class='text-camp-gold underline'>تسجيل الدخول</a> لتعديل بياناتك.");
                return View("Index", model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var recordId = await _idGenerator.GenerateUniqueIdAsync();

                var head = new Person {
                    FirstName = model.Head.FirstName,
                    SecondName = model.Head.SecondName,
                    ThirdName = model.Head.ThirdName,
                    LastName = model.Head.LastName,
                    IdNumber = model.Head.IdNumber,
                    Sector = model.Head.Sector,
                    DateOfBirth = model.Head.DateOfBirth,
                    Gender = model.Head.Gender,
                    PhoneNumber = model.Head.PhoneNumber,
                    OriginalGovernorate = model.Head.OriginalGovernorate,
                    MaritalStatus = model.Head.MaritalStatus,
                    EmploymentStatus = model.Head.EmploymentStatus,
                    EducationLevel = model.Head.EducationLevel,
                    HealthStatus = model.Head.HealthStatus,
                    ChronicDiseases = model.Head.ChronicDiseases,
                    DisabilityTypes = model.Head.DisabilityTypes,
                    HasInjury = model.Head.HasInjury,
                    InjuryDate = model.Head.InjuryDate,
                    InjuryDetails = model.Head.InjuryDetails,
                    IsHouseDestroyed = model.Head.IsHouseDestroyed,
                    IsPrisoner = model.Head.IsPrisoner,
                    Nationality = model.Head.Nationality,
                    Wallet = model.Head.Wallet,
                    IsPregnant = model.Head.IsPregnant,
                    PregnancyMonth = model.Head.PregnancyMonth,
                    IsNursing = model.Head.IsNursing,
                    NursingInfantName = model.Head.NursingInfantName,
                    NursingInfantDOB = model.Head.NursingInfantDOB,
                    NursingInfantID = model.Head.NursingInfantID
                };
                _context.Persons.Add(head);
                await _context.SaveChangesAsync();

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
                await _context.SaveChangesAsync();

                var registration = new FamilyRegistration {
                    RecordId = recordId,
                    FamilyHeadId = head.Id,
                    ApprovalStatus = RegistrationApprovalStatus.Pending,
                    IsChildHeaded = model.IsChildHeaded,
                    ChildHeadedDetails = model.ChildHeadedDetails,
                    IsFemaleHeaded = model.IsFemaleHeaded,
                    FemaleHeadedDetails = model.FemaleHeadedDetails,
                    SupportsOutsidePerson = model.SupportsOutsidePerson,
                    OutsidePersonName = model.OutsidePersonName,
                    OutsidePersonRelation = model.OutsidePersonRelation,
                    LivesInTent = model.LivesInTent,
                    TentType = model.TentType,
                    OtherTentType = model.OtherTentType,
                    HasBathroom = model.HasBathroom,
                    BathroomType = model.BathroomType,
                    NeedsDiapers = model.NeedsDiapers,
                    DiaperDetails = model.DiaperDetails,
                    HasMultipleFamiliesInTent = model.HasMultipleFamiliesInTent,
                    AdditionalFamiliesCount = model.AdditionalFamiliesCount,
                    NeedTents = (NeedPriority)model.NeedTents,
                    NeedBlankets = (NeedPriority)model.NeedBlankets,
                    NeedMattresses = (NeedPriority)model.NeedMattresses,
                    NeedKitchenTools = (NeedPriority)model.NeedKitchenTools,
                    NeedTarpaulins = (NeedPriority)model.NeedTarpaulins,
                    NeedClothes = (NeedPriority)model.NeedClothes,
                    NeedHygieneKit = (NeedPriority)model.NeedHygieneKit
                };
                _context.FamilyRegistrations.Add(registration);
                await _context.SaveChangesAsync();

                foreach (var mViewModel in model.Members)
                {
                    var memberPerson = new Person {
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
                        Nationality = mViewModel.Nationality,
                        Wallet = mViewModel.Wallet,
                        IsPregnant = mViewModel.IsPregnant,
                        PregnancyMonth = mViewModel.PregnancyMonth,
                        IsNursing = mViewModel.IsNursing,
                        NursingInfantName = mViewModel.NursingInfantName,
                        NursingInfantDOB = mViewModel.NursingInfantDOB,
                        NursingInfantID = mViewModel.NursingInfantID
                    };
                    _context.Persons.Add(memberPerson);
                    await _context.SaveChangesAsync();

                    var familyMember = new FamilyMember {
                        RegistrationId = registration.Id,
                        PersonId = memberPerson.Id,
                        RelationshipToHead = mViewModel.RelationshipToHead
                    };
                    _context.FamilyMembers.Add(familyMember);
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                await _notificationService.NotifyMandoobsAsync(
                    model.Head.Sector,
                    $"تسجيل جديد: {head.FullName} - رقم القيد: {recordId}",
                    $"/Admin/RefugeeDetails/{registration.Id}");
                return View("Success", recordId);
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "An error occurred while saving the registration.");
                return View("Index", model);
            }
        }

        public IActionResult Review(RegistrationViewModel model)
        {
            return View(model);
        }
    }
}
