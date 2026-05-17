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

        var rows = await _context.Nominations
            .AsNoTracking()
            .Include(n => n.Person)
            .Include(n => n.Sector)
            .Where(n => n.ProjectId == projectId && !n.IsDeleted)
            .Select(n => new NominationRowViewModel
            {
                Id = n.Id,
                PersonId = n.PersonId,
                    PersonName = n.Person.FirstName + " " + n.Person.SecondName + " " + n.Person.ThirdName + " " + n.Person.LastName,
                    IdNumber = n.Person.IdNumber,
                    Phone = n.Person.PhoneNumber,
                Sector = n.Sector.Name,
                Status = n.Status.ToString(),
                Description = n.Description,
                Notes = n.Notes
            })
            .ToListAsync();

        for (int i = 0; i < rows.Count; i++)
            rows[i].RowNumber = i + 1;

        return new NominationPageViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectStatus = project.Status,
            RequiredCount = project.RequiredCount,
            ExistingCount = rows.Count,
            Rows = rows,
            IsAdmin = isAdmin,
            IsPastEndDate = project.EndDate < DateTime.UtcNow
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
            q = q.Where(p => p.Sector == sectorName);

        return await q
            .Where(p => p.IdNumber.Contains(query)
                || (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.LastName).Contains(query))
            .Take(20)
            .ToListAsync();
    }
}
