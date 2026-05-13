using System;
using System.Collections.Generic;
using UnityEngine;
using TileAdventure.Config;

namespace TileAdventure.Core
{
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

        private List<(int iconId, int layer, Vector2Int gridPos)> GenerateSolvableLayout(
            int totalTiles, int layerCount, int activeIcons, System.Random rng)
        {
            var result = new List<(int, int, Vector2Int)>();
            var tripleCount = totalTiles / _constants.matchCount;
            var usedCells = new HashSet<Vector2Int>[layerCount];
            for (int i = 0; i < layerCount; i++)
                usedCells[i] = new HashSet<Vector2Int>();

            for (int t = 0; t < tripleCount; t++)
            {
                int iconId = rng.Next(activeIcons);
                var tripleCells = new List<(int layer, Vector2Int gridPos)>();

                for (int i = 0; i < _constants.matchCount; i++)
                {
                    int layer = rng.Next(layerCount);
                    Vector2Int pos;
                    int attempts = 0;
                    do
                    {
                        pos = new Vector2Int(rng.Next(6), rng.Next(6));
                        attempts++;
                    }
                    while (usedCells[layer].Contains(pos) && attempts < 50);

                    usedCells[layer].Add(pos);
                    tripleCells.Add((layer, pos));
                }

                foreach (var (layer, gridPos) in tripleCells)
                {
                    result.Add((iconId, layer, gridPos));
                }
            }

            return result;
        }

        public Vector2 GridToWorld(Vector2Int gridPos, int layer)
        {
            var tileW = _constants.tileSize.x + _constants.tileSpacing;
            var tileH = _constants.tileSize.y + _constants.tileSpacing;
            var x = gridPos.x * tileW;
            var y = gridPos.y * tileH;
            var layerOffX = layer * _constants.layerVisualOffset;
            var layerOffY = layer * _constants.layerVisualOffset;
            return new Vector2(x + layerOffX, y + layerOffY);
        }

        public void RefreshExposure()
        {
            foreach (var tile in _allTiles)
            {
                if (tile.isRemoved) continue;
                bool blocked = false;
                foreach (var other in _allTiles)
                {
                    if (other.isRemoved || other.tileId == tile.tileId) continue;
                    if (tile.Overlaps(other))
                    {
                        blocked = true;
                        break;
                    }
                }
                tile.SetExposed(!blocked);
            }
        }

        public TileData GetTileById(int id)
        {
            return _allTiles.Find(t => t.tileId == id);
        }

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

        public int GetRemainingCount()
        {
            int count = 0;
            foreach (var t in _allTiles)
                if (!t.isRemoved) count++;
            return count;
        }
    }
}
