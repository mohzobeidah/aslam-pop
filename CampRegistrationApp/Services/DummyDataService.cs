using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace CampRegistrationApp.Services
{
    public class DummyDataService : IDummyDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecordIdGenerator _idGenerator;
        private readonly Random _random = new Random();

        public DummyDataService(ApplicationDbContext context, IRecordIdGenerator idGenerator)
        {
            _context = context;
            _idGenerator = idGenerator;
        }

        private readonly string[] _firstNames = { "أحمد", "محمد", "محمود", "سارة", "فاطمة", "مريم", "يوسف", "عمر", "خالد", "ليلى", "زينب", "نور", "حمزة", "إبراهيم", "ياسين", "هدى", "منى", "سما", "كريم", "أنس" };
        private readonly string[] _lastNames = { "العبيدي", "منصور", "القاسم", "الزهراني", "العتيبي", "الشمري", "المطيري", "الدوسري", "الغامدي", "القحطاني", "سالم", "حسين", "علي", "حسن", "إبراهيم" };
        private readonly string[] _sectors = { "A", "B", "C", "D" };
        private readonly string[] _healthStatuses = { "سليم", "مريض" };
        private readonly string[] _bathroomStatuses = { "جيد", "متوسط", "سيء" };
        public async Task SeedDummyDataAsync()
        {
            // Skip seeding if there are already many registrations to avoid duplicates
            if (await _context.FamilyRegistrations.CountAsync() > 10) return;

            Console.WriteLine("Seeding dummy data...");

            // 1. Ensure Sectors exist
            var existingSectors = await _context.Sectors.ToListAsync();
            var sectorMap = new Dictionary<string, Sector>();
            foreach (var s in _sectors)
            {
                var sector = existingSectors.FirstOrDefault(x => x.Name == s);
                if (sector == null)
                {
                    sector = new Sector { Name = s, Camp = "Camp 1", Coordinate = "31.5, 34.4", Area = "Zone " + s, ManufacturedTentsCount = 50, HandmadeTentsCount = 30, BathroomsCount = 10 };
                    _context.Sectors.Add(sector);
                    await _context.SaveChangesAsync();
                }
                sectorMap[s] = sector;
            }

            // 2. Create Mandoobs for each sector
            foreach (var sector in sectorMap.Values)
            {
                if (!await _context.Admins.AnyAsync(a => a.SectorId == sector.Id))
                {
                    var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("mandoob123")));
                    _context.Admins.Add(new Admin
                    {
                        Name = "مندوب " + sector.Name,
                        NationalId = "mandoob_" + sector.Name,
                        Mobile = "05900000" + _random.Next(100, 999),
                        PasswordHash = hash,
                        Role = AdminRole.Mandoob,
                        SectorId = sector.Id
                    });
                }
            }
            await _context.SaveChangesAsync();

            // 3. Create Family Registrations
            int familyCount = _random.Next(50, 101);
            for (int i = 0; i < familyCount; i++)
            {
                var sectorName = _sectors[_random.Next(_sectors.Length)];
                var sector = sectorMap[sectorName];

                // Head of Family
                var head = new Person
                {
                    FirstName = _firstNames[_random.Next(_firstNames.Length)],
                    SecondName = _firstNames[_random.Next(_firstNames.Length)],
                    ThirdName = _firstNames[_random.Next(_firstNames.Length)],
                    LastName = _lastNames[_random.Next(_lastNames.Length)],
                    IdNumber = _random.Next(100000000, 999999999).ToString(),
                    DateOfBirth = JerusalemTime.Now.AddYears(-_random.Next(20, 70)),
                    Gender = _random.Next(0, 2) == 0 ? "ذكر" : "أنثى",
                    OriginalGovernorate = "غزة",
                    MaritalStatus = "متزوج",
                    EmploymentStatus = "عاطل",
                    EducationLevel = "جامعي",
                    HealthStatus = _healthStatuses[_random.Next(_healthStatuses.Length)],
                    BathroomStatus = _bathroomStatuses[_random.Next(_bathroomStatuses.Length)],
                    IsPrisoner = _random.Next(0, 10) == 0
                };
                _context.Persons.Add(head);
                await _context.SaveChangesAsync();

                var registration = new FamilyRegistration
                {
                    RecordId = await _idGenerator.GenerateUniqueIdAsync(),
                    FamilyHeadId = head.Id,
                    SectorId = sector.Id,
                    PhoneNumber = "059" + _random.Next(1000000, 9999999),
                    Wallet = _random.Next(0, 2) == 0 ? "" : _random.Next(100, 999).ToString(),
                    RegistrationTimestamp = JerusalemTime.Now.AddDays(-_random.Next(1, 365)),
                    ApprovalStatus = (RegistrationApprovalStatus)_random.Next(0, 3),
                    IsChildHeaded = _random.Next(0, 10) == 0,
                    ChildHeadedDetails = _random.Next(0, 10) == 0 ? "طفل يعيل أسرته بسبب فقدان الوالدين" : null,
                    IsFemaleHeaded = _random.Next(0, 10) == 0,
                    FemaleHeadedDetails = _random.Next(0, 10) == 0 ? "أرملة تعيل أطفالها" : null,
                    SupportsOutsidePerson = _random.Next(0, 5) == 0,
                    OutsidePersonName = _random.Next(0, 5) == 0 ? _firstNames[_random.Next(_firstNames.Length)] + " " + _lastNames[_random.Next(_lastNames.Length)] : null,
                    OutsidePersonRelation = _random.Next(0, 5) == 0 ? "قريب" : null,
                    LivesInTent = true,
                    TentType = _random.Next(0, 3) == 0 ? "كاروك" : (_random.Next(0, 2) == 0 ? "خشبي" : "بلاستيك"),
                    OtherTentType = _random.Next(0, 10) == 0 ? "خيمة كبيرة" : null,
                    HasBathroom = _random.Next(0, 2) == 0,
                    BathroomType = _random.Next(0, 2) == 0 ? "داخلي" : "خارجي",
                    NeedsDiapers = _random.Next(0, 5) == 0,
                    DiaperDetails = _random.Next(0, 5) == 0 ? "طفلان يحتاجان حفائض" : null,
                    HasMultipleFamiliesInTent = _random.Next(0, 5) == 0,
                    AdditionalFamiliesCount = _random.Next(0, 5) == 0 ? _random.Next(1, 4) : null,
                    StatusNotes = _random.Next(0, 4) == 0 ? "حالة عائلة صعبة تحتاج متابعة" : null,
                };
                _context.FamilyRegistrations.Add(registration);
                await _context.SaveChangesAsync();

                // Seed family desires
                var desires = await _context.Desires.OrderBy(d => d.Id).ToListAsync();
                var shuffledDesires = desires.OrderBy(_ => _random.Next()).ToList();
                for (int di = 0; di < shuffledDesires.Count; di++)
                {
                    _context.FamilyDesires.Add(new FamilyDesire
                    {
                        FamilyRegistrationId = registration.Id,
                        DesireId = shuffledDesires[di].Id,
                        Order = di + 1
                    });
                }
                await _context.SaveChangesAsync();

                // Family Members
                int memberCount = _random.Next(1, 8);
                for (int j = 0; j < memberCount; j++)
                {
                    var member = new Person
                    {
                        FirstName = _firstNames[_random.Next(_firstNames.Length)],
                        SecondName = _firstNames[_random.Next(_firstNames.Length)],
                        ThirdName = _firstNames[_random.Next(_firstNames.Length)],
                        LastName = head.LastName,
                        IdNumber = _random.Next(100000000, 999999999).ToString(),
                        DateOfBirth = JerusalemTime.Now.AddYears(-_random.Next(1, 60)),
                        Gender = _random.Next(0, 2) == 0 ? "ذكر" : "أنثى",
                        OriginalGovernorate = "غزة",
                        MaritalStatus = "أعزب",
                        EmploymentStatus = "طالب",
                        EducationLevel = "مدرسة",
                        HealthStatus = _healthStatuses[_random.Next(_healthStatuses.Length)],
                        BathroomStatus = _bathroomStatuses[_random.Next(_bathroomStatuses.Length)],
                        IsPrisoner = false
                    };
                    _context.Persons.Add(member);
                    await _context.SaveChangesAsync();

                    _context.FamilyMembers.Add(new FamilyMember
                    {
                        RegistrationId = registration.Id,
                        PersonId = member.Id,
                        RelationshipToHead = "ابن/ابنة"
                    });
                }
                await _context.SaveChangesAsync();
            }

            // 4. Create Projects
            var projects = new List<Project>();
            for (int i = 1; i <= 3; i++)
            {
                var project = new Project
                {
                    Name = "مشروع مساعدات " + i,
                    StartDate = JerusalemTime.Now.AddMonths(-2),
                    EndDate = JerusalemTime.Now.AddMonths(2),
                    RequiredCount = 100,
                    Status = ProjectStatus.Active,
                    CreatedById = 1, // Super admin
                    CreatedAt = JerusalemTime.Now
                };
                _context.Projects.Add(project);
                projects.Add(project);
            }
            await _context.SaveChangesAsync();

            // 5. Create Nominations
            var allPersons = await _context.Persons.ToListAsync();
            var allSectors = await _context.Sectors.ToListAsync();
            var allAdmins = await _context.Admins.Where(a => a.Role == AdminRole.Mandoob).ToListAsync();

            foreach (var project in projects)
            {
                int nominationCount = _random.Next(10, 30);
                for (int i = 0; i < nominationCount; i++)
                {
                    var person = allPersons[_random.Next(allPersons.Count)];
                    var sector = allSectors[_random.Next(allSectors.Count)];
                    var admin = allAdmins[_random.Next(allAdmins.Count)];

                    // Check if already nominated for this project
                    if (!await _context.Nominations.AnyAsync(n => n.ProjectId == project.Id && n.PersonId == person.Id))
                    {
                        _context.Nominations.Add(new Nomination
                        {
                            ProjectId = project.Id,
                            PersonId = person.Id,
                            SectorId = sector.Id,
                            DelegateId = admin.Id,
                            Status = NominationStatus.Submitted,
                            CreatedAt = JerusalemTime.Now
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();

            // ════════════════════════════════════════════
            //  6. Aid Management — Assistances
            // ════════════════════════════════════════════
            var existingAssistances = await _context.Assistances.CountAsync();
            if (existingAssistances == 0)
            {
                var assistanceNames = new[] { "مساعدات غذائية", "مساعدات طبية", "مساعدات نقدية", "مواد إيواء", "مساعدات شتوية", "حقيبة مدرسية", "مساعدات عاجلة" };
                var assistanceTypes = new[] { "غذائية", "طبية", "نقدية", "مواد إيواء", "غذائية", "تعليمية", "غذائية" };
                var sources = new[] { "الهلال الأحمر", "الأونروا", "وزارة التنمية", "تبرعات أهلية", "صندوق الزكاة", "منظمة الصحة العالمية", "اليونيسيف" };

                for (int i = 0; i < 20; i++)
                {
                    var sector = sectorMap[_sectors[_random.Next(_sectors.Length)]];
                    var idx = _random.Next(assistanceNames.Length);

                    var assistance = new Assistance
                    {
                        Name = assistanceNames[idx] + (_random.Next(0, 2) == 0 ? $" {i + 1}" : ""),
                        AssistanceType = assistanceTypes[idx],
                        Source = sources[_random.Next(sources.Length)],
                        AssistanceDate = JerusalemTime.Now.AddDays(-_random.Next(1, 180)),
                        SectorId = sector.Id,
                        Status = (AssistanceStatus)_random.Next(0, 3),
                        Description = "وصف " + assistanceNames[idx],
                        CreatedById = 1,
                        CreatedAt = JerusalemTime.Now.AddDays(-_random.Next(1, 90)),
                        IsDeleted = false
                    };

                    if (assistance.Status == AssistanceStatus.Approved)
                    {
                        assistance.ApprovedById = 1;
                        assistance.ApprovedAt = JerusalemTime.Now.AddDays(-_random.Next(1, 30));
                    }

                    _context.Assistances.Add(assistance);
                    await _context.SaveChangesAsync();

                    // Add beneficiaries for each assistance
                    int benCount = _random.Next(3, 15);
                    var usedIds = new HashSet<string>();
                    for (int j = 0; j < benCount; j++)
                    {
                        var nationalId = _random.Next(100000000, 999999999).ToString();
                        if (usedIds.Contains(nationalId)) continue;
                        usedIds.Add(nationalId);

                        var ben = new AssistanceBeneficiary
                        {
                            AssistanceId = assistance.Id,
                            FullName = _firstNames[_random.Next(_firstNames.Length)] + " " + _lastNames[_random.Next(_lastNames.Length)],
                            NationalId = nationalId,
                            Phone = "059" + _random.Next(1000000, 9999999).ToString(),
                            SectorId = sector.Id,
                            BenefitType = assistanceTypes[_random.Next(assistanceTypes.Length)],
                            Status = BeneficiaryStatus.Active,
                            Notes = _random.Next(0, 3) == 0 ? "ملاحظة اختبارية" : "",
                            CreatedById = 1,
                            CreatedAt = assistance.CreatedAt.AddMinutes(_random.Next(1, 1440)),
                            IsDeleted = false
                        };
                        _context.AssistanceBeneficiaries.Add(ben);
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // ════════════════════════════════════════════
            //  7. Aid Management — Import History
            // ════════════════════════════════════════════
            if (await _context.AssistanceImports.CountAsync() == 0)
            {
                for (int i = 0; i < 5; i++)
            {
                var sector = sectorMap[_sectors[_random.Next(_sectors.Length)]];
                var total = _random.Next(20, 100);
                var success = _random.Next(10, total - 5);
                var dup = _random.Next(0, 10);
                var failed = total - success - dup;

                var import = new AssistanceImport
                {
                    FileName = $"import_{JerusalemTime.Now.AddDays(-i):yyyyMMdd}.xlsx",
                    ImportedById = 1,
                    SectorId = sector.Id,
                    ImportedAt = JerusalemTime.Now.AddDays(-i * _random.Next(1, 14)),
                    TotalRows = total,
                    SuccessRows = success,
                    FailedRows = failed < 0 ? 0 : failed,
                    DuplicateRows = dup,
                    ErrorFilePath = failed > 0 ? Path.Combine("wwwroot", "uploads", "import-errors", $"error_{i}.xlsx") : null
                };
                _context.AssistanceImports.Add(import);
            }
                await _context.SaveChangesAsync();
            }

            Console.WriteLine("Dummy data seeding completed.");
        }
    }
}
