using System.Collections.Generic;
using TileAdventure.Config;
using TileAdventure.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.Gameplay
{
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

            BuildSlots();
        }

        private void BuildSlots()
        {
            for (int i = 0; i < _rack.Slots.Count; i++)
            {
                var slotView = Instantiate(_rackTilePrefab, _rackContainer);
                var rt = slotView.GetComponent<RectTransform>();

                float xPos = i * (_constants.tileSize.x + _constants.tileSpacing);
                rt.anchoredPosition = new Vector2(xPos, 0f);
                rt.sizeDelta = _constants.tileSize;

                var bg = slotView.GetComponent<Image>();
                if (bg != null && _backgroundSprite != null)
                    bg.sprite = _backgroundSprite;

                slotView.gameObject.name = $"RackSlot_{i}";
                _slotViews.Add(slotView);
            }
        }

        private void OnTileAdded(int slotIndex, TileData tile)
        {
            var slotView = _slotViews[slotIndex];
            var sprite = tile.iconId < _iconSprites.Length ? _iconSprites[tile.iconId] : null;

            var iconChild = slotView.transform.Find("Icon");
            if (iconChild == null)
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
            else
            {
                iconChild.GetComponent<Image>().sprite = sprite;
            }

            tile.isMoving = false;
        }

        private async void OnMatchCleared(int firstIndex, int lastIndex, int iconId)
        {
            var toClear = new List<TileView>();
            var toClearCanvasGroups = new List<CanvasGroup>();
            for (int i = firstIndex; i <= lastIndex; i++)
            {
                if (_slotViews[i] != null)
                {
                    toClear.Add(_slotViews[i]);
                    var cg = _slotViews[i].gameObject.AddComponent<CanvasGroup>();
                    toClearCanvasGroups.Add(cg);
                }
            }

            float duration = _constants.tileMatchClearDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                for (int i = 0; i < toClear.Count; i++)
                {
                    toClearCanvasGroups[i].alpha = 1f - t;
                    toClear[i].transform.localScale = Vector3.one * (1f + t * (_constants.tileMatchScaleUp - 1f));
                }

                await System.Threading.Tasks.Task.Yield();
            }

            for (int i = 0; i < toClear.Count; i++)
            {
                var iconChild = toClear[i].transform.Find("Icon");
                if (iconChild != null)
                    Destroy(iconChild.gameObject);
                Destroy(toClearCanvasGroups[i]);
                toClear[i].transform.localScale = Vector3.one;
            }

            RefreshRackVisuals();
        }

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
                    if (!hasIcon)
                    {
                        var iconGo = new GameObject("Icon", typeof(Image));
                        iconGo.transform.SetParent(_slotViews[i].transform, false);
                        var iconImg = iconGo.GetComponent<Image>();
                        var sprite = slot.tile.iconId < _iconSprites.Length ? _iconSprites[slot.tile.iconId] : null;
                        iconImg.sprite = sprite;
                        var iconRt = iconGo.GetComponent<RectTransform>();
                        iconRt.anchorMin = new Vector2(0.15f, 0.15f);
                        iconRt.anchorMax = new Vector2(0.85f, 0.85f);
                        iconRt.offsetMin = Vector2.zero;
                        iconRt.offsetMax = Vector2.zero;
                    }
                    else
                    {
                        var sprite = slot.tile.iconId < _iconSprites.Length ? _iconSprites[slot.tile.iconId] : null;
                        iconChild.GetComponent<Image>().sprite = sprite;
                    }
                }
            }
        }

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
            }
        }
    }
}
