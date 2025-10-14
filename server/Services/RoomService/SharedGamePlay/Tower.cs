using System;

namespace GamePlay.Shared
{
    [Serializable]
    public class Tower
    {
        public int Owner { get; set; }
        public float Hp { get; set; }
        public float Position { get; set; }
    }
}