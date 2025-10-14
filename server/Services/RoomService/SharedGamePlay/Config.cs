using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GamePlay.Shared
{
    [Serializable]
    public class Config
    {
        public int TickMs { get; set; }
        public float TowerHp { get; set; }
        public Dictionary<UnitType, UnitConfig> Units { get; set; }
        public float ManaRegenRate { get; set; }
        public float MaxMana { get; set; }
        public float StartMana { get; set; }

        public static Config Load(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Config>(json);
        }
    }
    [Serializable]
    public class UnitConfig
    {
        public float Hp { get; set; }
        public float Damage { get; set; }
        public float Speed { get; set; }
        public float Range { get; set; }
        public float Cost { get; set; }
        public float Cooldown { get; set; }
    }
}