---
title: 'Tile Trip Match — Full Game Implementation'
type: 'feature'
created: '2026-05-12'
baseline_commit: 'NO_VCS'
status: 'done'
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
- [x] `ProjectSettings/` — Create Unity 6.0 URP 2D project, configure manifest (UniTask, Addressables), set up folder structure — Foundation for all work
- [x] `Assets/Scripts/Config/GameConstants.cs` — ScriptableObject with all magic numbers (rackSize=7, matchCount=3, tileTypes=14, animationDuration, etc.) — Single source of truth
- [x] `Assets/Scripts/Config/LevelConfig.cs` — ScriptableObject per level (tile positions, layers, icon distribution, triple target, rack size) — Level data definition
- [x] `Assets/Scripts/Core/TileData.cs` — Plain C# model (iconId, gridPosition, layer, isExposed, isInRack) — Tile data without MonoBehaviour
- [x] `Assets/Scripts/Core/BoardLogic.cs` — Plain C# class: generate board from LevelConfig, track layers, determine exposure — Board state management
- [x] `Assets/Scripts/Core/RackLogic.cs` — Plain C# class: add tile, group by icon, detect match-3, remove matched, check overflow — Rack state and matching
- [x] `Assets/Scripts/Core/GameState.cs` — Plain C# model (currentLevel, triplesCleared, targetTriples, isWin, isLose) — Game state tracking
- [x] `Assets/Scripts/Core/LevelManager.cs` — Plain C# class: load level config, initialize board, coordinate win/lose — Level lifecycle
- [x] `Assets/Scripts/Gameplay/TileView.cs` — MonoBehaviour: render tile sprite on tile-base background, handle tap, dim when blocked, animate movement — Tile visual + interaction
- [x] `Assets/Scripts/Gameplay/BoardView.cs` — MonoBehaviour: instantiate TileViews, position on grid with layer offset, update exposure visuals — Board rendering
- [x] `Assets/Scripts/Gameplay/RackView.cs` — MonoBehaviour: display rack slots, animate tile insertion, play match-clear effect — Rack rendering
- [x] `Assets/Scripts/Gameplay/GameplayController.cs` — MonoBehaviour: wire BoardLogic+RackLogic+GameState, handle input, coordinate win/lose transitions — Gameplay orchestrator
- [x] `Assets/Scripts/Services/SaveService.cs` — JSON save/load for level progress, unlocked levels, high scores — Persistent progress
- [x] `Assets/Scripts/Services/SceneLoader.cs` — Async scene loading with fade canvas (Loading → Home → Gameplay) — Scene transitions
- [x] `Assets/Scripts/Audio/AudioManager.cs` — Persistent singleton, play bg_music loop, tap/match SFX via AudioSource pool — Audio
- [x] `Assets/Scripts/UI/HomeScreen.cs` — Play button, level grid (1-10 with lock/unlock state), logo — Home scene UI
- [x] `Assets/Scripts/UI/LoadingScreen.cs` — Addressables preload, progress bar, transition to Home — Loading scene
- [x] `Assets/Scenes/Loading.unity` — Setup LoadingScreen, Addressables reference, AudioManager persistent object — Loading scene
- [x] `Assets/Scenes/Home.unity` — Setup HomeScreen with Canvas Scaler (9:16/16:9), background, logo — Home scene
- [x] `Assets/Scenes/Gameplay.unity` — Setup GameplayController, BoardView, RackView, Canvas Scaler — Gameplay scene
- [x] `Assets/Scripts/Config/Levels/Level_01.asset` through `Level_10.asset` — 10 ScriptableObject level configs with progressive difficulty — Level content
- [x] `DESIGN.md` — Architecture decisions, level data structure, solvability strategy, trade-offs — Documentation deliverable

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

## Spec Change Log

_No spec-level changes during review. Two patches auto-applied: buttons hidden/shown with popups, Build Settings auto-registration._

## Suggested Review Order

**Entry Point — Gameplay Orchestrator**

- Wire all core logic to views, tap→rack→match flow, win/lose handling
  [GameplayController.cs:49](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L49)

**Core Logic — Rack insert + sort + match**

- Insert algorithm with grouping/shifting, match-3 detection, overflow prediction
  [RackLogic.cs:46](../../Tile Adventure/Assets/Scripts/Core/RackLogic.cs#L46)

- Insert-index finder: same-icon grouping, sort-order placement
  [RackLogic.cs:104](../../Tile Adventure/Assets/Scripts/Core/RackLogic.cs#L104)

- Match run-length scanner with recursive chain-reaction check
  [RackLogic.cs:233](../../Tile Adventure/Assets/Scripts/Core/RackLogic.cs#L233)

**Core Logic — Board exposure + layer overlap**

- Exposure: tile blocked if any higher-layer tile overlaps it
  [BoardLogic.cs:143](../../Tile Adventure/Assets/Scripts/Core/BoardLogic.cs#L143)

- Overlap test using actual tile dimensions (fixed from hardcoded bug)
  [TileData.cs:75](../../Tile Adventure/Assets/Scripts/Core/TileData.cs#L75)

- Grid-to-world position mapping with layer diagonal offset
  [BoardLogic.cs:125](../../Tile Adventure/Assets/Scripts/Core/BoardLogic.cs#L125)

**Core Logic — State + Level management**

- Game state machine: Playing→Won/Lost, triple counting
  [GameState.cs:22](../../Tile Adventure/Assets/Scripts/Core/GameState.cs#L22)

- Level manager: wires Board+Rack+State, event relay, dispose
  [LevelManager.cs:38](../../Tile Adventure/Assets/Scripts/Core/LevelManager.cs#L38)

**Views — Board rendering + animation**

- Sorted layer-first instantiation + bounding-box centering
  [BoardView.cs:50](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L50)

- Smoothstep animation from board to rack slot
  [BoardView.cs:119](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L119)

**Views — Rack rendering + shift/match animation**

- Slot build, tile shift/added icon updates, match-clear scale+fade
  [RackView.cs:48](../../Tile Adventure/Assets/Scripts/Gameplay/RackView.cs#L48)

**Views — Tile click handling + blocked feedback**

- Tap debounce, blocked-tile shake, exposure visual update
  [TileView.cs:80](../../Tile Adventure/Assets/Scripts/Gameplay/TileView.cs#L80)

**Config — Constants + Level definitions**

- Single source of truth, all magic numbers centralized
  [GameConstants.cs:11](../../Tile Adventure/Assets/Scripts/Config/GameConstants.cs#L11)

- 10-level difficulty curve with progressive params
  [LevelGenerator.cs:49](../../Tile Adventure/Assets/Scripts/Core/LevelGenerator.cs#L49)

**Services + UI**

- JSON save/load with corrupt-file graceful degradation
  [SaveService.cs:35](../../Tile Adventure/Assets/Scripts/Services/SaveService.cs#L35)

- Async scene loading with activation gate at 0.9
  [SceneLoader.cs:20](../../Tile Adventure/Assets/Scripts/Services/SceneLoader.cs#L20)

- Preloader with error+retry, 3-phase asset pipeline
  [LoadingScreen.cs:23](../../Tile Adventure/Assets/Scripts/UI/LoadingScreen.cs#L23)

- Level select grid with lock/unlock state from save
  [HomeScreen.cs:45](../../Tile Adventure/Assets/Scripts/UI/HomeScreen.cs#L45)

**Editor + Project Setup**

- Scene auto-generator with Build Settings registration
  [SceneGenerator.cs:15](../../Tile Adventure/Assets/Scripts/Editor/SceneGenerator.cs#L15)

## Spec Change Log

### 2026-05-14 — Pyramid Cascading Tile Layout

**Intent:** Replace the flat grid layout with a pyramid/cascading layout where tiles overlap so the tile below is still partially visible. Classic tile-matching feel.

**Changes:**

| File | Change |
|------|--------|
| [GameConstants.cs](../../Tile Adventure/Assets/Scripts/Config/GameConstants.cs#L35-L47) | Added four pyramid layout fields: `gridCellWidth` (48), `gridCellHeight` (40), `pyramidStaggerOffset` (24), `layerVerticalOffset` (28). |
| [BoardLogic.cs — GridToWorld](../../Tile Adventure/Assets/Scripts/Core/BoardLogic.cs#L133-L140) | Rewrote position math. Tight cell spacing (48×40 vs old 90×90) causes tiles to overlap ~40%. Odd-row stagger via `pyramidStaggerOffset`. Higher layers shift up via `layerVerticalOffset` so lower tiles peek through. |
| [BoardLogic.cs — GenerateSolvableLayout](../../Tile Adventure/Assets/Scripts/Core/BoardLogic.cs#L94-L95) | Replaced hardcoded `rng.Next(6)` grid with dynamic `gridSize = Ceil(Sqrt(tilesPerLayer * 1.5))`. Tighter cells need a larger grid to avoid all occupied cells. |
| [LevelGenerator.cs — GenerateSolvableLayout](../../Tile Adventure/Assets/Scripts/Core/LevelGenerator.cs#L93) | Same grid-size fix: `* 1.5f` multiplier matches BoardLogic's private version. |

**Design rationale:** `gridCellHeight = 40` against `tileSize.y = 80` means exactly 50% of a lower tile is hidden by the tile above it — the bottom half gets covered, the top half (with the icon) stays visible. Layer-0 tiles at 100% scale, layer-4 tiles at ~88% scale via `layerScaleFalloff` — sells a subtle depth illusion without affecting readability.

---

### 2026-05-14 — Game-Feel Visual Polish

**Intent:** Five low-cost polish features to make the game feel alive and satisfying.

**Changes:**

| # | Feature | Files | Description |
|---|---------|-------|-------------|
| 1 | Hover/touch glow | [TileView.cs](../../Tile Adventure/Assets/Scripts/Gameplay/TileView.cs#L17) + [L109-L123](../../Tile Adventure/Assets/Scripts/Gameplay/TileView.cs#L109-L123) | Added `IPointerEnterHandler` + `IPointerExitHandler`. Exposed tiles pulse to 108% scale on hover, snap back to base scale on exit. Blocked/removed tiles ignore hover. |
| 2 | Board entrance cascade | [BoardView.cs — BuildBoard](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L53-L115) + [AnimateCascadeIn](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L117-L132) | `BuildBoard` is now async. Tiles spawn at scale=0, alpha=0, grouped by layer. Each layer fades in with smoothstep, staggered 40ms (layer 0 → layer 1 → …). |
| 3 | Match-clear particles | [BoardView.cs — SpawnMatchParticles](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L243-L271) | Spawns 6 small colored squares (hue derived from iconId) at board center. They fly outward with randomized velocity + deceleration, fading over 0.35s. Called from `GameplayController.OnMatchCleared`. |
| 4 | Rack snap bounce | [BoardView.cs — AnimateMoveToRack](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L213-L228) | After the tile reaches its rack slot, it overshoots 4px downward then smoothstep-settles back over 0.08s. Physical "click" satisfaction. |
| 5 | Layer scale variance | [BoardView.cs — BuildBoard](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L91-L93) | Layer 0 = 100% scale, each higher layer = `1f - layer * layerScaleFalloff` (3% per layer). Cascades correctly through entrance animation. `TileView.SetBaseScale()` anchors the hover-glow math. |

**Tuning (all in GameConstants "Visual Polish" section):** `hoverGlowScale`, `boardCascadeDelayPerLayer`, `boardCascadeDuration`, `rackSnapOvershoot`, `rackSnapBounceDuration`, `layerScaleFalloff`, `matchParticleCount`, `matchParticleSpeed`, `matchParticleDuration`, `matchParticleSize`.

---

### 2026-05-14 — Rack Animation Fixes

**Intent:** Fix coordinate-space bugs and animation ordering in the tap-to-rack pipeline.

**Bug 1 — Coordinate space mismatch (tile flies nowhere):**

| File | Change |
|------|--------|
| [RackView.cs — GetSlotWorldPosition](../../Tile Adventure/Assets/Scripts/Gameplay/RackView.cs#L268) | Changed `.anchoredPosition` → `.position` — return world space, not rack-local. |
| [BoardView.cs — AnimateMoveToRack](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L199) | Added `_boardContainer.InverseTransformPoint(rackTargetWorld)` to convert world → board-local before interpolating `rt.anchoredPosition`. |

**Root cause:** A TileView child of `_boardContainer` uses `anchoredPosition` (board-local coordinates). Mixing that with `_rackContainer`-local coordinates produces garbage positions. World-space as common reference fixes it.

**Bug 2 — Shift-after-fly + wrong target slot:**

`OnTileTapped` was calling `Fly to GetOccupiedCount()` (leftmost-empty slot) then `AddTile` (which triggers shift-to-sorted-position). This meant the shift happened after the tile already landed, and the flight targeted the wrong slot.

**Fix — new animation sequence:**

| Step | Animation | Data |
|------|-----------|------|
| 1 | Compute `insertIndex = GetInsertIndex(iconId)` | [RackLogic.cs — GetInsertIndex](../../Tile Adventure/Assets/Scripts/Core/RackLogic.cs#L94-L97) (new public wrapper) |
| 2 | `await rackView.AnimateShiftForInsert(insertIndex, occupiedCount)` | [RackView.cs — AnimateShiftForInsert](../../Tile Adventure/Assets/Scripts/Gameplay/RackView.cs#L231-L269) — reparents icons to `_rackContainer`, slides them right via smoothstep, reparents into next slot |
| 3 | `await boardView.AnimateMoveToRack(tileView, rackTarget)` where `rackTarget = slot[insertIndex]` | [GameplayController.cs — OnTileTapped](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L133-L164) (restructured) |
| 4 | `AddTile + RemoveTile` commit | Same as before |

### Review Findings

- [x] [Review][Patch] `AnimateShiftForInsert` — `_slotViews[i+1]` out of bounds when rack is full + match pending [RackView.cs — AnimateShiftForInsert]
- [x] [Review][Patch] `TileView.OnPointerEnter` hardcodes `1.08f` instead of using `_constants.hoverGlowScale` [TileView.cs — OnPointerEnter]
- [x] [Review][Patch] `AnimateParticle` — framerate-dependent `speed *= 0.96f` per frame (missing `Time.deltaTime` normalization) [BoardView.cs — AnimateParticle]
- [x] [Review][Patch] Race condition: `ClearBoard()` destroys objects while `async void` animations (AnimateCascadeIn, AnimateParticle, AnimateTileRemoval) still run [BoardView.cs — BuildBoard/ClearBoard]
- [x] [Review][Patch] Dead code: `slotWidth` computed but never used in `AnimateShiftForInsert` [RackView.cs — AnimateShiftForInsert]
- [x] [Review][Patch] Magic number `0.07f` in particle hue calculation — violates "no magic numbers" constraint [BoardView.cs — SpawnMatchParticles]
- [x] [Review][Defer] `BuildBoard` cascade iterates all layers 0..maxLayers, causing unnecessary delays on empty layers — deferred, minor polish optimization [BoardView.cs — BuildBoard]
- [x] [Review][Defer] `OnPointerClick` hardcoded `0.3f` debounce (pre-existing, not in this diff) — deferred, pre-existing [TileView.cs — OnPointerClick]
