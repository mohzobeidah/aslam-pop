using CampRegistrationApp.Models;

namespace CampRegistrationApp.Services;

public interface IAssistanceService
{
    Task<List<Assistance>> GetAllAsync(string? sector, string? search, string? status, bool isAdmin, int adminId);
    Task<Assistance?> GetByIdAsync(int id);
    Task<Assistance> CreateAsync(Assistance assistance, int userId);
    Task<Assistance> UpdateAsync(Assistance assistance, int userId);
    Task DeleteAsync(int id, int userId);
    Task ApproveAsync(int id, int userId);
    Task CancelAsync(int id, int userId);

    // Beneficiaries
    Task<AssistanceBeneficiary> AddBeneficiaryAsync(AssistanceBeneficiary beneficiary, int userId);
    Task<AssistanceBeneficiary> AddBeneficiaryFromPersonAsync(int personId, int assistanceId, int userId);
    Task<AssistanceBeneficiary> UpdateBeneficiaryAsync(AssistanceBeneficiary beneficiary, int userId);
    Task DeleteBeneficiaryAsync(int id, int userId);
    Task<List<AssistanceBeneficiary>> GetBeneficiariesAsync(int assistanceId);

    // Person search
    Task<List<Person>> SearchPersonsAsync(string query, string? sectorName);
}
