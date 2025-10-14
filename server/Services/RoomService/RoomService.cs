using System.Collections.Concurrent;
using GamePlay.Shared;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Paeezan.Server.Hubs;
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
        private readonly IHubContext<GameHub> _hubContext;

        public IEnumerable<Room> ListRooms() => _rooms.Values.ToList();

        public RoomService(
            ILogger<RoomService> logger,
            MatchRepository matchRepo,
            UserRepository userRepo,
            IHubContext<GameHub> hubContext)
        {
            _logger = logger;
            _matchRepo = matchRepo;
            _userRepo = userRepo;
            _hubContext = hubContext;

            try
            {
                var cfgPath = Path.Combine(AppContext.BaseDirectory, "Config", "gameconfig.json");
                if (!File.Exists(cfgPath))
                {
                    _logger.LogError("failed load unit config");
                    return;
                }

                var txt = File.ReadAllText(cfgPath);
                _config = JsonConvert.DeserializeObject<Config>(txt)!;
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
                Code = code,
                PlayerAUserId = userId,
                PlayerAConnectionId = connectionId,
            };
            _rooms[code] = r;
            _logger.LogInformation("Created room {code}", code);
            return r;
        }

        public bool TryJoin(string code, string userId, string connectionId, out Room room)
        {
            room = null!;
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
            session.GetPlayerAId = room.PlayerAUserId;
            session.GetPlayerBId = room.PlayerBUserId;
            room.Session = session;

            var cts = new CancellationTokenSource();

            // Update state callback
            session.OnStateUpdated += snapshot =>
            {
                try
                {
                    foreach (var unit in snapshot.Units)
                        unit.Target = null;
                    if (!string.IsNullOrEmpty(room.PlayerAConnectionId))
                        _ = _hubContext.Clients.Client(room.PlayerAConnectionId)
                            .SendAsync("UpdateState", snapshot);
                    if (!string.IsNullOrEmpty(room.PlayerBConnectionId))
                        _ = _hubContext.Clients.Client(room.PlayerBConnectionId)
                            .SendAsync("UpdateState", snapshot);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending state update for room {code}", room.Code);
                }
            };

            // Send initial state
            try
            {
                if (!string.IsNullOrEmpty(room.PlayerAConnectionId))
                    _ = _hubContext.Clients.Client(room.PlayerAConnectionId)
                        .SendAsync("InitState", session);
                if (!string.IsNullOrEmpty(room.PlayerBConnectionId))
                    _ = _hubContext.Clients.Client(room.PlayerBConnectionId)
                        .SendAsync("InitState", session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending init state for room {code}", room.Code);
            }

            // Start game loop
            Task.Run(async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var winnerId = session.Tick(_tickMs / 1000f);

                        if (winnerId != -1)
                        {
                            // Match ended
                            var winnerUser = winnerId == 1 ? session.GetPlayerBId : session.GetPlayerAId;
                            var loserUser = winnerId == 1 ? session.GetPlayerAId : session.GetPlayerBId;

                            _logger.LogInformation("Match {code} ended. Winner: {winner}", room.Code, winnerUser);

                            try
                            {
                                if (!string.IsNullOrEmpty(room.PlayerAConnectionId))
                                    await _hubContext.Clients.Client(room.PlayerAConnectionId)
                                        .SendAsync("GameEnded", new { winner = winnerId });
                                if (!string.IsNullOrEmpty(room.PlayerBConnectionId))
                                    await _hubContext.Clients.Client(room.PlayerBConnectionId)
                                        .SendAsync("GameEnded", new { winner = winnerId });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed sending GameEnded event for room {code}", room.Code);
                            }

                            // Save result (optional)
                            try
                            {
                                var result = new MatchResult
                                {
                                    RoomCode = room.Code ?? string.Empty,
                                    WinnerUserId = winnerUser ?? string.Empty,
                                    LoserUserId = loserUser ?? string.Empty,
                                    EndedAt = DateTime.UtcNow
                                };

                                await _matchRepo.Save(result);
                                if (!string.IsNullOrEmpty(winnerUser))
                                    await _userRepo.IncrementWins(winnerUser);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error saving match result for {code}", room.Code);
                            }

                            // Cleanup
                            room.Session = null;
                            _rooms.TryRemove(room.Code ?? string.Empty, out _);
                            cts.Cancel();
                        }

                        await Task.Delay(_tickMs, cts.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // expected when match ends
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in game loop for room {code}", room.Code);
                }
            }, cts.Token);
        }

        public bool TryGetByCode(string code, out Room r) => _rooms.TryGetValue(code, out r!);

        public bool TryGetByConnection(string connectionId, out Room r)
        {
            r = _rooms.Values.FirstOrDefault(rm =>
                rm.PlayerAConnectionId == connectionId || rm.PlayerBConnectionId == connectionId)!;
            return r != null;
        }

        public bool TrySpawnUnit(string code, string userId, UnitType unitType, out string? error)
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