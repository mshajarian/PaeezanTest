using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace GamePlay.Shared
{
    [Serializable]
    public class GameState
    {
        public List<Tower> Towers { get; set; }
        public List<Unit> Units { get; set; }
        public List<PlayerState> Players { get; set; }
        public int UnitIdCounter { get; set; }
        public float ManaRegenRate { get; set; }
        public Dictionary<UnitType, UnitConfig> UnitConfigs { get; set; }
        public int Winner { get; set; } = -1;
        public bool Finished { get; private set; }

        public Func<string?>? GetPlayerAId { get; set; }
        public Func<string?>? GetPlayerBId { get; set; }

        public event Action<GameState>? OnStateUpdated;
        public event Action<string, string>? OnMatchEnded; // winnerUserId, loserUserId
        private int sendCounter = 0;
        private const int sendEvery = 30;

        public void SetConfig(Config config)
        {
            Towers = new List<Tower>
            {
                new Tower { Owner = 0, Hp = config.TowerHp, Position = 0 },
                new Tower { Owner = 1, Hp = config.TowerHp, Position = 30 }
            };
            Units = new List<Unit>();
            Players = new List<PlayerState>
            {
                new PlayerState
                {
                    Mana = config.StartMana, MaxMana = config.MaxMana,
                    StartCooldown = new Dictionary<UnitType, float> { { UnitType.Melee, 0 }, { UnitType.Ranged, 0 } }
                },
                new PlayerState
                {
                    Mana = config.StartMana, MaxMana = config.MaxMana,
                    StartCooldown = new Dictionary<UnitType, float> { { UnitType.Melee, 0 }, { UnitType.Ranged, 0 } }
                }
            };

            UnitIdCounter = 0;
            ManaRegenRate = config.ManaRegenRate;
            UnitConfigs = new Dictionary<UnitType, UnitConfig>
            {
                { UnitType.Melee, config.Units[UnitType.Melee] },
                { UnitType.Ranged, config.Units[UnitType.Ranged] }
            };
        }

        public bool DeployUnit(int player, UnitType type)
        {
            if (Players[player].StartCooldown[type] > 0) return false;
            var uc = UnitConfigs[type];
            if (Players[player].Mana < uc.Cost) return false;
            Players[player].Mana -= uc.Cost;
            Players[player].StartCooldown[type] = uc.Cooldown;
            float pos = player == 0 ? 0 : 30;
            float speed = player == 0 ? uc.Speed : -uc.Speed;
            var unit = new Unit
            {
                Id = UnitIdCounter++, Type = type, Owner = player, Position = pos, Hp = uc.Hp, Damage = uc.Damage,
                Speed = speed, Range = uc.Range
            };
            Units.Add(unit);
            OnStateUpdated?.Invoke(this);
            return true;
        }

        public bool Tick(float deltaTime)
        {
            if (Finished) return true;

            // Regen mana and cooldowns
            foreach (var p in Players)
            {
                p.Mana = (float)Math.Min(p.MaxMana, p.Mana + ManaRegenRate * deltaTime);
                var keys = p.StartCooldown.Keys.ToList();
                foreach (var key in keys)
                {
                    p.StartCooldown[key] = (float)Math.Max(0, p.StartCooldown[key] - deltaTime);
                }
            }

            // Find targets
            foreach (var u in Units)
            {
                u.Target = null;
                var candidates = Units.Where(x => x.Owner != u.Owner && Math.Abs(x.Position - u.Position) <= u.Range)
                    .ToList();
                if (candidates.Any())
                {
                    candidates = candidates.OrderBy(x => Math.Abs(x.Position - u.Position)).ToList();
                    u.Target = candidates.First();
                }
                else
                {
                    var tower = Towers[1 - u.Owner];
                    if (Math.Abs(tower.Position - u.Position) <= u.Range)
                    {
                        u.Target = tower;
                    }
                }
            }

            // Move if no target
            foreach (var u in Units)
            {
                if (u.Target == null)
                {
                    u.Position += u.Speed * deltaTime;
                }
            }

            // Attack
            foreach (var u in Units)
            {
                switch (u.Target)
                {
                    case null:
                        continue;
                    case Unit targetUnit:
                        targetUnit.Hp -= u.Damage * deltaTime;
                        break;
                    case Tower targetTower:
                    {
                        targetTower.Hp -= u.Damage * deltaTime;
                        if (targetTower.Hp <= 0)
                        {
                            Winner = u.Owner;
                            Finished = true;
                            var winner = Winner == 1
                                ? GetPlayerBId?.Invoke() ?? string.Empty
                                : GetPlayerAId?.Invoke() ?? string.Empty;
                            var loser = winner == GetPlayerAId?.Invoke()
                                ? GetPlayerBId?.Invoke() ?? string.Empty
                                : GetPlayerAId?.Invoke() ?? string.Empty;
                            OnMatchEnded?.Invoke(winner, loser);
                        }

                        break;
                    }
                }
            }


            Units = Units.Where(u => u.Hp > 0).ToList();
            sendCounter++;
            if (sendCounter % sendEvery == 0 && !Finished)
                OnStateUpdated?.Invoke(this);
            
            return Winner != -1;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}