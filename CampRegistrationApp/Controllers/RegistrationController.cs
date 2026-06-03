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
        private readonly IAuditService _audit;
        private readonly IRegistrationValidationService _validator;
        private readonly IFileCompressionService _compression;
        private readonly IRateLimiterService _rateLimiter;

        public RegistrationController(ApplicationDbContext context, IRecordIdGenerator idGenerator, IWebHostEnvironment env, INotificationService notificationService, IAuditService audit, IRegistrationValidationService validator, IFileCompressionService compression, IRateLimiterService rateLimiter)
        {
            _context = context;
            _idGenerator = idGenerator;
            _env = env;
            _notificationService = notificationService;
            _audit = audit;
            _validator = validator;
            _compression = compression;
            _rateLimiter = rateLimiter;
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
        public IActionResult CheckId(string idNumber)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateKey = $"checkid:{ip}";
            if (_rateLimiter.IsRateLimited(rateKey, 30, TimeSpan.FromMinutes(1)))
            {
                return Ok(new { exists = false });
            }

            if (string.IsNullOrEmpty(idNumber)) return Ok(new { exists = false });

            var exists = _context.Persons.Any(p => p.IdNumber == idNumber);
            return Ok(new { exists });
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(int personId, string fileType, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            var recordId = "TEMP";
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{personId}_{fileType}_{DateTime.Now:yyyyMMddHHmmss}_{ext}";
            var folderPath = Path.Combine(_env.WebRootPath, "uploads", "registrations", recordId);

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);

            using (var memStream = new MemoryStream())
            {
                await file.CopyToAsync(memStream);
                memStream.Position = 0;

                var compressed = await _compression.CompressAsync(memStream.ToArray(), file.FileName);
                await System.IO.File.WriteAllBytesAsync(filePath, compressed);
            }

            return Ok(new { path = $"/uploads/registrations/{recordId}/{fileName}" });
        }

        [HttpPost]
        public async Task<IActionResult> Submit(RegistrationViewModel model)
        {
            await PopulateLookupViewBags();
            if (!ModelState.IsValid) return View("Index", model);

            if (!_validator.ValidateRegistration(model, ModelState))
            {
                return View("Index", model);
            }

            // Check for duplicate IDs

            // Check for duplicate IDs
            var allIds = new List<string> { model.Head.IdNumber };
            allIds.AddRange(model.Members.Select(m => m.IdNumber));

            var duplicateInForm = allIds.GroupBy(id => id).Any(g => g.Count() > 1);
            if (duplicateInForm)
            {
                ModelState.AddModelError("", "يوجد تكرار في أرقام الهوية داخل نفس الطلب. يجب أن يكون لكل شخص رقم هوية فريد.");
                return View("Index", model);
            }

            var existingIds = await _context.Persons
                .Where(p => allIds.Contains(p.IdNumber))
                .Select(p => p.IdNumber)
                .ToListAsync();

            if (existingIds.Any())
            {
                if (existingIds.Contains(model.Head.IdNumber))
                {
                    ModelState.AddModelError("", "هذا الرقم مسجل مسبقاً. يمكنك <a href='/Record/Login' class='text-camp-gold underline'>تسجيل الدخول</a> لتعديل بياناتك.");
                }
                else
                {
                    var duplicateMembers = model.Members
                        .Where(m => existingIds.Contains(m.IdNumber))
                        .Select(m => $"{m.FirstName} {m.LastName} (رقم: {m.IdNumber})");
                    ModelState.AddModelError("", $"أرقام الهوية التالية مسجلة مسبقاً لأفراد العائلة: {string.Join("، ", duplicateMembers)}");
                }
                return View("Index", model);
            }

            // Hash password if provided
            var password = model.Password;
            var passwordHash = !string.IsNullOrEmpty(password)
                ? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password)))
                : null;

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
                    DateOfBirth = model.Head.DateOfBirth,
                    Gender = model.Head.Gender,
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
                    BathroomStatus = model.Head.BathroomStatus,
                    MotherIdNumber = model.Head.MotherIdNumber,
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
                    IsHusbandAbroad = model.IsHusbandAbroad,
                    SupportsOutsidePerson = model.SupportsOutsidePerson,
                    OutsidePersonName = model.OutsidePersonName,
                    OutsidePersonRelation = model.OutsidePersonRelation,
                    LivesInTent = model.LivesInTent,
                    TentType = model.TentType,
                    OtherTentType = model.OtherTentType,
                    SectorId = model.SectorId ?? 0,
                    PhoneNumber = model.PhoneNumber,
                    Wallet = model.Wallet,
                    WalletType = model.WalletType,
                    HasBathroom = model.HasBathroom,
                    BathroomType = model.BathroomType,
                    NeedsDiapers = model.NeedsDiapers,
                    DiaperDetails = model.DiaperDetails,
                    HasMultipleFamiliesInTent = model.HasMultipleFamiliesInTent,
                    AdditionalFamiliesCount = model.AdditionalFamiliesCount,
                    StatusNotes = model.StatusNotes,
                    PasswordHash = passwordHash
                };
                _context.FamilyRegistrations.Add(registration);
                await _context.SaveChangesAsync();

                // Save family desires
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
                await _context.SaveChangesAsync();

                foreach (var mViewModel in model.Members)
                {
                    var memberPerson = new Person {
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
                        MotherIdNumber = mViewModel.MotherIdNumber,
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

                var sectorName = await _context.Sectors
                    .Where(s => s.Id == model.SectorId)
                    .Select(s => s.Name)
                    .FirstAsync();

                await _audit.LogAsync(0, "Create", "FamilyRegistrations", recordId, null, new
                {
                    head.IdNumber, head.FullName, Sector = sectorName,
                    memberCount = model.Members.Count,
                    registrationId = registration.Id
                }, source: "Web");

                await _notificationService.NotifyMandoobsAsync(
                    sectorName,
                    $"تسجيل جديد: {head.FullName} - رقم القيد: {recordId}",
                    $"/Admin/RefugeeDetails/{registration.Id}");

                TempData["Success"] = $"تم تسجيل العائلة بنجاح! رقم القيد: <strong>{recordId}</strong>";
                return View("Success", recordId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "حدث خطأ أثناء حفظ التسجيل: " + ex.ToString());
                return View("Index", model);
            }
        }

        public IActionResult Review(RegistrationViewModel model)
        {
            return View(model);
        }
    }
}
