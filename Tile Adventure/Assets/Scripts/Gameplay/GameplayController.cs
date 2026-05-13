using System;
using TileAdventure.Config;
using TileAdventure.Core;
using TileAdventure.Services;
using UnityEngine;
using UnityEngine.UI;

namespace TileAdventure.Gameplay
{
    /// <summary>
    /// Main gameplay orchestrator — the MonoBehaviour bridge between core logic and views.
    ///
    /// Lifecycle:
    ///   1. Start() reads PlayerPrefs for the selected level number
    ///   2. Loads a LevelConfig asset (falls back to procedural generation)
    ///   3. Creates LevelManager (BoardLogic + RackLogic + GameState)
    ///   4. Wires up BoardView, RackView, and event handlers
    ///   5. Update() ticks the game timer
    ///
    /// Events:
    ///   OnTileTapped → Animate tile to rack → AddTile + RemoveTile
    ///   OnMatchCleared → Play SFX → Update UI → Refresh board
    ///   OnWon/OnLost → Show popup → Save progress
    /// </summary>
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

        // Core layer (plain C#)
        private LevelManager _levelManager;

        // Asset cache (loaded once at scene start)
        private Sprite[] _iconSprites;
        private Sprite _tileBackground;
        private AudioManager _audio;

        /// <summary>
        /// Entry point. Reads the level selected on the Home screen,
        /// loads its config, and kicks off Initialize().
        /// </summary>
        private void Start()
        {
            var levelNumber = PlayerPrefs.GetInt("SelectedLevel", 1);
            var config = Resources.Load<LevelConfig>($"Config/Levels/Level_{levelNumber:D2}");
            Initialize(levelNumber, config);
        }

        /// <summary>
        /// Set up the entire gameplay session for one level.
        /// Can also be called externally with a specific LevelConfig
        /// (e.g., from a debugging or replay system).
        /// </summary>
        /// <param name="levelNumber">Fallback level number if config is null.</param>
        /// <param name="config">Pre-authored level config, or null for procedural.</param>
        public async void Initialize(int levelNumber, LevelConfig config = null)
        {
            _audio = AudioManager.Instance;
            _levelManager = new LevelManager(_constants);
            LoadSprites();

            // Load level data: use config if provided, otherwise generate procedurally
            if (config != null)
            {
                _levelManager.LoadLevel(config);
            }
            else
            {
                var def = LevelGenerator.GetLevelDefinition(levelNumber);
                _levelManager.LoadLevelProcedural(levelNumber,
                    def.targetTriples, def.layerCount, def.activeIconCount, def.rackSlotCount);
            }

            // Wire core → controller events
            _levelManager.OnLevelWon += OnWon;
            _levelManager.OnLevelLost += OnLost;
            _levelManager.Rack.OnMatchCleared += OnMatchCleared;
            _levelManager.Rack.OnRackOverflow += OnRackOverflow;

            // Wire views to core data
            _boardView.Initialize(_levelManager.Board, _iconSprites, _tileBackground);
            _rackView.Initialize(_levelManager.Rack, _iconSprites, _tileBackground);

            _boardView.OnTileTapped += OnTileTapped;
            _boardView.BuildBoard();

            UpdateUI();

            // Button wiring (buttons persist across the scene lifespan)
            _restartButton.onClick.AddListener(Restart);
            _homeButton.onClick.AddListener(GoHome);

            // Popups start hidden
            _winPopup.SetActive(false);
            _losePopup.SetActive(false);

            _audio?.PlayMusic();
        }

        /// <summary>
        /// Load all tile icon sprites and the tile-base background from Resources.
        /// Sprites are 1-indexed (1.png through 14.png) → array index 0-13.
        /// Returns null entries if assets are missing — tiles render blank.
        /// </summary>
        private void LoadSprites()
        {
            _iconSprites = new Sprite[_constants.totalTileIcons];
            for (int i = 0; i < _constants.totalTileIcons; i++)
            {
                _iconSprites[i] = Resources.Load<Sprite>($"Images/Tiles/{i + 1}");
            }
            _tileBackground = Resources.Load<Sprite>("Images/UI/tile-base");
        }

        /// <summary>
        /// Player tapped an exposed tile on the board.
        /// Flow:
        ///   1. Guard: ignore if game is over, tile is removed, or tile is mid-animation
        ///   2. Check overflow: if rack is full with no pending match, trigger lose
        ///   3. Play tap SFX
        ///   4. Animate tile from board to rack → on complete: AddTile + RemoveTile
        /// </summary>
        private async void OnTileTapped(TileView tileView)
        {
            if (_levelManager.State.phase != GamePhase.Playing)
                return;

            var tile = tileView.Data;
            if (tile.isRemoved || tile.isMoving)
                return;

            // If rack is full and no match is possible, immediate loss
            if (_levelManager.Rack.WouldOverflowWithNext())
            {
                _levelManager.State.MarkLost();
                return;
            }

            _audio?.PlayTap();

            // Target is the current last occupied slot + 1 (leftmost empty)
            var rackTarget = _rackView.GetSlotWorldPosition(_levelManager.Rack.GetOccupiedCount());
            await _boardView.AnimateMoveToRack(tileView, rackTarget, () =>
            {
                _levelManager.Rack.AddTile(tile);
                _levelManager.Board.RemoveTile(tile);
            });

            UpdateUI();
        }

        /// <summary>
        /// Match-3 was cleared in the rack.
        /// Plays match SFX, increments the triple counter, and refreshes board visuals.
        /// Rack visual refresh is handled by RackView's own OnMatchCleared async handler.
        /// </summary>
        private void OnMatchCleared(int first, int last, int iconId)
        {
            _audio?.PlayMatch();
            _levelManager.State.RecordTripleCleared();

            // Board tiles may become exposed after covering tiles are removed
            _boardView.RefreshAllTiles();
            UpdateUI();
        }

        private void OnRackOverflow()
        {
            UpdateUI();
        }

        /// <summary> Win condition reached. Save progress and show popup. </summary>
        private void OnWon()
        {
            var saveService = new SaveService();
            saveService.UnlockLevel(_levelManager.State.currentLevel + 1);

            _winPopup.SetActive(true);
        }

        /// <summary> Lose condition triggered (rack overflow). Show popup. </summary>
        private void OnLost()
        {
            _losePopup.SetActive(true);
        }

        /// <summary> Update HUD: level number and triple progress. </summary>
        private void UpdateUI()
        {
            if (_levelText != null)
                _levelText.text = $"Level {_levelManager.State.currentLevel}";

            if (_progressText != null)
                _progressText.text =
                    $"{_levelManager.State.triplesCleared} / {_levelManager.State.targetTriples}";
        }

        /// <summary> Reload the gameplay scene (full reset). </summary>
        private async void Restart()
        {
            _winPopup.SetActive(false);
            _losePopup.SetActive(false);
            _levelManager.Dispose();

            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_constants.gameplaySceneName);
        }

        /// <summary> Return to Home scene. Disposes LevelManager to prevent leaks. </summary>
        private async void GoHome()
        {
            _levelManager.Dispose();
            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_constants.homeSceneName);
        }

        /// <summary> Tick the game timer every frame (paused on win/lose). </summary>
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
