using Paeezan.Server.Repositories;

namespace Paeezan.Server.Services
{
    public class UserService
    {
        private readonly UserRepository _repo;
        public UserService(UserRepository repo) { _repo = repo; }
        public async Task IncrementWins(string userId) => await _repo.IncrementWins(userId);
    }
}