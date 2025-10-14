using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GamePlay.Shared;
using Microsoft.AspNetCore.SignalR;
using Paeezan.Server.Models;
using Paeezan.Server.Services;
using Paeezan.Server.Services.RoomService;

namespace Paeezan.Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly RoomService _rooms;
        private readonly ILogger<GameHub> _logger;

        public GameHub(RoomService rooms, ILogger<GameHub> logger)
        {
            _rooms = rooms;
            _logger = logger;
            // _rooms.SetHubNotifiers(NotifyState, NotifyInitState, NotifyMatchEnd);
        }


        public async Task CreateRoom()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User
                ?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (userId == null)
            {
                await Clients.Caller.SendAsync("Error", "auth_required");
                return;
            }

            var room = _rooms.Create(userId, Context.ConnectionId);
            await Clients.Caller.SendAsync("RoomCreated", new { code = room.Code });
        }

        public async Task JoinRoom(string code)
        {
            code = code.ToUpper();
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User
                ?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (userId == null)
            {
                await Clients.Caller.SendAsync("Error", "auth_required");
                return;
            }

            if (_rooms.TryJoin(code, userId, Context.ConnectionId, out var room))
            {
                await Clients.Client(room.PlayerAConnectionId).SendAsync("MatchStart", new { code = room.Code , index = 0 });
                await Clients.Client(room.PlayerBConnectionId).SendAsync("MatchStart", new { code = room.Code  , index = 1});
            }
            else await Clients.Caller.SendAsync("Error", "cannot_join");
        }

        public async Task DeployUnit(string code, UnitType unitType)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User
                ?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (userId == null)
            {
                await Clients.Caller.SendAsync("Error", "auth_required");
                return;
            }

            if (!_rooms.TrySpawnUnit(code, userId, unitType, out var err))
            {
                await Clients.Caller.SendAsync("Error", err);
                return;
            }

            await Clients.Caller.SendAsync("Deployed", new { unitType, code });
        }

        private void NotifyState(string code, GameState snapshot)
        {
            if (_rooms.TryGetByCode(code, out var room))
            {
                if (!string.IsNullOrEmpty(room.PlayerAConnectionId))
                    Clients.Client(room.PlayerAConnectionId).SendAsync("UpdateState", snapshot);
                if (!string.IsNullOrEmpty(room.PlayerBConnectionId))
                    Clients.Client(room.PlayerBConnectionId).SendAsync("UpdateState", snapshot);
            }
        }

        private void NotifyInitState(string code, GameState snapshot)
        {
            if (_rooms.TryGetByCode(code, out var room))
            {
                if (!string.IsNullOrEmpty(room.PlayerAConnectionId))
                    Clients.Client(room.PlayerAConnectionId).SendAsync("InitState", snapshot);
                if (!string.IsNullOrEmpty(room.PlayerBConnectionId))
                    Clients.Client(room.PlayerBConnectionId).SendAsync("InitState", snapshot);
            }
        }

        private void NotifyMatchEnd(string PlayerAConnectionId , string PlayerBConnectionId , int winner)
        {
            _logger.LogError("Send end game!");
            _logger.LogError(PlayerAConnectionId);
            _logger.LogError(winner.ToString());

            if (!string.IsNullOrEmpty(PlayerAConnectionId))
                Clients.Client(PlayerAConnectionId).SendAsync("GameEnded", new { winner });
            if (!string.IsNullOrEmpty(PlayerBConnectionId))
                Clients.Client(PlayerBConnectionId).SendAsync("GameEnded", new { winner});
        }


        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Connected {cid}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? ex)
        {
            _logger.LogInformation("Disconnected {cid}", Context.ConnectionId);
            return base.OnDisconnectedAsync(ex);
        }
    }
}