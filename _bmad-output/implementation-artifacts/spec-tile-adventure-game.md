---
title: 'Tile Trip Match — Full Game Implementation'
type: 'feature'
created: '2026-05-12'
baseline_commit: 'NO_VCS'
status: 'in-review'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A Unity 6.0 greenfield project with raw assets (14 tile icons, UI images, audio) but zero code, scenes, or project structure. The Unity Developer Test document specifies building a complete tile-matching puzzle game with 3 scenes, 10 levels, layered board mechanics, rack-based matching, and persistent progress.

**Approach:** Build the full game as a spec-driven Unity project with clean MVC separation: plain C# data/logic classes, ScriptableObject configs, MonoBehaviour views/controllers. Set up Unity project, implement 3-scene flow (Loading → Home → Gameplay), core tile-board-rack mechanics, 10 levels with JSON persistence, and responsive layout. Use async/await (UniTask), uGUI, and ScriptableObject-based level data.

## Boundaries & Constraints

**Always:**
- Unity 6.0 (6000.x), C# 9, URP 2D
- uGUI for UI (Canvas + Canvas Scaler for responsive)
- UniTask for async (Loading scene, animations)
- ScriptableObjects for level config, game constants, tile definitions — no magic numbers
- MVC separation: plain C# classes for data/logic (no MonoBehaviour), MonoBehaviours for view/controller only
- JSON file for level progress persistence
- 10 playable levels with increasing difficulty
- 14 tile icons, rack size 7, match-3 clearing
- Exposed = not overlapped by higher layer; blocked tiles appear dimmer
- Tweened tile movement; match-clear animation
- Background music looping + tap/match SFX
- Portrait (9:16) and landscape (16:9) support

**Ask First:**
- Level layout design approach (procedural generation vs hand-authored ScriptableObjects)
- Difficulty curve specifics per level (exact triple targets, layer counts, icon variety per level)

**Never:**
- No coroutines — use async/await only
- No MonoBehaviour for pure data/logic
- No hardcoded magic numbers in gameplay code
- No 3D — strictly 2D

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| First launch | No save file exists | Show Home with level 1 unlocked, others locked | N/A |
| Level completion | All target triples cleared | Win animation, unlock next level, save progress | N/A |
| Rack overflow | Rack full (7/7 slots), no 3-match possible | Lose screen with retry button | N/A |
| Tap blocked tile | Tile overlapped by higher-layer tile | No action, visual feedback (shake/dim pulse) | N/A |
| Rapid double-tap | Same tile tapped twice in <200ms | Ignore second tap | Unity EventSystem handles |
| Resume with progress | Save file exists with level 5 unlocked | Levels 1-5 unlocked, level 6+ locked | If save corrupt, treat as first launch |
| Match-3 at full rack | 3 tiles match, rack had 7, now 4 after clear | Clear animation plays, rack compacts, game continues | N/A |
| Asset load failure | Addressable bundle missing or network error | Loading scene shows error + retry button | Catch OperationException, display UI |
| Scene transition | Loading → Home → Gameplay | Async load with fade transition | If scene missing, log error, stay on current scene |

</frozen-after-approval>

## Code Map

- `Assets/Scripts/Core/` — Plain C# data models (TileData, LevelData, RackSlot, GameState) and logic services (BoardLogic, RackLogic, MatchLogic, LevelManager)
- `Assets/Scripts/Config/` — ScriptableObjects (TileIconSet, LevelConfig, GameConstants, AudioConfig)
- `Assets/Scripts/Gameplay/` — MonoBehaviour views/controllers (BoardView, TileView, RackView, GameplayController)
- `Assets/Scripts/UI/` — UI controllers (HomeScreen, LevelSelectButton, LoadingScreen, WinLosePopup)
- `Assets/Scripts/Audio/` — AudioManager (persistent, plays bg music + SFX via AudioSource pooling)
- `Assets/Scripts/Services/` — SaveService (JSON persistence), SceneLoader (async scene transitions)
- `Assets/Scenes/` — Loading.unity, Home.unity, Gameplay.unity
- `Assets/Resources/Config/` — Runtime-loadable ScriptableObject configs
- `Assets/AddressableAssetsData/` — Addressables group configuration for tile sprites, UI images, audio

## Tasks & Acceptance

**Execution:**
- [ ] `ProjectSettings/` — Create Unity 6.0 URP 2D project, configure manifest (UniTask, Addressables), set up folder structure — Foundation for all work
- [ ] `Assets/Scripts/Config/GameConstants.cs` — ScriptableObject with all magic numbers (rackSize=7, matchCount=3, tileTypes=14, animationDuration, etc.) — Single source of truth
- [ ] `Assets/Scripts/Config/LevelConfig.cs` — ScriptableObject per level (tile positions, layers, icon distribution, triple target, rack size) — Level data definition
- [ ] `Assets/Scripts/Core/TileData.cs` — Plain C# model (iconId, gridPosition, layer, isExposed, isInRack) — Tile data without MonoBehaviour
- [ ] `Assets/Scripts/Core/BoardLogic.cs` — Plain C# class: generate board from LevelConfig, track layers, determine exposure — Board state management
- [ ] `Assets/Scripts/Core/RackLogic.cs` — Plain C# class: add tile, group by icon, detect match-3, remove matched, check overflow — Rack state and matching
- [ ] `Assets/Scripts/Core/GameState.cs` — Plain C# model (currentLevel, triplesCleared, targetTriples, isWin, isLose) — Game state tracking
- [ ] `Assets/Scripts/Core/LevelManager.cs` — Plain C# class: load level config, initialize board, coordinate win/lose — Level lifecycle
- [ ] `Assets/Scripts/Gameplay/TileView.cs` — MonoBehaviour: render tile sprite on tile-base background, handle tap, dim when blocked, animate movement — Tile visual + interaction
- [ ] `Assets/Scripts/Gameplay/BoardView.cs` — MonoBehaviour: instantiate TileViews, position on grid with layer offset, update exposure visuals — Board rendering
- [ ] `Assets/Scripts/Gameplay/RackView.cs` — MonoBehaviour: display rack slots, animate tile insertion, play match-clear effect — Rack rendering
- [ ] `Assets/Scripts/Gameplay/GameplayController.cs` — MonoBehaviour: wire BoardLogic+RackLogic+GameState, handle input, coordinate win/lose transitions — Gameplay orchestrator
- [ ] `Assets/Scripts/Services/SaveService.cs` — JSON save/load for level progress, unlocked levels, high scores — Persistent progress
- [ ] `Assets/Scripts/Services/SceneLoader.cs` — Async scene loading with fade canvas (Loading → Home → Gameplay) — Scene transitions
- [ ] `Assets/Scripts/Audio/AudioManager.cs` — Persistent singleton, play bg_music loop, tap/match SFX via AudioSource pool — Audio
- [ ] `Assets/Scripts/UI/HomeScreen.cs` — Play button, level grid (1-10 with lock/unlock state), logo — Home scene UI
- [ ] `Assets/Scripts/UI/LoadingScreen.cs` — Addressables preload, progress bar, transition to Home — Loading scene
- [ ] `Assets/Scenes/Loading.unity` — Setup LoadingScreen, Addressables reference, AudioManager persistent object — Loading scene
- [ ] `Assets/Scenes/Home.unity` — Setup HomeScreen with Canvas Scaler (9:16/16:9), background, logo — Home scene
- [ ] `Assets/Scenes/Gameplay.unity` — Setup GameplayController, BoardView, RackView, Canvas Scaler — Gameplay scene
- [ ] `Assets/Scripts/Config/Levels/Level_01.asset` through `Level_10.asset` — 10 ScriptableObject level configs with progressive difficulty — Level content
- [ ] `DESIGN.md` — Architecture decisions, level data structure, solvability strategy, trade-offs — Documentation deliverable

**Acceptance Criteria:**
- Given no save file, when game launches, then Loading→Home transition shows, level 1 unlocked, levels 2-10 locked
- Given a tap on an exposed tile, when tile moves to rack, then tile animates to rack slot and groups next to matching icon
- Given 3 matching tiles adjacent in rack, when the third is placed, then clear animation plays and tiles are removed
- Given all target triples cleared, when last match resolves, then win screen appears and next level unlocks
- Given rack has 7 tiles and no match possible on next tap, when tile is added, then lose screen appears with retry option
- Given the app is closed and reopened, when Home loads, then previously unlocked levels remain accessible
- Given both portrait and landscape orientation, when device rotates, then UI and board layout adapt without clipping

## Spec Change Log

## Design Notes

**Architecture — MVC with Plain C# Core:**
All game logic lives in `Assets/Scripts/Core/` as plain C# classes (no MonoBehaviour). This enforces the test's requirement and makes logic unit-testable without Unity. Views/Controllers in `Gameplay/` and `UI/` are MonoBehaviours that observe the core state and drive rendering.

**Level Solvability Strategy:**
Generate levels by creating matched triples first, then assigning positions across layers. Every level starts from a solvable seed — N triples × 3 tiles = 3N tiles total, placed across layers ensuring no triple is fully blocked. Difficulty increases by: more triples required, more layers, more icon types active, fewer rack slots.

**Async Pattern:**
Use UniTask (de facto Unity standard for async). Loading scene uses `Addressables.LoadAssetsAsync` with UniTask integration. Scene transitions use `SceneManager.LoadSceneAsync` wrapped in UniTask. Tile animations use `DOTween` (works with UniTask via `.ToUniTask()`).

**Responsive Layout:**
Canvas Scaler set to `Scale With Screen Size`, reference 1080×1920, match width or height 0.5. All UI anchored to safe-area margins. Board scales to fit available space.

## Verification

**Commands:**
- Unity Editor: Open project, verify no console errors on play
- Unity Editor: Play through all 10 levels, verify win/lose flows
- Unity Editor: Test portrait and landscape in Game view

**Manual checks (no CLI):**
- Loading scene transitions smoothly to Home within 5 seconds
- All 14 tile icons render correctly with tile-base background
- Blocked tiles appear visually darker than exposed tiles
- Rack grouping places same-icon tiles adjacent
- Match clear animation is visible and tiles are removed
- Level progress persists across editor play/stop cycles
