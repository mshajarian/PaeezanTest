using System;
using Newtonsoft.Json;

namespace GamePlay.Shared
{
    [Serializable]
    public class Unit
    {
        public int Id { get; set; }
        public UnitType Type { get; set; }
        public int Owner { get; set; }
        public float Position { get; set; }
        public float Hp { get; set; }
        public float Damage { get; set; }
        public float Speed { get; set; }
        public float Range { get; set; }

        [JsonIgnore] // Transient, not serialized
        public object? Target { get; set; }
    }
}