using System;

namespace TileAdventure.Core
{
    public class ComboSystem
    {
        private readonly float _windowDuration;
        private readonly int _maxMultiplier;

        public int CurrentCombo { get; private set; }
        public int MaxComboThisLevel { get; private set; }
        public float ComboTimer { get; private set; }

        public event Action<int> OnComboIncreased;
        public event Action OnComboBroken;
        public event Action<float> OnComboTick;

        public ComboSystem(float windowDuration, int maxMultiplier)
        {
            _windowDuration = Math.Max(windowDuration, 0.1f);
            _maxMultiplier = Math.Max(maxMultiplier, 1);
            CurrentCombo = 0;
            MaxComboThisLevel = 0;
            ComboTimer = 0f;
        }

        public void RegisterMatch()
        {
            if (CurrentCombo < _maxMultiplier)
            {
                CurrentCombo++;
            }

            if (CurrentCombo > MaxComboThisLevel)
                MaxComboThisLevel = CurrentCombo;

            ComboTimer = _windowDuration;
            OnComboIncreased?.Invoke(CurrentCombo);
        }

        public void Tick(float dt)
        {
            if (CurrentCombo == 0)
                return;

            ComboTimer -= dt;

            if (ComboTimer <= 0f)
            {
                ComboTimer = 0f;
                CurrentCombo = 0;
                OnComboBroken?.Invoke();
                return;
            }

            OnComboTick?.Invoke(ComboTimer / _windowDuration);
        }

        public void Reset()
        {
            CurrentCombo = 0;
            MaxComboThisLevel = 0;
            ComboTimer = 0f;
        }
    }
}
