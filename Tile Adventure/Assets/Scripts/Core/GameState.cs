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

        /// <summary> Silver star time threshold for this level (from LevelDefinition/LevelConfig). </summary>
        public float silverTimeThreshold;

        /// <summary> Highest combo level achieved this level (for star rating). </summary>
        public int maxComboAchieved;

        /// <summary> Stars earned this run: 1=bronze, 2=silver, 3=gold. </summary>
        public int starsEarned;

        /// <summary> Combo streak tracking — chained matches with escalating multipliers. </summary>
        public ComboSystem Combo { get; private set; }

        /// <summary> Fired when triplesCleared reaches targetTriples. </summary>
        public event Action OnWin;

        /// <summary> Fired when the rack overflows (called by LevelManager, not here). </summary>
        public event Action OnLose;

        /// <summary> Fired after each match with the new triplesCleared count. </summary>
        public event Action<int> OnTripleCleared;

        public GameState(int level, int targetTrips, float comboWindowDuration, int maxComboMultiplier, float silverTimeThreshold)
        {
            currentLevel = level;
            targetTriples = targetTrips;
            triplesCleared = 0;
            phase = GamePhase.Playing;
            timeElapsed = 0f;
            this.silverTimeThreshold = silverTimeThreshold;
            starsEarned = 0;
            Combo = new ComboSystem(comboWindowDuration, maxComboMultiplier);
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
        /// Compute 1-3 stars based on this level's performance.
        /// Bronze: always awarded on win. Silver: time <= threshold. Gold: silver + combo >= 3.
        /// </summary>
        public int CalculateStars()
        {
            SyncComboAchieved();

            int stars = 1;

            if (timeElapsed <= silverTimeThreshold)
                stars = 2;

            if (stars >= 2 && maxComboAchieved >= 3)
                stars = 3;

            starsEarned = stars;
            return stars;
        }

        /// <summary>
        /// Advance the play timer and combo timer. Called from GameplayController.Update().
        /// Only ticks while Playing — paused on win/lose.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (phase == GamePhase.Playing)
            {
                timeElapsed += deltaTime;
                Combo.Tick(deltaTime);
            }
        }

        /// <summary>
        /// Sync maxComboAchieved from the ComboSystem. Called externally when combo state is read.
        /// </summary>
        public void SyncComboAchieved()
        {
            if (Combo.MaxComboThisLevel > maxComboAchieved)
                maxComboAchieved = Combo.MaxComboThisLevel;
        }
    }
}
