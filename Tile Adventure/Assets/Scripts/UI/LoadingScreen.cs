using TileAdventure.Services;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.UI
{
    /// <summary>
    /// Loading screen that preloads all required assets before transitioning to Home.
    ///
    /// Asset loading order:
    ///   1. UI sprites (background, buttons, logo, tile-base, hand, failed)
    ///   2. Tile icons (14 sprites, 1.png through 14.png)
    ///   3. Audio clips (bg_music, tap, match)
    ///
    /// Each asset is checked for null after loading. If any fails,
    /// an error panel with retry button is shown.
    ///
    /// After all assets are loaded, the scene transitions to Home.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Text _progressText;
        [SerializeField] private Text _statusText;
        [SerializeField] private GameObject _errorPanel;
        [SerializeField] private Button _retryButton;
        [SerializeField] private string _homeSceneName = "Home";

        private async void Start()
        {
            _retryButton?.onClick.AddListener(OnRetryClicked);

            await LoadAssetsAndTransition();
        }

        private void OnRetryClicked()
        {
            _errorPanel?.SetActive(false);
            _ = LoadAssetsAndTransition();
        }

        private async System.Threading.Tasks.Task LoadAssetsAndTransition()
        {
            UpdateProgress(0f, "Loading assets...");

            // --- Phase 1: UI sprites (7 files) ---
            var sprites = new string[]
            {
                "Images/UI/background", "Images/UI/tile-base", "Images/UI/game_logo",
                "Images/UI/btn_green", "Images/UI/btn_orange", "Images/UI/failed",
                "Images/UI/hand"
            };

            var totalSteps = sprites.Length + 14 + 3;

            for (int i = 0; i < sprites.Length; i++)
            {
                var req = Resources.LoadAsync<Sprite>(sprites[i]);
                while (!req.isDone)
                    await System.Threading.Tasks.Task.Yield();
                if (req.asset == null)
                {
                    ShowError($"Failed to load: {sprites[i]}");
                    return;
                }
                UpdateProgress((float)(i + 1) / totalSteps, $"Loading {sprites[i]}...");
            }

            // --- Phase 2: Tile icons (14 files, 1-indexed) ---
            for (int i = 1; i <= 14; i++)
            {
                var req = Resources.LoadAsync<Sprite>($"Images/Tiles/{i}");
                while (!req.isDone)
                    await System.Threading.Tasks.Task.Yield();
                if (req.asset == null)
                {
                    ShowError($"Failed to load tile icon: {i}");
                    return;
                }
                UpdateProgress((float)(sprites.Length + i) / totalSteps, $"Loading tile {i}...");
            }

            // --- Phase 3: Audio clips (3 files) ---
            var audioClips = new string[] { "bg_music", "tap", "match" };
            for (int i = 0; i < audioClips.Length; i++)
            {
                var req = Resources.LoadAsync<AudioClip>(audioClips[i]);
                while (!req.isDone)
                    await System.Threading.Tasks.Task.Yield();
                if (req.asset == null)
                {
                    ShowError($"Failed to load audio: {audioClips[i]}");
                    return;
                }
                UpdateProgress((float)(sprites.Length + 14 + i + 1) / totalSteps, $"Loading audio...");
            }

            UpdateProgress(1f, "Done!");
            await System.Threading.Tasks.Task.Delay(500);

            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_homeSceneName);
        }

        /// <summary> Display an error message and show the retry panel. </summary>
        private void ShowError(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
            if (_errorPanel != null)
                _errorPanel.SetActive(true);
        }

        /// <summary> Update progress bar and text labels. </summary>
        private void UpdateProgress(float progress, string status)
        {
            if (_progressBar != null)
                _progressBar.value = progress;
            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            if (_statusText != null)
                _statusText.text = status;
        }
    }
}
