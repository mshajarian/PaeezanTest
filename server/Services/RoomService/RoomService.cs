using System.Collections.Concurrent;
using GamePlay.Shared;
using Newtonsoft.Json;
using Paeezan.Server.Models;
using Paeezan.Server.Repositories;

namespace Paeezan.Server.Services.RoomService
{
    public class RoomService
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new();
        private readonly ILogger<RoomService> _logger;
        private readonly int _tickMs;
        private readonly MatchRepository _matchRepo;
        private readonly UserRepository _userRepo;
        private readonly Config _config;


        private Action<string, GameState>? _hubNotifier;
        private Action<string, GameState>? _initNotifier;
        private Action<string, string, string>? _matchEndNotifier;
        public IEnumerable<Room> ListRooms() => _rooms.Values.ToList();


        public RoomService(ILogger<RoomService> logger, MatchRepository matchRepo,
            UserRepository userRepo)
        {
            _logger = logger;
            _matchRepo = matchRepo;
            _userRepo = userRepo;

            try
            {
                var cfgPath = Path.Combine(AppContext.BaseDirectory, "Config", "gameconfig.json");
                if (!File.Exists(cfgPath))
                {
                    _logger.LogError("failed load unit config");
                    return;
                }

                var txt = File.ReadAllText(cfgPath);
                _config = JsonConvert.DeserializeObject<Config>(txt);
                _tickMs = _config.TickMs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed load unit config");
            }
        }

        private static string MakeCode()
        {
            var rng = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }

        public Room Create(string userId, string connectionId)
        {
            var code = MakeCode();
            var r = new Room
            {
                Code = code, PlayerAUserId = userId, PlayerAConnectionId = connectionId,
            };
            _rooms[code] = r;
            _logger.LogInformation("Created room {code}", code);
            return r;
        }

        public bool TryJoin(string code, string userId, string connectionId, out Room room)
        {
            room = null;
            if (!_rooms.TryGetValue(code, out room)) return false;
            if (room.PlayerBConnectionId != null) return false;
            room.PlayerBConnectionId = connectionId;
            room.PlayerBUserId = userId;
            room.Started = true;
            StartSession(room);
            return true;
        }

        private void StartSession(Room room)
        {
            if (room.Session != null) return;
            var session = new GameState();
            session.SetConfig(_config);

            session.GetPlayerAId = () => room.PlayerAUserId;
            session.GetPlayerBId = () => room.PlayerBUserId;


            _initNotifier?.Invoke(room.Code ?? string.Empty, session);
            session.OnStateUpdated += snapshot => { _hubNotifier?.Invoke(room.Code ?? string.Empty, snapshot); };

            session.OnMatchEnded += (winner, loser) =>
            {
                _logger.LogInformation("Match {code} ended winner={w}", room.Code, winner);
                var mr = new MatchResult
                {
                    RoomCode = room.Code ?? string.Empty, WinnerUserId = winner ?? string.Empty,
                    LoserUserId = loser ?? string.Empty, EndedAt = DateTime.UtcNow
                };
                _matchRepo.Save(mr).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(winner)) _userRepo.IncrementWins(winner).GetAwaiter().GetResult();
                _matchEndNotifier?.Invoke(room.Code ?? string.Empty, winner ?? string.Empty, loser ?? string.Empty);
                room.Session = null;
                _rooms.TryRemove(room.Code ?? string.Empty, out _);
            };
            room.Session = session;
            var ct = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!session.Finished)
                {
                    session.Tick(_tickMs);
                    await Task.Delay(_tickMs, ct.Token);
                }
            }, ct.Token);
        }

        public void SetHubNotifiers(Action<string, GameState> hubNotifier, Action<string, GameState> initNotifier,
            Action<string, string, string> matchEndNotifier)
        {
            _hubNotifier = hubNotifier;
            _initNotifier = initNotifier;
            _matchEndNotifier = matchEndNotifier;
        }

        public bool TryGetByCode(string code, out Room r) => _rooms.TryGetValue(code, out r);

        public bool TryGetByConnection(string connectionId, out Room r) =>
            (_rooms.Values.FirstOrDefault(rm =>
                rm.PlayerAConnectionId == connectionId || rm.PlayerBConnectionId == connectionId) is Room rr)
                ? (r = rr) != null
                : (r = null) != null;

        public bool TrySpawnUnit(string code, string userId, UnitType unitType, out string error)
        {
            error = null;
            if (!_rooms.TryGetValue(code, out var room))
            {
                error = "room_not_found";
                return false;
            }

            if (room.Session == null)
            {
                error = "session_not_started";
                return false;
            }

            if (room.PlayerAUserId != userId && room.PlayerBUserId != userId)
            {
                error = "not_in_room";
                return false;
            }


            var isPlayerA = room.PlayerAUserId == userId;
            return room.Session.DeployUnit(isPlayerA ? 0 : 1, unitType);
        }
    }
}