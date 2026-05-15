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

        /// <summary>
        /// Validate unlock state, persist selection to PlayerPrefs, load Gameplay scene.
        /// </summary>
        private async System.Threading.Tasks.Task OnLevelSelected(int levelNumber)
        {
            if (!_saveService.IsLevelUnlocked(levelNumber))
                return;

            // Pass selected level to GameplayController via PlayerPrefs
            PlayerPrefs.SetInt("SelectedLevel", levelNumber);
            PlayerPrefs.Save();

            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_gameplaySceneName);
        }
    }
}
