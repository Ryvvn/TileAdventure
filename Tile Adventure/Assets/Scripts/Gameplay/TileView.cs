using System;
using TileAdventure.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TileAdventure.Gameplay
{
    public class TileView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _iconImage;

        public TileData Data { get; private set; }
        public event Action<TileView> OnTileTapped;

        private RectTransform _rectTransform;
        private float _lastTapTime;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(TileData data, Sprite iconSprite, Sprite backgroundSprite, Color blockedColor, Color exposedColor)
        {
            Data = data;
            _iconImage.sprite = iconSprite;
            _background.sprite = backgroundSprite;
            _background.color = data.isExposed ? exposedColor : blockedColor;
            _iconImage.color = data.isExposed ? exposedColor : blockedColor;

            data.OnExposureChanged += OnExposureChanged;
        }

        private void OnExposureChanged(TileData data)
        {
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (Data == null || Data.isRemoved) return;
            bool exposed = Data.isExposed;
            _background.color = exposed ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
            _iconImage.color = exposed ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Data == null || Data.isRemoved || Data.isMoving)
                return;

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

        public void MarkRemoved()
        {
            Data.isRemoved = true;
        }

        private void OnDestroy()
        {
            if (Data != null)
                Data.OnExposureChanged -= OnExposureChanged;
        }
    }
}
