using MongoDB.Driver;
using Paeezan.Server.Models;
using Microsoft.Extensions.Options;

namespace Paeezan.Server.Repositories
{
    public class MatchRepository
    {
        private readonly IMongoCollection<MatchResult> _matches;
        public MatchRepository(IOptions<MongoSettings> opts)
        {
            var client = new MongoClient(opts.Value.ConnectionString ?? "mongodb://localhost:27017");
            var db = client.GetDatabase(opts.Value.Database ?? "paeezan");
            _matches = db.GetCollection<MatchResult>("matches");
        }

        public async Task Save(MatchResult m) => await _matches.InsertOneAsync(m);
    }
}