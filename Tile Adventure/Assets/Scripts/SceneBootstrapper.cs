using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure
{
    /// <summary>
    /// Minimal per-scene setup component. Attached to the Canvas in each scene
    /// by the SceneGenerator Editor script. Handles camera background color and
    /// CanvasScaler configuration.
    ///
    /// The bool flags (_isLoadingScene, etc.) are set in the Inspector by the
    /// generator — only one should be true per scene.
    ///
    /// NOTE: This class is a quick bootstrap. Game logic starts elsewhere:
    ///   Loading: LoadingScreen.Start()
    ///   Home:    HomeScreen.Start()
    ///   Gameplay: GameplayController.Start()
    /// </summary>
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
            SetupCamera(Color.black);
            SetupCanvasScaler("GameplayCanvas");
        }

        private void SetupHomeScene()
        {
            SetupCamera(new Color(0.1f, 0.15f, 0.3f));
            SetupCanvasScaler("HomeCanvas");
        }

        private void SetupLoadingScene()
        {
            SetupCamera(Color.black);
            SetupCanvasScaler("LoadingCanvas");
        }

        private void SetupCamera(Color backgroundColor)
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = backgroundColor;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        /// <summary>
        /// Configure the CanvasScaler for responsive layout.
        /// Scale With Screen Size + 1080×1920 reference + match 0.5
        /// ensures consistent UI across portrait (9:16) and landscape (16:9).
        /// </summary>
        private void SetupCanvasScaler(string canvasName)
        {
            var existing = GameObject.Find(canvasName);
            Canvas canvas;
            if (existing != null)
            {
                canvas = existing.GetComponent<Canvas>();
            }
            else
            {
                var go = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            if (canvas == null) return;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
