using System;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    public class LevelManager
    {
        private readonly GameConstants _constants;
        public BoardLogic Board { get; private set; }
        public RackLogic Rack { get; private set; }
        public GameState State { get; private set; }

        public event Action OnLevelWon;
        public event Action OnLevelLost;

        public LevelManager(GameConstants constants)
        {
            _constants = constants;
            Board = new BoardLogic(constants);
        }

        public void LoadLevel(LevelConfig config)
        {
            Board = new BoardLogic(_constants);
            Rack = new RackLogic(_constants, config.rackSlotCount);
            State = new GameState(config.levelNumber, config.targetTriples);

            if (config.tiles != null && config.tiles.Count > 0)
            {
                Board.InitializeFromConfig(config);
            }
            else
            {
                Board.GenerateBoard(config.targetTriples, config.layerCount, config.activeIconCount);
            }

            State.OnWin += HandleWin;
            State.OnLose += HandleLose;
            Rack.OnRackOverflow += HandleRackOverflow;
        }

        public void LoadLevelProcedural(int levelNumber, int targetTriples, int layerCount, int activeIcons, int rackSlots)
        {
            Board = new BoardLogic(_constants);
            Rack = new RackLogic(_constants, rackSlots);
            State = new GameState(levelNumber, targetTriples);

            Board.GenerateBoard(targetTriples, layerCount, activeIcons);

            State.OnWin += HandleWin;
            State.OnLose += HandleLose;
            Rack.OnRackOverflow += HandleRackOverflow;
        }

        private void HandleWin()
        {
            OnLevelWon?.Invoke();
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
                State.OnWin -= HandleWin;
                State.OnLose -= HandleLose;
            }
            if (Rack != null)
            {
                Rack.OnRackOverflow -= HandleRackOverflow;
            }
        }
    }
}
