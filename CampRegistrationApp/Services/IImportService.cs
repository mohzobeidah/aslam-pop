using CampRegistrationApp.Models;

namespace CampRegistrationApp.Services;

public interface IImportService
{
    Task<AssistanceImport> ImportFromExcelAsync(Stream excelStream, string fileName, int assistanceId, int userId, int sectorId);
    byte[] GenerateTemplate();
    byte[] GenerateErrorReport(List<(int Row, string Error)> errors);
}
