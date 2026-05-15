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

        [Header("Debug")]
        [SerializeField] private bool _showDebugGrid;

        /// <summary> tileId → TileView mapping for O(1) lookup during removal. </summary>
        private Dictionary<int, TileView> _tileViews;

        private BoardLogic _board;
        private Sprite[] _iconSprites;
        private Sprite _backgroundSprite;
        private List<GameObject> _debugObjects = new List<GameObject>();
        private Vector2 _boardOffset;

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
        /// Layer scale variance: higher layers are slightly smaller (depth illusion).
        /// Board entrance cascade: tiles animate in layer-by-layer with stagger.
        ///
        /// Call this once after Initialize(). Re-calling on restart just rebuilds from scratch.
        /// </summary>
        public async System.Threading.Tasks.Task BuildBoard()
        {
            ClearBoard();

            var sortedTiles = new List<TileData>();
            foreach (var t in _board.AllTiles)
                if (!t.isRemoved) sortedTiles.Add(t);
            sortedTiles.Sort((a, b) => a.layerIndex.CompareTo(b.layerIndex));

            if (sortedTiles.Count == 0) return;

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
            _boardOffset = offset;

            // Group tiles by layer for cascade animation
            var tilesByLayer = new Dictionary<int, List<(TileData data, TileView view)>>();

            foreach (var tile in sortedTiles)
            {
                var tileView = Instantiate(_tilePrefab, _boardContainer);
                tileView.name = $"Tile_{tile.tileId}";
                var rt = tileView.GetComponent<RectTransform>();

                rt.anchoredPosition = new Vector2(tile.worldPosition.x + offset.x, tile.worldPosition.y + offset.y);
                rt.sizeDelta = _constants.tileSize;

                var sprite = tile.iconId < _iconSprites.Length ? _iconSprites[tile.iconId] : null;
                tileView.Initialize(tile, sprite, _backgroundSprite,
                    _constants.blockedTileTint, _constants.exposedTileColor);
                tileView.OnTileTapped += HandleTileTapped;

                // Layer scale variance: higher layers look slightly smaller (depth illusion)
                var layerScale = 1f - tile.layerIndex * _constants.layerScaleFalloff;
                tileView.SetBaseScale(Vector3.one * layerScale);
                tileView.SetHoverScale(_constants.hoverGlowScale);

                // Start hidden for cascade animation
                var cg = tileView.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                tileView.transform.localScale = Vector3.zero;

                if (!tilesByLayer.ContainsKey(tile.layerIndex))
                    tilesByLayer[tile.layerIndex] = new List<(TileData, TileView)>();
                tilesByLayer[tile.layerIndex].Add((tile, tileView));

                _tileViews[tile.tileId] = tileView;
            }

            // Cascade entrance: each layer fades in with a stagger delay
            var maxLayer = _constants.maxLayers;
            for (int layer = 0; layer <= maxLayer; layer++)
            {
                if (!tilesByLayer.TryGetValue(layer, out var layerTiles))
                    continue;

                foreach (var (_, view) in layerTiles)
                {
                    AnimateCascadeIn(view);
                }

                await System.Threading.Tasks.Task.Delay(
                    (int)(_constants.boardCascadeDelayPerLayer * 1000f));
            }

            SortTilesByLayer();
            DrawDebugGrid();
        }

        private async void AnimateCascadeIn(TileView tileView)
        {
            var cg = tileView.gameObject.GetComponent<CanvasGroup>();
            var targetScale = 1f - tileView.Data.layerIndex * _constants.layerScaleFalloff;

            float duration = _constants.boardCascadeDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (tileView == null) return;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t);
                tileView.transform.localScale = Vector3.one * Mathf.Lerp(0f, targetScale, t);
                cg.alpha = t;
                await System.Threading.Tasks.Task.Yield();
            }

            if (tileView == null) return;
            tileView.SetBaseScale(Vector3.one * targetScale);
            cg.alpha = 1f;
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
            var canvasGroup = tileView.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = tileView.gameObject.AddComponent<CanvasGroup>();

            float duration = _constants.tileMatchClearDuration;
            float elapsed = 0f;
            var startScale = rt.localScale;

            while (elapsed < duration)
            {
                if (tileView == null) return;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.localScale = startScale * (1f + t * (_constants.tileMatchScaleUp - 1f));
                canvasGroup.alpha = 1f - t;
                await System.Threading.Tasks.Task.Yield();
            }

            if (tileView == null) return;
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
            Vector2 rackTargetWorld, System.Action onComplete)
        {
            var rt = tileView.GetComponent<RectTransform>();
            var startPos = rt.anchoredPosition;

            var rackTarget = (Vector2)_boardContainer.InverseTransformPoint(rackTargetWorld);

            float duration = _constants.tileMoveDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t);
                rt.anchoredPosition = Vector2.Lerp(startPos, rackTarget, t);
                await System.Threading.Tasks.Task.Yield();
            }

            // Snap overshoot: briefly overshoot below the target, then settle back
            rt.anchoredPosition = rackTarget + new Vector2(0f, -_constants.rackSnapOvershoot);

            elapsed = 0f;
            var snapStart = rt.anchoredPosition;
            float bounceDuration = _constants.rackSnapBounceDuration;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceDuration;
                t = t * t * (3f - 2f * t);
                rt.anchoredPosition = Vector2.Lerp(snapStart, rackTarget, t);
                await System.Threading.Tasks.Task.Yield();
            }

            rt.anchoredPosition = rackTarget;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Spawn a burst of colored particles when a match-3 is cleared.
        /// Particles are small plain-color squares that fly outward from the board center
        /// and fade out. Called by GameplayController.OnMatchCleared.
        /// </summary>
        public void SpawnMatchParticles(int iconId, int comboLevel = 0)
        {
            var hue = (iconId * _constants.matchParticleHueStep) % 1f;
            var color = Color.HSVToRGB(hue, 0.8f, 0.95f);

            int particleCount = _constants.matchParticleCount;
            if (comboLevel > 0)
            {
                particleCount = comboLevel == 5 ? 24 : 2 + comboLevel * 4;
            }

            for (int i = 0; i < particleCount; i++)
            {
                var go = new GameObject("Particle", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                go.transform.SetParent(_boardContainer, false);
                var prt = go.GetComponent<RectTransform>();
                prt.anchoredPosition = Vector2.zero;
                prt.sizeDelta = new Vector2(_constants.matchParticleSize, _constants.matchParticleSize);
                go.GetComponent<Image>().color = color;

                var dir = UnityEngine.Random.insideUnitCircle.normalized;
                AnimateParticle(go, dir);
            }
        }

        private async void AnimateParticle(GameObject go, Vector2 direction)
        {
            var rt = go.GetComponent<RectTransform>();
            var cg = go.GetComponent<CanvasGroup>();
            var speed = _constants.matchParticleSpeed * (0.6f + UnityEngine.Random.value * 0.8f);

            float duration = _constants.matchParticleDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (go == null) return;
                rt.anchoredPosition += direction * speed * Time.deltaTime;
                cg.alpha = 1f - t;
                speed *= Mathf.Pow(0.96f, Time.deltaTime * 60f);
                await System.Threading.Tasks.Task.Yield();
            }

            Destroy(go);
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

        /// <summary>
        /// Create TileViews for new refill tiles and cascade them in.
        /// Called by GameplayController when EndlessLevelManager generates refill tiles.
        /// </summary>
        public async System.Threading.Tasks.Task AnimateRefillTiles(List<TileData> newTiles)
        {
            if (newTiles == null || newTiles.Count == 0) return;

            var tilesByLayer = new Dictionary<int, List<(TileData data, TileView view)>>();

            foreach (var tile in newTiles)
            {
                var tileView = Instantiate(_tilePrefab, _boardContainer);
                tileView.name = $"Tile_{tile.tileId}";
                var rt = tileView.GetComponent<RectTransform>();

                rt.anchoredPosition = new Vector2(tile.worldPosition.x + _boardOffset.x, tile.worldPosition.y + _boardOffset.y);
                rt.sizeDelta = _constants.tileSize;

                var sprite = tile.iconId < _iconSprites.Length ? _iconSprites[tile.iconId] : null;
                tileView.Initialize(tile, sprite, _backgroundSprite,
                    _constants.blockedTileTint, _constants.exposedTileColor);
                tileView.OnTileTapped += HandleTileTapped;

                var layerScale = 1f - tile.layerIndex * _constants.layerScaleFalloff;
                tileView.SetBaseScale(Vector3.one * layerScale);
                tileView.SetHoverScale(_constants.hoverGlowScale);

                var cg = tileView.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                tileView.transform.localScale = Vector3.zero;

                if (!tilesByLayer.ContainsKey(tile.layerIndex))
                    tilesByLayer[tile.layerIndex] = new List<(TileData, TileView)>();
                tilesByLayer[tile.layerIndex].Add((tile, tileView));

                _tileViews[tile.tileId] = tileView;
            }

            var maxLayer = _constants.maxLayers;
            for (int layer = 0; layer <= maxLayer; layer++)
            {
                if (!tilesByLayer.TryGetValue(layer, out var layerTiles))
                    continue;

                foreach (var (_, view) in layerTiles)
                {
                    AnimateCascadeIn(view);
                }

                await System.Threading.Tasks.Task.Delay(
                    (int)(_constants.boardCascadeDelayPerLayer * 1000f));
            }

            SortTilesByLayer();
            DrawDebugGrid();
        }

        /// <summary> BoardLogic event handler — animates and destroys a tile view when its data is removed. </summary>
        private void OnTileRemoved(TileData tile)
        {
            if (_tileViews.TryGetValue(tile.tileId, out var view))
            {
                AnimateTileRemoval(view);
            }
        }

        /// <summary> Returns the highest tile ID currently rendered (for refill tracking). </summary>
        public int GetMaxTileId()
        {
            var max = 0;
            foreach (var key in _tileViews.Keys)
                if (key > max) max = key;
            return max;
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

        private void SortTilesByLayer()
        {
            var tileList = new List<TileView>();
            foreach (var kvp in _tileViews)
            {
                if (kvp.Value != null && !kvp.Value.Data.isRemoved)
                    tileList.Add(kvp.Value);
            }

            tileList.Sort((a, b) => a.Data.layerIndex.CompareTo(b.Data.layerIndex));

            foreach (var tileView in tileList)
                tileView.transform.SetAsLastSibling();
        }

        private void DrawDebugGrid()
        {
            ClearDebugGrid();

            if (!_showDebugGrid || _board == null) return;

            var layerColors = new Color[]
            {
                new Color(0f, 0.5f, 1f, 0.3f),
                new Color(0f, 1f, 0.3f, 0.3f),
                new Color(1f, 0.9f, 0f, 0.3f),
                new Color(1f, 0.2f, 0f, 0.3f),
                new Color(1f, 0f, 1f, 0.3f),
                new Color(0f, 1f, 1f, 0.3f),
            };

            foreach (var tile in _board.AllTiles)
            {
                if (tile.isRemoved) continue;

                var color = layerColors[tile.layerIndex % layerColors.Length];

                var go = new GameObject($"Debug_{tile.tileId}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_boardContainer, false);
                go.transform.SetAsFirstSibling();

                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(tile.worldPosition.x + _boardOffset.x, tile.worldPosition.y + _boardOffset.y);
                rt.sizeDelta = _constants.tileSize;

                var img = go.GetComponent<Image>();
                img.color = color;
                img.raycastTarget = false;

                _debugObjects.Add(go);
            }
        }

        private void ClearDebugGrid()
        {
            foreach (var go in _debugObjects)
            {
                if (go != null) Destroy(go);
            }
            _debugObjects.Clear();
        }

        private void OnDestroy()
        {
            if (_board != null)
                _board.OnTileRemoved -= OnTileRemoved;
        }
    }
}
