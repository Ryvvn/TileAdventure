using UnityEngine;

namespace TileAdventure.Config
{
    /// <summary>
    /// Single source of truth for all tunable gameplay constants.
    /// No magic numbers anywhere in code — every numeric value flows from here.
    /// Create via: TileAdventure > Generate GameConstants Asset (Editor menu).
    /// </summary>
    [CreateAssetMenu(fileName = "GameConstants", menuName = "TileAdventure/Game Constants")]
    public class GameConstants : ScriptableObject
    {
        [Header("Rack")]
        [Tooltip("How many slots in the rack (7 by default).")]
        public int rackSlotCount = 7;

        [Tooltip("How many identical adjacent tiles trigger a clear (3 = match-3).")]
        public int matchCount = 3;

        [Header("Tiles")]
        [Tooltip("Total distinct tile icon sprites available (1.png through 14.png).")]
        public int totalTileIcons = 14;

        [Tooltip("Width and height of each tile in canvas units.")]
        public Vector2 tileSize = new Vector2(80f, 80f);

        [Tooltip("Gap between adjacent tiles on the board and rack.")]
        public float tileSpacing = 10f;

        [Tooltip("Diagonal pixel offset per layer to make stacked tiles visible.")]
        public float layerVisualOffset = 10f;

        [Header("Pyramid Layout")]
        [Tooltip("Horizontal distance between grid cells (less than tile width for overlap).")]
        public float gridCellWidth = 48f;

        [Tooltip("Vertical distance between grid cells (less than tile height so lower tile peeks through).")]
        public float gridCellHeight = 40f;

        [Tooltip("Horizontal stagger for odd rows (creates pyramid/hexagonal feel).")]
        public float pyramidStaggerOffset = 24f;

        [Tooltip("Vertical shift per layer so lower tiles remain visible below higher layers.")]
        public float layerVerticalOffset = 28f;

        [Header("Board")]
        [Tooltip("Maximum number of layers a board can have.")]
        public int maxLayers = 5;

        [Header("Animation")]
        [Tooltip("Duration of board-to-rack movement in seconds.")]
        public float tileMoveDuration = 0.3f;

        [Tooltip("Duration of match-clear scale+fade animation in seconds.")]
        public float tileMatchClearDuration = 0.4f;

        [Tooltip("Scale multiplier at the peak of the match-clear animation (1.3 = 130%).")]
        public float tileMatchScaleUp = 1.3f;

        [Header("Input")]
        [Tooltip("Minimum seconds between valid taps on the same tile (debounce).")]
        public float doubleTapThreshold = 0.3f;

        [Header("Blocked Tile")]
        [Tooltip("Tint applied to blocked tiles (dimmer).")]
        public Color blockedTileTint = new Color(0.4f, 0.4f, 0.4f, 1f);

        [Tooltip("Tint for exposed/tappable tiles.")]
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
