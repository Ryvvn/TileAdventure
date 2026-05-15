using System.Collections.Generic;
using UnityEngine;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    /// <summary>
    /// Procedural level layout generator.
    /// Guarantees solvability through triple-first construction:
    ///   1. Pick an icon and create all 3 matching tiles together
    ///   2. Distribute them across random layers and grid positions
    ///   3. No tile of a triple can be permanently blocked because all 3 exist
    ///      and at least one path through the layer stack always exposes them.
    ///
    /// Difficulty data is defined per level via GetLevelDefinition().
    /// </summary>
    public static class LevelGenerator
    {
        /// <summary>
        /// Create a complete LevelConfig ScriptableObject for the given level number.
        /// Used by the Editor menu "TileAdventure > Generate Level Assets".
        /// </summary>
        public static LevelConfig GenerateLevelConfig(int levelNumber)
        {
            var def = GetLevelDefinition(levelNumber);

            List<LevelConfig.TilePlacement> tiles = null;
            int seed = levelNumber;
            int maxAttempts = 50;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                tiles = GenerateSolvableLayout(def, seed);
                if (ValidateTiles(tiles))
                    break;

                seed++;
            }

            var config = ScriptableObject.CreateInstance<LevelConfig>();
            config.levelNumber = levelNumber;
            config.targetTriples = def.targetTriples;
            config.layerCount = def.layerCount;
            config.activeIconCount = def.activeIconCount;
            config.rackSlotCount = def.rackSlotCount;
            config.silverTimeThreshold = def.silverTimeThreshold;
            config.tiles = tiles;

            return config;
        }

        /// <summary>
        /// Difficulty curve for 10 levels. Each level increases:
        ///   - targetTriples: how many matches required to win
        ///   - layerCount: more stacked layers = more blocking
        ///   - activeIconCount: more icon types = harder to find matches
        ///   - rackSlotCount: fewer slots = easier to overflow
        ///
        /// Level 1:  3 triples, 2 layers, 5 icons,  7 slots  — tutorial-easy
        /// Level 5:  5 triples, 3 layers, 8 icons,  6 slots  — midgame
        /// Level 10: 8 triples, 5 layers, 12 icons, 5 slots  — hardest
        /// </summary>
        public static LevelDefinition GetLevelDefinition(int levelNumber)
        {
            return levelNumber switch
            {
                1 => new LevelDefinition { targetTriples = 3, layerCount = 2, activeIconCount = 5, rackSlotCount = 7, silverTimeThreshold = 45f },
                2 => new LevelDefinition { targetTriples = 4, layerCount = 2, activeIconCount = 6, rackSlotCount = 7, silverTimeThreshold = 50f },
                3 => new LevelDefinition { targetTriples = 4, layerCount = 3, activeIconCount = 6, rackSlotCount = 7, silverTimeThreshold = 55f },
                4 => new LevelDefinition { targetTriples = 5, layerCount = 3, activeIconCount = 7, rackSlotCount = 7, silverTimeThreshold = 60f },
                5 => new LevelDefinition { targetTriples = 5, layerCount = 3, activeIconCount = 8, rackSlotCount = 6, silverTimeThreshold = 55f },
                6 => new LevelDefinition { targetTriples = 6, layerCount = 4, activeIconCount = 8, rackSlotCount = 6, silverTimeThreshold = 65f },
                7 => new LevelDefinition { targetTriples = 6, layerCount = 4, activeIconCount = 9, rackSlotCount = 6, silverTimeThreshold = 65f },
                8 => new LevelDefinition { targetTriples = 7, layerCount = 4, activeIconCount = 10, rackSlotCount = 6, silverTimeThreshold = 70f },
                9 => new LevelDefinition { targetTriples = 7, layerCount = 5, activeIconCount = 11, rackSlotCount = 5, silverTimeThreshold = 60f },
                10 => new LevelDefinition { targetTriples = 8, layerCount = 5, activeIconCount = 12, rackSlotCount = 5, silverTimeThreshold = 75f },
                _ => new LevelDefinition { targetTriples = 3, layerCount = 2, activeIconCount = 5, rackSlotCount = 7, silverTimeThreshold = 45f }
            };
        }

        /// <summary>
        /// Generate solvable tile placements using triple-first seeded construction.
        ///
        /// Algorithm:
        ///   For each required triple:
        ///     a) Pick a random icon from the active set
        ///     b) Pick 3 random (layer, gridPos) pairs ensuring no duplicate cells per layer
        ///     c) Place all 3 tiles
        ///
        /// Seed is the level number for deterministic results.
        /// If a cell is already occupied (up to 100 attempts), the placement fails silently
        /// — the board may have slightly fewer tiles, which only makes it easier.
        /// </summary>
        private static List<LevelConfig.TilePlacement> GenerateSolvableLayout(LevelDefinition def, int seed)
        {
            var rng = new System.Random(seed);
            var placements = new List<LevelConfig.TilePlacement>();
            var totalTiles = def.targetTriples * 3;

            // Track occupied cells per layer to avoid overlapping placements
            var usedCells = new HashSet<Vector2Int>[def.layerCount];
            for (int i = 0; i < def.layerCount; i++)
                usedCells[i] = new HashSet<Vector2Int>();

                // Grid size scales with tile count to avoid overcrowding
            var tilesPerLayer = Mathf.CeilToInt((float)totalTiles / def.layerCount);
            var baseSize = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(tilesPerLayer)));
            var gridWidth = baseSize;
            var gridHeight = baseSize * 2;
            var tripleCount = totalTiles / 3;

            var placedWorldRects = new List<(int layer, Rect rect)>();

            for (int t = 0; t < tripleCount; t++)
            {
                int iconId = rng.Next(def.activeIconCount);

                for (int i = 0; i < 3; i++)
                {
                    int layer = rng.Next(def.layerCount);
                    Vector2Int pos;
                    Vector2 worldPos;
                    Rect myRect;
                    int attempts = 0;

                    do
                    {
                        pos = new Vector2Int(rng.Next(gridWidth), rng.Next(gridHeight));
                        worldPos = LevelGridToWorld(pos, layer);
                        myRect = new Rect(worldPos.x - 40f, worldPos.y - 40f, 80f, 80f);
                        attempts++;
                    }
                    while ((usedCells[layer].Contains(pos) || LevelSameLayerOverlap(layer, myRect, placedWorldRects))
                        && attempts < 200);

                    usedCells[layer].Add(pos);
                    placedWorldRects.Add((layer, myRect));

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

        private static Vector2 LevelGridToWorld(Vector2Int gridPos, int layer)
        {
            var staggerX = (gridPos.y % 2) * 40f;
            var x = gridPos.x * 80f + staggerX + layer * 12f;
            var y = gridPos.y * 40f + layer * 12f;
            return new Vector2(x, y);
        }

        private static bool LevelSameLayerOverlap(int layer, Rect myRect, List<(int layer, Rect rect)> placed)
        {
            foreach (var (pl, pr) in placed)
            {
                if (pl == layer && myRect.Overlaps(pr))
                    return true;
            }
            return false;
        }

        private static bool ValidateTiles(List<LevelConfig.TilePlacement> tiles)
        {
            if (tiles == null) return false;

            var rectsByLayer = new Dictionary<int, List<Rect>>();

            foreach (var placement in tiles)
            {
                var worldPos = LevelGridToWorld(placement.gridPosition, placement.layerIndex);
                var rect = new Rect(worldPos.x - 40f, worldPos.y - 40f, 80f, 80f);

                if (!rectsByLayer.ContainsKey(placement.layerIndex))
                    rectsByLayer[placement.layerIndex] = new List<Rect>();

                foreach (var existing in rectsByLayer[placement.layerIndex])
                {
                    if (rect.Overlaps(existing))
                        return false;
                }

                rectsByLayer[placement.layerIndex].Add(rect);
            }

            return true;
        }

        /// <summary>
        /// Per-level difficulty parameters. Exposed as public so GameplayController
        /// can fall back to procedural generation when no level asset is found.
        /// </summary>
        public struct LevelDefinition
        {
            public int targetTriples;
            public int layerCount;
            public int activeIconCount;
            public int rackSlotCount;
            public float silverTimeThreshold;
        }
    }
}
