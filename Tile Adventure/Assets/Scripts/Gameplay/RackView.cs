using System.Collections.Generic;
using TileAdventure.Config;
using TileAdventure.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.Gameplay
{
    /// <summary>
    /// Renders the rack — a horizontal row of N slots at the bottom of the screen.
    /// Slots are empty background images. As tiles are added, Icon child objects
    /// are created/updated to show the tile sprites.
    ///
    /// Observes RackLogic events:
    ///   OnTileAdded  — creates/updates the icon at the insert slot
    ///   OnTileShift  — updates the icon at a slot that received a shifted tile
    ///   OnMatchCleared — plays scale+fade animation then reconciles all slots
    /// </summary>
    public class RackView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rackContainer;
        [SerializeField] private TileView _rackTilePrefab;
        [SerializeField] private GameConstants _constants;

        private List<TileView> _slotViews;
        private RackLogic _rack;
        private Sprite[] _iconSprites;
        private Sprite _backgroundSprite;

        public void Initialize(RackLogic rack, Sprite[] iconSprites, Sprite backgroundSprite)
        {
            _rack = rack;
            _iconSprites = iconSprites;
            _backgroundSprite = backgroundSprite;
            _slotViews = new List<TileView>();

            _rack.OnTileAdded += OnTileAdded;
            _rack.OnMatchCleared += OnMatchCleared;
            _rack.OnTileShift += OnTileShift;

            BuildSlots();
        }

        /// <summary>
        /// Create the visual slot objects. Centered horizontally within the rack container.
        /// Spacing uses tileSize + tileSpacing from GameConstants.
        /// </summary>
        private void BuildSlots()
        {
            int count = _rack.Slots.Count;
            float totalWidth = count * _constants.tileSize.x + (count - 1) * _constants.tileSpacing;
            float startX = -totalWidth / 2f + _constants.tileSize.x / 2f;

            for (int i = 0; i < count; i++)
            {
                var slotView = Instantiate(_rackTilePrefab, _rackContainer);
                var rt = slotView.GetComponent<RectTransform>();

                float xPos = startX + i * (_constants.tileSize.x + _constants.tileSpacing);
                rt.anchoredPosition = new Vector2(xPos, 0f);
                rt.sizeDelta = _constants.tileSize;

                var bg = slotView.GetComponent<Image>();
                if (bg != null && _backgroundSprite != null)
                    bg.sprite = _backgroundSprite;

                slotView.gameObject.name = $"RackSlot_{i}";
                _slotViews.Add(slotView);
            }
        }

        /// <summary>
        /// A tile shifted within the rack (during AddTile's ShiftSlots).
        /// Updates the icon sprite at the destination slot to reflect the shifted tile.
        /// </summary>
        private void OnTileShift(int slotIndex, TileData tile)
        {
            if (tile == null || slotIndex < 0 || slotIndex >= _slotViews.Count) return;

            var slotView = _slotViews[slotIndex];
            var sprite = tile.iconId < _iconSprites.Length ? _iconSprites[tile.iconId] : null;

            var iconChild = slotView.transform.Find("Icon");
            if (iconChild != null)
            {
                iconChild.GetComponent<Image>().sprite = sprite;
            }
            else
            {
                CreateIconChild(slotView, sprite);
            }
        }

        /// <summary>
        /// A new tile was placed in the rack (after any shifts completed).
        /// Creates or updates the icon at the insertion slot.
        /// Also marks the tile as no longer moving so future taps work.
        /// </summary>
        private void OnTileAdded(int slotIndex, TileData tile)
        {
            var slotView = _slotViews[slotIndex];
            var sprite = tile.iconId < _iconSprites.Length ? _iconSprites[tile.iconId] : null;

            var iconChild = slotView.transform.Find("Icon");
            if (iconChild == null)
            {
                CreateIconChild(slotView, sprite);
            }
            else
            {
                iconChild.GetComponent<Image>().sprite = sprite;
            }

            tile.isMoving = false;
        }

        /// <summary>
        /// Match-3 detected. Plays a scale-up+fade-out animation on the matched slot views,
        /// then calls RefreshRackVisuals() to reconcile all slots with the compacted data.
        ///
        /// IMPORTANT: This runs async (over multiple frames). By the time the animation
        /// finishes, CompactRack() has already rearranged the data. RefreshRackVisuals()
        /// handles the reconciliation — it reads the current rack state, not the stale indices.
        /// </summary>
        private async void OnMatchCleared(int firstIndex, int lastIndex, int iconId)
        {
            // Collect affected slot views and add CanvasGroup for alpha control
            var affected = new List<(TileView view, CanvasGroup cg)>();
            for (int i = firstIndex; i <= lastIndex; i++)
            {
                if (_slotViews[i] != null)
                {
                    var cg = _slotViews[i].gameObject.AddComponent<CanvasGroup>();
                    affected.Add((_slotViews[i], cg));
                }
            }

            float duration = _constants.tileMatchClearDuration;
            float elapsed = 0f;

            // Animation loop: scale up + fade out
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                for (int i = 0; i < affected.Count; i++)
                {
                    affected[i].cg.alpha = 1f - t;
                    affected[i].view.transform.localScale =
                        Vector3.one * (1f + t * (_constants.tileMatchScaleUp - 1f));
                }

                await System.Threading.Tasks.Task.Yield();
            }

            // Cleanup: remove CanvasGroups, reset scales
            for (int i = 0; i < affected.Count; i++)
            {
                Destroy(affected[i].cg);
                affected[i].view.transform.localScale = Vector3.one;
            }

            // Reconcile ALL slots with current data (handles compaction)
            RefreshRackVisuals();
        }

        /// <summary>
        /// Full reconciliation pass — reads current RackLogic slot data and updates
        /// or destroys Icon children accordingly. Called after match clears or shifts.
        ///
        /// For each slot:
        ///   Empty + has icon → destroy icon
        ///   Occupied + no icon → create icon with correct sprite
        ///   Occupied + has icon → update sprite if different
        /// </summary>
        public void RefreshRackVisuals()
        {
            for (int i = 0; i < _slotViews.Count && i < _rack.Slots.Count; i++)
            {
                var slot = _rack.Slots[i];
                var iconChild = _slotViews[i].transform.Find("Icon");
                var hasIcon = iconChild != null;

                if (slot.IsEmpty && hasIcon)
                {
                    Destroy(iconChild.gameObject);
                }
                else if (!slot.IsEmpty)
                {
                    var sprite = slot.tile.iconId < _iconSprites.Length
                        ? _iconSprites[slot.tile.iconId] : null;

                    if (!hasIcon)
                    {
                        CreateIconChild(_slotViews[i], sprite);
                    }
                    else
                    {
                        iconChild.GetComponent<Image>().sprite = sprite;
                    }
                }
            }
        }

        /// <summary>
        /// Helper: create an Icon child GameObject with proper RectTransform anchoring
        /// and the given sprite. Icon fills 70% of the slot (anchors at 0.15–0.85).
        /// </summary>
        private void CreateIconChild(TileView slotView, Sprite sprite)
        {
            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(slotView.transform, false);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = sprite;
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Get the anchored position of a rack slot for animation targeting.
        /// Used by GameplayController to compute the destination when moving a tile from board to rack.
        /// </summary>
        public Vector2 GetSlotWorldPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotViews.Count) return Vector2.zero;
            return _slotViews[slotIndex].GetComponent<RectTransform>().anchoredPosition;
        }

        private void OnDestroy()
        {
            if (_rack != null)
            {
                _rack.OnTileAdded -= OnTileAdded;
                _rack.OnMatchCleared -= OnMatchCleared;
                _rack.OnTileShift -= OnTileShift;
            }
        }
    }
}
