using ClosedXML.Excel;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CampRegistrationApp.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ColumnGroup> GetColumnGroups()
        {
            return new List<ColumnGroup>
            {
                new ColumnGroup
                {
                    GroupName = "بيانات التسجيل",
                    Columns = new List<ColumnDef>
                    {
                        new() { Key = "RecordId", Label = "معرف التسجيل", IsDefault = true },
                        new() { Key = "RegistrationDate", Label = "تاريخ التسجيل", IsDefault = true },
                        new() { Key = "ApprovalStatus", Label = "الحالة", IsDefault = true },
                        new() { Key = "MemberCount", Label = "عدد الأفراد", IsDefault = true },
                        new() { Key = "Wallet", Label = "المحفظة" },
                        new() { Key = "WalletType", Label = "نوع المحفظة" },
                        new() { Key = "StatusNotes", Label = "ملاحظات" },
                    }
                },
                new ColumnGroup
                {
                    GroupName = "رب الأسرة",
                    Columns = new List<ColumnDef>
                    {
                        new() { Key = "HeadName", Label = "الاسم الكامل", IsDefault = true },
                        new() { Key = "IdNumber", Label = "رقم الهوية", IsDefault = true },
                        new() { Key = "Phone", Label = "رقم الجوال", IsDefault = true },
                        new() { Key = "Sector", Label = "القاطع", IsDefault = true },
                        new() { Key = "HeadGender", Label = "الجنس" },
                        new() { Key = "HeadDOB", Label = "تاريخ الميلاد" },
                        new() { Key = "HeadAge", Label = "العمر" },
                        new() { Key = "OriginalGovernorate", Label = "المحافظة الأصلية" },
                        new() { Key = "MaritalStatus", Label = "الحالة الاجتماعية" },
                        new() { Key = "EmploymentStatus", Label = "الوظيفة" },
                        new() { Key = "EducationLevel", Label = "المستوى التعليمي" },
                        new() { Key = "MotherIdNumber", Label = "رقم هوية الأم" },
                    }
                },
                new ColumnGroup
                {
                    GroupName = "الصحة (رب الأسرة)",
                    Columns = new List<ColumnDef>
                    {
                        new() { Key = "HeadHealthStatus", Label = "الحالة الصحية" },
                        new() { Key = "HeadChronicDiseases", Label = "الأمراض المزمنة" },
                        new() { Key = "HeadDisabilityTypes", Label = "الإعاقات" },
                        new() { Key = "HasInjury", Label = "إصابة" },
                        new() { Key = "InjuryDetails", Label = "تفاصيل الإصابة" },
                        new() { Key = "HeadBathroomStatus", Label = "حالة الحمام" },
                    }
                },
                new ColumnGroup
                {
                    GroupName = "حالات خاصة (رب الأسرة)",
                    Columns = new List<ColumnDef>
                    {
                        new() { Key = "IsPregnant", Label = "حامل" },
                        new() { Key = "PregnancyMonth", Label = "شهر الحمل" },
                        new() { Key = "IsNursing", Label = "مرضع" },
                        new() { Key = "NursingInfantName", Label = "اسم الطفل الرضيع" },
                        new() { Key = "IsPrisoner", Label = "أسير" },
                        new() { Key = "IsHusbandPrisoner", Label = "زوج أسير" },
                    }
                },
                new ColumnGroup
                {
                    GroupName = "السكن",
                    Columns = new List<ColumnDef>
                    {
                        new() { Key = "LivesInTent", Label = "يسكن خيمة" },
                        new() { Key = "TentType", Label = "نوع الخيمة" },
                        new() { Key = "HasBathroom", Label = "يوجد حمام" },
                        new() { Key = "BathroomType", Label = "نوع الحمام" },
                        new() { Key = "RegBathroomStatus", Label = "حالة الحمام (المسكن)" },
                        new() { Key = "IsChildHeaded", Label = "طفل يعيل" },
                        new() { Key = "IsFemaleHeaded", Label = "امرأة تعيل" },
                        new() { Key = "SupportsOutsidePerson", Label = "دعم خارج العائلة" },
                        new() { Key = "HasMultipleFamiliesInTent", Label = "أسر بنفس الخيمة" },
                    }
                },
                new ColumnGroup
                {
                    GroupName = "أفراد العائلة",
                    Columns = new List<ColumnDef>
                    {
                        new() { Key = "Wives", Label = "الزوجات (أسماء + بيانات)" },
                        new() { Key = "Children", Label = "الأبناء (أسماء + بيانات)" },
                        new() { Key = "OtherMembers", Label = "أفراد آخرون (أسماء)" },
                    }
                },
                new ColumnGroup
                {
                    GroupName = "الشخص المعني (تقارير الإعاقة/الأمراض)",
                    Columns = new List<ColumnDef>
                    {
                        new() { Key = "PersonName", Label = "اسم الشخص" },
                        new() { Key = "PersonIdNumber", Label = "رقم هوية الشخص" },
                        new() { Key = "PersonRelationship", Label = "صلة القرابة" },
                        new() { Key = "PersonGender", Label = "جنس الشخص" },
                        new() { Key = "PersonDOB", Label = "تاريخ ميلاد الشخص" },
                        new() { Key = "PersonAge", Label = "عمر الشخص" },
                        new() { Key = "PersonHealthStatus", Label = "الحالة الصحية للشخص" },
                        new() { Key = "PersonChronicDiseases", Label = "أمراضه المزمنة" },
                        new() { Key = "PersonDisabilityTypes", Label = "إعاقاته" },
                        new() { Key = "PersonBathroomStatus", Label = "حالة حمام الشخص" },
                        new() { Key = "PersonIsPregnant", Label = "حامل (الشخص)" },
                        new() { Key = "PersonIsNursing", Label = "مرضع (الشخص)" },
                        new() { Key = "PersonIsPrisoner", Label = "أسير (الشخص)" },
                        new() { Key = "PersonMaritalStatus", Label = "حالة الشخص الاجتماعية" },
                    }
                }
            };
        }

        public async Task<List<ReportRow>> GetReportDataAsync(ReportFilter filter, List<string> selectedColumns, int? adminSectorId = null)
        {
            var canIncludeMembers = selectedColumns.Contains("Wives") || selectedColumns.Contains("Children") || selectedColumns.Contains("OtherMembers");

            if (filter.ReportType == "Disabled" || filter.ReportType == "ChronicSick" || filter.ReportType == "Pregnant" || filter.ReportType == "Nursing")
            {
                return await GetPersonReportAsync(filter, selectedColumns, adminSectorId);
            }

            return await GetFamilyReportAsync(filter, selectedColumns, adminSectorId, canIncludeMembers);
        }

        private async Task<List<ReportRow>> GetFamilyReportAsync(ReportFilter filter, List<string> selectedColumns, int? adminSectorId, bool includeMembers)
        {
            var query = _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Sector)
                .Include(f => f.Members).ThenInclude(m => m.Person)
                .AsQueryable();

            if (adminSectorId.HasValue)
                query = query.Where(f => f.SectorId == adminSectorId.Value);

            if (filter.SectorId.HasValue)
                query = query.Where(f => f.SectorId == filter.SectorId.Value);

            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<RegistrationApprovalStatus>(filter.Status, out var statusFilter))
                query = query.Where(f => f.ApprovalStatus == statusFilter);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var s = filter.Search;
                query = query.Where(f =>
                    f.FamilyHead.IdNumber.Contains(s) ||
                    f.FamilyHead.FirstName.Contains(s) ||
                    f.FamilyHead.LastName.Contains(s) ||
                    f.FamilyHead.FullName.Contains(s) ||
                    f.RecordId.Contains(s));
            }

            if (!string.IsNullOrEmpty(filter.Gender))
                query = query.Where(f => f.FamilyHead.Gender == filter.Gender);

            if (!string.IsNullOrEmpty(filter.HealthStatus))
                query = query.Where(f => f.FamilyHead.HealthStatus == filter.HealthStatus);

            var registrations = await query
                .OrderByDescending(f => f.RegistrationTimestamp)
                .ToListAsync();

            var rows = new List<ReportRow>();

            foreach (var reg in registrations)
            {
                var head = reg.FamilyHead!;
                var row = new ReportRow();
                var v = row.Values;

                v["RecordId"] = reg.RecordId;
                v["RegistrationDate"] = reg.RegistrationTimestamp.ToString("yyyy-MM-dd");
                v["ApprovalStatus"] = reg.ApprovalStatus switch
                {
                    RegistrationApprovalStatus.Approved => "مقبول",
                    RegistrationApprovalStatus.Rejected => "مرفوض",
                    _ => "قيد المراجعة"
                };
                v["MemberCount"] = reg.Members.Count;
                v["Wallet"] = reg.Wallet ?? "";
                v["WalletType"] = reg.WalletType ?? "";
                v["StatusNotes"] = reg.StatusNotes ?? "";

                v["HeadName"] = head.FullName;
                v["IdNumber"] = head.IdNumber;
                v["Phone"] = reg.PhoneNumber;
                v["Sector"] = reg.Sector?.Name ?? "";
                v["HeadGender"] = head.Gender == "male" ? "ذكر" : head.Gender == "female" ? "أنثى" : head.Gender;
                v["HeadDOB"] = head.DateOfBirth.ToString("yyyy-MM-dd");
                v["HeadAge"] = CalculateAge(head.DateOfBirth);
                v["OriginalGovernorate"] = head.OriginalGovernorate;
                v["MaritalStatus"] = head.MaritalStatus;
                v["EmploymentStatus"] = head.EmploymentStatus ?? "";
                v["EducationLevel"] = head.EducationLevel ?? "";
                v["MotherIdNumber"] = head.MotherIdNumber ?? "";

                v["HeadHealthStatus"] = head.HealthStatus ?? "";
                v["HeadChronicDiseases"] = head.ChronicDiseases ?? "";
                v["HeadDisabilityTypes"] = head.DisabilityTypes ?? "";
                v["HasInjury"] = head.HasInjury ? "نعم" : "لا";
                v["InjuryDetails"] = head.InjuryDetails ?? "";
                v["HeadBathroomStatus"] = head.BathroomStatus ?? "";

                v["IsPregnant"] = head.IsPregnant == true ? "نعم" : "لا";
                v["PregnancyMonth"] = head.PregnancyMonth?.ToString() ?? "";
                v["IsNursing"] = head.IsNursing == true ? "نعم" : "لا";
                v["NursingInfantName"] = head.NursingInfantName ?? "";
                v["IsPrisoner"] = head.IsPrisoner ? "نعم" : "لا";
                v["IsHusbandPrisoner"] = head.IsHusbandPrisoner ? "نعم" : "لا";

                v["LivesInTent"] = reg.LivesInTent ? "نعم" : "لا";
                v["TentType"] = reg.TentType ?? "";
                v["HasBathroom"] = reg.HasBathroom ? "نعم" : "لا";
                v["BathroomType"] = reg.BathroomType ?? "";
                v["RegBathroomStatus"] = reg.FamilyHead?.BathroomStatus ?? "";
                v["IsChildHeaded"] = reg.IsChildHeaded ? "نعم" : "لا";
                v["IsFemaleHeaded"] = reg.IsFemaleHeaded ? "نعم" : "لا";
                v["SupportsOutsidePerson"] = reg.SupportsOutsidePerson ? "نعم" : "لا";
                v["HasMultipleFamiliesInTent"] = reg.HasMultipleFamiliesInTent ? "نعم" : "لا";

                if (includeMembers)
                {
                    var members = reg.Members.ToList();
                    var wives = members.Where(m => m.RelationshipToHead == "زوجة").ToList();
                    var children = members.Where(m => m.RelationshipToHead == "ابن" || m.RelationshipToHead == "ابنة").ToList();
                    var others = members.Where(m => m.RelationshipToHead != "زوجة" && m.RelationshipToHead != "ابن" && m.RelationshipToHead != "ابنة").ToList();

                    if (selectedColumns.Contains("Wives"))
                    {
                        for (int i = 0; i < wives.Count; i++)
                        {
                            var w = wives[i].Person;
                            var idx = i + 1;
                            v[$"Wife{idx}_Name"] = w.FullName;
                            v[$"Wife{idx}_IdNumber"] = w.IdNumber;
                            v[$"Wife{idx}_DOB"] = w.DateOfBirth.ToString("yyyy-MM-dd");
                            v[$"Wife{idx}_Age"] = CalculateAge(w.DateOfBirth);
                            v[$"Wife{idx}_HealthStatus"] = w.HealthStatus ?? "";
                            v[$"Wife{idx}_ChronicDiseases"] = w.ChronicDiseases ?? "";
                            v[$"Wife{idx}_DisabilityTypes"] = w.DisabilityTypes ?? "";
                            v[$"Wife{idx}_BathroomStatus"] = w.BathroomStatus ?? "";
                            v[$"Wife{idx}_IsPregnant"] = w.IsPregnant == true ? "نعم" : "لا";
                            v[$"Wife{idx}_IsNursing"] = w.IsNursing == true ? "نعم" : "لا";
                        }
                    }

                    if (selectedColumns.Contains("Children"))
                    {
                        for (int i = 0; i < children.Count; i++)
                        {
                            var c = children[i].Person;
                            var idx = i + 1;
                            v[$"Child{idx}_Name"] = c.FullName;
                            v[$"Child{idx}_IdNumber"] = c.IdNumber;
                            v[$"Child{idx}_DOB"] = c.DateOfBirth.ToString("yyyy-MM-dd");
                            v[$"Child{idx}_Age"] = CalculateAge(c.DateOfBirth);
                            v[$"Child{idx}_Gender"] = c.Gender == "male" ? "ذكر" : "أنثى";
                            v[$"Child{idx}_HealthStatus"] = c.HealthStatus ?? "";
                            v[$"Child{idx}_ChronicDiseases"] = c.ChronicDiseases ?? "";
                            v[$"Child{idx}_DisabilityTypes"] = c.DisabilityTypes ?? "";
                            v[$"Child{idx}_BathroomStatus"] = c.BathroomStatus ?? "";
                        }
                    }

                    if (selectedColumns.Contains("OtherMembers"))
                    {
                        var otherNames = others.Select(o => o.Person.FullName + " (" + o.RelationshipToHead + ")").ToList();
                        v["OtherMembers"] = string.Join("، ", otherNames);
                    }
                }

                rows.Add(row);
            }

            return rows;
        }

        private async Task<List<ReportRow>> GetPersonReportAsync(ReportFilter filter, List<string> selectedColumns, int? adminSectorId)
        {
            var familyQuery = _context.FamilyRegistrations
                .Include(f => f.FamilyHead)
                .Include(f => f.Sector)
                .Include(f => f.Members).ThenInclude(m => m.Person)
                .AsQueryable();

            if (adminSectorId.HasValue)
                familyQuery = familyQuery.Where(f => f.SectorId == adminSectorId.Value);

            if (filter.SectorId.HasValue)
                familyQuery = familyQuery.Where(f => f.SectorId == filter.SectorId.Value);

            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<RegistrationApprovalStatus>(filter.Status, out var statusFilter))
                familyQuery = familyQuery.Where(f => f.ApprovalStatus == statusFilter);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var s = filter.Search;
                familyQuery = familyQuery.Where(f =>
                    f.FamilyHead.IdNumber.Contains(s) ||
                    f.FamilyHead.FirstName.Contains(s) ||
                    f.FamilyHead.LastName.Contains(s) ||
                    f.RecordId.Contains(s));
            }

            var registrations = await familyQuery
                .OrderByDescending(f => f.RegistrationTimestamp)
                .ToListAsync();

            var rows = new List<ReportRow>();

            foreach (var reg in registrations)
            {
                var head = reg.FamilyHead!;
                var allPeople = new List<(Person person, string relationship)>();

                allPeople.Add((head, "رب أسرة"));

                foreach (var m in reg.Members)
                    allPeople.Add((m.Person, m.RelationshipToHead));

                foreach (var (person, relationship) in allPeople)
                {
                    bool include = filter.ReportType switch
                    {
                        "Disabled" => !string.IsNullOrEmpty(person.DisabilityTypes),
                        "ChronicSick" => !string.IsNullOrEmpty(person.ChronicDiseases),
                        "Pregnant" => person.IsPregnant == true,
                        "Nursing" => person.IsNursing == true,
                        _ => false
                    };

                    if (!include) continue;

                    if (!string.IsNullOrEmpty(filter.Gender) && person.Gender != filter.Gender) continue;
                    if (!string.IsNullOrEmpty(filter.HealthStatus) && person.HealthStatus != filter.HealthStatus) continue;

                    var age = CalculateAge(person.DateOfBirth);
                    if (filter.AgeFrom.HasValue && age < filter.AgeFrom.Value) continue;
                    if (filter.AgeTo.HasValue && age > filter.AgeTo.Value) continue;

                    var row = new ReportRow();
                    var v = row.Values;

                    v["RecordId"] = reg.RecordId;
                    v["RegistrationDate"] = reg.RegistrationTimestamp.ToString("yyyy-MM-dd");
                    v["ApprovalStatus"] = reg.ApprovalStatus switch
                    {
                        RegistrationApprovalStatus.Approved => "مقبول",
                        RegistrationApprovalStatus.Rejected => "مرفوض",
                        _ => "قيد المراجعة"
                    };
                    v["MemberCount"] = reg.Members.Count;

                    v["HeadName"] = head.FullName;
                    v["IdNumber"] = head.IdNumber;
                    v["Phone"] = reg.PhoneNumber;
                    v["Sector"] = reg.Sector?.Name ?? "";
                    v["HeadGender"] = head.Gender == "male" ? "ذكر" : head.Gender == "female" ? "أنثى" : head.Gender;
                    v["Wallet"] = reg.Wallet ?? "";

                    v["PersonName"] = person.FullName;
                    v["PersonIdNumber"] = person.IdNumber;
                    v["PersonRelationship"] = relationship;
                    v["PersonGender"] = person.Gender == "male" ? "ذكر" : person.Gender == "female" ? "أنثى" : person.Gender;
                    v["PersonDOB"] = person.DateOfBirth.ToString("yyyy-MM-dd");
                    v["PersonAge"] = age;
                    v["PersonHealthStatus"] = person.HealthStatus ?? "";
                    v["PersonChronicDiseases"] = person.ChronicDiseases ?? "";
                    v["PersonDisabilityTypes"] = person.DisabilityTypes ?? "";
                    v["PersonBathroomStatus"] = person.BathroomStatus ?? "";
                    v["PersonIsPregnant"] = person.IsPregnant == true ? "نعم" : "لا";
                    v["PersonIsNursing"] = person.IsNursing == true ? "نعم" : "لا";
                    v["PersonIsPrisoner"] = person.IsPrisoner ? "نعم" : "لا";
                    v["PersonMaritalStatus"] = person.MaritalStatus ?? "";

                    rows.Add(row);
                }
            }

            return rows;
        }

        public async Task<byte[]> GenerateExcelAsync(List<ReportRow> rows, List<string> selectedColumns)
        {
            var allColumns = GetColumnGroups().SelectMany(g => g.Columns).ToList();
            var columnsToExport = allColumns.Where(c => selectedColumns.Contains(c.Key)).ToList();

            var allDynamicKeys = new List<string>();
            foreach (var row in rows)
            {
                foreach (var key in row.Values.Keys)
                {
                    if (!selectedColumns.Contains(key) && !key.StartsWith("Wife") && !key.StartsWith("Child") && key != "OtherMembers")
                        continue;
                    if (!allDynamicKeys.Contains(key))
                        allDynamicKeys.Add(key);
                }
            }

            var orderedKeys = new List<string>();
            foreach (var col in columnsToExport)
            {
                if (col.Key == "Wives")
                {
                    var wifeKeys = allDynamicKeys.Where(k => k.StartsWith("Wife")).ToList();
                    orderedKeys.AddRange(wifeKeys);
                }
                else if (col.Key == "Children")
                {
                    var childKeys = allDynamicKeys.Where(k => k.StartsWith("Child")).ToList();
                    orderedKeys.AddRange(childKeys);
                }
                else if (col.Key == "OtherMembers")
                {
                    if (allDynamicKeys.Contains("OtherMembers"))
                        orderedKeys.Add("OtherMembers");
                }
                else
                {
                    orderedKeys.Add(col.Key);
                }
            }

            var headerLabels = new Dictionary<string, string>();
            foreach (var col in allColumns)
                headerLabels[col.Key] = col.Label;

            foreach (var key in orderedKeys)
            {
                if (!headerLabels.ContainsKey(key))
                {
                    if (key.StartsWith("Wife") && key.Contains("_"))
                    {
                        var parts = key.Split('_');
                        var num = parts[0].Replace("Wife", "");
                        var field = parts[1] switch
                        {
                            "Name" => "الاسم",
                            "IdNumber" => "رقم الهوية",
                            "DOB" => "تاريخ الميلاد",
                            "Age" => "العمر",
                            "HealthStatus" => "الحالة الصحية",
                            "ChronicDiseases" => "أمراض مزمنة",
                            "DisabilityTypes" => "إعاقات",
                            "BathroomStatus" => "حالة الحمام",
                            "IsPregnant" => "حامل",
                            "IsNursing" => "مرضع",
                            _ => parts[1]
                        };
                        headerLabels[key] = $"الزوجة {num} - {field}";
                    }
                    else if (key.StartsWith("Child") && key.Contains("_"))
                    {
                        var parts = key.Split('_');
                        var num = parts[0].Replace("Child", "");
                        var field = parts[1] switch
                        {
                            "Name" => "الاسم",
                            "IdNumber" => "رقم الهوية",
                            "DOB" => "تاريخ الميلاد",
                            "Age" => "العمر",
                            "Gender" => "الجنس",
                            "HealthStatus" => "الحالة الصحية",
                            "ChronicDiseases" => "أمراض مزمنة",
                            "DisabilityTypes" => "إعاقات",
                            "BathroomStatus" => "حالة الحمام",
                            _ => parts[1]
                        };
                        headerLabels[key] = $"الابن {num} - {field}";
                    }
                    else if (key == "OtherMembers")
                    {
                        headerLabels[key] = "أفراد آخرون";
                    }
                }
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("التقرير");

            for (int c = 0; c < orderedKeys.Count; c++)
            {
                ws.Cell(1, c + 1).Value = headerLabels.GetValueOrDefault(orderedKeys[c], orderedKeys[c]);
                ws.Cell(1, c + 1).Style.Font.Bold = true;
                ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.Gold;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                for (int c = 0; c < orderedKeys.Count; c++)
                {
                    var val = rows[r].Values.GetValueOrDefault(orderedKeys[c]);
                    ws.Cell(r + 2, c + 1).Value = val?.ToString() ?? "";
                    ws.Cell(r + 2, c + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = JerusalemTime.Now.Date;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
