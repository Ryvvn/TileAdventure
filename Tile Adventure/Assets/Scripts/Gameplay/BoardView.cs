using System;
using System.Collections.Generic;
using TileAdventure.Config;
using TileAdventure.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.Gameplay
{
    /// <summary>
    /// Renders the board: instantiates TileView prefabs for each TileData in BoardLogic,
    /// handles tile-to-rack movement animation, and removes/destroys tiles on match.
    ///
    /// Relationship: BoardLogic (data) ↔ BoardView (visual). BoardView observes
    /// BoardLogic.OnTileRemoved to trigger destroy animations.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private RectTransform _boardContainer;
        [SerializeField] private GameConstants _constants;

        /// <summary> tileId → TileView mapping for O(1) lookup during removal. </summary>
        private Dictionary<int, TileView> _tileViews;

        private BoardLogic _board;
        private Sprite[] _iconSprites;
        private Sprite _backgroundSprite;

        /// <summary> Fired when any tile is tapped (relayed to GameplayController). </summary>
        public event Action<TileView> OnTileTapped;

        public void Initialize(BoardLogic board, Sprite[] iconSprites, Sprite backgroundSprite)
        {
            _board = board;
            _iconSprites = iconSprites;
            _backgroundSprite = backgroundSprite;
            _tileViews = new Dictionary<int, TileView>();

            board.OnTileRemoved += OnTileRemoved;
        }

        /// <summary>
        /// Create TileView instances for every tile in BoardLogic.AllTiles.
        /// Positions are set from TileData.worldPosition (computed by BoardLogic.GridToWorld).
        /// Each tile subscribes to BoardView.OnTileTapped for click relay.
        ///
        /// Call this once after Initialize(). Re-calling on restart just rebuilds from scratch.
        /// </summary>
        public void BuildBoard()
        {
            ClearBoard();

            // Sort by layerIndex ascending so lower layers instantiate first.
            // In ScreenSpaceOverlay Canvas, later children render ON TOP.
            // This ensures layer 0 tiles are below layer 1 tiles, etc.
            var sortedTiles = new List<TileData>();
            foreach (var t in _board.AllTiles)
                if (!t.isRemoved) sortedTiles.Add(t);
            sortedTiles.Sort((a, b) => a.layerIndex.CompareTo(b.layerIndex));

            if (sortedTiles.Count == 0) return;

            // Compute bounding box of all tile world positions,
            // then center the whole group within the BoardContainer.
            // GridToWorld starts at (0,0) and grows right/up, so without this
            // the board would float to the top-right corner.
            var halfSize = _constants.tileSize * 0.5f;
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var tile in sortedTiles)
            {
                var wp = tile.worldPosition;
                minX = Mathf.Min(minX, wp.x - halfSize.x);
                minY = Mathf.Min(minY, wp.y - halfSize.y);
                maxX = Mathf.Max(maxX, wp.x + halfSize.x);
                maxY = Mathf.Max(maxY, wp.y + halfSize.y);
            }
            var offset = new Vector2(-(minX + maxX) / 2f, -(minY + maxY) / 2f);

            foreach (var tile in sortedTiles)
            {
                var tileView = Instantiate(_tilePrefab, _boardContainer);
                tileView.name = $"Tile_{tile.tileId}";
                var rt = tileView.GetComponent<RectTransform>();

                // Position relative to BoardContainer center (pivot 0.5,0.5).
                // offset re-centers the bounding-box so the tile group is not skewed up-right.
                rt.anchoredPosition = new Vector2(tile.worldPosition.x + offset.x, tile.worldPosition.y + offset.y);
                rt.sizeDelta = _constants.tileSize;

                var sprite = tile.iconId < _iconSprites.Length ? _iconSprites[tile.iconId] : null;
                tileView.Initialize(tile, sprite, _backgroundSprite,
                    _constants.blockedTileTint, _constants.exposedTileColor);
                tileView.OnTileTapped += HandleTileTapped;

                _tileViews[tile.tileId] = tileView;
            }
        }

        private void HandleTileTapped(TileView tileView)
        {
            OnTileTapped?.Invoke(tileView);
        }

        /// <summary>
        /// Play scale-up + fade-out on a tile view before destroying it.
        /// Called by OnTileRemoved() when BoardLogic reports a removed tile.
        /// Async void — fire and forget, doesn't block game logic.
        /// </summary>
        public async void AnimateTileRemoval(TileView tileView)
        {
            tileView.MarkRemoved();

            var rt = tileView.GetComponent<RectTransform>();
            var canvasGroup = tileView.gameObject.AddComponent<CanvasGroup>();

            float duration = _constants.tileMatchClearDuration;
            float elapsed = 0f;
            var startScale = rt.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.localScale = startScale * (1f + t * (_constants.tileMatchScaleUp - 1f));
                canvasGroup.alpha = 1f - t;
                await System.Threading.Tasks.Task.Yield();
            }

            Destroy(tileView.gameObject);
            _tileViews.Remove(tileView.Data.tileId);
        }

        /// <summary>
        /// Animate a tile moving from its current board position to a rack slot position.
        /// Uses smoothstep easing (cubic Hermite) for natural-looking motion.
        /// The onComplete callback fires synchronously after the animation finishes.
        ///
        /// Called by GameplayController.OnTileTapped() — the callback chains into
        /// RackLogic.AddTile + BoardLogic.RemoveTile.
        /// </summary>
        /// <param name="tileView">The tile view being moved.</param>
        /// <param name="rackTarget">World position of the target rack slot.</param>
        /// <param name="onComplete">Action to run after movement finishes.</param>
        public async System.Threading.Tasks.Task AnimateMoveToRack(TileView tileView,
            Vector2 rackTarget, System.Action onComplete)
        {
            var rt = tileView.GetComponent<RectTransform>();
            var startPos = rt.anchoredPosition;

            float duration = _constants.tileMoveDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Smoothstep: 3t² - 2t³ for ease-in-out
                t = t * t * (3f - 2f * t);
                rt.anchoredPosition = Vector2.Lerp(startPos, rackTarget, t);
                await System.Threading.Tasks.Task.Yield();
            }

            rt.anchoredPosition = rackTarget;
            onComplete?.Invoke();
        }

        /// <summary> Refresh all remaining tile visuals (e.g., after a match clears to update exposure). </summary>
        public void RefreshAllTiles()
        {
            foreach (var kvp in _tileViews)
            {
                if (!kvp.Value.Data.isRemoved)
                    kvp.Value.UpdateVisual();
            }
        }

        /// <summary> BoardLogic event handler — animates and destroys a tile view when its data is removed. </summary>
        private void OnTileRemoved(TileData tile)
        {
            if (_tileViews.TryGetValue(tile.tileId, out var view))
            {
                AnimateTileRemoval(view);
            }
        }

        /// <summary> Destroy all instantiated tile GameObjects and clear the lookup. </summary>
        private void ClearBoard()
        {
            foreach (var kvp in _tileViews)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _tileViews.Clear();
        }

        private void OnDestroy()
        {
            if (_board != null)
                _board.OnTileRemoved -= OnTileRemoved;
        }
    }
}
