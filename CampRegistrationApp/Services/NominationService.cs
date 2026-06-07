using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;

namespace CampRegistrationApp.Services;

public interface INominationService
{
    Task<NominationPageViewModel> GetNominationPageAsync(int projectId, int delegateId, bool isAdmin);
    Task AddOrUpdateRowAsync(int projectId, int personId, int sectorId, int delegateId, string? description, string? notes);
    Task AddMultipleRowsAsync(int projectId, List<int> personIds, int delegateId, string? notes);
    Task DeleteRowAsync(int nominationId);
    Task<bool> PersonIsNominatedInProjectAsync(int projectId, int personId);
    Task<Person?> FindPersonByIdNumberAsync(string idNumber);
    Task<List<Person>> SearchPersonsAsync(string query, string? sectorName = null);
    Task<List<FamilyHeadListItem>> GetFamilyHeadsAsync(string? sectorName = null, int? projectId = null);
    Task<BulkImportResult> ImportFromExcelAsync(Stream excelStream, int projectId, int delegateId);
}

public class FamilyHeadListItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Sector { get; set; }
    public bool AlreadyNominated { get; set; }
}

public class BulkImportResult
{
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int NotFoundCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class NominationService : INominationService
{
    private readonly ApplicationDbContext _context;

    public NominationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NominationPageViewModel> GetNominationPageAsync(int projectId, int delegateId, bool isAdmin)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
            throw new KeyNotFoundException("Project not found");

        int? adminSectorId = null;
        if (!isAdmin)
        {
            var admin = await _context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == delegateId);
            adminSectorId = admin?.SectorId;
        }

        var query = _context.Nominations
            .AsNoTracking()
            .Include(n => n.Person)
            .Include(n => n.Sector)
            .Where(n => n.ProjectId == projectId && !n.IsDeleted);

        if (adminSectorId.HasValue)
            query = query.Where(n => n.SectorId == adminSectorId.Value);

        var rows = await query
            .Select(n => new NominationRowViewModel
            {
                Id = n.Id,
                PersonId = n.PersonId,
                    PersonName = n.Person.FirstName + " " + n.Person.SecondName + " " + n.Person.ThirdName + " " + n.Person.LastName,
                    IdNumber = n.Person.IdNumber,
                    Phone = _context.FamilyRegistrations
                        .Where(fr => fr.FamilyHeadId == n.PersonId)
                        .Select(fr => fr.PhoneNumber)
                        .FirstOrDefault() ?? "",
                Sector = n.Sector.Name,
                Status = n.Status.ToString(),
                Description = n.Description,
                Notes = n.Notes
            })
            .ToListAsync();

        for (int i = 0; i < rows.Count; i++)
            rows[i].RowNumber = i + 1;

        var quotas = await _context.ProjectSectorQuotas
            .AsNoTracking()
            .Where(q => q.ProjectId == projectId)
            .ToListAsync();

        var sectorCounts = await _context.Nominations
            .AsNoTracking()
            .Where(n => n.ProjectId == projectId && !n.IsDeleted)
            .GroupBy(n => n.SectorId)
            .Select(g => new { SectorId = g.Key, Count = g.Count() })
            .ToListAsync();

        var countMap = sectorCounts.ToDictionary(x => x.SectorId, x => x.Count);

        var sectors = await _context.Sectors.AsNoTracking().ToListAsync();
        var sectorQuotaInfos = sectors.Select(s =>
        {
            var q = quotas.FirstOrDefault(qq => qq.SectorId == s.Id);
            return new SectorQuotaInfo
            {
                SectorId = s.Id,
                SectorName = s.Name,
                MaxCount = q?.MaxCount ?? 0,
                CurrentCount = countMap.GetValueOrDefault(s.Id, 0)
            };
        }).ToList();

        var createdBy = await _context.Admins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == project.CreatedById);

        return new NominationPageViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectStatus = project.Status,
            RequiredCount = project.RequiredCount,
            ExistingCount = rows.Count,
            Rows = rows,
            IsAdmin = isAdmin,
            IsPastEndDate = project.EndDate < JerusalemTime.Now,
            DelegateSectorId = adminSectorId,
            SectorQuotas = sectorQuotaInfos,
            Description = project.Description,
            Notes = project.Notes,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            CreatedByName = createdBy?.Name ?? ""
        };
    }

    public async Task AddOrUpdateRowAsync(int projectId, int personId, int sectorId, int delegateId, string? description, string? notes)
    {
        var existing = await _context.Nominations
            .FirstOrDefaultAsync(n => n.ProjectId == projectId && n.PersonId == personId && !n.IsDeleted);

        if (existing != null)
        {
            existing.SectorId = sectorId;
            existing.DelegateId = delegateId;
            existing.Description = description ?? existing.Description;
            existing.Notes = notes ?? existing.Notes;
            existing.Status = NominationStatus.Draft;
            existing.UpdatedAt = JerusalemTime.Now;
        }
        else
        {
            var quota = await _context.ProjectSectorQuotas
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.ProjectId == projectId && q.SectorId == sectorId);

            if (quota != null && quota.MaxCount > 0)
            {
                var currentCount = await _context.Nominations
                    .CountAsync(n => n.ProjectId == projectId && n.SectorId == sectorId && !n.IsDeleted);

                if (currentCount >= quota.MaxCount)
                    throw new InvalidOperationException("تم الوصول للحد الأقصى لترشيحات هذا القطاع");
            }

            _context.Nominations.Add(new Nomination
            {
                ProjectId = projectId,
                PersonId = personId,
                SectorId = sectorId,
                DelegateId = delegateId,
                Description = description,
                Notes = notes,
                Status = NominationStatus.Draft
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteRowAsync(int nominationId)
    {
        var nom = await _context.Nominations.FindAsync(nominationId);
        if (nom != null)
        {
            nom.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> PersonIsNominatedInProjectAsync(int projectId, int personId)
    {
        return await _context.Nominations
            .AnyAsync(n => n.ProjectId == projectId && n.PersonId == personId && !n.IsDeleted);
    }

    public async Task<Person?> FindPersonByIdNumberAsync(string idNumber)
    {
        return await _context.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdNumber == idNumber);
    }

    public async Task<List<Person>> SearchPersonsAsync(string query, string? sectorName = null)
    {
        var q = _context.Persons.AsNoTracking();

        if (!string.IsNullOrEmpty(sectorName))
        {
            var sectorPersonIds = _context.FamilyRegistrations
                .Where(fr => fr.Sector.Name == sectorName)
                .Select(fr => fr.FamilyHeadId);
            q = q.Where(p => sectorPersonIds.Contains(p.Id));
        }

        return await q
            .Where(p => p.IdNumber.Contains(query)
                || (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.LastName).Contains(query))
            .Take(20)
            .ToListAsync();
    }

    public async Task AddMultipleRowsAsync(int projectId, List<int> personIds, int delegateId, string? notes)
    {
        foreach (var personId in personIds)
        {
            var existing = await _context.Nominations
                .AnyAsync(n => n.ProjectId == projectId && n.PersonId == personId && !n.IsDeleted);
            if (existing) continue;

            var sectorName = await _context.FamilyRegistrations
                .Where(fr => fr.FamilyHeadId == personId)
                .Select(fr => fr.Sector.Name)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(sectorName)) continue;

            var sectorEntity = await _context.Sectors
                .FirstOrDefaultAsync(s => s.Name == sectorName);
            if (sectorEntity == null) continue;

            var quota = await _context.ProjectSectorQuotas
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.ProjectId == projectId && q.SectorId == sectorEntity.Id);

            if (quota != null && quota.MaxCount > 0)
            {
                var currentCount = await _context.Nominations
                    .CountAsync(n => n.ProjectId == projectId && n.SectorId == sectorEntity.Id && !n.IsDeleted);
                if (currentCount >= quota.MaxCount) continue;
            }

            _context.Nominations.Add(new Nomination
            {
                ProjectId = projectId,
                PersonId = personId,
                SectorId = sectorEntity.Id,
                DelegateId = delegateId,
                Notes = notes,
                Status = NominationStatus.Draft
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<FamilyHeadListItem>> GetFamilyHeadsAsync(string? sectorName = null, int? projectId = null)
    {
        var query = _context.FamilyRegistrations
            .AsNoTracking()
            .Include(fr => fr.FamilyHead)
            .Include(fr => fr.Sector)
            .Where(fr => !fr.IsDeleted);

        if (!string.IsNullOrEmpty(sectorName))
            query = query.Where(fr => fr.Sector.Name == sectorName);

        var heads = await query
            .Select(fr => new
            {
                fr.FamilyHead.Id,
                fr.FamilyHead.FirstName,
                fr.FamilyHead.SecondName,
                fr.FamilyHead.ThirdName,
                fr.FamilyHead.LastName,
                fr.FamilyHead.IdNumber,
                fr.PhoneNumber,
                SectorName = fr.Sector.Name
            })
            .ToListAsync();

        List<int>? alreadyNominatedIds = null;
        if (projectId.HasValue)
        {
            alreadyNominatedIds = await _context.Nominations
                .AsNoTracking()
                .Where(n => n.ProjectId == projectId.Value && !n.IsDeleted)
                .Select(n => n.PersonId)
                .ToListAsync();
        }

        return heads
            .OrderBy(h => h.FirstName)
            .ThenBy(h => h.SecondName)
            .ThenBy(h => h.ThirdName)
            .ThenBy(h => h.LastName)
            .Select(h => new FamilyHeadListItem
            {
                Id = h.Id,
                FullName = $"{h.FirstName} {h.SecondName} {h.ThirdName} {h.LastName}",
                IdNumber = h.IdNumber,
                Phone = h.PhoneNumber,
                Sector = h.SectorName,
                AlreadyNominated = alreadyNominatedIds?.Contains(h.Id) ?? false
            }).ToList();
    }

    public async Task<BulkImportResult> ImportFromExcelAsync(Stream excelStream, int projectId, int delegateId)
    {
        var result = new BulkImportResult();

        using var workbook = new ClosedXML.Excel.XLWorkbook(excelStream);
        var ws = workbook.Worksheet(1);
        var range = ws.RangeUsed();
        var rows = range != null ? range.RowsUsed().Skip(1) : Enumerable.Empty<ClosedXML.Excel.IXLRangeRow>();

        foreach (var row in rows)
        {
            var nationalId = row.Cell(1).GetString().Trim();
            var notes = row.Cell(2).GetString().Trim();

            if (string.IsNullOrEmpty(nationalId))
            {
                result.NotFoundCount++;
                continue;
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(p => p.IdNumber == nationalId);

            if (person == null)
            {
                result.NotFoundCount++;
                result.Errors.Add($"رقم الهوية {nationalId} غير موجود في النظام");
                continue;
            }

            var isHead = await _context.FamilyRegistrations
                .AnyAsync(fr => fr.FamilyHeadId == person.Id && !fr.IsDeleted);

            if (!isHead)
            {
                result.SkippedCount++;
                result.Errors.Add($"{person.FullName} ({nationalId}) ليس رب أسرة");
                continue;
            }

            var existing = await _context.Nominations
                .AnyAsync(n => n.ProjectId == projectId && n.PersonId == person.Id && !n.IsDeleted);
            if (existing)
            {
                result.SkippedCount++;
                result.Errors.Add($"{person.FullName} ({nationalId}) مرشح مسبقاً");
                continue;
            }

            var sectorName = await _context.FamilyRegistrations
                .Where(fr => fr.FamilyHeadId == person.Id)
                .Select(fr => fr.Sector.Name)
                .FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(sectorName))
            {
                result.SkippedCount++;
                continue;
            }

            var sectorEntity = await _context.Sectors
                .FirstOrDefaultAsync(s => s.Name == sectorName);
            if (sectorEntity == null)
            {
                result.SkippedCount++;
                continue;
            }

            var quota = await _context.ProjectSectorQuotas
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.ProjectId == projectId && q.SectorId == sectorEntity.Id);
            if (quota != null && quota.MaxCount > 0)
            {
                var currentCount = await _context.Nominations
                    .CountAsync(n => n.ProjectId == projectId && n.SectorId == sectorEntity.Id && !n.IsDeleted);
                if (currentCount >= quota.MaxCount)
                {
                    result.SkippedCount++;
                    result.Errors.Add($"{person.FullName} ({nationalId}) تجاوز الحد الأقصى للقطاع");
                    continue;
                }
            }

            _context.Nominations.Add(new Nomination
            {
                ProjectId = projectId,
                PersonId = person.Id,
                SectorId = sectorEntity.Id,
                DelegateId = delegateId,
                Notes = string.IsNullOrEmpty(notes) ? null : notes,
                Status = NominationStatus.Draft
            });
            result.SuccessCount++;
        }

        await _context.SaveChangesAsync();
        return result;
    }
}
