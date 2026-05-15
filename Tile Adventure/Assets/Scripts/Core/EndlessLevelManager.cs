using System;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    public class EndlessLevelManager
    {
        private readonly GameConstants _constants;
        private int _activeIcons;
        private int _layerCount;
        private int _rackSlots;

        public BoardLogic Board { get; private set; }
        public RackLogic Rack { get; private set; }
        public EndlessGameState State { get; private set; }

        public event Action OnLevelLost;
        public event Action OnRefillGenerated;

        public EndlessLevelManager(GameConstants constants)
        {
            _constants = constants;
            Board = new BoardLogic(constants);
        }

        public void Initialize()
        {
            _activeIcons = _constants.endlessStartIcons;
            _layerCount = _constants.endlessStartLayers;
            _rackSlots = _constants.endlessStartRackSlots;

            Board = new BoardLogic(_constants);
            Rack = new RackLogic(_constants, _rackSlots);
            State = new EndlessGameState(_constants.endlessTriplesPerTier,
                _constants.comboWindowDuration, _constants.maxComboMultiplier);
            State.Combo.Reset();

            Board.GenerateBoard(3, _layerCount, _activeIcons);

            State.OnLose += HandleLose;
            Rack.OnRackOverflow += HandleRackOverflow;
        }

        public void CheckRefill()
        {
            if (State.phase != GamePhase.Playing)
                return;

            ApplyTierParams();

            var aliveCount = 0;
            foreach (var tile in Board.AllTiles)
                if (!tile.isRemoved) aliveCount++;

            if (aliveCount <= _constants.endlessRefillThreshold)
            {
                var refillCount = _rackSlots + 2;
                Board.AddRefillTiles(refillCount, _activeIcons, _layerCount);
                OnRefillGenerated?.Invoke();
            }
        }

        private void ApplyTierParams()
        {
            var tier = State.currentTier;

            _activeIcons = Math.Min(_constants.endlessStartIcons + tier - 1, _constants.endlessMaxIcons);
            _layerCount = Math.Min(_constants.endlessStartLayers + (tier - 1) / 2, _constants.endlessMaxLayers);

            var targetRackSlots = Math.Max(
                _constants.endlessStartRackSlots - (tier - 1) / 3,
                _constants.endlessMinRackSlots);

            if (targetRackSlots < _rackSlots)
            {
                if (Rack.TryResize(targetRackSlots))
                {
                    _rackSlots = targetRackSlots;
                }
            }
        }

        public int GetCurrentTier()
        {
            return State.currentTier;
        }

        public int GetCurrentScore()
        {
            return State.triplesCleared;
        }

        public int GetActiveIconCount()
        {
            return _activeIcons;
        }

        public int GetLayerCount()
        {
            return _layerCount;
        }

        public int GetRackSlotCount()
        {
            return _rackSlots;
        }

        private void HandleLose()
        {
            OnLevelLost?.Invoke();
        }

        private void HandleRackOverflow()
        {
            State.MarkLost();
        }

        public void Tick(float deltaTime)
        {
            State?.Tick(deltaTime);
        }

        public void Dispose()
        {
            if (State != null)
            {
                State.OnLose -= HandleLose;
            }
            if (Rack != null)
            {
                Rack.OnRackOverflow -= HandleRackOverflow;
            }
        }
    }
}
