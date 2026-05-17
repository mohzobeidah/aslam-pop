using System.Text;
using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Data;

namespace CampRegistrationApp.Services
{
    public interface IRecordIdGenerator
    {
        Task<string> GenerateUniqueIdAsync();
    }

    public class RecordIdGenerator : IRecordIdGenerator
    {
        private readonly ApplicationDbContext _context;
        private const string Characters = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // Exclude 0, 1, O, I, l for clarity

        public RecordIdGenerator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUniqueIdAsync()
        {
            string id;
            do
            {
                id = GenerateRandomString(8);
            } while (await _context.FamilyRegistrations.AnyAsync(f => f.RecordId == id));

            return id;
        }

        private string GenerateRandomString(int length)
        {
            var random = new Random();
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(Characters[random.Next(Characters.Length)]);
            }
            return result.ToString();
        }
    }
}
