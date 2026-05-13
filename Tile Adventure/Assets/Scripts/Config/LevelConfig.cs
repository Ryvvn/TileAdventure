using System;
using System.Collections.Generic;
using UnityEngine;

namespace TileAdventure.Config
{
    /// <summary>
    /// ScriptableObject defining one level's parameters and tile layout.
    /// Can either be hand-authored in the Editor OR procedurally generated
    /// by LevelGenerator. Create via: TileAdventure > Generate Level Assets.
    ///
    /// If tiles list is empty, BoardLogic falls back to procedural generation
    /// using the difficulty parameters (targetTriples, layerCount, etc).
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig_XX", menuName = "TileAdventure/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Tooltip("Level number (1-10). Used for save progress and lookup.")]
        public int levelNumber;

        [Header("Objectives")]
        [Tooltip("How many triples must be cleared to win this level.")]
        public int targetTriples = 4;

        [Header("Board Setup")]
        [Tooltip("How many stacked layers of tiles. Higher = more blocking.")]
        public int layerCount = 2;

        [Tooltip("How many distinct icons are active in this level (out of 14).")]
        public int activeIconCount = 6;

        [Header("Difficulty")]
        [Tooltip("Rack size for this level (smaller = easier to overflow = harder).")]
        public int rackSlotCount = 7;

        /// <summary>
        /// Describes a single tile on the board: which icon, which layer, which grid cell.
        /// Used both in hand-authored configs and procedurally generated ones.
        /// </summary>
        [Serializable]
        public struct TilePlacement
        {
            [Tooltip("Icon sprite index (0-13).")]
            public int iconId;

            [Tooltip("Z-order layer. 0 = bottom, higher = on top.")]
            public int layerIndex;

            [Tooltip("Grid cell position. Converted to world coords by BoardLogic.GridToWorld.")]
            public Vector2Int gridPosition;
        }

        /// <summary>
        /// Ordered list of tile placements. Empty list = use procedural generation.
        /// Total tile count should equal targetTriples × 3 for a full board.
        /// </summary>
        public List<TilePlacement> tiles;
    }
}
