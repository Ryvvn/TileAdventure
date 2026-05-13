using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure
{
    public class SceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool _isLoadingScene;
        [SerializeField] private bool _isHomeScene;
        [SerializeField] private bool _isGameplayScene;

        private void Awake()
        {
            if (_isGameplayScene)
            {
                SetupGameplayScene();
            }
            else if (_isHomeScene)
            {
                SetupHomeScene();
            }
            else if (_isLoadingScene)
            {
                SetupLoadingScene();
            }
        }

        private void SetupGameplayScene()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = Color.black;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }

            var canvas = FindOrCreateCanvas("GameplayCanvas");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void SetupHomeScene()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = new Color(0.1f, 0.15f, 0.3f);
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }

            var canvas = FindOrCreateCanvas("HomeCanvas");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void SetupLoadingScene()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = Color.black;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }

            var canvas = FindOrCreateCanvas("LoadingCanvas");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private Canvas FindOrCreateCanvas(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                var cv = existing.GetComponent<Canvas>();
                if (cv != null) return cv;
            }

            var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }
    }
}
