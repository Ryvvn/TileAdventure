using System;
using TileAdventure.Config;
using TileAdventure.Core;
using TileAdventure.Services;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.Gameplay
{
    public class GameplayController : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private RackView _rackView;
        [SerializeField] private GameConstants _constants;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _progressText;
        [SerializeField] private GameObject _winPopup;
        [SerializeField] private GameObject _losePopup;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton;

        private LevelManager _levelManager;
        private Sprite[] _iconSprites;
        private Sprite _tileBackground;
        private AudioManager _audio;

        private void Start()
        {
            var levelNumber = PlayerPrefs.GetInt("SelectedLevel", 1);
            var config = Resources.Load<LevelConfig>($"Config/Levels/Level_{levelNumber:D2}");
            Initialize(levelNumber, config);
        }

        public async void Initialize(int levelNumber, LevelConfig config = null)
        {
            _audio = AudioManager.Instance;
            _levelManager = new LevelManager(_constants);
            LoadSprites();

            if (config != null)
                _levelManager.LoadLevel(config);
            else
            {
                var def = LevelGenerator.GetLevelDefinition(levelNumber);
                _levelManager.LoadLevelProcedural(levelNumber, def.targetTriples, def.layerCount, def.activeIconCount, def.rackSlotCount);
            }

            _levelManager.OnLevelWon += OnWon;
            _levelManager.OnLevelLost += OnLost;
            _levelManager.Rack.OnMatchCleared += OnMatchCleared;
            _levelManager.Rack.OnRackOverflow += OnRackOverflow;

            _boardView.Initialize(_levelManager.Board, _iconSprites, _tileBackground);
            _rackView.Initialize(_levelManager.Rack, _iconSprites, _tileBackground);

            _boardView.OnTileTapped += OnTileTapped;
            _boardView.BuildBoard();

            UpdateUI();

            _restartButton.onClick.AddListener(Restart);
            _homeButton.onClick.AddListener(GoHome);

            _winPopup.SetActive(false);
            _losePopup.SetActive(false);

            if (_audio != null)
                _audio.PlayMusic();
        }

        private void LoadSprites()
        {
            _iconSprites = new Sprite[_constants.totalTileIcons];
            for (int i = 0; i < _constants.totalTileIcons; i++)
            {
                _iconSprites[i] = Resources.Load<Sprite>($"Images/Tiles/{i + 1}");
            }
            _tileBackground = Resources.Load<Sprite>("Images/UI/tile-base");
        }

        private async void OnTileTapped(TileView tileView)
        {
            if (_levelManager.State.phase != GamePhase.Playing)
                return;

            var tile = tileView.Data;
            if (tile.isRemoved || tile.isMoving)
                return;

            if (_levelManager.Rack.WouldOverflowWithNext())
            {
                _levelManager.State.MarkLost();
                return;
            }

            _audio?.PlayTap();

            var rackTarget = _rackView.GetSlotWorldPosition(_levelManager.Rack.GetOccupiedCount());
            await _boardView.AnimateMoveToRack(tileView, rackTarget, () =>
            {
                _levelManager.Rack.AddTile(tile);
                _levelManager.Board.RemoveTile(tile);
            });

            UpdateUI();
        }

        private void OnMatchCleared(int first, int last, int iconId)
        {
            _audio?.PlayMatch();
            _levelManager.State.RecordTripleCleared();

            _boardView.RefreshAllTiles();
            UpdateUI();
        }

        private void OnRackOverflow()
        {
            UpdateUI();
        }

        private void OnWon()
        {
            var saveService = new SaveService();
            saveService.UnlockLevel(_levelManager.State.currentLevel + 1);

            _winPopup.SetActive(true);
        }

        private void OnLost()
        {
            _losePopup.SetActive(true);
        }

        private void UpdateUI()
        {
            if (_levelText != null)
                _levelText.text = $"Level {_levelManager.State.currentLevel}";

            if (_progressText != null)
                _progressText.text = $"{_levelManager.State.triplesCleared} / {_levelManager.State.targetTriples}";
        }

        private async void Restart()
        {
            _winPopup.SetActive(false);
            _losePopup.SetActive(false);
            _levelManager.Dispose();

            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_constants.gameplaySceneName);
        }

        private async void GoHome()
        {
            _levelManager.Dispose();
            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_constants.homeSceneName);
        }

        private void Update()
        {
            _levelManager?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _levelManager?.Dispose();
        }
    }
}
