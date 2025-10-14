using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Paeezan.Server.Models
{
    public class User
    {
        [BsonId][BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }
        public int Wins { get; set; }
    }
}