using System.Text.Json;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CampRegistrationApp.Services;

public class AssistanceService : IAssistanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;

    public AssistanceService(ApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<List<Assistance>> GetAllAsync(string? sector, string? search, string? status, bool isAdmin, int adminId)
    {
        var query = _context.Assistances
            .Include(a => a.Sector)
            .Include(a => a.CreatedBy)
            .Include(a => a.ApprovedBy)
            .AsQueryable();

        if (!isAdmin)
        {
            var admin = await _context.Admins.Include(a => a.Sector).FirstAsync(a => a.Id == adminId);
            if (admin.Sector != null)
                query = query.Where(a => a.SectorId == admin.Sector.Id);
        }

        if (!string.IsNullOrEmpty(sector))
            query = query.Where(a => a.Sector.Name == sector);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a =>
                a.Name.Contains(search) ||
                a.Source.Contains(search));

        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Deleted")
                query = query.Where(a => a.IsDeleted);
            else if (Enum.TryParse<AssistanceStatus>(status, true, out var st))
                query = query.Where(a => a.Status == st && !a.IsDeleted);
        }
        else
        {
            query = query.Where(a => !a.IsDeleted);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Assistance?> GetByIdAsync(int id)
    {
        return await _context.Assistances
            .Include(a => a.Sector)
            .Include(a => a.CreatedBy)
            .Include(a => a.ApprovedBy)
            .Include(a => a.Beneficiaries.Where(b => !b.IsDeleted))
                .ThenInclude(b => b.Sector)
            .Include(a => a.Beneficiaries.Where(b => !b.IsDeleted))
                .ThenInclude(b => b.CreatedBy)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Assistance> CreateAsync(Assistance assistance, int userId)
    {
        assistance.CreatedById = userId;
        assistance.CreatedAt = JerusalemTime.Now;
        _context.Assistances.Add(assistance);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "إنشاء مساعدة", "Assistances",
            assistance.Id.ToString(), null, new { assistance.Name, assistance.Source });

        return assistance;
    }

    public async Task<Assistance> UpdateAsync(Assistance updated, int userId)
    {
        var existing = await _context.Assistances.FindAsync(updated.Id)
            ?? throw new InvalidOperationException("المساعدة غير موجودة");

        var old = new { existing.Name, existing.Source, existing.Status, existing.Description };

        existing.Name = updated.Name;
        existing.AssistanceType = updated.AssistanceType;
        existing.Source = updated.Source;
        existing.AssistanceDate = updated.AssistanceDate;
        existing.Description = updated.Description;
        existing.SectorId = updated.SectorId;
        existing.AttachmentsPath = updated.AttachmentsPath;

        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "تعديل مساعدة", "Assistances",
            existing.Id.ToString(), old, new { existing.Name, existing.Source, existing.Status });

        return existing;
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var entity = await _context.Assistances.FindAsync(id)
            ?? throw new InvalidOperationException("المساعدة غير موجودة");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "حذف مساعدة", "Assistances",
            id.ToString(), new { entity.Name }, null);
    }

    public async Task ApproveAsync(int id, int userId)
    {
        var entity = await _context.Assistances.FindAsync(id)
            ?? throw new InvalidOperationException("المساعدة غير موجودة");

        entity.Status = AssistanceStatus.Approved;
        entity.ApprovedById = userId;
        entity.ApprovedAt = JerusalemTime.Now;
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "اعتماد مساعدة", "Assistances",
            id.ToString(), new { Status = "Draft" }, new { Status = "Approved" });
    }

    public async Task CancelAsync(int id, int userId)
    {
        var entity = await _context.Assistances.FindAsync(id)
            ?? throw new InvalidOperationException("المساعدة غير موجودة");

        entity.Status = AssistanceStatus.Cancelled;
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "إلغاء مساعدة", "Assistances",
            id.ToString(), null, new { Status = "Cancelled" });
    }

    // ── Person Search ──

    public async Task<List<Person>> SearchPersonsAsync(string query, string? sectorName)
    {
        var q = _context.Persons.AsQueryable();

        if (!string.IsNullOrEmpty(sectorName))
        {
            q = q.Where(p =>
                _context.FamilyRegistrations
                    .Where(fr => fr.FamilyHeadId == p.Id && fr.Sector.Name == sectorName)
                    .Any() ||
                _context.FamilyMembers
                    .Where(fm => fm.PersonId == p.Id && fm.Registration.Sector.Name == sectorName)
                    .Any());
        }

        if (query.All(char.IsDigit))
        {
            q = q.Where(p => p.IdNumber.Contains(query));
        }
        else if (query.Contains('%'))
        {
            var parts = query.Split('%', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var pn = part;
                q = q.Where(p => (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.LastName).Contains(pn));
            }
        }
        else
        {
            q = q.Where(p => (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.LastName).Contains(query));
        }

        return await q.Take(100).ToListAsync();
    }

    // ── Beneficiaries ──

    public async Task<AssistanceBeneficiary> AddBeneficiaryAsync(AssistanceBeneficiary beneficiary, int userId)
    {
        var exists = await _context.AssistanceBeneficiaries
            .AnyAsync(b => b.NationalId == beneficiary.NationalId && b.AssistanceId == beneficiary.AssistanceId);
        if (exists)
            throw new InvalidOperationException("رقم الهوية موجود مسبقاً في هذه المساعدة");

        beneficiary.CreatedById = userId;
        beneficiary.CreatedAt = JerusalemTime.Now;
        _context.AssistanceBeneficiaries.Add(beneficiary);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "إضافة مستفيد", "AssistanceBeneficiaries",
            beneficiary.Id.ToString(), null, new { beneficiary.FullName, beneficiary.NationalId });

        return beneficiary;
    }

    public async Task<AssistanceBeneficiary> AddBeneficiaryFromPersonAsync(int personId, int assistanceId, int userId)
    {
        var person = await _context.Persons.FindAsync(personId)
            ?? throw new InvalidOperationException("الشخص غير موجود");

        var exists = await _context.AssistanceBeneficiaries
            .AnyAsync(b => b.NationalId == person.IdNumber && b.AssistanceId == assistanceId);
        if (exists)
            throw new InvalidOperationException("رقم الهوية موجود مسبقاً في هذه المساعدة");

        string? sectorName = null;
        var headRegistration = await _context.FamilyRegistrations
            .Include(fr => fr.Sector)
            .FirstOrDefaultAsync(fr => fr.FamilyHeadId == personId);
        if (headRegistration != null)
            sectorName = headRegistration.Sector?.Name;

        if (sectorName == null)
        {
            var memberRegistration = await _context.FamilyMembers
                .Include(fm => fm.Registration).ThenInclude(fr => fr.Sector)
                .FirstOrDefaultAsync(fm => fm.PersonId == personId);
            sectorName = memberRegistration?.Registration.Sector?.Name;
        }

        var sector = !string.IsNullOrEmpty(sectorName)
            ? await _context.Sectors.FirstOrDefaultAsync(s => s.Name == sectorName)
            : null;

        var beneficiary = new AssistanceBeneficiary
        {
            AssistanceId = assistanceId,
            FullName = person.FullName,
            NationalId = person.IdNumber,
            Phone = _context.FamilyRegistrations
                .Where(fr => fr.FamilyHeadId == personId)
                .Select(fr => fr.PhoneNumber)
                .FirstOrDefault() ?? "",
            SectorId = sector?.Id ?? 0,
            Status = BeneficiaryStatus.Active,
            CreatedById = userId,
            CreatedAt = JerusalemTime.Now
        };

        _context.AssistanceBeneficiaries.Add(beneficiary);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "إضافة مستفيد", "AssistanceBeneficiaries",
            beneficiary.Id.ToString(), null, new { beneficiary.FullName, beneficiary.NationalId, source = "Person" });

        return beneficiary;
    }

    public async Task<AssistanceBeneficiary> UpdateBeneficiaryAsync(AssistanceBeneficiary updated, int userId)
    {
        var existing = await _context.AssistanceBeneficiaries.FindAsync(updated.Id)
            ?? throw new InvalidOperationException("المستفيد غير موجود");

        var old = new { existing.FullName, existing.Phone, existing.Status };

        existing.FullName = updated.FullName;
        existing.Phone = updated.Phone;
        existing.BenefitType = updated.BenefitType;
        existing.Notes = updated.Notes;
        existing.Status = updated.Status;
        existing.UpdatedAt = JerusalemTime.Now;

        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "تعديل مستفيد", "AssistanceBeneficiaries",
            existing.Id.ToString(), old, new { existing.FullName, existing.Phone });

        return existing;
    }

    public async Task DeleteBeneficiaryAsync(int id, int userId)
    {
        var entity = await _context.AssistanceBeneficiaries.FindAsync(id)
            ?? throw new InvalidOperationException("المستفيد غير موجود");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "حذف مستفيد", "AssistanceBeneficiaries",
            id.ToString(), new { entity.FullName }, null);
    }

    public async Task<List<AssistanceBeneficiary>> GetBeneficiariesAsync(int assistanceId)
    {
        return await _context.AssistanceBeneficiaries
            .Include(b => b.Sector)
            .Include(b => b.CreatedBy)
            .Where(b => b.AssistanceId == assistanceId && !b.IsDeleted)
            .ToListAsync();
    }

    // ── Bulk operations ──

    public async Task<List<FamilyHeadListItem>> GetFamilyHeadsAsync(string? sectorName = null, int? assistanceId = null)
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

        List<string>? existingNationalIds = null;
        if (assistanceId.HasValue)
        {
            existingNationalIds = await _context.AssistanceBeneficiaries
                .AsNoTracking()
                .Where(b => b.AssistanceId == assistanceId.Value && !b.IsDeleted)
                .Select(b => b.NationalId)
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
                AlreadyNominated = existingNationalIds?.Contains(h.IdNumber) ?? false
            }).ToList();
    }

    public async Task AddMultipleBeneficiariesAsync(int assistanceId, List<int> personIds, int userId)
    {
        foreach (var personId in personIds)
        {
            var person = await _context.Persons.FindAsync(personId);
            if (person == null) continue;

            var exists = await _context.AssistanceBeneficiaries
                .AnyAsync(b => b.NationalId == person.IdNumber && b.AssistanceId == assistanceId);
            if (exists) continue;

            string? sectorName = null;
            var headRegistration = await _context.FamilyRegistrations
                .Include(fr => fr.Sector)
                .FirstOrDefaultAsync(fr => fr.FamilyHeadId == personId);
            if (headRegistration != null)
                sectorName = headRegistration.Sector?.Name;

            if (sectorName == null)
            {
                var memberRegistration = await _context.FamilyMembers
                    .Include(fm => fm.Registration).ThenInclude(fr => fr.Sector)
                    .FirstOrDefaultAsync(fm => fm.PersonId == personId);
                sectorName = memberRegistration?.Registration.Sector?.Name;
            }

            var sector = !string.IsNullOrEmpty(sectorName)
                ? await _context.Sectors.FirstOrDefaultAsync(s => s.Name == sectorName)
                : null;

            _context.AssistanceBeneficiaries.Add(new AssistanceBeneficiary
            {
                AssistanceId = assistanceId,
                FullName = person.FullName,
                NationalId = person.IdNumber,
                Phone = await _context.FamilyRegistrations
                    .Where(fr => fr.FamilyHeadId == personId)
                    .Select(fr => fr.PhoneNumber)
                    .FirstOrDefaultAsync() ?? "",
                SectorId = sector?.Id ?? 0,
                Status = BeneficiaryStatus.Active,
                CreatedById = userId,
                CreatedAt = JerusalemTime.Now
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> PersonExistsInAssistanceAsync(int assistanceId, int personId)
    {
        var person = await _context.Persons.FindAsync(personId);
        if (person == null) return false;

        return await _context.AssistanceBeneficiaries
            .AnyAsync(b => b.NationalId == person.IdNumber && b.AssistanceId == assistanceId);
    }
}
