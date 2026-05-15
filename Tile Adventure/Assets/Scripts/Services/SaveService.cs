using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TileAdventure.Services
{
    /// <summary>
    /// Serializable data container for player progress.
    /// Lives as a JSON file in Application.persistentDataPath.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        /// <summary> Highest level number the player has unlocked (starts at 1). </summary>
        public int highestUnlockedLevel = 1;

        /// <summary> Per-level best scores (optional, not currently displayed). </summary>
        public List<LevelScore> levelScores = new List<LevelScore>();

        /// <summary> Best endless mode score (most triples cleared in one run). </summary>
        public int bestEndlessScore;

        [System.Serializable]
        public class LevelScore
        {
            public int level;
            public int bestTriples;
            public float bestTime;
            public int bestStars;
        }
    }

    /// <summary>
    /// Plain C# JSON persistence service. Handles save/load of player progress.
    /// File path: {persistentDataPath}/tile_adventure_save.json
    ///
    /// Corrupt saves are silently replaced with a fresh SaveData (graceful degradation).
    /// </summary>
    public class SaveService
    {
        private readonly string _filePath;
        private SaveData _cachedData;

        public SaveService()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "tile_adventure_save.json");
            Load();
        }

        /// <summary>
        /// Load save data from disk. On first launch or corrupt file, returns defaults.
        /// </summary>
        public SaveData Load()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    _cachedData = JsonUtility.FromJson<SaveData>(json);
                    return _cachedData;
                }
                catch
                {
                    // Corrupted file — start fresh
                    _cachedData = new SaveData();
                    return _cachedData;
                }
            }

            _cachedData = new SaveData();
            return _cachedData;
        }

        /// <summary> Write current data to disk (pretty-printed JSON). </summary>
        public void Save()
        {
            var json = JsonUtility.ToJson(_cachedData, true);
            File.WriteAllText(_filePath, json);
        }

        /// <summary> Highest level the player can access. Used by HomeScreen to lock/unlock buttons. </summary>
        public int GetHighestUnlockedLevel()
        {
            return _cachedData?.highestUnlockedLevel ?? 1;
        }

        /// <summary> Can the player play this level? </summary>
        public bool IsLevelUnlocked(int levelNumber)
        {
            return levelNumber <= GetHighestUnlockedLevel();
        }

        /// <summary> Get the best star rating for a specific level (0 = no score yet). </summary>
        public int GetBestStars(int levelNumber)
        {
            if (_cachedData == null) Load();
            var score = _cachedData.levelScores.Find(s => s.level == levelNumber);
            return score?.bestStars ?? 0;
        }

        /// <summary> Unlock a level (called on win). Only saves if the new level is higher than current max. </summary>
        public void UnlockLevel(int levelNumber)
        {
            if (_cachedData == null) Load();
            if (levelNumber > _cachedData.highestUnlockedLevel)
            {
                _cachedData.highestUnlockedLevel = levelNumber;
                Save();
            }
        }

        /// <summary>
        /// Record a level score (best time and most triples).
        /// Not currently used in UI, but stored for future features.
        /// </summary>
        public void RecordLevelScore(int level, int triples, float time, int stars)
        {
            if (_cachedData == null) Load();

            var existing = _cachedData.levelScores.Find(s => s.level == level);
            if (existing != null)
            {
                if (time < existing.bestTime || existing.bestTime <= 0f)
                    existing.bestTime = time;
                if (triples > existing.bestTriples)
                    existing.bestTriples = triples;
                if (stars > existing.bestStars)
                    existing.bestStars = stars;
            }
            else
            {
                _cachedData.levelScores.Add(new SaveData.LevelScore
                {
                    level = level,
                    bestTriples = triples,
                    bestTime = time,
                    bestStars = stars
                });
            }

            Save();
        }

        /// <summary>
        /// Record an endless mode run score. Only saves if it's a new personal best.
        /// </summary>
        public void RecordEndlessScore(int triples)
        {
            if (_cachedData == null) Load();
            if (triples > _cachedData.bestEndlessScore)
            {
                _cachedData.bestEndlessScore = triples;
                Save();
            }
        }

        /// <summary> Get the best endless mode score (0 = no run yet). </summary>
        public int GetBestEndlessScore()
        {
            if (_cachedData == null) Load();
            return _cachedData?.bestEndlessScore ?? 0;
        }
    }
}
