using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace CampRegistrationApp.Controllers
{
    public class FileController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public FileController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult Download(string path)
        {
            if (string.IsNullOrEmpty(path)) return BadRequest();

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
