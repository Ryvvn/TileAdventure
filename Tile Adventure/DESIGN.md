# TileAdventure — Architecture & Design Decisions

## Overview

TileAdventure is a match-3 puzzle game built in Unity. A stack of layered tiles sits on the board — tap exposed tiles to collect them into a sorted rack at the bottom. When three tiles with the same icon land adjacently in the rack, they clear and count as a "triple." Clear enough triples to win, but don't let the rack overflow.

**Tech stack:** Unity (C#), UnityEngine.UI (Canvas-based rendering), JSON persistence

---

## 1. Architecture Decisions

### 1.1 Core / View Separation (Data-Driven Architecture)

**Decision:** Every game system is split into a **plain C# class** (Core) and a **MonoBehaviour** (View).

| Layer | Location | Role | Example |
|-------|----------|------|---------|
| Core | `Scripts/Core/` | Pure data, logic, state. No Unity references beyond `Vector2`. Fully unit-testable without a scene. | `BoardLogic`, `RackLogic`, `GameState` |
| View | `Scripts/Gameplay/` | MonoBehaviour rendering and animation. Reads Core data, fires events back. | `BoardView`, `RackView`, `TileView` |

**Why:**
- Logic can be tested without Unity — drag a `.cs` file into any test runner.
- Rendering can change (e.g., switch from Canvas to SpriteRenderer) without touching game rules.
- Thread safety: Core classes don't touch `GameObject`, `Transform`, or `Time`.
- Replay/debugging: you can serialize the entire `BoardLogic` state and replay it.

**The bridge:** `GameplayController` is the single MonoBehaviour orchestrator. It creates a `LevelManager` (which owns `BoardLogic` + `RackLogic` + `GameState`), wires Core events to View reactions, and ticks the timer.

```
GameplayController (MonoBehaviour)
    └── LevelManager (plain C#)
            ├── BoardLogic → tile list, exposure, grid math
            ├── RackLogic  → slot array, sort-by-icon, match detection
            └── GameState  → phase machine, triples counter, timer
```

### 1.2 Event-Driven Communication

**Decision:** Core classes expose C# `event Action<T>` delegates. Views subscribe, never call into Core directly.

The flow for a single player tap:

```
1. BoardView → fires OnTileTapped(TileView)
2. GameplayController.OnTileTapped():
   a. Guard checks (phase, isRemoved, isMoving, rack overflow)
   b. Gets insert index from RackLogic.GetInsertIndex()
   c. Animates rack shift (RackView.AnimateShiftForInsert)
   d. Animates tile flight (BoardView.AnimateMoveToRack)
   e. On animation complete → RackLogic.AddTile() + BoardLogic.RemoveTile()
3. RackLogic.AddTile() → checks for match-3 → fires OnMatchCleared
4. GameplayController.OnMatchCleared() → SFX, particles, GameState.RecordTripleCleared()
5. RackView.OnMatchCleared() → animate matched icons scaling/fading out
```

**Why events instead of direct calls:**
- Decouples timing: Views animate over multiple frames (async), Core commits data instantly (sync).
- Multiple listeners: both `GameplayController` and `RackView` listen to `OnMatchCleared` independently.
- Easy to add new listeners (e.g., combo system, analytics, screen shake) without changing RackLogic.

### 1.3 Plain C# Data Model: TileData

**Decision:** `TileData` is a plain class — no `MonoBehaviour`, no `ScriptableObject`.

```csharp
public class TileData {
    public int tileId;           // unique per session
    public int iconId;           // 0-13, maps to sprite
    public int layerIndex;       // 0 = bottom, higher = on top
    public Vector2Int gridPosition;
    public Vector2 worldPosition;
    public bool isExposed;       // tappable? (computed by RefreshExposure)
    public bool isRemoved;       // gone from board?
    public bool isMoving;        // mid-flight to rack?
}
```

**Why:**
- `TileData` is created/destroyed dozens of times per session — no GC overhead from Unity objects.
- `TileView` wraps one `TileData` and renders it. Multiple views for one data is possible (e.g., minimap).
- `isExposed` is computed by `BoardLogic.RefreshExposure()`, not by individual tiles — single pass, O(n²) per layer comparison.

### 1.4 Scene Flow & Bootstrapping

**Decision:** Three scenes with a lightweight bootstrapper.

```
Loading → Home → Gameplay → (win/lose) → Home (or restart)
```

`SceneBootstrapper` handles Camera configuration and `CanvasScaler` setup per scene. Each scene's logic starts from its own entry point (`HomeScreen.Start()`, `GameplayController.Start()`). `SceneLoader` wraps `SceneManager.LoadSceneAsync` with a loading screen fade.

### 1.5 Combo System

**Decision:** `ComboSystem` is a standalone plain C# class — owned by `GameState`, driven by `GameplayController`.

```
GameState
    └── ComboSystem
            ├── CurrentCombo: int (0 = no active combo)
            ├── MaxComboThisLevel: int (tracked for star rating)
            ├── ComboTimer: float (counts down from 4s)
            ├── events: OnComboIncreased(int), OnComboBroken, OnComboTick(float)
            ├── RegisterMatch() — called from OnMatchCleared, increments combo + resets timer
            ├── Tick(float dt) — called from Update(), decrements timer
            └── Reset() — called on level restart
```

**Why separate from GameState:**
- Single Responsibility: `GameState` tracks win/lose/triples; `ComboSystem` tracks combo timing.
- Testable in isolation: combo timing logic can be unit-tested without level context.
- Clean event surface: `GameplayController` subscribes to `OnComboIncreased` → floating text + screen shake + rack border pulse, `OnComboBroken` → stops border pulse, `OnComboTick` → UI combo bar.

**Combo rules:**

| Rule | Value |
|------|-------|
| Window duration | 4.0s (configurable in `GameConstants`) |
| Multiplier cap | ×5 |
| Progression | 1 → 2 → 3 → 4 → 5 (each match increments) |
| Break | Timer expires → `CurrentCombo` resets to 0 |
| Chain reactions | Multiple matches in one frame all increment the combo |

**Visual feedback per combo level:**

| Level | Floating Text | Rack Border | Particles | Screen Shake |
|-------|---------------|-------------|-----------|--------------|
| ×1 | "Nice!" (white) | White | 6 (default) | None |
| ×2 | "COMBO x2!" (yellow) | Yellow pulse | 10 | Light |
| ×3 | "COMBO x3!" (orange) | Orange pulse | 14 | Medium |
| ×4 | "COMBO x4!" (red) | Red pulse | 18 | Heavy |
| ×5 | "MAX COMBO!" (magenta) | Magenta + sparkle | 24 | Max |

**Integration points:**
- `GameConstants` provides `comboWindowDuration` (default 4.0f) and `maxComboMultiplier` (default 5)
- `GameplayController.OnMatchCleared()` calls `State.Combo.RegisterMatch()` before anything else
- `GameplayController.Update()` calls `State.Combo.Tick(Time.deltaTime)` via `LevelManager.Tick()`
- Combo text animation reuses the existing async `Task.Yield()` pattern (consistent with tile flight)

---

## 2. How Level Data Is Structured

### 2.1 LevelConfig (ScriptableObject)

The canonical level definition:

```csharp
LevelConfig {
    levelNumber: int          // 1-10
    targetTriples: int        // matches needed to win
    layerCount: int           // stacked Z-layers
    activeIconCount: int      // distinct icons (from 14)
    rackSlotCount: int        // rack size
    tiles: List<TilePlacement> {
        iconId: int           // 0-13
        layerIndex: int       // 0 = bottom
        gridPosition: Vector2Int
    }
}
```

Created via Editor menu `TileAdventure > Generate Level Assets`, which calls `LevelGenerator.GenerateLevelConfig()`.

### 2.2 Dual Loading Strategy

`GameplayController.Start()` tries to load a `LevelConfig` asset from `Resources/Config/Levels/`. If it finds one, it uses the pre-authored tile placements. If the asset is missing, it falls back to **procedural generation** using difficulty parameters from `LevelGenerator.GetLevelDefinition()`.

```csharp
var config = Resources.Load<LevelConfig>($"Config/Levels/Level_{levelNumber:D2}");
if (config != null)
    _levelManager.LoadLevel(config);         // hand-authored tiles
else
    _levelManager.LoadLevelProcedural(...);  // procedural fallback
```

This means level designers can author levels in the Editor, or the game can generate levels on the fly. Both paths converge into `BoardLogic` which doesn't care where the tile list came from.

### 2.3 Difficulty Curve

| Level | Triples | Layers | Icons | Rack Slots | Silver Time | Difficulty |
|-------|---------|--------|-------|------------|-------------|------------|
| 1 | 3 | 2 | 5 | 7 | 45s | Tutorial |
| 2 | 4 | 2 | 6 | 7 | 50s | Easy |
| 3 | 4 | 3 | 6 | 7 | 55s | Easy+ |
| 4 | 5 | 3 | 7 | 7 | 60s | Medium |
| 5 | 5 | 3 | 8 | 6 | 55s | Medium+ |
| 6 | 6 | 4 | 8 | 6 | 65s | Hard |
| 7 | 6 | 4 | 9 | 6 | 65s | Hard+ |
| 8 | 7 | 4 | 10 | 6 | 70s | Expert |
| 9 | 7 | 5 | 11 | 5 | 60s | Expert+ |
| 10 | 8 | 5 | 12 | 5 | 75s | Master |

The design tightens three knobs simultaneously: more layers (blocking), more icons (harder to group), fewer rack slots (easier to overflow). The silver time threshold controls star rating — beat this time for a silver star; add combo ×3 for gold.

### 2.4 LevelDatabase (ScriptableObject)

Optional. Wraps an array of `LevelConfig` assets for bulk lookup. `LevelDatabase.GetLevel(n)` does a linear scan. Not strictly required since `GameplayController` loads configs directly by name convention, but useful for editor tooling and level selection UIs.

### 2.5 Star Rating System

**Decision:** Stars are evaluated on win by `GameState.CalculateStars()` and saved per-level via `SaveService`. The best star count never downgrades.

**Star criteria:**

| Star | Requirement | Visual |
|------|-------------|--------|
| ⭐ Bronze | Clear the level (meet targetTriples) | Always awarded on win |
| ⭐⭐ Silver | Bronze + `timeElapsed` ≤ `silverTimeThreshold` | Time gate, varies per level |
| ⭐⭐⭐ Gold | Silver + `maxComboAchieved` ≥ 3 | Combo gate, same threshold across all levels |

**Data flow:**

```
Level win → GameState.CalculateStars()
         → SaveService.RecordLevelScore(level, triples, time, stars)
         → Win popup: RevealStars(stars, previousBest) — sequential scale bounce animation
         → HomeScreen: SaveService.GetBestStars(level) → 3 star icons per level button
```

**Key design decisions:**
- `CalculateStars()` self-syncs `maxComboAchieved` from `ComboSystem.MaxComboThisLevel` before scoring — always accurate even if combo expired between last match and win.
- Stars are **best-ever** — `RecordLevelScore` only writes when `stars > existing.bestStars`.
- "NEW BEST!" text appears on the win popup whenever `stars > previousBest` (including first-time clears going from 0→1).
- Star reveal animation uses token-based cancellation — bumped on Restart/GoHome/NextLevel/OnDestroy to prevent orphaned animations on scene transitions.
- `silverTimeThreshold` flows through both auth paths: `LevelDefinition` (procedural) and `LevelConfig` (ScriptableObject) → `LevelManager` → `GameState` constructor.

**Home screen integration:**
- Three small star text objects ("★"/"☆") per level button, anchored at button bottom.
- Colors: bronze = warm orange, silver = gray, gold = yellow, empty = dark gray.
- Stars display for locked levels too (always empty until unlocked + cleared).

---

## 3. How Level Solvability Is Ensured

### 3.1 Triple-First Construction

**Every level is solvable by design.** The procedural generator never creates an orphan tile.

Algorithm (`LevelGenerator.GenerateSolvableLayout`):

```
FOR each triple needed:
    1. Pick a random icon from the active set
    2. Place ALL 3 tiles with that same icon
    3. Random layers, random grid positions (no duplicates per layer)
```

**Why this guarantees solvability:**
- Every icon that appears on the board appears exactly 3 times (one complete triple).
- Since all 3 tiles share an icon, at least one of them will be on a layer where the other two don't completely block it.
- As upper-layer tiles are removed (by matching their own triples), lower-layer tiles become exposed.
- The board always has at least one exposed tile — there's no deadlock.

### 3.2 Exposure System

`BoardLogic.RefreshExposure()` runs after every tile removal:

```
FOR each tile A (not removed):
    FOR each tile B (not removed, B != A):
        IF B.layerIndex > A.layerIndex AND B overlaps A in world space:
            A.isExposed = false  (blocked!)
    IF no blocker found:
        A.isExposed = true      (tappable!)
```

Overlap detection uses world-space rectangle intersection (`TileData.Overlaps`). The staggered pyramid grid layout (odd rows offset horizontally) plus the layer vertical offset means tiles partially overlap rather than fully cover each other — this creates the visual "cascading stack" effect and ensures lower tiles peek through gaps.

### 3.3 Rack Sorting Guarantees

`RackLogic.AddTile()` inserts tiles at the correct sorted position:

- **Same-icon grouping:** new tile goes after the last tile with the same icon
- **Sort order:** new tile goes before the first tile with a strictly higher icon
- **Empty fallback:** if all existing icons are lower, fills the leftmost empty slot

This guarantees that three matching tiles always land adjacently as long as no other icon tiles are between them, which the sort order prevents.

### 3.4 Edge Cases Handled

| Edge Case | Handling |
|-----------|----------|
| Rack full with pending match | `RackLogic.AddTile()` detects the match, clears it, compacts, leaves room for next tile |
| Rack truly full (all slots occupied, no match) | `OnRackOverflow` → `GameState.MarkLost()` |
| Board emptied before target reached | Impossible by triple-first construction (targetTriples × 3 = total tiles) |
| Player taps during animation | `isMoving` flag blocks interaction |
| Save file corrupted | `SaveService` catches exception, returns fresh `SaveData` |
| LevelConfig asset missing | Falls back to procedural generation with correct difficulty params |

---

## 4. Trade-offs & Areas for Improvement

### 4.1 Known Trade-offs

| Trade-off | What We Chose | Cost |
|-----------|---------------|------|
| **O(n²) exposure refresh** | Simple loop over all tile pairs on every removal | Could be O(n log n) with spatial partitioning, but for ~24 tiles per level it's negligible |
| **Linear rack scan for insert** | `FindInsertIndex` scans all slots | For 5-7 slot racks, the difference vs. binary search is 0.0ms |
| **No object pooling for TileViews** | `Instantiate`/`Destroy` per tile | Simpler code. For ~30 tiles per level, GC impact is minimal. Would matter for endless mode. |
| **PlayerPrefs for level selection** | `PlayerPrefs.GetInt("SelectedLevel")` bridges Home→Gameplay scenes | Couples scenes through a string key. A static manager would be cleaner but PlayerPrefs works and survives scene loads. |
| **Resources.Load for assets** | Sprites and configs loaded from `Resources/` folders | Addressables would be better for production (async, memory management), but Resources is fine for small asset sets. |
| **Canvas-based rendering (UI toolkit)** | Entire board rendered as UI Images under a Canvas | Simpler, works great on mobile. But for 50+ animated tiles, SpriteRenderer + world-space would be faster. |

### 4.2 Areas Worth Improving

1. **Object Pooling for Endless Mode** — When levels go infinite, `Instantiate`/`Destroy` churn will cause frame spikes. A pool of ~50 TileViews should suffice.

2. **Spatial Hashing for Exposure** — If layer counts go above 5 (endless mode), the current O(n²) overlap check becomes O(n² × layers). A simple grid-based spatial hash would cut this to O(n).

3. **Save System Extensibility** — `SaveService` uses `JsonUtility` which doesn't support dictionaries or polymorphic types. For future features (daily challenges, collectibles), switching to Newtonsoft.Json or OdinSerializer would be wise.

4. **Animation Cancellation** — Currently, if the player taps rapidly during a tile-flight animation, the game ignores it via `isMoving`. A better UX would queue the tap and auto-process it when the animation completes.

5. **Input Rebinding / Accessibility** — No keyboard or gamepad support. Pure touch/tap. Adding WASD/arrow navigation and a "confirm" key would open the game to more platforms.

6. **Analytics Hooks** — No telemetry. Adding lightweight events (level start, level win, level fail, match count, time-to-first-match) would give data for difficulty tuning.

7. **Localization** — All strings (level names, button labels, win/lose text) are currently hardcoded. A simple `LocalizationService` with a JSON dictionary would be a quick win for multi-language support.

8. **Endless Mode Architecture** — The current `LevelManager` is designed for finite levels with a `targetTriples` win condition. Endless mode needs a different win condition (none — you always eventually lose) and a dynamic difficulty ramp rather than static definitions.

---

## 5. File Map

```
Assets/Scripts/
├── Config/
│   ├── GameConstants.cs       # Tuning values (tile sizes, animation speeds, etc.)
│   └── LevelConfig.cs         # ScriptableObject for level data
├── Core/
│   ├── BoardLogic.cs          # Tile list, exposure, grid math, overlap detection
│   ├── ComboSystem.cs         # Combo streak timer, multiplier tracking, events
│   ├── GameState.cs           # Phase machine, triples counter, timer, combo, star rating
│   ├── LevelDatabase.cs       # ScriptableObject wrapper for LevelConfig[]
│   ├── LevelGenerator.cs      # Procedural level creation + difficulty curve + star thresholds
│   ├── LevelManager.cs        # Orchestrator: wires Board+Rack+State for one level
│   ├── RackLogic.cs           # Slot array, icon-sorted insertion, match-3 detection
│   └── TileData.cs            # Plain C# data model for one tile
├── Gameplay/
│   ├── BoardView.cs           # Renders board tiles, flight animation, particles
│   ├── GameplayController.cs  # MonoBehaviour bridge: Core ↔ View, star reveal animation
│   ├── RackView.cs            # Renders rack slots, shift/match-clear animation, combo border pulse
│   └── TileView.cs            # Single tile renderer with hover/block/exposed states
├── Services/
│   ├── AudioManager.cs        # Singleton: music, tap SFX, match SFX
│   ├── SaveService.cs         # JSON persistence: level progress + star ratings
│   └── SceneLoader.cs         # Async scene loading with loading screen
├── UI/
│   ├── HomeScreen.cs          # Level select grid (5×2), star display, play button
│   └── LoadingScreen.cs       # Transition screen
├── Editor/
│   └── SceneGenerator.cs      # Editor tool: generate scenes from templates
└── SceneBootstrapper.cs       # Per-scene Camera + CanvasScaler setup
```
