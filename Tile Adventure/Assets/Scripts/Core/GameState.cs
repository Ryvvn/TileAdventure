using System;

namespace TileAdventure.Core
{
    public enum GamePhase
    {
        Playing,
        Won,
        Lost
    }

    public class GameState
    {
        public int currentLevel;
        public int targetTriples;
        public int triplesCleared;
        public GamePhase phase;
        public float timeElapsed;

        public event Action OnWin;
        public event Action OnLose;
        public event Action<int> OnTripleCleared;

        public GameState(int level, int targetTrips)
        {
            currentLevel = level;
            targetTriples = targetTrips;
            triplesCleared = 0;
            phase = GamePhase.Playing;
            timeElapsed = 0f;
        }

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
            }
        }
    }
}
