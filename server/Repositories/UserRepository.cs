using MongoDB.Driver;
using Paeezan.Server.Models;
using Microsoft.Extensions.Options;

namespace Paeezan.Server.Repositories
{
    public class MongoSettings { public string? ConnectionString { get; set; } public string? Database { get; set; } }

    public class UserRepository
    {
        private readonly IMongoCollection<User> _users;
        public UserRepository(IOptions<MongoSettings> opts)
        {
            var client = new MongoClient(opts.Value.ConnectionString ?? "mongodb://localhost:27017");
            var db = client.GetDatabase(opts.Value.Database ?? "paeezan");
            _users = db.GetCollection<User>("users");
        }

        public async Task<User?> GetByUsername(string username) => await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        public async Task<User?> GetById(string id) => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        public async Task Create(User u) => await _users.InsertOneAsync(u);
        public async Task IncrementWins(string id) => await _users.UpdateOneAsync(u => u.Id == id, Builders<User>.Update.Inc(x => x.Wins, 1));
    }
}