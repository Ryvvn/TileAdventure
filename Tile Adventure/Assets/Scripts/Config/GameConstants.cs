using UnityEngine;

namespace TileAdventure.Config
{
    [CreateAssetMenu(fileName = "GameConstants", menuName = "TileAdventure/Game Constants")]
    public class GameConstants : ScriptableObject
    {
        [Header("Rack")]
        public int rackSlotCount = 7;
        public int matchCount = 3;

        [Header("Tiles")]
        public int totalTileIcons = 14;
        public Vector2 tileSize = new Vector2(80f, 80f);
        public float tileSpacing = 4f;
        public float layerVisualOffset = 4f;

        [Header("Board")]
        public int maxLayers = 5;

        [Header("Animation")]
        public float tileMoveDuration = 0.3f;
        public float tileMatchClearDuration = 0.4f;
        public float tileMatchScaleUp = 1.3f;

        [Header("Input")]
        public float doubleTapThreshold = 0.3f;

        [Header("Blocked Tile")]
        public Color blockedTileTint = new Color(0.4f, 0.4f, 0.4f, 1f);
        public Color exposedTileColor = Color.white;

        [Header("Level Defaults")]
        public int defaultTargetTriples = 4;
        public int defaultLayerCount = 2;
        public int defaultActiveIcons = 6;

        [Header("Scene Names")]
        public string loadingSceneName = "Loading";
        public string homeSceneName = "Home";
        public string gameplaySceneName = "Gameplay";
    }
}
