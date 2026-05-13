using System;
using System.Collections.Generic;
using UnityEngine;

namespace TileAdventure.Config
{
    [CreateAssetMenu(fileName = "LevelConfig_XX", menuName = "TileAdventure/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        public int levelNumber;

        [Header("Objectives")]
        public int targetTriples = 4;

        [Header("Board Setup")]
        public int layerCount = 2;
        public int activeIconCount = 6;

        [Header("Difficulty")]
        public int rackSlotCount = 7;

        [Serializable]
        public struct TilePlacement
        {
            public int iconId;
            public int layerIndex;
            public Vector2Int gridPosition;
        }

        public List<TilePlacement> tiles;
    }
}
