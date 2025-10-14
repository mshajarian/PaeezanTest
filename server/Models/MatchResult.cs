using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Paeezan.Server.Models
{
    public class MatchResult
    {
        [BsonId][BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string WinnerUserId { get; set; } = string.Empty;
        public string LoserUserId { get; set; } = string.Empty;
        public DateTime EndedAt { get; set; } = DateTime.UtcNow;
    }
}