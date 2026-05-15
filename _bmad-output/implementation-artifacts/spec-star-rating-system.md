---
title: 'Star Rating System'
type: 'feature'
created: '2026-05-15'
status: 'done'
baseline_commit: 'NO_VCS'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Campaign levels give no performance feedback beyond win/lose. Players have no incentive to replay levels for a better score, and there's no skill ceiling to chase.

**Approach:** Award 1-3 stars on every campaign level clear: bronze for winning, silver for beating the level's time threshold, gold for achieving combo ×3. Save the best star count per level and display stars on the Home screen.

## Boundaries & Constraints

**Always:**
- Star evaluation runs on win only — no stars on loss
- Stars are saved per level, best-ever (never downgrade)
- Time thresholds come from LevelDefinition / LevelConfig, one per level
- Combo ×3 is the gold-star gate (same threshold across all levels)
- Home screen shows filled/empty star icons per level

**Ask First:**
- Time threshold tuning after playtesting
- Visual style of star icons (use existing UI sprites if available, otherwise generated)

**Never:**
- No star scoring in endless mode (endless mode doesn't exist yet)
- No mid-level star preview — stars revealed only on the win popup
- No negative carryover — losing a level with better time but not winning doesn't count

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| First clear, great time | Win level 1 in 30s (threshold 45s), combo ×1 | 2 stars (silver). Saved. | N/A |
| First clear, max performance | Win level 1 in 30s (threshold 45s), combo ×3+ | 3 stars (gold). Saved. "NEW BEST!" shown. | N/A |
| Replay with worse performance | Win level 1 in 60s, combo ×1 | 1 star earned this run, but saved best remains 3 | No save write for stars (no regression) |
| Replay with better performance | Previously 1 star, now win in 30s with combo ×2 | 2 stars earned, saved best updates from 1→2 | N/A |
| Win without combo system | Win level, combo never reached ×3 | Max 2 stars (silver) | N/A |
| Corrupt save data | SaveService fails to load | Stars treated as 0 for all levels, display empty | Graceful degradation (existing behavior) |

</frozen-after-approval>

## Code Map

- `Assets/Scripts/Core/LevelGenerator.cs` — LevelDefinition struct gains `silverTimeThreshold` with per-level values from GDD
- `Assets/Scripts/Config/LevelConfig.cs` — ScriptableObject gains `silverTimeThreshold` field
- `Assets/Scripts/Core/GameState.cs` — Gains `silverTimeThreshold`, `starsEarned`, `CalculateStars()` method
- `Assets/Scripts/Core/LevelManager.cs` — Pass `silverTimeThreshold` through to GameState constructor
- `Assets/Scripts/Services/SaveService.cs` — LevelScore gains `bestStars`, RecordLevelScore updated
- `Assets/Scripts/Gameplay/GameplayController.cs` — OnWon calculates stars, records score, runs star reveal animation
- `Assets/Scripts/UI/HomeScreen.cs` — Loads and displays star icons per level from save data

## Tasks & Acceptance

**Execution:**
- [x] `Assets/Scripts/Core/LevelGenerator.cs` — Add `silverTimeThreshold` field to `LevelDefinition` struct, populate with GDD-defined thresholds per level (L1:45s, L2:50s, L3:55s, L4:60s, L5:55s, L6:65s, L7:65s, L8:70s, L9:60s, L10:75s) — Star evaluation input
- [x] `Assets/Scripts/Config/LevelConfig.cs` — Add `silverTimeThreshold` field to ScriptableObject — Supports hand-authored configs
- [x] `Assets/Scripts/Core/GameState.cs` — Add `silverTimeThreshold` parameter to constructor, `starsEarned` field, `CalculateStars()` method: bronze if win, silver if time <= threshold, gold if silver + maxComboAchieved >= 3 — Core star logic
- [x] `Assets/Scripts/Core/LevelManager.cs` — Pass `silverTimeThreshold` from LevelConfig/LevelDefinition into GameState constructor — Plumbing
- [x] `Assets/Scripts/Services/SaveService.cs` — Add `bestStars` field to `LevelScore`, update `RecordLevelScore` signature to accept `stars` parameter, write only if new stars > bestStars — Persistence
- [x] `Assets/Scripts/Gameplay/GameplayController.cs` — OnWon: call CalculateStars, record score via SaveService, run async star reveal animation (0.5s delay, then sequential star fills with scale bounce), show "NEW BEST!" if improved — Win flow
- [x] `Assets/Scripts/UI/HomeScreen.cs` — After building level grid, load LevelScore for each level from SaveService, instantiate 3 star icons per button, fill based on bestStars — Display

**Acceptance Criteria:**
- Given a first-time level 1 clear in 30s with combo ×1, when the win popup appears, then 2 stars are revealed and saved
- Given a level 1 clear with combo ×3+ and time under 45s, when the win popup appears, then 3 stars are revealed with "NEW BEST!" text
- Given a player returns to Home after earning stars, when Home loads, then star icons reflect saved best stars per level
- Given a replay with worse performance, when the win popup appears, then fewer stars animate but saved best is unchanged

## Spec Change Log

## Design Notes

**Silver time threshold flow:** LevelDefinition (procedural) and LevelConfig (authored) both carry `silverTimeThreshold`. LevelManager passes it to GameState constructor. GameState stores it and uses it in `CalculateStars()`. This avoids coupling GameState to LevelConfig/LevelDefinition types.

**Star reveal animation pattern:** Uses existing async style (Task.Yield based, consistent with combo text animation). Sequential delay + scale bounce for each star. Total reveal time ~1.7s for 3 stars.

**Home screen star layout:** Three small star icons below each level button's number text. Uses Resources.Load for star sprite (filled/empty). If sprites not found, falls back to colored text "★" / "☆".

## Verification

**Commands:**
- Unity Editor: Open project, verify no console errors on play

**Manual checks:**
- Play level 1, win with good time + combo, verify stars appear on win popup
- Return to Home, verify stars display under level 1 button
- Replay level 1 with worse performance, verify saved stars don't regress

### Review Findings

- [x] [Review][Decision] D1: "NEW BEST!" shown on first-time bronze (1-star) clear — resolved: show for every improvement (stars > previousBest)
- [x] [Review][Patch] P1: SaveService created on every OnWon (redundant disk I/O) [GameplayController.cs:L257-L258]
- [x] [Review][Patch] P2: No cancellation mechanism for star reveal animation on scene transition [GameplayController.cs:L280-L321]
- [x] [Review][Patch] P3: `maxComboAchieved` never synced during gameplay, only at win — should move `SyncComboAchieved()` into `CalculateStars()` [GameplayController.cs:L253]

## Suggested Review Order

**Star calculation — core logic**

- Entry point: star evaluation logic — self-syncs combo before scoring, always correct
  [GameState.cs:103](../../Tile Adventure/Assets/Scripts/Core/GameState.cs#L103)

- Win orchestration: calculate stars, save via cached SaveService, kick off reveal with cancellation token
  [GameplayController.cs:227](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L227)

**Star reveal animation**

- Sequential star fill with scale bounce, "NEW BEST!" pulse, token-based cancellation on scene exit
  [GameplayController.cs:248](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L248)

**Time thresholds — data flow**

- LevelDefinition gains silverTimeThreshold, per-level values from GDD
  [LevelGenerator.cs:135](../../Tile Adventure/Assets/Scripts/Core/LevelGenerator.cs#L135)

- GetLevelDefinition populates all 10 levels with time thresholds
  [LevelGenerator.cs:51](../../Tile Adventure/Assets/Scripts/Core/LevelGenerator.cs#L51)

- ScriptableObject field for hand-authored level configs
  [LevelConfig.cs:38](../../Tile Adventure/Assets/Scripts/Config/LevelConfig.cs#L38)

- Plumbing threshold from both config paths into GameState constructor
  [LevelManager.cs:42](../../Tile Adventure/Assets/Scripts/Core/LevelManager.cs#L42)

**Persistence**

- LevelScore gains bestStars, RecordLevelScore writes only on improvement (never downgrade)
  [SaveService.cs:27](../../Tile Adventure/Assets/Scripts/Services/SaveService.cs#L27)

- GetBestStars helper for HomeScreen display
  [SaveService.cs:92](../../Tile Adventure/Assets/Scripts/Services/SaveService.cs#L92)

**Home screen display**

- Three star text objects per level button, filled from save data
  [HomeScreen.cs:81](../../Tile Adventure/Assets/Scripts/UI/HomeScreen.cs#L81)

**Cancellation + caching infrastructure**

- Token guard in RevealStars every loop iteration, bumped on Restart/GoHome/NextLevel/OnDestroy
  [GameplayController.cs:50](../../Tile Adventure/Assets/Scripts/Gameplay/GameplayController.cs#L50)
