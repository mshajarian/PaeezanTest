using System;
using Best.SignalR;
using Best.SignalR.Encoders;
using BestHTTP.SignalRCore.Authentication;
using GamePlay.Shared;
using UnityEngine;

namespace GamePlay.UnityClasses.Hub
{
    class GamePlayHub : GameHubBase
    {
        [SerializeField] private string url;

        private HubConnection _hub;

        private static GamePlayHub _instance;
        private const string JwtKey = "jwt_token";

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void Connect()
        {
            IProtocol protocol = null;

            protocol = new JsonProtocol(new LitJsonEncoder());

            _hub = new HubConnection(new Uri(url), protocol)
            {
                ReconnectPolicy = new DefaultRetryPolicy()
            };
            string jwt = PlayerPrefs.GetString(JwtKey, string.Empty);

            _hub = new HubConnection(new Uri(url),
                new JsonProtocol(new JsonDotNetEncoder()))
            {
                AuthenticationProvider = new HeaderAuthenticator(jwt),
                ReconnectPolicy = new DefaultRetryPolicy(),
                Options = { ConnectTimeout = TimeSpan.FromSeconds(60) }
            };

            _hub.OnConnected += Hub_OnConnected;
            _hub.OnError += Hub_OnError;
            _hub.OnClosed += Hub_OnClosed;
            _hub.On<string>("Error", (errorMessage) =>
            {
                Debug.Log(string.Format("Hub Error: <color=red>{0}</color>", errorMessage));
                Error?.Invoke(errorMessage);
            });

            _hub.OnTransportEvent += (hubConnection, transport, ev) =>
                Debug.Log(string.Format("Transport(<color=green>{0}</color>) event: <color=green>{1}</color>",
                    transport.TransportType, ev));


            _hub.On<GameState>("InitState", json => InitState?.Invoke(json));
            _hub.On<GameState>("UpdateState", stateJson => UpdateState?.Invoke(stateJson));
            _hub.On<EndResponse>("GameEnded", res =>
            {
                Debug.Log($"GameEnded {res.winner}");

                GameEnded?.Invoke(res.winner);
            });
            _hub.On<int>("PlayerDisconnected", p => PlayerDisconnected?.Invoke(p));
            _hub.On<CreateRoomResponse>("RoomCreated", p => RoomCreated?.Invoke(p.code));
            _hub.On<MatchStartResponse>("MatchStart", p =>
            {
                Debug.Log($"Assigned player {p.index}");

                playerId = p.index;
                roomId = p.code;
            });
            _hub.On("GameFull", () => Debug.Log("Game full"));
            _hub.StartConnect();
        }

        public void OnCloseButton()
        {
            _hub?.StartClose();
        }


        private void Hub_OnConnected(HubConnection hub)
        {
            Debug.Log((string.Format(
                "Hub Connected with <color=green>{0}</color> transport using the <color=green>{1}</color> encoder.",
                hub.Transport.TransportType.ToString(), hub.Protocol.Name)));
            connected = true;
        }


        public override void SendCreateRoom()
        {
            _hub.Send("CreateRoom");
        }

        public override void SendJoinRoom(string code)
        {
            _hub.Send("JoinRoom", code);
        }


        public override void DeployUnit(UnitType type)
        {
            _hub.Send("DeployUnit", roomId, (int)type);
        }

        void OnDestroy()
        {
            if (_hub != null)
                _hub.StartClose();
        }

        private void Hub_OnClosed(HubConnection hub)
        {
            Debug.Log("Hub Closed");
            connected = false;
        }

        private void Hub_OnError(HubConnection hub, string error)
        {
            Debug.Log(string.Format("Hub Error: <color=red>{0}</color>", error));
            Error?.Invoke(error);
        }
    }

    [Serializable]
    public class CreateRoomResponse
    {
        public string code;
    }

    [Serializable]
    public class MatchStartResponse
    {
        public string code;
        public int index;
    }
    
    
    [Serializable]
    public class EndResponse
    {
        public int winner;
    }
}