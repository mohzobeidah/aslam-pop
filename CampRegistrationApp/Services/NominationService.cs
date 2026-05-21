using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using CampRegistrationApp.Models.ViewModels;

namespace CampRegistrationApp.Services;

public interface INominationService
{
    Task<NominationPageViewModel> GetNominationPageAsync(int projectId, int delegateId, bool isAdmin);
    Task AddOrUpdateRowAsync(int projectId, int personId, int sectorId, int delegateId, string? description, string? notes);
    Task DeleteRowAsync(int nominationId);
    Task<bool> PersonIsNominatedInProjectAsync(int projectId, int personId);
    Task<Person?> FindPersonByIdNumberAsync(string idNumber);
    Task<List<Person>> SearchPersonsAsync(string query, string? sectorName = null);
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
            IsPastEndDate = project.EndDate < DateTime.UtcNow,
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
            existing.UpdatedAt = DateTime.UtcNow;
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
                .Where(fr => fr.Sector == sectorName)
                .Select(fr => fr.FamilyHeadId);
            q = q.Where(p => sectorPersonIds.Contains(p.Id));
        }

        return await q
            .Where(p => p.IdNumber.Contains(query)
                || (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.LastName).Contains(query))
            .Take(20)
            .ToListAsync();
    }
}
