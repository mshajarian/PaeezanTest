using System;
using System.Collections.Generic;
using GamePlay.Shared;

namespace Paeezan.Server.Models
{
    public class Room
    {
        public string? Code { get; set; }
        public string? PlayerAConnectionId { get; set; }
        public string? PlayerBConnectionId { get; set; }
        public string? PlayerAUserId { get; set; }
        public string? PlayerBUserId { get; set; }
        public bool Started { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // server authoritative session
        [Newtonsoft.Json.JsonIgnore]
        public GameState? Session { get; set; }
    }
}