using System;

namespace TileAdventure.Core
{
    /// <summary>
    /// Plain C# state machine for an endless mode run.
    /// Tracks score (triplesCleared), difficulty tier, and game phase.
    /// No win condition — the only exit is rack overflow (lose).
    ///
    /// Tier is computed as 1 + (triplesCleared / triplesPerTier) and fires
    /// OnTierChanged when the player crosses a tier boundary.
    /// </summary>
    public class EndlessGameState
    {
        public int triplesCleared;
        public int currentTier;
        public GamePhase phase;

        public float timeElapsed;
        public bool tierJustChanged;

        public ComboSystem Combo { get; private set; }

        private readonly int _triplesPerTier;

        public event Action<int> OnScoreChanged;
        public event Action<int> OnTierChanged;
        public event Action OnLose;

        public EndlessGameState(int triplesPerTier, float comboWindowDuration, int maxComboMultiplier)
        {
            _triplesPerTier = Math.Max(triplesPerTier, 1);
            triplesCleared = 0;
            currentTier = 1;
            phase = GamePhase.Playing;
            timeElapsed = 0f;
            tierJustChanged = false;
            Combo = new ComboSystem(comboWindowDuration, maxComboMultiplier);
        }

        public void RecordTripleCleared()
        {
            triplesCleared++;
            OnScoreChanged?.Invoke(triplesCleared);

            var newTier = 1 + (triplesCleared / _triplesPerTier);
            if (newTier > currentTier)
            {
                currentTier = newTier;
                tierJustChanged = true;
                OnTierChanged?.Invoke(currentTier);
            }
        }

        public void MarkLost()
        {
            if (phase == GamePhase.Playing)
            {
                phase = GamePhase.Lost;
                OnLose?.Invoke();
            }
        }

        public void Tick(float deltaTime)
        {
            if (phase == GamePhase.Playing)
            {
                timeElapsed += deltaTime;
                Combo.Tick(deltaTime);
            }
        }
    }
}
