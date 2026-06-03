using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;
using System.IO;

namespace CampRegistrationApp.Controllers
{
    public class FileController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public FileController(IWebHostEnvironment env, ApplicationDbContext context)
        {
            _env = env;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Download(string path)
        {
            if (string.IsNullOrEmpty(path)) return BadRequest();

            var adminId = HttpContext.Session.GetInt32("AdminId");
            var refugeeRegId = HttpContext.Session.GetInt32("EditRegistrationId");

            if (adminId == null && refugeeRegId == null)
                return Unauthorized();

            // If refugee, verify the file belongs to their registration
            if (refugeeRegId != null)
            {
                var recordId = await _context.FamilyRegistrations
                    .Where(f => f.Id == refugeeRegId.Value)
                    .Select(f => f.RecordId)
                    .FirstOrDefaultAsync();

                if (recordId == null || !path.TrimStart('/').StartsWith($"uploads/registrations/{recordId}/", StringComparison.OrdinalIgnoreCase))
                    return Unauthorized();
            }

            var absolutePath = Path.Combine(_env.WebRootPath, path.TrimStart('/'));
            if (!System.IO.File.Exists(absolutePath)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(absolutePath);
            var mimeType = "application/octet-stream";

            if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) mimeType = "application/pdf";
            else if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) mimeType = "image/jpeg";
            else if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) mimeType = "image/png";

            return File(bytes, mimeType);
        }
    }
}
