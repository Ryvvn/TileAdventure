using UnityEngine;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    /// <summary>
    /// ScriptableObject wrapper for an array of LevelConfig assets.
    /// Provides O(n) lookup by level number. Usage is optional — GameplayController
    /// can either load a LevelConfig directly or use procedural generation.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "TileAdventure/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        /// <summary> Ordered array of level configs (index 0 = level 1, etc. or explicit). </summary>
        public LevelConfig[] levels;

        /// <summary> Linear scan for a level by its levelNumber field. Returns null if not found. </summary>
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

        /// <summary> Quick range check without iterating the array. </summary>
        public bool IsValidLevel(int levelNumber)
        {
            return levelNumber >= 1 && levelNumber <= 10;
        }
    }
}
