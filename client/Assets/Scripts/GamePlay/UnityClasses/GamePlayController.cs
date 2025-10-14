using System.Collections.Generic;
using System.Linq;
using GamePlay.Shared;
using GamePlay.UnityClasses.Hub;
using Newtonsoft.Json;
using UnityEngine;

namespace GamePlay.UnityClasses
{
    public class GamePlayController : MonoBehaviour
    {
        public bool offline = false;

        public UnitView meleePrefab;
        public UnitView rangedPrefab;
        public TowerView towerPrefab;

        private readonly TowerView[] _towers = new TowerView[2];
        private readonly Dictionary<int, UnitView> _units = new();
        private Dictionary<UnitType, float> _lastSimCooldown = new();
        public static GameState LocalState;

        private bool _gameOver = false;
        private float _lastSimTime;
        private float _lastSimMana;
        private int PlayerId => _signalR.playerId;
        private GameHubBase _signalR;

        private void Start()
        {
            _signalR = FindObjectOfType<GameHubBase>();
            Time.fixedDeltaTime = 1f / 40f;
            _towers[0] = Instantiate(towerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            _towers[0].owner = 0;
            _towers[1] = Instantiate(towerPrefab, new Vector3(30, 0, 0), Quaternion.identity);
            _towers[1].owner = 1;

            _signalR.InitState += OnInitState;
            _signalR.UpdateState += OnServerUpdate;
            _signalR.GameEnded += OnGameEnded;
            _signalR.PlayerDisconnected += OnPlayerDisconnected;

            if (!offline) return;

            var config = new Config
            {
                Units = new Dictionary<UnitType, UnitConfig>(),
                MaxMana = 100,
                ManaRegenRate = 4,
                StartMana = 30,
                TowerHp = 500,
            };
            config.Units.Add(UnitType.Melee, new UnitConfig
            {
                Hp = 150,
                Cost = 20,
                Speed = 1,
                Cooldown = 2,
                Damage = 10,
                Range = 2,
            });
            config.Units.Add(UnitType.Ranged, new UnitConfig
            {
                Hp = 50,
                Cost = 25,
                Speed = 2,
                Cooldown = 3,
                Damage = 15,
                Range = 5,
            });

            var offlineState = new GameState();
            offlineState.SetConfig(config);
            OnInitState(offlineState);
        }

        private static void OnGameStarted()
        {
            Debug.Log("Game started!");
        }

        private void OnInitState(GameState gameState)
        {
            LocalState = gameState;
        }

        private void OnServerUpdate(GameState gameState)
        {
            if (_gameOver) return;
            LocalState = gameState;
            UpdateViewsFromState(LocalState);
            _lastSimTime = Time.time;
            _lastSimMana = LocalState.Players[PlayerId].Mana;
            _lastSimCooldown = LocalState.Players[PlayerId].StartCooldown
                .ToDictionary(entry => entry.Key, entry => entry.Value);
        }

        private void FixedUpdate()
        {
            if (_gameOver || LocalState == null) return;

            // Run game logic simulation step
            LocalState.Tick(Time.fixedDeltaTime);

            UpdateViewsFromState(LocalState);

            // Cache last sim data for prediction
            _lastSimMana = LocalState.Players[PlayerId].Mana;
            _lastSimCooldown = LocalState.Players[PlayerId].StartCooldown
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            _lastSimTime = Time.time;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F1) && offline)
            {
                _signalR.playerId = 0;
            }

            if (Input.GetKeyDown(KeyCode.F2) && offline)
            {
                _signalR.playerId = 1;
            }
#endif
            if (_gameOver || LocalState == null) return;

            // Predict client-side visuals (smoothness)
            var elapsed = Time.time - _lastSimTime;
            var predictedMana = Mathf.Min(
                LocalState.Players[PlayerId].MaxMana,
                _lastSimMana + LocalState.ManaRegenRate * elapsed
            );
            var predictedCds = _lastSimCooldown.ToDictionary(
                entry => entry.Key, entry => Mathf.Max(0, entry.Value - elapsed)
            );

            // Update UI
            GameUIManager.Instance.manaBar.UpdateMana(predictedMana, LocalState.Players[PlayerId].MaxMana);
            GameUIManager.Instance.cardUI.UpdateCooldown(predictedCds, predictedMana);

            // Predict unit positions
            foreach (var kv in _units)
            {
                var view = kv.Value;
                float predPos = view.lastPosition;
                if (view.wasMoving)
                {
                    predPos += view.lastSpeed * elapsed;
                }

                view.transform.position = new Vector3(predPos, 0, 0);
            }
        }

        private void UpdateViewsFromState(GameState state)
        {
            for (var i = 0; i < 2; i++)
            {
                _towers[i].hp = state.Towers[i].Hp;
                _towers[i].UpdateView();
            }

            var currentIds = state.Units.Select(u => u.Id).ToList();
            foreach (var id in _units.Keys.ToList().Where(id => !currentIds.Contains(id)))
            {
                Destroy(_units[id].gameObject);
                _units.Remove(id);
            }

            foreach (var u in state.Units)
            {
                if (!_units.ContainsKey(u.Id))
                {
                    var prefab = u.Type == UnitType.Melee ? meleePrefab : rangedPrefab;
                    var go = Instantiate(prefab, new Vector3(u.Position, 0, 0), Quaternion.identity);
                    go.id = u.Id;
                    go.owner = u.Owner;
                    go.hp = u.Hp;
                    go.maxHp = u.Hp;
                    go.lastPosition = u.Position;
                    go.lastSpeed = u.Speed;
                    if (u.Owner == 1)
                        go.transform.rotation = Quaternion.Euler(0, 180, 0);
                    _units[u.Id] = go;
                }
                else
                {
                    var go = _units[u.Id];
                    go.lastPosition = u.Position;
                    go.lastSpeed = u.Speed;
                    go.hp = u.Hp;
                    go.transform.position = new Vector3(u.Position, 0, 0);
                }
            }

            // Set attack visuals
            foreach (var u in state.Units)
            {
                if (_units.TryGetValue(u.Id, out var view))
                {
                    view.isAttacking = u.Target != null;
                    view.wasMoving = u.Target == null;
                    if (u.Target != null)
                    {
                        if (u.Target is Unit tu)
                        {
                            view.targetPos = tu.Position;
                        }
                        else if (u.Target is Tower tt)
                        {
                            view.targetPos = tt.Position;
                        }
                    }
                    else
                    {
                        view.targetPos = null;
                    }

                    view.UpdateView();
                }
            }
        }

        public bool PredictDeploy(UnitType type)
        {
            return LocalState.DeployUnit(PlayerId, type);
        }

        public float GetUnitCost(UnitType type)
        {
            return LocalState.UnitConfigs[type].Cost;
        }

        private void OnGameEnded(int winner)
        {
            if (_gameOver)
                return;

            _gameOver = true;

            Debug.Log($"Game over! Winner: Player {winner}");
            var win = winner == _signalR.playerId;
            if (win)
                GameUIManager.Instance.winPanel.SetActive(true);
            else
                GameUIManager.Instance.loosePanel.SetActive(true);
        }

        private void OnPlayerDisconnected(int p)
        {
            Debug.Log($"Player {p} disconnected. Game ending.");
        }
    }
}