using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TileAdventure.Services
{
    [System.Serializable]
    public class SaveData
    {
        public int highestUnlockedLevel = 1;
        public List<LevelScore> levelScores = new List<LevelScore>();

        [System.Serializable]
        public class LevelScore
        {
            public int level;
            public int bestTriples;
            public float bestTime;
        }
    }

    public class SaveService
    {
        private readonly string _filePath;
        private SaveData _cachedData;

        public SaveService()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "tile_adventure_save.json");
            Load();
        }

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
                    _cachedData = new SaveData();
                    return _cachedData;
                }
            }

            _cachedData = new SaveData();
            return _cachedData;
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(_cachedData, true);
            File.WriteAllText(_filePath, json);
        }

        public int GetHighestUnlockedLevel()
        {
            return _cachedData?.highestUnlockedLevel ?? 1;
        }

        public bool IsLevelUnlocked(int levelNumber)
        {
            return levelNumber <= GetHighestUnlockedLevel();
        }

        public void UnlockLevel(int levelNumber)
        {
            if (_cachedData == null) Load();
            if (levelNumber > _cachedData.highestUnlockedLevel)
            {
                _cachedData.highestUnlockedLevel = levelNumber;
                Save();
            }
        }

        public void RecordLevelScore(int level, int triples, float time)
        {
            if (_cachedData == null) Load();

            var existing = _cachedData.levelScores.Find(s => s.level == level);
            if (existing != null)
            {
                if (time < existing.bestTime || existing.bestTime <= 0f)
                    existing.bestTime = time;
                if (triples > existing.bestTriples)
                    existing.bestTriples = triples;
            }
            else
            {
                _cachedData.levelScores.Add(new SaveData.LevelScore
                {
                    level = level,
                    bestTriples = triples,
                    bestTime = time
                });
            }

            Save();
        }
    }
}
