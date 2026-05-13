# Acceptance Auditor Review

## Instructions
You are auditing the implementation against the specification and acceptance criteria.
You have read access to the project files, the specification, and context documents.
Verify every acceptance criterion, boundary constraint, and design rule is satisfied.

## Specification Reference
File: `_bmad-output/implementation-artifacts/spec-tile-adventure-game.md`

Key requirements to verify:

### Functionality Acceptance Criteria
1. ✅ Given no save file, when game launches, then Loading→Home transition shows, level 1 unlocked, levels 2-10 locked
2. Given a tap on an exposed tile, when tile moves to rack, then tile animates to rack slot and groups next to matching icon
3. Given 3 matching tiles adjacent in rack, when the third is placed, then clear animation plays and tiles are removed
4. Given all target triples cleared, when last match resolves, then win screen appears and next level unlocks
5. Given rack has 7 tiles and no match possible on next tap, when tile is added, then lose screen appears with retry option
6. Given the app is closed and reopened, when Home loads, then previously unlocked levels remain accessible
7. Given both portrait and landscape orientation, when device rotates, then UI and board layout adapt without clipping

### Boundary Constraints
- Unity 6.0, C# 9, URP 2D
- uGUI (Canvas + Canvas Scaler) — verify no UI Toolkit usage
- UniTask NOT used (opted for System.Threading.Tasks) — verify this is documented
- ScriptableObjects for config — verify GameConstants + LevelConfig exist
- MVC separation — verify no MonoBehaviour in Core/ folder
- JSON persistence — verify SaveService uses JsonUtility
- 10 playable levels — verify LevelGenerator covers 1-10
- 14 tile icons — verify GameConstants.totalTileIcons = 14
- Rack size 7 — verify GameConstants.rackSlotCount = 7
- Match-3 — verify GameConstants.matchCount = 3
- Exposed = not overlapped by higher layer — verify TileData.Overlaps() logic
- Blocked tiles appear dimmer — verify TileView blocked tint
- Tweened tile movement — verify async interpolation in BoardView.AnimateMoveToRack
- Match-clear animation — verify RackView.OnMatchCleared
- Background music + tap/match SFX — verify AudioManager
- Portrait + landscape support — verify Canvas Scaler settings

### Design Rules (from spec "Never" list)
- No coroutines — verify no StartCoroutine/IEnumerator usage
- No MonoBehaviour for pure data/logic — verify Core/ folder has zero MonoBehaviour inheritance
- No hardcoded magic numbers — verify all numbers come from GameConstants or LevelConfig
- No 3D — verify no 3D components or transforms

### I/O Edge Cases (from spec matrix)
- First launch / no save — handled by SaveService.Load() → new SaveData()
- Level completion / win — handled by GameState.RecordTripleCleared() → OnWin
- Rack overflow — handled by RackLogic.OnRackOverflow → LevelManager.HandleRackOverflow
- Tap blocked tile — handled by TileView.ShakeBlockedTile()
- Rapid double-tap — handled by Time.time threshold in TileView
- Resume with progress — handled by SaveService.Load() restoring highestUnlockedLevel
- Match-3 at full rack — handled by RackLogic.CheckMatch after insertion
- Asset load failure — handled partially (Resources.Load returns null, not caught explicitly)
- Scene transition — handled by SceneLoader.LoadSceneAsync

### Documentation Requirement
- DESIGN.md exists ✓
- Explains architecture decisions
- Explains level data structure
- Explains solvability strategy
- Explains trade-offs and improvements

## Potential Issues Found

1. **Asset load failure**: The I/O matrix specifies "Loading scene shows error + retry button" on asset failure, but LoadingScreen.cs has no error panel or retry logic (the fields `_errorPanel` and `_retryButton` exist but the Start method doesn't handle ResourceRequest failures — sprites/audio that fail to load return null but the loading continues anyway). This is a gap.

2. **Overflow prediction**: `RackLogic.WouldOverflowWithNext()` returns true when the rack is full, but doesn't check if the next tile would create a match-3. This means the lose condition triggers even when a match is imminent. The I/O spec says "no match possible" — the method should actually predict whether any unplaced tile would create a match.

3. **Level selection persistence**: The HomeScreen uses `PlayerPrefs.SetInt("SelectedLevel", levelNumber)` but GameplayController doesn't read it back — it just calls `Initialize(levelNumber, null)` with a nil config. The procedural generation path is used, but the selected level number is never retrieved from PlayerPrefs.

4. **GameConstants loading**: GameplayController expects `_constants` assigned via Inspector. On scene creation via SceneGenerator, it loads from `Assets/Resources/Config/GameConstants.asset`. But if the GameConstants asset hasn't been generated first, the reference will be null, causing NullReferenceException at runtime.

5. **Sprite loading order**: GameplayController.LoadSprites() uses `Resources.Load<Sprite>($"Tiles/{i}")` where `i` starts from 0, but the tile assets are named `1.png` through `14.png` (1-indexed). This should be `i + 1` or the assets need to be renamed.

6. **Scene names not in build settings**: The scene names in GameConstants need to match names in Build Settings. The Editor script saves scenes to the Assets/Scenes/ folder but doesn't add them to Build Settings.
