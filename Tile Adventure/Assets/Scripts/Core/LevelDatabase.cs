using UnityEngine;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "TileAdventure/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        public LevelConfig[] levels;

        public LevelConfig GetLevel(int levelNumber)
        {
            if (levels == null) return null;

            foreach (var level in levels)
            {
                if (level != null && level.levelNumber == levelNumber)
                    return level;
            }
            return null;
        }

        public bool IsValidLevel(int levelNumber)
        {
            return levelNumber >= 1 && levelNumber <= 10;
        }
    }
}
