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
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private RectTransform _comboTextContainer;

        // Core layer (plain C#)
        private LevelManager _levelManager;

        // Asset cache (loaded once at scene start)
        private Sprite[] _iconSprites;
        private Sprite _tileBackground;
        private AudioManager _audio;

        private Camera _camera;
        private Vector3 _cameraRestPosition;
        private int _shakeToken;

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
            _camera = Camera.main;
            _cameraRestPosition = _camera != null ? _camera.transform.position : Vector3.zero;
            _levelManager?.Dispose();
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

            // Wire combo events
            _levelManager.State.Combo.OnComboIncreased += OnComboIncreased;
            _levelManager.State.Combo.OnComboBroken += OnComboBroken;

            // Wire views to core data
            _boardView.Initialize(_levelManager.Board, _iconSprites, _tileBackground);
            _rackView.Initialize(_levelManager.Rack, _iconSprites, _tileBackground);

            _boardView.OnTileTapped += OnTileTapped;
            await _boardView.BuildBoard();

            UpdateUI();

            // Button wiring (buttons persist across the scene lifespan)
            _restartButton.onClick.AddListener(Restart);
            _homeButton.onClick.AddListener(GoHome);
            _restartButton.gameObject.SetActive(false);
            _homeButton.gameObject.SetActive(false);
            _nextLevelButton.gameObject.SetActive(false);
            _nextLevelButton.onClick.AddListener(NextLevel);

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
        ///   3. Compute insert index via rack sorting (icon grouping)
        ///   4. Animate rack tiles shifting to make room at the insert slot
        ///   5. Play tap SFX
        ///   6. Animate tile from board to the correct insert slot
        ///   7. On complete: AddTile + RemoveTile (data commit)
        /// </summary>
        private async void OnTileTapped(TileView tileView)
        {
            if (_levelManager.State.phase != GamePhase.Playing)
                return;

            var tile = tileView.Data;
            if (tile.isRemoved || tile.isMoving)
                return;

            tile.isMoving = true;

            if (_levelManager.Rack.WouldOverflowWithNext())
            {
                _levelManager.State.MarkLost();
                return;
            }

            _audio?.PlayTap();

            int insertIndex = _levelManager.Rack.GetInsertIndex(tile.iconId);
            int occupiedCount = _levelManager.Rack.GetOccupiedCount();

            await _rackView.AnimateShiftForInsert(insertIndex, occupiedCount);

            var rackTarget = _rackView.GetSlotWorldPosition(insertIndex);
            await _boardView.AnimateMoveToRack(tileView, rackTarget, () =>
            {
                _levelManager.Rack.AddTile(tile);
                _levelManager.Board.RemoveTile(tile);
            });

            UpdateUI();
        }

        /// <summary>
        /// Match-3 was cleared in the rack.
        /// Registers combo, plays match SFX, increments the triple counter,
        /// spawns scaled particles, and refreshes board visuals.
        /// Rack visual refresh is handled by RackView's own OnMatchCleared async handler.
        /// </summary>
        private void OnMatchCleared(int first, int last, int iconId)
        {
            if (_levelManager.State.phase != GamePhase.Playing)
                return;

            _levelManager.State.Combo.RegisterMatch();

            _audio?.PlayMatch();
            _levelManager.State.RecordTripleCleared();

            _boardView.SpawnMatchParticles(iconId, _levelManager.State.Combo.CurrentCombo);
            _boardView.RefreshAllTiles();
            UpdateUI();
        }

        private void OnComboIncreased(int comboLevel)
        {
            SpawnComboText(comboLevel);
            ShakeCamera(comboLevel);
            _rackView.StartBorderPulse(comboLevel);
        }

        private void OnComboBroken()
        {
            _rackView.StopBorderPulse();
        }

        private void OnRackOverflow()
        {
            UpdateUI();
        }

        /// <summary> Win condition reached. Save progress and show popup. </summary>
        private void OnWon()
        {
            _levelManager.State.SyncComboAchieved();

            var saveService = new SaveService();
            saveService.UnlockLevel(_levelManager.State.currentLevel + 1);

            _winPopup.SetActive(true);
            _restartButton.gameObject.SetActive(true);
            _homeButton.gameObject.SetActive(true);
            _nextLevelButton.gameObject.SetActive(true);
        }

        /// <summary> Lose condition triggered (rack overflow). Show popup. </summary>
        private void OnLost()
        {
            _losePopup.SetActive(true);
            _restartButton.gameObject.SetActive(true);
            _homeButton.gameObject.SetActive(true);
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
            _restartButton.gameObject.SetActive(false);
            _homeButton.gameObject.SetActive(false);
            _nextLevelButton.gameObject.SetActive(false);
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

        /// <summary> Go to the next level. </summary>
        private async void NextLevel()
        {
            var nextLevel = _levelManager.State.currentLevel + 1;
            PlayerPrefs.SetInt("SelectedLevel", nextLevel);
            PlayerPrefs.Save();

            _levelManager.Dispose();
            var sceneLoader = new SceneLoader();
            await sceneLoader.LoadSceneAsync(_constants.gameplaySceneName);
        }

        /// <summary> Tick the game timer every frame (paused on win/lose). </summary>
        private void Update()
        {
            _levelManager?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_levelManager != null && _levelManager.State != null && _levelManager.State.Combo != null)
            {
                _levelManager.State.Combo.OnComboIncreased -= OnComboIncreased;
                _levelManager.State.Combo.OnComboBroken -= OnComboBroken;
            }
            _levelManager?.Dispose();
        }

        private void SpawnComboText(int comboLevel)
        {
            var container = _comboTextContainer != null
                ? _comboTextContainer
                : (RectTransform)transform;

            var go = new GameObject("ComboText", typeof(Text));
            go.transform.SetParent(container, false);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36 + comboLevel * 4;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, 100f + comboLevel * 20f);
            rt.sizeDelta = new Vector2(400f, 60f);

            var (label, color) = comboLevel switch
            {
                1 => ("Nice!", Color.white),
                2 => ("COMBO x2!", Color.yellow),
                3 => ("COMBO x3!", new Color(1f, 0.6f, 0f)),
                4 => ("COMBO x4!", Color.red),
                5 => ("MAX COMBO!", Color.magenta),
                _ => ($"COMBO x{comboLevel}!", Color.magenta)
            };

            text.text = label;
            text.color = color;

            AnimateComboText(go);
        }

        private async void AnimateComboText(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            var text = go.GetComponent<Text>();
            var startPos = rt.anchoredPosition;
            var lifetime = _constants.comboTextLifetime;
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                if (go == null) return;
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;
                rt.anchoredPosition = startPos + new Vector2(0f, 40f * t);
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1f - t);
                await System.Threading.Tasks.Task.Yield();
            }

            if (go != null)
                Destroy(go);
        }

        private void ShakeCamera(int comboLevel)
        {
            if (_camera == null || comboLevel <= 1) return;

            _shakeToken++;
            var token = _shakeToken;
            StartCoroutine(ShakeCameraCoroutine(comboLevel, token));
        }

        private System.Collections.IEnumerator ShakeCameraCoroutine(int comboLevel, int token)
        {
            var amplitude = comboLevel * _constants.screenShakeIntensity;
            var duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (token != _shakeToken || _camera == null)
                {
                    if (_camera != null)
                        _camera.transform.position = _cameraRestPosition;
                    yield break;
                }
                elapsed += Time.deltaTime;
                float decay = 1f - elapsed / duration;
                float x = Mathf.Sin(elapsed * 40f) * amplitude * decay;
                float y = Mathf.Cos(elapsed * 50f) * amplitude * decay;
                _camera.transform.position = _cameraRestPosition + new Vector3(x, y, 0f);
                yield return null;
            }

            if (token == _shakeToken && _camera != null)
                _camera.transform.position = _cameraRestPosition;
        }
    }
}
