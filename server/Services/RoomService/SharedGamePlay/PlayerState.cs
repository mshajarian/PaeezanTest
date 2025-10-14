using System;
using System.Collections.Generic;

namespace GamePlay.Shared
{
    [Serializable]
    public class PlayerState
    {
        public float Mana { get; set; }
        public float MaxMana { get; set; }
        public Dictionary<UnitType, float> StartCooldown { get; set; }
    }
}
