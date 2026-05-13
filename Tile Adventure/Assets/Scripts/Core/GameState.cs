using System;

namespace TileAdventure.Core
{
    /// <summary>
    /// The three possible phases of a single game level.
    /// </summary>
    public enum GamePhase
    {
        Playing,
        Won,
        Lost
    }

    /// <summary>
    /// Plain C# state machine for a single level play-through.
    /// Tracks triples cleared, win/lose conditions, and elapsed time.
    /// Fires events that LevelManager and GameplayController observe.
    ///
    /// Key rule: once Won or Lost, no further state transitions are allowed.
    /// </summary>
    public class GameState
    {
        /// <summary> Current level number (1-10). </summary>
        public int currentLevel;

        /// <summary> How many triples must be cleared to win this level. </summary>
        public int targetTriples;

        /// <summary> How many triples the player has cleared so far. </summary>
        public int triplesCleared;

        /// <summary> Current game phase. Starts as Playing. </summary>
        public GamePhase phase;

        /// <summary> Seconds elapsed since level start (only ticks while Playing). </summary>
        public float timeElapsed;

        /// <summary> Fired when triplesCleared reaches targetTriples. </summary>
        public event Action OnWin;

        /// <summary> Fired when the rack overflows (called by LevelManager, not here). </summary>
        public event Action OnLose;

        /// <summary> Fired after each match with the new triplesCleared count. </summary>
        public event Action<int> OnTripleCleared;

        public GameState(int level, int targetTrips)
        {
            currentLevel = level;
            targetTriples = targetTrips;
            triplesCleared = 0;
            phase = GamePhase.Playing;
            timeElapsed = 0f;
        }

        /// <summary>
        /// Called when a match-3 is detected. Increments counter and checks win.
        /// Win fires OnWin immediately within this call — listeners should be prepared.
        /// </summary>
        public void RecordTripleCleared()
        {
            triplesCleared++;
            OnTripleCleared?.Invoke(triplesCleared);

            if (triplesCleared >= targetTriples)
            {
                phase = GamePhase.Won;
                OnWin?.Invoke();
            }
        }

        /// <summary>
        /// Mark the level as lost due to rack overflow.
        /// Only transitions from Playing — once Won/Lost, calls are ignored.
        /// </summary>
        public void MarkLost()
        {
            if (phase == GamePhase.Playing)
            {
                phase = GamePhase.Lost;
                OnLose?.Invoke();
            }
        }

        /// <summary>
        /// Advance the play timer. Called from GameplayController.Update().
        /// Only ticks while Playing — paused on win/lose.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (phase == GamePhase.Playing)
            {
                timeElapsed += deltaTime;
            }
        }
    }
}
