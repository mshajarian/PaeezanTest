using System;
using System.Threading.Tasks;
using GamePlay.Shared;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace GamePlay.UnityClasses.Hub
{
    [Serializable]
    public abstract class GameHubBase : SerializedMonoBehaviour
    {
        
        public int playerId ;
        public string roomId ;
        public bool connected = false ;

        public Action<GameState> UpdateState;
        public Action<GameState> InitState;
        public Action<string> Error;
        public Action<int> GameEnded;
        public Action<int> PlayerDisconnected;
        public Action<string> RoomCreated;
        public abstract void Connect();
        public abstract void DeployUnit(UnitType type);
        public abstract void SendCreateRoom();
        public abstract void SendJoinRoom(string code);

    }
   
}