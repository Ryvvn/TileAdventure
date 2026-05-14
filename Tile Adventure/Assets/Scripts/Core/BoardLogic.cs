using System;
using System.Collections.Generic;
using UnityEngine;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    /// <summary>
    /// Manages all tiles on the board: creation from config, hit-testing, removal, and exposure.
    /// Exposure determines whether a tile is tappable:
    ///   A tile is EXPOSED when NO tile on a higher layer overlaps it in world space.
    ///
    /// This is a plain C# class — no MonoBehaviour. BoardView is the MonoBehaviour
    /// that renders the tiles this class manages.
    /// </summary>
    public class BoardLogic
    {
        private readonly GameConstants _constants;
        private List<TileData> _allTiles;

        public IReadOnlyList<TileData> AllTiles => _allTiles;
        public event Action<TileData> OnTileRemoved;
        public event Action OnBoardCleared;

        public BoardLogic(GameConstants constants)
        {
            _constants = constants;
            _allTiles = new List<TileData>();
        }

        /// <summary>
        /// Build the board from a pre-authored LevelConfig asset.
        /// Each TilePlacement maps to one TileData with world position computed from grid position.
        /// </summary>
        public void InitializeFromConfig(LevelConfig config)
        {
            _allTiles.Clear();

            for (int i = 0; i < config.tiles.Count; i++)
            {
                var placement = config.tiles[i];
                var worldPos = GridToWorld(placement.gridPosition, placement.layerIndex);
                var tile = new TileData(
                    i,
                    placement.iconId,
                    placement.layerIndex,
                    placement.gridPosition,
                    worldPos
                );
                _allTiles.Add(tile);
            }

            RefreshExposure();
        }

        /// <summary>
        /// Generate a random board with the given difficulty parameters.
        /// Uses the same solvability algorithm as LevelGenerator (triple-first construction).
        /// </summary>
        public void GenerateBoard(int targetTriples, int layerCount, int activeIcons)
        {
            _allTiles.Clear();

            var totalTiles = targetTriples * _constants.matchCount;
            var rng = new System.Random();

            var placements = GenerateSolvableLayout(totalTiles, layerCount, activeIcons, rng);

            for (int i = 0; i < placements.Count; i++)
            {
                var (iconId, layer, gridPos) = placements[i];
                var worldPos = GridToWorld(gridPos, layer);
                var tile = new TileData(i, iconId, layer, gridPos, worldPos);
                _allTiles.Add(tile);
            }

            RefreshExposure();
        }

        /// <summary>
        /// Generate tile placements ensuring every triple has all 3 matching tiles present.
        /// Same algorithm as LevelGenerator.GenerateSolvableLayout.
        /// </summary>
        private List<(int iconId, int layer, Vector2Int gridPos)> GenerateSolvableLayout(
            int totalTiles, int layerCount, int activeIcons, System.Random rng)
        {
            var result = new List<(int, int, Vector2Int)>();
            var tripleCount = totalTiles / _constants.matchCount;

            var usedCells = new HashSet<Vector2Int>[layerCount];
            for (int i = 0; i < layerCount; i++)
                usedCells[i] = new HashSet<Vector2Int>();

            var tilesPerLayer = Mathf.CeilToInt((float)totalTiles / layerCount);
            var gridSize = Mathf.Max(6, Mathf.CeilToInt(Mathf.Sqrt(tilesPerLayer * 1.5f)));

            for (int t = 0; t < tripleCount; t++)
            {
                int iconId = rng.Next(activeIcons);

                for (int i = 0; i < _constants.matchCount; i++)
                {
                    int layer = rng.Next(layerCount);
                    Vector2Int pos;
                    int attempts = 0;
                    do
                    {
                        pos = new Vector2Int(rng.Next(gridSize), rng.Next(gridSize));
                        attempts++;
                    }
                    while (usedCells[layer].Contains(pos) && attempts < 50);

                    usedCells[layer].Add(pos);
                    result.Add((iconId, layer, pos));
                }
            }

            return result;
        }

        /// <summary>
        /// Convert a grid position and layer index to a world-space position on the Canvas.
        /// Uses pyramid/cascading layout: tight cell spacing so tiles overlap, odd-row stagger
        /// for hexagonal/pyramid feel, and vertical layer offset so lower tiles peek through.
        ///
        /// Layout: gridCellWidth=48, gridCellHeight=40 (both less than tileSize=80 → tiles overlap)
        ///         pyramidStaggerOffset=24 (half cellWidth, offset on odd rows)
        ///         layerVerticalOffset=28 (higher layers shift up, lower layers peek from bottom)
        ///
        /// Example: grid(0,0) layer0 → (0,0), grid(0,0) layer1 → (28,28)
        ///          grid(1,0) layer0 → (48,0), grid(0,1) layer0 → (24,40) [odd row stagger]
        /// </summary>
        public Vector2 GridToWorld(Vector2Int gridPos, int layer)
        {
            var staggerX = (gridPos.y % 2) * _constants.pyramidStaggerOffset;
            var x = gridPos.x * _constants.gridCellWidth + staggerX + layer * _constants.layerVerticalOffset;
            var y = gridPos.y * _constants.gridCellHeight + layer * _constants.layerVerticalOffset;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Recompute exposure for every tile on the board.
        /// For each tile A, scan all tiles B where B.layer &gt; A.layer.
        /// If B's world rect overlaps A's world rect, A is blocked (not exposed).
        ///
        /// Called after: board initialization, tile removal, any structural change.
        /// </summary>
        public void RefreshExposure()
        {
            var halfSize = _constants.tileSize * 0.5f;

            foreach (var tile in _allTiles)
            {
                if (tile.isRemoved) continue;

                bool blocked = false;
                foreach (var other in _allTiles)
                {
                    if (other.isRemoved || other.tileId == tile.tileId) continue;
                    if (tile.Overlaps(other, halfSize))
                    {
                        blocked = true;
                        break;
                    }
                }
                tile.SetExposed(!blocked);
            }
        }

        /// <summary> Lookup a tile by its unique ID within this board. </summary>
        public TileData GetTileById(int id)
        {
            return _allTiles.Find(t => t.tileId == id);
        }

        /// <summary>
        /// Hit-test the board at a screen point (e.g., player tap).
        /// Returns the topmost exposed tile at that position, or null.
        /// Iterates in reverse order (last = topmost = highest layer first).
        /// </summary>
        public TileData GetTileAtScreenPoint(Vector2 screenPoint, Camera cam)
        {
            var worldPoint = cam.ScreenToWorldPoint(screenPoint);
            for (int i = _allTiles.Count - 1; i >= 0; i--)
            {
                var tile = _allTiles[i];
                if (tile.isRemoved || !tile.isExposed) continue;

                var halfSize = _constants.tileSize * 0.5f;
                if (worldPoint.x >= tile.worldPosition.x - halfSize.x &&
                    worldPoint.x <= tile.worldPosition.x + halfSize.x &&
                    worldPoint.y >= tile.worldPosition.y - halfSize.y &&
                    worldPoint.y <= tile.worldPosition.y + halfSize.y)
                {
                    return tile;
                }
            }
            return null;
        }

        /// <summary>
        /// Mark a tile as removed from the board (moved to rack or matched).
        /// Triggers RefreshExposure because removing a covering tile may expose tiles below.
        /// </summary>
        public void RemoveTile(TileData tile)
        {
            tile.isRemoved = true;
            OnTileRemoved?.Invoke(tile);
            RefreshExposure();

            if (GetRemainingCount() == 0)
            {
                OnBoardCleared?.Invoke();
            }
        }

        /// <summary> How many tiles remain on the board (not yet removed). </summary>
        public int GetRemainingCount()
        {
            int count = 0;
            foreach (var t in _allTiles)
                if (!t.isRemoved) count++;
            return count;
        }
    }
}
