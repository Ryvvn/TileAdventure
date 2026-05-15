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
        /// Validate a LevelConfig for same-layer visual tile overlaps.
        /// Returns true if the layout is clean (no same-layer overlaps), false otherwise.
        /// Used to detect and reject old pre-generated configs with overlapping layouts.
        /// </summary>
        public static bool ValidateLayout(LevelConfig config)
        {
            if (config == null || config.tiles == null) return true;

            var halfSize = new Vector2(40f, 40f);
            var rectsByLayer = new Dictionary<int, List<Rect>>();

            foreach (var placement in config.tiles)
            {
                var worldPos = LevelGridToWorldStatic(placement.gridPosition, placement.layerIndex);
                var rect = new Rect(worldPos.x - halfSize.x, worldPos.y - halfSize.y, 80f, 80f);

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

        private static Vector2 LevelGridToWorldStatic(Vector2Int gridPos, int layer)
        {
            var staggerX = (gridPos.y % 2) * 40f;
            var x = gridPos.x * 80f + staggerX + layer * 12f;
            var y = gridPos.y * 40f + layer * 12f;
            return new Vector2(x, y);
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
            var baseSize = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(tilesPerLayer)));
            var gridWidth = baseSize;
            var gridHeight = baseSize * 2;

            var halfSize = _constants.tileSize * 0.5f;
            var placedWorldRects = new List<(int layer, Rect rect)>();

            for (int t = 0; t < tripleCount; t++)
            {
                int iconId = rng.Next(activeIcons);

                for (int i = 0; i < _constants.matchCount; i++)
                {
                    int layer = rng.Next(layerCount);
                    Vector2Int pos;
                    Vector2 worldPos;
                    int attempts = 0;
                    do
                    {
                        pos = new Vector2Int(rng.Next(gridWidth), rng.Next(gridHeight));
                        worldPos = GridToWorld(pos, layer);
                        attempts++;
                    }
                    while ((usedCells[layer].Contains(pos)
                        || SameLayerOverlap(layer, new Rect(worldPos.x - halfSize.x, worldPos.y - halfSize.y,
                            _constants.tileSize.x, _constants.tileSize.y), placedWorldRects))
                        && attempts < 100);

                    usedCells[layer].Add(pos);
                    placedWorldRects.Add((layer, new Rect(worldPos.x - halfSize.x, worldPos.y - halfSize.y,
                        _constants.tileSize.x, _constants.tileSize.y)));
                    result.Add((iconId, layer, pos));
                }
            }

            return result;
        }

        private bool SameLayerOverlap(int layer, Rect myRect, List<(int layer, Rect rect)> placed)
        {
            foreach (var (pl, pr) in placed)
            {
                if (pl == layer && myRect.Overlaps(pr))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Convert a grid position and layer index to a world-space position on the Canvas.
        /// Quarter-overlap pyramid: each row is halfTileHeight below the previous,
        /// odd rows stagger right by halfTileWidth. This creates 40×40 = ¼-area overlap
        /// between diagonally adjacent tiles. Two covering tiles = half covered.
        /// Higher layers shift diagonally (layerVerticalOffset per layer) for depth.
        ///
        /// Layout: gridCellWidth=80, gridCellHeight=40, pyramidStaggerOffset=40
        ///         layerVerticalOffset=12
        ///
        /// Example:
        ///   grid(0,0) layer0 → (0,0)     grid(1,0) layer0 → (80,0)
        ///   grid(0,1) layer0 → (40,40)   grid(1,1) layer0 → (120,40)
        ///   grid(0,0) layer1 → (12,12)   grid(0,1) layer1 → (52,52)
        ///   Overlap: (0,0)L0 + (0,1)L0 → 40×40 = ¼ tile area
        /// </summary>
        public Vector2 GridToWorld(Vector2Int gridPos, int layer)
        {
            var staggerX = (gridPos.y % 2) * _constants.pyramidStaggerOffset;
            var x = gridPos.x * _constants.gridCellWidth + staggerX + layer * _constants.layerVerticalOffset;
            var y = gridPos.y * _constants.gridCellHeight + layer * _constants.layerVerticalOffset;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Check if a proposed tile position would visually overlap any existing tile
        /// on the same layer. Uses world-space rect overlap with tileSize.
        /// </summary>
        public bool WouldOverlapSameLayer(int layer, Vector2 worldPos, List<TileData> existingTiles)
        {
            var halfSize = _constants.tileSize * 0.5f;
            var myRect = new Rect(worldPos.x - halfSize.x, worldPos.y - halfSize.y,
                _constants.tileSize.x, _constants.tileSize.y);

            foreach (var tile in existingTiles)
            {
                if (tile.isRemoved || tile.layerIndex != layer) continue;
                var otherRect = new Rect(tile.worldPosition.x - halfSize.x,
                    tile.worldPosition.y - halfSize.y,
                    _constants.tileSize.x, _constants.tileSize.y);
                if (myRect.Overlaps(otherRect))
                    return true;
            }
            return false;
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

        /// <summary>
        /// Generate and add new tiles to the board for endless mode refill.
        /// Uses triple-first construction with the given difficulty params.
        /// New tiles are placed on the same layers as existing tiles, avoiding occupied cells.
        /// Returns the list of newly created TileData for view cascade animation.
        /// </summary>
        public List<TileData> AddRefillTiles(int tileCount, int activeIcons, int layerCount)
        {
            var rng = new System.Random();
            var newTiles = new List<TileData>();

            var maxId = 0;
            foreach (var t in _allTiles)
                if (t.tileId >= maxId) maxId = t.tileId;
            var nextId = maxId + 1;

            var layerCountActual = Math.Max(layerCount, 1);

            var usedCells = new HashSet<Vector2Int>[layerCountActual];
            for (int i = 0; i < layerCountActual; i++)
                usedCells[i] = new HashSet<Vector2Int>();

            foreach (var tile in _allTiles)
            {
                if (!tile.isRemoved && tile.layerIndex < layerCountActual)
                    usedCells[tile.layerIndex].Add(tile.gridPosition);
            }

            var tripleCount = tileCount / _constants.matchCount;
            var tilesPerLayer = Mathf.CeilToInt((float)tileCount / layerCountActual);
            var baseSize = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(tilesPerLayer)));
            var refillGridWidth = baseSize;
            var refillGridHeight = baseSize * 2;

            var halfSize = _constants.tileSize * 0.5f;
            var allWorldRects = new List<(int layer, Rect rect)>();

            foreach (var tile in _allTiles)
            {
                if (tile.isRemoved) continue;
                allWorldRects.Add((tile.layerIndex, new Rect(
                    tile.worldPosition.x - halfSize.x, tile.worldPosition.y - halfSize.y,
                    _constants.tileSize.x, _constants.tileSize.y)));
            }

            for (int t = 0; t < tripleCount; t++)
            {
                int iconId = rng.Next(activeIcons);

                for (int i = 0; i < _constants.matchCount; i++)
                {
                    int maxRefillLayer = Math.Max(1, (layerCountActual + 1) / 2);
                    int layer = rng.Next(maxRefillLayer);
                    Vector2Int pos;
                    Vector2 worldPos;
                    int attempts = 0;
                    do
                    {
                        pos = new Vector2Int(rng.Next(refillGridWidth), rng.Next(refillGridHeight));
                        worldPos = GridToWorld(pos, layer);
                        attempts++;
                    }
                    while ((usedCells[layer].Contains(pos)
                        || SameLayerOverlap(layer, new Rect(worldPos.x - halfSize.x, worldPos.y - halfSize.y,
                            _constants.tileSize.x, _constants.tileSize.y), allWorldRects))
                        && attempts < 100);

                    usedCells[layer].Add(pos);

                    allWorldRects.Add((layer, new Rect(worldPos.x - halfSize.x, worldPos.y - halfSize.y,
                        _constants.tileSize.x, _constants.tileSize.y)));
                    var tile = new TileData(nextId++, iconId, layer, pos, worldPos);
                    newTiles.Add(tile);
                    _allTiles.Add(tile);
                }
            }

            var bumpCount = Math.Max(1, (layerCountActual + 1) / 2);
            foreach (var tile in _allTiles)
            {
                if (tile.isRemoved) continue;
                if (newTiles.Contains(tile)) continue;
                tile.layerIndex += bumpCount;
            }

            RefreshExposure();
            return newTiles;
        }
    }
}
