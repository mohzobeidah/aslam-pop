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

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AssistanceStatus>(status, true, out var st))
            query = query.Where(a => a.Status == st);

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
        assistance.CreatedAt = DateTime.UtcNow;
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
        entity.ApprovedAt = DateTime.UtcNow;
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

    // ── Beneficiaries ──

    public async Task<AssistanceBeneficiary> AddBeneficiaryAsync(AssistanceBeneficiary beneficiary, int userId)
    {
        var exists = await _context.AssistanceBeneficiaries
            .AnyAsync(b => b.NationalId == beneficiary.NationalId && b.AssistanceId == beneficiary.AssistanceId && !b.IsDeleted);
        if (exists)
            throw new InvalidOperationException("رقم الهوية موجود مسبقاً في هذه المساعدة");

        beneficiary.CreatedById = userId;
        beneficiary.CreatedAt = DateTime.UtcNow;
        _context.AssistanceBeneficiaries.Add(beneficiary);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, "إضافة مستفيد", "AssistanceBeneficiaries",
            beneficiary.Id.ToString(), null, new { beneficiary.FullName, beneficiary.NationalId });

        return beneficiary;
    }

    public async Task<AssistanceBeneficiary> UpdateBeneficiaryAsync(AssistanceBeneficiary updated, int userId)
    {
        var existing = await _context.AssistanceBeneficiaries.FindAsync(updated.Id)
            ?? throw new InvalidOperationException("المستفيد غير موجود");

        var old = new { existing.FullName, existing.Phone, existing.Status };

        existing.FullName = updated.FullName;
        existing.Phone = updated.Phone;
        existing.FileNumber = updated.FileNumber;
        existing.FamilyName = updated.FamilyName;
        existing.City = updated.City;
        existing.FamilyCount = updated.FamilyCount;
        existing.BenefitType = updated.BenefitType;
        existing.Notes = updated.Notes;
        existing.Status = updated.Status;
        existing.UpdatedAt = DateTime.UtcNow;

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
}
