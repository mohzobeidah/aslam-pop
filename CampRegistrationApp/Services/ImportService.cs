using System.Text.Json;
using CampRegistrationApp.Data;
using CampRegistrationApp.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CampRegistrationApp.Services;

public class ImportService : IImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;

    public ImportService(ApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("المستفيدين");

        var headers = new[] { "FullName", "NationalId", "Phone", "Sector" };
        int col = 1;
        foreach (var h in headers)
        {
            ws.Cell(1, col).Value = h;
            ws.Cell(1, col).Style.Font.Bold = true;
            ws.Cell(1, col).Style.Fill.BackgroundColor = XLColor.Gold;
            col++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<AssistanceImport> ImportFromExcelAsync(Stream excelStream, string fileName, int assistanceId, int userId, int sectorId)
    {
        var import = new AssistanceImport
        {
            FileName = fileName,
            ImportedById = userId,
            SectorId = sectorId,
            ImportedAt = DateTime.UtcNow
        };

        var errors = new List<(int Row, string Error)>();
        int success = 0, duplicates = 0;
        var beneficiaries = new List<AssistanceBeneficiary>();

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheet(1);
        var range = ws.RangeUsed();
        var rows = range != null ? range.RowsUsed().Skip(1) : Enumerable.Empty<IXLRangeRow>();

        int rowNum = 1;
        foreach (var row in rows)
        {
            rowNum++;
            try
            {
                var nationalId = row.Cell(2).GetString().Trim();
                if (string.IsNullOrEmpty(nationalId))
                {
                    errors.Add((rowNum, "رقم الهوية فارغ"));
                    continue;
                }

                var exists = await _context.AssistanceBeneficiaries
                    .AnyAsync(b => b.NationalId == nationalId && b.AssistanceId == assistanceId && !b.IsDeleted);
                if (exists)
                {
                    duplicates++;
                    errors.Add((rowNum, $"مكرر: {nationalId}"));
                    continue;
                }

                var beneficiary = new AssistanceBeneficiary
                {
                    AssistanceId = assistanceId,
                    FullName = row.Cell(1).GetString().Trim(),
                    NationalId = nationalId,
                    Phone = row.Cell(3).GetString().Trim(),
                    SectorId = sectorId,
                    Status = BeneficiaryStatus.Active,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    ImportId = null // set after save
                };

                beneficiaries.Add(beneficiary);
                success++;
            }
            catch (Exception ex)
            {
                errors.Add((rowNum, $"خطأ: {ex.Message}"));
            }
        }

        import.TotalRows = rowNum - 1;
        import.SuccessRows = success;
        import.FailedRows = errors.Count;
        import.DuplicateRows = duplicates;

        _context.AssistanceImports.Add(import);
        await _context.SaveChangesAsync();

        // Link beneficiaries to import
        foreach (var b in beneficiaries)
        {
            b.ImportId = import.Id;
            _context.AssistanceBeneficiaries.Add(b);
        }
        await _context.SaveChangesAsync();

        // Generate error report if any failures
        if (errors.Count > 0)
        {
            var errorBytes = GenerateErrorReport(errors);
            var errorDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "import-errors");
            Directory.CreateDirectory(errorDir);
            var errorFile = Path.Combine(errorDir, $"error_{import.Id}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            await File.WriteAllBytesAsync(errorFile, errorBytes);
            import.ErrorFilePath = errorFile;
            await _context.SaveChangesAsync();
        }

        await _audit.LogAsync(userId, "رفع Excel", "AssistanceImports",
            import.Id.ToString(), null, new
            {
                import.FileName, import.TotalRows, import.SuccessRows,
                import.FailedRows, import.DuplicateRows
            });

        return import;
    }

    public byte[] GenerateErrorReport(List<(int Row, string Error)> errors)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("الأخطاء");
        ws.Cell(1, 1).Value = "رقم الصف";
        ws.Cell(1, 2).Value = "الخطأ";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 2).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.Red;
        ws.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.Red;

        int r = 2;
        foreach (var (row, error) in errors)
        {
            ws.Cell(r, 1).Value = row;
            ws.Cell(r, 2).Value = error;
            r++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
