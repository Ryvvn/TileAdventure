using System;
using TileAdventure.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TileAdventure.Gameplay
{
    /// <summary>
    /// MonoBehaviour view for a single tile. Renders the tile-base background + icon sprite,
    /// handles tap/click input, visual states (exposed/dimmed), and blocked-tile shake feedback.
    ///
    /// Uses IPointerClickHandler (Unity UI event system) — the Canvas Raycaster masks clicks
    /// to the topmost visible tile at the tap position.
    ///
    /// Note: _iconImage and _background are injected via the scene hierarchy (Editor script).
    /// If missing, the tile renders blank but taps still register.
    /// </summary>
    public class TileView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _iconImage;

        /// <summary> The TileData this view represents. Set during Initialize(). </summary>
        public TileData Data { get; private set; }

        /// <summary> Fired on valid tap (exposed, not removed, not moving, not double-tap). </summary>
        public event Action<TileView> OnTileTapped;

        private RectTransform _rectTransform;
        private float _lastTapTime;
        private Color _blockedColor;
        private Color _exposedColor;
        private Vector3 _baseScale = Vector3.one;
        private float _hoverScale = 1.08f;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Bind this view to a TileData model. Subscribes to exposure changes
        /// so the visual updates automatically when a covering tile is removed.
        /// </summary>
        public void Initialize(TileData data, Sprite iconSprite, Sprite backgroundSprite,
            Color blockedColor, Color exposedColor)
        {
            Data = data;
            _blockedColor = blockedColor;
            _exposedColor = exposedColor;
            _iconImage.sprite = iconSprite;
            _background.sprite = backgroundSprite;
            _background.color = data.isExposed ? exposedColor : blockedColor;
            _iconImage.color = data.isExposed ? exposedColor : blockedColor;

            data.OnExposureChanged += OnExposureChanged;
        }

        public void SetBaseScale(Vector3 scale)
        {
            _baseScale = scale;
            transform.localScale = scale;
        }

        public void SetHoverScale(float scale)
        {
            _hoverScale = scale;
        }

        /// <summary> Called automatically when the tile's exposure state changes. </summary>
        private void OnExposureChanged(TileData data)
        {
            UpdateVisual();
        }

        /// <summary>
        /// Refresh background/icon tint based on current isExposed state.
        /// Exposed = full white, Blocked = dark gray (dimmed).
        /// Also called by BoardView.RefreshAllTiles() after match clears.
        /// </summary>
        public void UpdateVisual()
        {
            if (Data == null || Data.isRemoved) return;
            bool exposed = Data.isExposed;
            _background.color = exposed ? _exposedColor : _blockedColor;
            _iconImage.color = exposed ? _exposedColor : _blockedColor;
        }

        /// <summary>
        /// Unity UI click handler. Decision flow:
        ///   1. Ignore if tile is removed, mid-animation, or data is missing
        ///   2. Debounce double-taps (300ms threshold)
        ///   3. If blocked: play shake animation and ignore
        ///   4. If exposed: fire OnTileTapped → GameplayController processes it
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Data == null || Data.isRemoved || Data.isMoving)
                return;

            // Debounce rapid taps (prevents double-processing from EventSystem)
            if (Time.time - _lastTapTime < 0.3f)
                return;

            _lastTapTime = Time.time;

            if (!Data.isExposed)
            {
                ShakeBlockedTile();
                return;
            }

            OnTileTapped?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Data == null || Data.isRemoved || Data.isMoving || !Data.isExposed)
                return;
            transform.localScale = _baseScale * _hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = _baseScale;
        }

        /// <summary>
        /// Short horizontal shake when the player taps a blocked (dimmed) tile.
        /// Uses sinusoidal oscillation with decaying amplitude over 0.15 seconds.
        /// Async void is acceptable here since it's fire-and-forget visual feedback.
        /// </summary>
        private async void ShakeBlockedTile()
        {
            var startPos = _rectTransform.anchoredPosition;
            float duration = 0.15f;
            float elapsed = 0f;
            float amplitude = 5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = startPos.x + Mathf.Sin(elapsed * 50f) * amplitude * (1f - elapsed / duration);
                _rectTransform.anchoredPosition = new Vector2(x, startPos.y);
                await System.Threading.Tasks.Task.Yield();
            }

            _rectTransform.anchoredPosition = startPos;
        }

        /// <summary> Called by BoardView.AnimateTileRemoval() before destruction. </summary>
        public void MarkRemoved()
        {
            if (Data != null)
                Data.isRemoved = true;
        }

        /// <summary> Clean up event subscription to prevent leaks. </summary>
        private void OnDestroy()
        {
            if (Data != null)
                Data.OnExposureChanged -= OnExposureChanged;
        }
    }
}
