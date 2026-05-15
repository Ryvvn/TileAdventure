---
title: 'Endless Mode'
type: 'feature'
created: '2026-05-15'
status: 'done'

## Suggested Review Order

**Entry point — mode branching**

- Mode detection reads PlayerPrefs, branches to campaign or endless initialization
  [`GameplayController.cs:64`](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L64)

- Endless initialization creates EndlessLevelManager, wires events, builds views
  [`GameplayController.cs:145`](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L145)

**Core state — EndlessGameState**

- No win condition, score tracking, tier computation from triples cleared
  [`EndlessGameState.cs:1`](../../Tile Adventure/Assets/Scripts/Core/EndlessGameState.cs#L1)

**Core orchestration — EndlessLevelManager**

- Refill logic: counts alive tiles, triggers AddRefillTiles when below threshold
  [`EndlessLevelManager.cs:48`](../../Tile Adventure/Assets/Scripts/Core/EndlessLevelManager.cs#L48)

- Tier ramp: recalculates icons, layers, rack slots from current tier, caps applied
  [`EndlessLevelManager.cs:67`](../../Tile Adventure/Assets/Scripts/Core/EndlessLevelManager.cs#L67)

**Board refill generation**

- AddRefillTiles generates new tiles with triple-first construction, returns new TileData list
  [`BoardLogic.cs:232`](../../Tile Adventure/Assets/Scripts/Core/BoardLogic.cs#L232)

**Rack resize support**

- TryResize shrinks rack slot count when difficulty ramps, refuses if tiles in trimmed slots
  [`RackLogic.cs:388`](../../Tile Adventure/Assets/Scripts/Core/RackLogic.cs#L388)

**Controller — shared event handlers**

- OnTileTapped: concrete branching replaces dynamic dispatch, same flow for both modes
  [`GameplayController.cs:215`](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L215)

- OnMatchCleared: combo + score recording with refill check in endless mode
  [`GameplayController.cs:269`](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L269)

- HUD update: tier + score in endless, level + progress in campaign
  [`GameplayController.cs:469`](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L469)

**Game Over flow**

- OnEndlessLost saves score, shows Game Over popup with "NEW BEST!" if improved
  [`GameplayController.cs:451`](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L451)

**BoardView refill animation**

- AnimateRefillTiles creates TileViews for new refill tiles with cascade animation
  [`BoardView.cs:313`](../../Tile Adventure/Assets/Scripts/Gameplay/BoardView.cs#L313)

**Persistence**

- SaveData gains bestEndlessScore, RecordEndlessScore/GetBestEndlessScore methods
  [`SaveService.cs:144`](../../Tile Adventure/Assets/Scripts/Services/SaveService.cs#L144)

**Config**

- Eight endless mode constants for refill threshold, tier ramp, caps
  [`GameConstants.cs:124`](../../Tile Adventure/Assets/Scripts/Config/GameConstants.cs#L124)

**Home screen entry**

- Endless button with best score display, writes SelectedMode=1 to PlayerPrefs
  [`HomeScreen.cs:126`](../../Tile Adventure/Assets/Scripts/UI/HomeScreen.cs#L126)
baseline_commit: 'a3d18252fd81a7cd87fb77b53afc0e4a7cc39282'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Campaign mode is 10 finite levels. Once beaten, there's no replay value beyond star-chasing. Players have no way to test their endurance or compete for a high score.

**Approach:** Add an Endless Mode — a survival gauntlet with no win condition. The board refills dynamically as tiles are cleared. Difficulty ramps every 3 triples (more icons, more layers, fewer rack slots). The only exit is rack overflow. High score = most triples cleared, saved persistently. Accessible from the Home screen via a dedicated button.

## Boundaries & Constraints

**Always:**
- Endless mode has no win condition — always ends in loss (rack overflow)
- Score = total triples cleared in the run
- Difficulty ramps every 3 triples: +1 active icon, layers scale with tier, rack slots decrease (floor 4)
- Board refills when ≤ 6 tiles remain, generating `(rackSlotCount + 2)` new tiles
- Board refill uses the current tier's difficulty parameters (icon count, layer count)
- Combo system works identically to campaign mode
- Best endless score is saved persistently via SaveService
- Accessible from Home screen via a new "Endless Mode" button
- New tiles cascade in from the top on refill (same cascade animation)

**Ask First:**
- Whether to create a separate "Endless" scene or reuse Gameplay scene with a mode flag
- Exact difficulty ramp values (GDD provides a table — confirm or adjust)
- Visual style of the tier indicator and score display

**Never:**
- No star rating in endless mode
- No level progression or unlock system in endless mode
- No mid-run save/load — if player leaves, the run is lost
- No target triples or win condition
- No board regen from scratch — always refill incremental

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Start endless run | Tap "Endless Mode" on Home | Scene loads with tier-1 params: 5 icons, 2 layers, 7 rack slots | If scene load fails, stay on Home with error log |
| Board refill | Board has ≤ 6 tiles after match clear | Generate `(rackSlots + 2)` new tiles with current tier params, cascade in | If refill creates 0 exposed tiles (shouldn't with triple-first), log warning |
| Difficulty ramp | Player clears 3rd triple (triplesCleared=3) | Tier 2: 6 icons, 2 layers, 7 slots. Board refill now uses tier-2 params | | 
| Max difficulty | Player reaches tier 11+ (30+ triples) | Icons cap at 14, layers cap at 6, rack slots floor at 4 | No crash on overflow |
| Rack overflow (lose) | Rack would overflow on next tap | Mark lost, show "Game Over" popup with final score, save if new best | | 
| New high score | Final score > saved bestEndlessScore | Save new best. Show "NEW BEST!" on Game Over popup | Save failure → log error, don't block game-over flow |
| Replay endless | Tap Restart on Game Over popup | Reload Endless scene fresh (tier 1) or use Restart logic | | 
| Go home mid-run | Tap Home button on Game Over popup | Return to Home, run is lost, score is saved (if new best) | | 
| Very long run | 100+ triples cleared | Board stays at ~24 tiles (refill threshold), performance stable. Score displays 3+ digits | | 
| Combo persists across refill | Combo active when refill triggers | Combo timer keeps ticking during refill animation. If it expires before next match, combo breaks — fair | | 

</frozen-after-approval>

## Code Map

- `Assets/Scripts/Core/GameState.cs` — Existing campaign state. Endless mode creates EndlessGameState instead (no win, score-based)
- `Assets/Scripts/Core/LevelManager.cs` — Existing campaign orchestrator. EndlessLevelManager extends pattern with refill + tier logic
- `Assets/Scripts/Core/BoardLogic.cs` — Board data model. Already supports GenerateBoard — will also support AddRefillTiles() 
- `Assets/Scripts/Core/RackLogic.cs` — Rack data model. Reused as-is for endless mode
- `Assets/Scripts/Core/ComboSystem.cs` — Combo tracking. Reused as-is
- `Assets/Scripts/Services/SaveService.cs` — Persistence. SaveData gains bestEndlessScore, SaveService gains RecordEndlessScore/GetBestEndlessScore
- `Assets/Scripts/Config/GameConstants.cs` — Constants. Gains endless mode params: refillThreshold, triplesPerTier, minRackSlots, maxIcons, maxLayers
- `Assets/Scripts/Gameplay/GameplayController.cs` — Existing campaign controller. Gains mode detection (PlayerPrefs "GameMode") and endless-specific UI: score text, tier indicator, game-over popup
- `Assets/Scripts/UI/HomeScreen.cs` — Home screen. Gains "Endless Mode" button that writes GameMode.Endless to PlayerPrefs and loads Gameplay scene

## Tasks & Acceptance

**Execution:**
- [x] `Assets/Scripts/Config/GameConstants.cs` — Add endless mode constants: `endlessRefillThreshold` (default 6), `endlessTriplesPerTier` (default 3), `endlessMinRackSlots` (default 4), `endlessMaxIcons` (default 14), `endlessMaxLayers` (default 6), `endlessStartIcons` (default 5), `endlessStartLayers` (default 2), `endlessStartRackSlots` (default 7) — Config source of truth
- [x] `Assets/Scripts/Core/EndlessGameState.cs` — New class: tracks triplesCleared (score), currentTier, phase (Playing/Lost). No win condition. Tier computed from triplesCleared. Fires OnScoreChanged, OnTierChanged, OnLose — Core state
- [x] `Assets/Scripts/Core/EndlessLevelManager.cs` — New class: orchestrates BoardLogic, RackLogic, EndlessGameState, ComboSystem. Handles board refill (CheckRefill after each match), tier ramp (reconfigure rack/board params on tier change). Calls BoardLogic.AddRefillTiles for refill — Orchestration
- [x] `Assets/Scripts/Core/BoardLogic.cs` — Add `AddRefillTiles(int tileCount, int activeIcons, int layerCount)` method: generates new tiles with triple-first construction, appends to _allTiles, recalculates exposure. Returns list of new TileData for view cascade — Refill generation
- [x] `Assets/Scripts/Services/SaveService.cs` — Add `bestEndlessScore` to SaveData. Add `RecordEndlessScore(int triples)` and `GetBestEndlessScore()` methods — Persistence
- [x] `Assets/Scripts/Gameplay/GameplayController.cs` — Add mode detection in Start(): read PlayerPrefs "GameMode" (Campaign/Endless). In endless mode: create EndlessLevelManager instead of LevelManager, wire endless-specific events, show score UI + tier indicator instead of progress UI, show "Game Over" popup on lose instead of "Level Failed". Handle refill view cascade — Controller
- [x] `Assets/Scripts/UI/HomeScreen.cs` — Add "Endless Mode" button. On tap: write PlayerPrefs.SetInt("SelectedMode", 1), load Gameplay scene. Display best endless score below button — Entry point

**Acceptance Criteria:**
- Given the player taps "Endless Mode" on Home, when the scene loads, then a board with 5 icons, 2 layers, 7 rack slots appears and the HUD shows score (not progress)
- Given the player clears 3 triples, when the next match resolves, then the tier indicator shows "Tier 2" and difficulty parameters increase
- Given the board has 6 or fewer tiles after a match clear, when CheckRefill runs, then new tiles cascade in with triple-first generation using current tier params
- Given the rack overflows, when Game Over triggers, then the final score is displayed and saved (if new best)
- Given the player returns to Home after an endless run, when Home loads, then the best score is shown below the Endless Mode button

## Spec Change Log

## Design Notes

**Why reuse Gameplay scene with mode flag instead of separate scene:** Creating a separate scene means duplicating the entire GameObject hierarchy (BoardView, RackView, Canvas, etc.) which doubles maintenance burden. A mode flag in GameplayController lets us share the view prefabs and scene structure while branching logic where needed. The HomeScreen writes `PlayerPrefs.SetInt("GameMode", (int)GameMode.Endless)` before loading.

**EndlessGameState tier calculation:** `currentTier = 1 + (triplesCleared / triplesPerTier)`. Tier 1 = 0-2 triples, tier 2 = 3-5, etc. Icons = min(startIcons + tier - 1, maxIcons). Layers = startLayers + floor((tier-1) / 2). Rack slots = max(startRackSlots - floor((tier-1) / 3), minRackSlots).

**Board refill:** `AddRefillTiles` generates `(rackSlotCount + 2)` new tiles using the same triple-first algorithm as `GenerateBoard`, but places them starting from the highest existing layer + 1 (so new tiles cascade on top). This ensures new tiles are visible and tappable after the cascade.

**Difficulty ramp triggers on refill:** When `triplesCleared` crosses a tier boundary, the next `CheckRefill` call uses the new tier's params. Existing tiles on the board don't change — only newly generated tiles use the updated difficulty.

## Verification

**Commands:**
- Unity Editor: Open project, verify no console errors on play. Check that Endless Mode button appears on Home screen.

**Manual checks:**
- Tap "Endless Mode" → verify board loads with tier-1 params, score display says "0"
- Clear 3 triples → verify tier changes, new tiles use harder params
- Play until rack overflow → verify Game Over popup shows score, "NEW BEST!" if applicable
- Return to Home → verify best score displayed below Endless Mode button
- Restart from Game Over → verify fresh tier-1 run starts
