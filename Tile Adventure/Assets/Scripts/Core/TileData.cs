using System;
using UnityEngine;

namespace TileAdventure.Core
{
    /// <summary>
    /// Plain C# data model for a single tile on the board.
    /// Does NOT inherit from MonoBehaviour — this is pure data, testable without Unity.
    ///
    /// Lifecycle: Created by BoardLogic → rendered by TileView → removed on match.
    /// isExposed determines if the player can tap this tile (no higher-layer tile covers it).
    /// isMoving prevents interaction during board-to-rack animation.
    /// </summary>
    public class TileData
    {
        /// <summary> Unique ID within the board for this play session. </summary>
        public int tileId;

        /// <summary> Icon sprite index (0-13 maps to 1.png through 14.png). </summary>
        public int iconId;

        /// <summary> Z-order layer. 0 is bottom layer, higher numbers are on top. </summary>
        public int layerIndex;

        /// <summary> Logical grid cell position (used for overlap detection). </summary>
        public Vector2Int gridPosition;

        /// <summary> World-space position on screen (pixel coordinates from Canvas). </summary>
        public Vector2 worldPosition;

        /// <summary> True when no higher-layer tile overlaps this tile — player can tap it. </summary>
        public bool isExposed;

        /// <summary> True after the tile has been matched and removed from the board. </summary>
        public bool isRemoved;

        /// <summary> True during the animation from board to rack — taps are ignored. </summary>
        public bool isMoving;

        /// <summary> Fired when exposure state changes (e.g., a covering tile is removed). </summary>
        public event Action<TileData> OnExposureChanged;

        public TileData(int id, int icon, int layer, Vector2Int gridPos, Vector2 worldPos)
        {
            tileId = id;
            iconId = icon;
            layerIndex = layer;
            gridPosition = gridPos;
            worldPosition = worldPos;
            isExposed = false;
            isRemoved = false;
            isMoving = false;
        }

        /// <summary>
        /// Called by BoardLogic.RefreshExposure(). Only fires the event if the value actually changes,
        /// avoiding redundant UI updates.
        /// </summary>
        public void SetExposed(bool exposed)
        {
            if (isExposed != exposed)
            {
                isExposed = exposed;
                OnExposureChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Check if the other tile (which MUST be on a higher layer) covers this tile.
        /// Two tiles overlap if their world-space rectangles intersect.
        /// </summary>
        /// <param name="other">A tile on a strictly higher layer.</param>
        /// <param name="halfSize">Half the tile dimensions in world space (tileSize / 2).</param>
        /// <returns>True if the two tiles visually overlap.</returns>
        public bool Overlaps(TileData other, Vector2 halfSize)
        {
            if (other.layerIndex <= layerIndex)
                return false;

            var myRect = new Rect(worldPosition - halfSize, halfSize * 2f);
            var otherRect = new Rect(other.worldPosition - halfSize, halfSize * 2f);
            return myRect.Overlaps(otherRect);
        }
    }
}
