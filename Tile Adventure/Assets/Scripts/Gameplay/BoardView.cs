using System;
using System.Collections.Generic;
using TileAdventure.Config;
using TileAdventure.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.Gameplay
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private RectTransform _boardContainer;
        [SerializeField] private GameConstants _constants;

        private Dictionary<int, TileView> _tileViews;
        private BoardLogic _board;
        private Sprite[] _iconSprites;
        private Sprite _backgroundSprite;

        public event Action<TileView> OnTileTapped;

        public void Initialize(BoardLogic board, Sprite[] iconSprites, Sprite backgroundSprite)
        {
            _board = board;
            _iconSprites = iconSprites;
            _backgroundSprite = backgroundSprite;
            _tileViews = new Dictionary<int, TileView>();

            board.OnTileRemoved += OnTileRemoved;
        }

        public void BuildBoard()
        {
            ClearBoard();

            foreach (var tile in _board.AllTiles)
            {
                if (tile.isRemoved) continue;

                var tileView = Instantiate(_tilePrefab, _boardContainer);
                var rt = tileView.GetComponent<RectTransform>();

                rt.anchoredPosition = new Vector2(tile.worldPosition.x, tile.worldPosition.y);
                rt.sizeDelta = _constants.tileSize;

                var sprite = tile.iconId < _iconSprites.Length ? _iconSprites[tile.iconId] : null;
                tileView.Initialize(tile, sprite, _backgroundSprite, _constants.blockedTileTint, _constants.exposedTileColor);
                tileView.OnTileTapped += HandleTileTapped;

                _tileViews[tile.tileId] = tileView;
            }
        }

        private void HandleTileTapped(TileView tileView)
        {
            OnTileTapped?.Invoke(tileView);
        }

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

        public async System.Threading.Tasks.Task AnimateMoveToRack(TileView tileView, Vector2 rackTarget, System.Action onComplete)
        {
            var rt = tileView.GetComponent<RectTransform>();
            var startPos = rt.anchoredPosition;

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

            rt.anchoredPosition = rackTarget;
            onComplete?.Invoke();
        }

        public void RefreshAllTiles()
        {
            foreach (var kvp in _tileViews)
            {
                if (!kvp.Value.Data.isRemoved)
                    kvp.Value.UpdateVisual();
            }
        }

        private void OnTileRemoved(TileData tile)
        {
            if (_tileViews.TryGetValue(tile.tileId, out var view))
            {
                AnimateTileRemoval(view);
            }
        }

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
