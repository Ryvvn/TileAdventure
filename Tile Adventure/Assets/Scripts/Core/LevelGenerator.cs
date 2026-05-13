using System.Collections.Generic;
using UnityEngine;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    public static class LevelGenerator
    {
        public static LevelConfig GenerateLevelConfig(int levelNumber)
        {
            var def = GetLevelDefinition(levelNumber);
            var config = ScriptableObject.CreateInstance<LevelConfig>();

            config.levelNumber = levelNumber;
            config.targetTriples = def.targetTriples;
            config.layerCount = def.layerCount;
            config.activeIconCount = def.activeIconCount;
            config.rackSlotCount = def.rackSlotCount;
            config.tiles = GenerateSolvableLayout(def, levelNumber);

            return config;
        }

        public static LevelDefinition GetLevelDefinition(int levelNumber)
        {
            return levelNumber switch
            {
                1 => new LevelDefinition { targetTriples = 3, layerCount = 2, activeIconCount = 5, rackSlotCount = 7 },
                2 => new LevelDefinition { targetTriples = 4, layerCount = 2, activeIconCount = 6, rackSlotCount = 7 },
                3 => new LevelDefinition { targetTriples = 4, layerCount = 3, activeIconCount = 6, rackSlotCount = 7 },
                4 => new LevelDefinition { targetTriples = 5, layerCount = 3, activeIconCount = 7, rackSlotCount = 7 },
                5 => new LevelDefinition { targetTriples = 5, layerCount = 3, activeIconCount = 8, rackSlotCount = 6 },
                6 => new LevelDefinition { targetTriples = 6, layerCount = 4, activeIconCount = 8, rackSlotCount = 6 },
                7 => new LevelDefinition { targetTriples = 6, layerCount = 4, activeIconCount = 9, rackSlotCount = 6 },
                8 => new LevelDefinition { targetTriples = 7, layerCount = 4, activeIconCount = 10, rackSlotCount = 6 },
                9 => new LevelDefinition { targetTriples = 7, layerCount = 5, activeIconCount = 11, rackSlotCount = 5 },
                10 => new LevelDefinition { targetTriples = 8, layerCount = 5, activeIconCount = 12, rackSlotCount = 5 },
                _ => new LevelDefinition { targetTriples = 3, layerCount = 2, activeIconCount = 5, rackSlotCount = 7 }
            };
        }

        private static List<LevelConfig.TilePlacement> GenerateSolvableLayout(LevelDefinition def, int seed)
        {
            var rng = new System.Random(seed);
            var placements = new List<LevelConfig.TilePlacement>();
            var totalTiles = def.targetTriples * 3;
            var usedCells = new HashSet<Vector2Int>[def.layerCount];
            for (int i = 0; i < def.layerCount; i++)
                usedCells[i] = new HashSet<Vector2Int>();

            var gridSize = Mathf.Max(4, Mathf.CeilToInt(Mathf.Sqrt(totalTiles / def.layerCount + 1)));
            var tripleCount = totalTiles / 3;

            for (int t = 0; t < tripleCount; t++)
            {
                int iconId = rng.Next(def.activeIconCount);

                for (int i = 0; i < 3; i++)
                {
                    int layer = rng.Next(def.layerCount);
                    Vector2Int pos;
                    int attempts = 0;

                    do
                    {
                        pos = new Vector2Int(rng.Next(gridSize), rng.Next(gridSize));
                        attempts++;
                    }
                    while (usedCells[layer].Contains(pos) && attempts < 100);

                    usedCells[layer].Add(pos);

                    placements.Add(new LevelConfig.TilePlacement
                    {
                        iconId = iconId,
                        layerIndex = layer,
                        gridPosition = pos
                    });
                }
            }

            return placements;
        }

        public struct LevelDefinition
        {
            public int targetTriples;
            public int layerCount;
            public int activeIconCount;
            public int rackSlotCount;
        }
    }
}
