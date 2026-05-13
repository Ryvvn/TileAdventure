using System.Collections.Generic;
using TileAdventure.Services;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.UI
{
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
                    buttonSprite = Resources.Load<Sprite>("UI/btn_green");
                else
                    buttonSprite = Resources.Load<Sprite>("UI/btn_orange");

                if (buttonSprite != null && button.targetGraphic is Image img)
                    img.sprite = buttonSprite;

                int levelNum = i;
                button.onClick.AddListener(() => OnLevelSelected(levelNum));

                _levelButtons.Add(button);
            }
        }

        private async void OnPlayClicked()
        {
            await OnLevelSelected(1);
        }

        private async System.Threading.Tasks.Task OnLevelSelected(int levelNumber)
        {
            if (!_saveService.IsLevelUnlocked(levelNumber))
                return;

            PlayerPrefs.SetInt("SelectedLevel", levelNumber);
            PlayerPrefs.Save();

            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_gameplaySceneName);
        }
    }
}
