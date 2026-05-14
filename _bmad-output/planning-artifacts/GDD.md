# TileAdventure — Game Design Document: v2.0 Feature Pack

**Status:** Design Phase
**Author:** Samus Shepard (Game Designer)
**Date:** 2026-05-14

---

## Executive Summary

TileAdventure v1.0 is a solid match-3 puzzle game with 10 hand-tuned levels. v2.0 adds three features that transform it from a finite puzzle box into an infinitely replayable arcade experience:

1. **Combo Streak System** — Chained matches create escalating multipliers with visual juice
2. **Star Rating** — 1-3 star performance metrics on every campaign level
3. **Endless Mode** — Procedural survival mode with dynamic difficulty ramping

Play a campaign level for stars, then chase your personal best in endless. Two metas, one game.

---

## Feature 1: Combo Streak System

### 1.1 Design Intent

**Player fantasy:** "I'm on fire!" — The player feels brilliant when they chain matches together without hesitation.

**Mechanic:** Each match-3 clear starts or extends a combo. If the next match clears within the combo window, the multiplier increases. If the window expires, the combo resets. This rewards *speed* and *planning ahead* — pick tiles that set up the next match before collecting them.

### 1.2 Rules

| Rule | Value |
|------|-------|
| Combo start | First match-3 cleared |
| Combo window | 4.0 seconds (configurable in `GameConstants`) |
| Window reset | Each successful match resets the timer to 4.0s |
| Multiplier cap | ×5 |
| Multiplier progression | 1 → 2 → 3 → 4 → 5 (each match increments) |
| Combo break | Timer expires → multiplier resets to ×1 |
| Visual feedback | Floating text, screen shake, rack border pulse, particle burst size scales with combo |

### 1.3 Architecture Integration

**New class: `ComboSystem`**

```csharp
// Core/ComboSystem.cs  (plain C#, no Unity references)
public class ComboSystem {
    public int CurrentCombo { get; private set; }    // 0 = no active combo
    public int MaxComboThisLevel { get; private set; } // tracked for star rating
    public float ComboTimer { get; private set; }      // counts down

    public event Action<int> OnComboIncreased;  // fires with new combo level
    public event Action OnComboBroken;           // timer expired
    public event Action<float> OnComboTick;      // fires with remaining time ratio (for UI bar)

    public void RegisterMatch();     // called from OnMatchCleared
    public void Tick(float dt);      // called from GameplayController.Update()
    public void Reset();             // on level restart
}
```

**Integration points:**

- `GameState` gains a `ComboSystem` field (created in constructor)
- `GameplayController.OnMatchCleared()` calls `_levelManager.State.Combo.RegisterMatch()` before anything else
- `GameplayController.Update()` calls `_levelManager.State.Combo.Tick(Time.deltaTime)`
- `GameConstants` gains `comboWindowDuration` (default 4.0f) and `maxComboMultiplier` (default 5)

**Why ComboSystem is separate from GameState:**
- Single Responsibility: GameState tracks win/lose/triples; ComboSystem tracks combo state
- Testable in isolation: combo timing logic can be unit-tested without any level context
- The combo system doesn't need to know about triples or win conditions

### 1.4 Visual Design

| Combo Level | Floating Text | Rack Border Color | Particle Count | Screen Shake |
|-------------|---------------|-------------------|----------------|--------------|
| ×1 | "Nice!" (white) | White | 6 (default) | None |
| ×2 | "COMBO x2!" (yellow) | Yellow pulse | 10 | Light |
| ×3 | "COMBO x3!" (orange) | Orange pulse | 14 | Medium |
| ×4 | "COMBO x4!" (red) | Red pulse | 18 | Heavy |
| ×5 | "MAX COMBO!" (magenta, larger) | Magenta pulse + sparkle | 24 | Max |

**Implementation notes:**
- Floating text: instantiate a `GameObject` with `Text`/`TextMeshPro`, animate up + fade, destroy after ~1.5s
- Rack border: `RackView` has a border `Image`; pulse = scale oscillate between 1.0 and 1.05 at `comboLevel * 2 Hz`
- Particles: `SpawnMatchParticles` already takes `iconId`; add an overload that scales count by combo level
- Screen shake: add a `ShakeCamera` coroutine on the main camera (offset sin wave, amplitude = comboLevel * 0.015)

### 1.5 Edge Cases

| Case | Handling |
|------|----------|
| Combo timer expires mid-animation | Timer keeps ticking during animations; if it expires before next match, combo breaks. Fair. |
| Win on a combo match | Combo registers, then win popup appears. Combo text is visible behind popup — feels great. |
| Lose from rack overflow | Combo state freezes (phase = Lost, Tick() stops). Next level starts fresh. |
| Rapid multi-match (chain reaction) | `CheckMatch` can trigger multiple matches in one frame. Each calls `RegisterMatch()` — all increment the combo. Chain reactions are rewarded! |
| Level restart | `ComboSystem.Reset()` called in `LevelManager.LoadLevel()` |

---

## Feature 2: Star Rating System

### 2.1 Design Intent

**Player fantasy:** "I mastered this level." — Stars give replay incentive to levels the player already beat.

**Mechanic:** At level completion, the player earns 1-3 stars based on performance. Stars are saved per level and displayed on the Home screen.

### 2.2 Rules

| Star | Requirement | Visual |
|------|-------------|--------|
| ⭐ Bronze | Clear the level (meet targetTriples) | Gray star fills bronze |
| ⭐⭐ Silver | Bronze + complete under the level's silver time threshold | Second star fills silver |
| ⭐⭐⭐ Gold | Silver + achieve at least combo ×3 during the level | Third star fills gold |

**Time thresholds per level** (tunable via `LevelDefinition`):

| Level | Silver Time | Rationale |
|-------|-------------|-----------|
| 1 | 45s | Tutorial — generous, rewards familiarity |
| 2 | 50s | Slightly more tiles |
| 3 | 55s | First 3-layer board |
| 4 | 60s | Gating |
| 5 | 55s | Fewer rack slots = faster decisions needed |
| 6 | 65s | More tiles, more layers |
| 7 | 65s | |
| 8 | 70s | Tight rack |
| 9 | 60s | 5 slots = intense, but fewer tiles |
| 10 | 75s | Boss level — generous but combo requirement is the real gate |

> **Tuning note:** These are initial estimates. Playtest and adjust. The goal is: bronze = anyone, silver = attentive play, gold = mastery.

### 2.3 Architecture Integration

**Changes to existing files:**

`LevelGenerator.LevelDefinition` gains:
```csharp
public float silverTimeThreshold;   // seconds to beat for silver star
```

`GameState` gains:
```csharp
public int maxComboAchieved;        // updated by ComboSystem
public int starsEarned;             // computed on win
```

`GameState` gains a method:
```csharp
public int CalculateStars() {
    int stars = 1; // bronze: always awarded on win

    if (timeElapsed <= silverTimeThreshold)  // from LevelDefinition
        stars = 2;

    if (stars >= 2 && maxComboAchieved >= 3) // gold: need combo x3
        stars = 3;

    return stars;
}
```

`SaveService.LevelScore` gains:
```csharp
public int bestStars;   // 1-3, best across all plays
```

**Data flow:**

```
Level win → GameState.CalculateStars()
         → SaveService.RecordLevelScore(level, triples, time, stars)
         → Win popup shows star reveal animation
         → HomeScreen reads SaveService → displays stars under each level button
```

### 2.4 Star Reveal Animation

Win popup exists already. Enhancement:
1. Popup appears as before
2. 0.5s delay → Star 1 fills (scale bounce + SFX)
3. 0.4s delay → Star 2 fills if earned (faster if not earned, just gray outline stays)
4. 0.4s delay → Star 3 fills if earned
5. If player improved their previous best: "NEW BEST!" text appears

### 2.5 Home Screen Integration

Each level button already exists (5×2 grid). Enhancement:
- Three small star icons below each level number
- Filled = earned, outlined = not yet earned
- Stars persist across sessions (save data)

---

## Feature 3: Endless Mode

### 3.1 Design Intent

**Player fantasy:** "How far can I go?" — After beating the campaign, the player enters a survival mode where difficulty escalates until they inevitably overflow. The score is their legacy.

**Mechanic:** No win condition. No target triples. The board regenerates as tiles are cleared. Difficulty ramps every 3 triples. The only exit is rack overflow (lose). High score = most triples cleared in one run.

### 3.2 Rules

| Parameter | Behavior |
|-----------|----------|
| Win condition | None — always ends in loss |
| Score | Total triples cleared in the run |
| Best score | Saved separately from campaign progress |
| Difficulty ramp | Every 3 triples cleared → increase difficulty tier |
| Starting difficulty | Tier 1: 5 icons, 7 slots, 2 layers (equivalent to Level 1) |
| Ramp per tier | +1 active icon, layers increase per tier, rack slots decrease (floor 4) |

### 3.3 Difficulty Ramp Table

| Tier | Triples Range | Icons | Layers | Rack Slots |
|------|---------------|-------|--------|------------|
| 1 | 0-2 | 5 | 2 | 7 |
| 2 | 3-5 | 6 | 2 | 7 |
| 3 | 6-8 | 7 | 3 | 7 |
| 4 | 9-11 | 7 | 3 | 6 |
| 5 | 12-14 | 8 | 3 | 6 |
| 6 | 15-17 | 8 | 4 | 6 |
| 7 | 18-20 | 9 | 4 | 6 |
| 8 | 21-23 | 9 | 4 | 5 |
| 9 | 24-26 | 10 | 5 | 5 |
| 10 | 27-29 | 10 | 5 | 5 |
| 11+ | 30+ | 11 | 5 | 5 (floor) |

Beyond tier 11: icons cap at 14 (all icons active), layers cap at 6, slots floor at 4. The game becomes about raw speed and precision.

### 3.4 Board Refill Logic

Unlike campaign mode (static board), endless mode needs to regenerate tiles:

- **Refill trigger:** Board has ≤ 6 tiles remaining
- **Refill amount:** `(rackSlotCount + 2)` new tiles (ensures enough to keep playing)
- **Refill algorithm:** Same triple-first construction, but tiles are generated in batches of 3 with the *current* difficulty tier's parameters
- **Refill animation:** New tiles cascade in from the top (same cascade animation, just triggered on refill instead of level start)

### 3.5 Architecture Integration

**New class: `EndlessLevelController`** (extends or replaces `LevelManager` behavior)

**Option A (recommended):** New `EndlessModeController` MonoBehaviour with its own scene, reusing `BoardLogic`/`RackLogic`/`GameState` with modified parameters.

**Option B:** Extend `GameplayController` with a `isEndlessMode` bool flag.

**Recommendation: Option A.** Cleaner separation. The endless scene has different UI (score instead of progress, no win popup, "Game Over" instead of "Level Failed"). Reuses all View prefabs.

**Architecture:**

```
EndlessModeController (new MonoBehaviour)
    ├── BoardView (reused)
    ├── RackView (reused)
    ├── EndlessLevelManager (new, plain C#)
    │       ├── BoardLogic (reused)
    │       ├── RackLogic (reused)
    │       └── EndlessGameState (new, extends GameState or separate)
    └── EndlessUI (new: score text, tier indicator, game over popup)
```

`EndlessGameState` differences from `GameState`:
- No `targetTriples` — no win condition
- `totalTriplesCleared: int` — the score
- `currentTier: int` — difficulty tier (computed from triples)
- `MarkLost()` only — no `RecordTripleCleared` (no win check)

`EndlessLevelManager` differences from `LevelManager`:
- `CheckRefill()` called after each match — if board is low, generate and add more tiles
- `GetCurrentTier()` computes tier from triples cleared
- No `LoadLevel()` / `LoadLevelProcedural()` — starts at tier 1 and ramps

### 3.6 Home Screen Integration

- New button on HomeScreen: "Endless Mode 🌊"
- Shows best endless score below the button: "Best: 42 triples"
- Tapping it writes `PlayerPrefs.SetInt("SelectedMode", (int)GameMode.Endless)` then loads Gameplay scene

Alternative: separate "Endless" scene. The `SceneLoader` already supports loading any scene by name.

### 3.7 Save System Integration

`SaveData` gains:
```csharp
public int bestEndlessScore;
```

`SaveService` gains:
```csharp
public void RecordEndlessScore(int triples);
public int GetBestEndlessScore();
```

### 3.8 Edge Cases

| Case | Handling |
|------|----------|
| Board refill creates no exposed tiles | Not possible — new tiles are placed on existing higher layers or replace removed tiles; triple-first ensures at least one of each triple is on a reachable layer |
| Difficulty ramp during animation | Ramp only triggers when `RegisterMatch()` fires (post-animation). Combo system keeps ticking. |
| Player hits tier 11+ | Icons cap at 14, layers cap at 6, rack floor 4. Game becomes pure endurance. |
| Very long runs (100+ triples) | Performance: with ~24 tiles on board at any time (old tiles removed, new ones generated), memory is stable. Score display handles 3+ digits. |

---

## 4. Implementation Plan

### Phase 1: Combo Streak + Star Rating (together)
*Estimated scope: 1 development session*

1. Add `ComboSystem.cs` (Core)
2. Add combo fields to `GameConstants`
3. Wire `ComboSystem` into `GameState` and `GameplayController`
4. Add `silverTimeThreshold` to `LevelDefinition`
5. Implement `GameState.CalculateStars()`
6. Add star save data to `SaveService.LevelScore`
7. Create combo UI prefabs (floating text, rack border pulse)
8. Implement star reveal animation in win popup
9. Update `HomeScreen` to display stars
10. Add screen shake utility

### Phase 2: Endless Mode
*Estimated scope: 1-2 development sessions*

1. Add `bestEndlessScore` to `SaveData`/`SaveService`
2. Create `EndlessGameState.cs` (Core)
3. Create `EndlessLevelManager.cs` (Core)
4. Create `EndlessModeController.cs` (Gameplay)
5. Add Endless Mode button to `HomeScreen`
6. Create Endless scene (or reuse Gameplay scene with mode flag)
7. Implement board refill logic
8. Implement difficulty ramp
9. Add tier indicator UI
10. Game Over screen with final score + "New Best!" detection

---

## 5. New Constants / Config Changes

Additions to `GameConstants`:
```csharp
[Header("Combo")]
public float comboWindowDuration = 4f;
public int maxComboMultiplier = 5;
public float comboTextLifetime = 1.5f;
public float screenShakeIntensity = 0.015f;

[Header("Endless Mode")]
public int endlessRefillThreshold = 6;
public int endlessTriplesPerTier = 3;
public int endlessMinRackSlots = 4;
public int endlessMaxIcons = 14;
public int endlessMaxLayers = 6;
```

---

## 6. Risk Assessment

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Combo timer feels too short/long | Medium | Expose in GameConstants, test with multiple skill levels, adjust after playtest |
| Star time thresholds mis-calibrated | High | Start generous, tighten after playtesting. Log average clear times per level to inform tuning. |
| Endless mode board refill causes frame spike | Medium | Generate refill tiles over multiple frames (coroutine), or pre-generate next batch while player is matching |
| Endless difficulty ramp feels unfair | Medium | Start with gentle ramp. The 3-triples-per-tier cadence is easy to adjust — just change one constant. |
| Home screen star display adds visual clutter | Low | Stars are small (12-16px), positioned below level number. Clean. |

---

## 7. Success Metrics

What "good" looks like after launch:

| Metric | Target |
|--------|--------|
| Players replay at least 3 levels for stars | > 50% of players |
| Players try endless mode | > 40% of players who complete level 5 |
| Average endless run | > 8 triples (past tier 3) |
| Gold stars on level 1 | > 60% (tutorial should be masterable) |
| Gold stars on level 10 | < 10% (boss level should be hard) |
