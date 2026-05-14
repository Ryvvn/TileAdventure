using System;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    /// <summary>
    /// Orchestrator that wires BoardLogic, RackLogic, and GameState together for one level.
    /// Owns the lifecycle: create → load level data → relay events → dispose.
    ///
    /// This is a plain C# class — no MonoBehaviour. GameplayController is the MonoBehaviour
    /// that creates and drives the LevelManager each frame.
    /// </summary>
    public class LevelManager
    {
        private readonly GameConstants _constants;

        public BoardLogic Board { get; private set; }
        public RackLogic Rack { get; private set; }
        public GameState State { get; private set; }

        /// <summary> Fired via HandleWin → GameplayController shows win popup. </summary>
        public event Action OnLevelWon;

        /// <summary> Fired via HandleLose → GameplayController shows lose popup. </summary>
        public event Action OnLevelLost;

        public LevelManager(GameConstants constants)
        {
            _constants = constants;
            Board = new BoardLogic(constants);
        }

        /// <summary>
        /// Load a level from a pre-authored LevelConfig ScriptableObject.
        /// If the config has tile placements, those are used directly.
        /// Otherwise, falls back to procedural generation using the config's difficulty params.
        /// </summary>
        public void LoadLevel(LevelConfig config)
        {
            Board = new BoardLogic(_constants);
            Rack = new RackLogic(_constants, config.rackSlotCount);
            State = new GameState(config.levelNumber, config.targetTriples,
                _constants.comboWindowDuration, _constants.maxComboMultiplier);
            State.Combo.Reset();

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

        /// <summary>
        /// Load a level procedurally using raw difficulty numbers (no ScriptableObject).
        /// Used when LevelConfig asset is missing or the level was selected without one.
        /// </summary>
        public void LoadLevelProcedural(int levelNumber, int targetTriples, int layerCount, int activeIcons, int rackSlots)
        {
            Board = new BoardLogic(_constants);
            Rack = new RackLogic(_constants, rackSlots);
            State = new GameState(levelNumber, targetTriples,
                _constants.comboWindowDuration, _constants.maxComboMultiplier);
            State.Combo.Reset();

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

        /// <summary>
        /// Called every frame from GameplayController.Update() to advance the play timer.
        /// </summary>
        public void Tick(float deltaTime)
        {
            State?.Tick(deltaTime);
        }

        /// <summary>
        /// Unsubscribe all event handlers to prevent memory leaks.
        /// Must be called before discarding this LevelManager (scene restart, go home).
        /// </summary>
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
