using Paeezan.Server.Repositories;
using Paeezan.Server.Models;
using System;

namespace Paeezan.Server.Repositories
{
    public static class SeedData
    {
        public static async Task EnsureSeed(UserRepository userRepo)
        {
            var exists = await userRepo.GetByUsername("admin");
            if (exists == null)
            {
                await userRepo.Create(new User { Username = "admin", PasswordHash = Hash("adminpass"), Wins = 0 });
            }
        }

        private static string Hash(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var b = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(b);
        }
    }
}