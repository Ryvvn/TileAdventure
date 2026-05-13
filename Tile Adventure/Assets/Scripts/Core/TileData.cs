using System;
using UnityEngine;

namespace TileAdventure.Core
{
    public class TileData
    {
        public int tileId;
        public int iconId;
        public int layerIndex;
        public Vector2Int gridPosition;
        public Vector2 worldPosition;
        public bool isExposed;
        public bool isRemoved;
        public bool isMoving;

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

        public void SetExposed(bool exposed)
        {
            if (isExposed != exposed)
            {
                isExposed = exposed;
                OnExposureChanged?.Invoke(this);
            }
        }

        public bool Overlaps(TileData other)
        {
            if (layerIndex != other.layerIndex + 1)
                return false;

            var halfSize = Vector2.one * 0.5f;
            var myRect = new Rect(worldPosition - halfSize, Vector2.one);
            var otherRect = new Rect(other.worldPosition - halfSize, Vector2.one);
            return myRect.Overlaps(otherRect);
        }
    }
}
