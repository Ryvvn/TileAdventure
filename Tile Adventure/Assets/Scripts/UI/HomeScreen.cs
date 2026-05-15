using System.Collections.Generic;
using TileAdventure.Services;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.UI
{
    /// <summary>
    /// Home screen controller. Displays the game logo, a "Play" button,
    /// and a 5×2 grid of level buttons (1–10).
    ///
    /// Level buttons:
    ///   - Green (btn_green.png) + interactable = unlocked
    ///   - Orange (btn_orange.png) + non-interactable = locked
    ///
    /// Tapping an unlocked level writes the selection to PlayerPrefs then
    /// loads the Gameplay scene. Tapping "Play" loads level 1 directly.
    ///
    /// Level unlock state comes from SaveService (JSON persistence).
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        [SerializeField] private Image _logoImage;
        [SerializeField] private Button _playButton;
        [SerializeField] private Transform _levelGridContainer;
        [SerializeField] private GameObject _levelButtonPrefab;
        [SerializeField] private string _gameplaySceneName = "Gameplay";

        private SaveService _saveService;
        private List<Button> _levelButtons;

        private void Start()
        {
            _saveService = new SaveService();
            _levelButtons = new List<Button>();

            BuildLevelGrid();
            BuildEndlessButton();
            _playButton.onClick.AddListener(OnPlayClicked);
        }

        /// <summary>
        /// Instantiate 10 level buttons in the grid container.
        /// Button state (unlocked/locked) is determined by save data.
        /// Stars are displayed below each unlocked level button.
        /// </summary>
        private void BuildLevelGrid()
        {
            var highestUnlocked = _saveService.GetHighestUnlockedLevel();

            for (int i = 1; i <= 10; i++)
            {
                var buttonGo = Instantiate(_levelButtonPrefab, _levelGridContainer);
                var button = buttonGo.GetComponent<Button>();
                var label = buttonGo.GetComponentInChildren<Text>();
                var isUnlocked = i <= highestUnlocked;

                if (label != null)
                    label.text = i.ToString();

                button.interactable = isUnlocked;

                Sprite buttonSprite = null;
                if (isUnlocked)
                    buttonSprite = Resources.Load<Sprite>("Images/UI/btn_green");
                else
                    buttonSprite = Resources.Load<Sprite>("Images/UI/btn_orange");

                if (buttonSprite != null && button.targetGraphic is Image img)
                    img.sprite = buttonSprite;

                AddStarsToButton(buttonGo, i);

                int levelNum = i;
                button.onClick.AddListener(async () => await OnLevelSelected(levelNum));

                _levelButtons.Add(button);
            }
        }

        private void AddStarsToButton(GameObject buttonGo, int levelNumber)
        {
            var bestStars = _saveService.GetBestStars(levelNumber);

            for (int s = 0; s < 3; s++)
            {
                var starGo = new GameObject($"Star_{s}", typeof(Text));
                starGo.transform.SetParent(buttonGo.transform, false);

                var text = starGo.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 16;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;

                var filled = s < bestStars;
                text.text = filled ? "★" : "☆";
                text.color = filled
                    ? s switch
                    {
                        0 => new Color(0.8f, 0.5f, 0.2f),
                        1 => new Color(0.75f, 0.75f, 0.75f),
                        _ => Color.yellow
                    }
                    : new Color(0.4f, 0.4f, 0.4f, 1f);

                var rt = starGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2((s - 1) * 20f, 6f);
                rt.sizeDelta = new Vector2(18f, 18f);
            }
        }

        /// <summary> "Play" button shortcut — loads level 1. </summary>
        private async void OnPlayClicked()
        {
            await OnLevelSelected(1);
        }

        /// <summary> Build the Endless Mode button below the level grid with best score display. </summary>
        private void BuildEndlessButton()
        {
            var bgGo = new GameObject("EndlessButtonBg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_levelGridContainer, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0f);
            bgRt.anchorMax = new Vector2(0.5f, 0f);
            bgRt.anchoredPosition = new Vector2(0f, 55f);
            bgRt.sizeDelta = new Vector2(160f, 50f);
            var bgImg = bgGo.GetComponent<Image>();
            var bgSprite = Resources.Load<Sprite>("Images/UI/btn_green");
            if (bgSprite != null) bgImg.sprite = bgSprite;

            var button = bgGo.AddComponent<Button>();
            button.onClick.AddListener(async () => await OnEndlessClicked());

            var labelGo = new GameObject("Label", typeof(Text));
            labelGo.transform.SetParent(bgGo.transform, false);
            var labelText = labelGo.GetComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 22;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = "Endless";
            labelText.color = Color.white;
            labelText.fontStyle = FontStyle.Bold;
            labelText.raycastTarget = false;
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(160f, 50f);

            var best = _saveService.GetBestEndlessScore();
            var scoreGo = new GameObject("BestScore", typeof(Text));
            scoreGo.transform.SetParent(_levelGridContainer, false);
            var scoreText = scoreGo.GetComponent<Text>();
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 16;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.text = best > 0 ? $"Best: {best}" : "No score yet";
            scoreText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            scoreText.raycastTarget = false;
            var srt = scoreGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0f);
            srt.anchorMax = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 20f);
            srt.sizeDelta = new Vector2(160f, 30f);
        }

        private async System.Threading.Tasks.Task OnEndlessClicked()
        {
            PlayerPrefs.SetInt("SelectedMode", 1);
            PlayerPrefs.Save();

            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_gameplaySceneName);
        }

        /// <summary>
        /// Validate unlock state, persist selection to PlayerPrefs, load Gameplay scene.
        /// </summary>
        private async System.Threading.Tasks.Task OnLevelSelected(int levelNumber)
        {
            if (!_saveService.IsLevelUnlocked(levelNumber))
                return;

            // Pass selected level to GameplayController via PlayerPrefs
            PlayerPrefs.SetInt("SelectedMode", 0);
            PlayerPrefs.SetInt("SelectedLevel", levelNumber);
            PlayerPrefs.Save();

            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_gameplaySceneName);
        }
    }
}
