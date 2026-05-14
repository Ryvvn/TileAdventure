# Skipped Findings — Deferred Action Reference

**Date:** 2026-05-14
**Source:** Full-snapshot adversarial code review of 20 C# source files
**Patched:** 6 findings (F1, N1, F12, N2, N3, F6)
**Skipped:** 13 findings below

---

## Category 1: Need Async Architecture Rework

These all need `UniTask` + `CancellationToken` — a project-wide async pass.

### F2 — Chain-reaction OnMatchCleared fires before recursive CheckMatch completes

**Severity:** MEDIUM — visual glitch only, no crash, no data corruption.

**Location:** [RackLogic.cs](../Tile%20Adventure/Assets/Scripts/Core/RackLogic.cs) `CheckMatch()`

**What happens:** `CheckMatch()` finds match-3 → `CompactRack()` (sync) → fires `OnMatchCleared` → recurses into another `CheckMatch()` → another `CompactRack()`. When `RackView.OnMatchCleared` runs its async animation, the slot indices it captured may now hold different tiles (because a second compaction shifted them). `RefreshRackVisuals()` at the end reconciles everything, so the end state is correct — only the animation visual is briefly wrong.

**Fix:** Fire `OnMatchCleared` *after* all recursive `CheckMatch` calls complete, not inside each one. Collect all match ranges, compact once, then fire one event with all affected ranges.

---

### F3 — async void proliferation, no error handling, no cancellation

**Severity:** MEDIUM — crash risk exists but rare.

**Locations:**

| Method | File | Risk |
|--------|------|------|
| `ShakeBlockedTile()` | TileView.cs | Low — only touches `_rectTransform` |
| `AnimateTileRemoval()` | BoardView.cs | Medium — tileView destroyed during scene restart |
| `OnMatchCleared()` | RackView.cs | Medium — `_slotViews[i]` destroyed on scene unload |
| `Initialize()` | GameplayController.cs | Low — `async` keyword unnecessary, no awaits |
| `Restart()` / `GoHome()` | GameplayController.cs | Medium — no `try/catch`, no timeout on scene load |
| `OnPlayClicked()` | HomeScreen.cs | Low — delegates to `OnLevelSelected()` which returns `Task` |

**Fix:** Convert fire-and-forget animation methods to return `UniTask`. Add `CancellationToken` that cancels on `OnDestroy`. Wrap scene-load awaits in `try/catch`.

---

### F10 — RackView.OnMatchCleared accesses _slotViews after potential scene unload

**Severity:** MEDIUM — crash requires restarting during a 0.4s match animation.

**Location:** [RackView.cs](../Tile%20Adventure/Assets/Scripts/Gameplay/RackView.cs) `OnMatchCleared()`

**What happens:** The 0.4s animation loop accesses `_slotViews[i]`. If `GameplayController.Restart()` starts a scene load during those 0.4 seconds, the `MonoBehaviour` references are destroyed. Next loop iteration throws `MissingReferenceException` — crashes Unity because it's `async void`.

**Fix:** Same as F3 — add `CancellationToken` checked in the animation loop, cancel on `OnDestroy`.

---

## Category 2: True But Trivial / Low Impact

### F4 — WouldOverflowWithNext redundant empty-slot scan

**Severity:** LOW

**Location:** [RackLogic.cs](../Tile%20Adventure/Assets/Scripts/Core/RackLogic.cs) `WouldOverflowWithNext()`

**Issue:** After `IsFull()` returns true, the method double-checks by iterating all slots for emptiness. Redundant but O(7). Harmless.

**Fix if desired:** Remove the `foreach (var slot in _slots) { if (slot.IsEmpty) return false; }` block.

---

### F7 — SceneBootstrapper can create duplicate Canvas via GameObject.Find fallback

**Severity:** LOW

**Location:** [SceneBootstrapper.cs](../Tile%20Adventure/Assets/Scripts/SceneBootstrapper.cs) `SetupCanvasScaler()`

**Issue:** `GameObject.Find(canvasName)` + fallback creation. If someone renames the Canvas in the Editor, a second Canvas is created. In normal use the `SceneBootstrapper` is attached to the actual Canvas, so `Find` always succeeds.

**Fix if desired:** Remove the fallback creation. Just warn and return if `Find` returns null.

---

### F8 — TileView.Initialize no null check on _iconImage/_background

**Severity:** LOW

**Location:** [TileView.cs](../Tile%20Adventure/Assets/Scripts/Gameplay/TileView.cs) `Initialize()`

**Issue:** `_iconImage.sprite = iconSprite` throws `NullReferenceException` if the `[SerializeField]` Image is null. But `SceneGenerator` creates these via `AddComponent<Image>()` and serializes them. Always non-null in practice.

**Fix if desired:** Add `if (_iconImage == null || _background == null) return;` guard.

---

### F9 — LevelManager.LoadLevel overwrites Board/Rack/State without Dispose

**Severity:** LOW

**Location:** [LevelManager.cs](../Tile%20Adventure/Assets/Scripts/Core/LevelManager.cs) `LoadLevel()` / `LoadLevelProcedural()`

**Issue:** Creates new instances without disposing old ones. But these are called exactly once from `GameplayController.Start()`. `Initialize` re-entry is already patched (N3).

**Fix if desired:** `Board = new BoardLogic(...)` → `Board.Dispose(); Board = new BoardLogic(...)` if a Dispose pattern is added.

---

### F11 — SaveService new instance per win, redundant file I/O

**Severity:** LOW

**Location:** [GameplayController.cs](../Tile%20Adventure/Assets/Scripts/Gameplay/GameplayController.cs) `OnWon()`

**Issue:** `new SaveService()` triggers file read in constructor, then `UnlockLevel` writes back. `HomeScreen` already created a `SaveService` earlier. This is a second disk read. Harmless — single-threaded, one file. Total cost: ~1ms on modern hardware.

**Fix if desired:** Make `SaveService` a static singleton, or pass the instance from `HomeScreen`.

---

### F13 — BoardLogic.RefreshExposure O(n²) with no spatial bucketing

**Severity:** LOW

**Location:** [BoardLogic.cs](../Tile%20Adventure/Assets/Scripts/Core/BoardLogic.cs) `RefreshExposure()`

**Issue:** O(n²) overlap checks. Max level has 24 tiles = ~576 checks per `RefreshExposure` call, ~24 calls per level = ~14,000 total checks. `Overlaps()` already skips same/lower layers with early return. Trivial for any hardware including mobile.

**Fix if desired:** Group tiles by `layerIndex` into separate lists. Only check higher layers instead of all tiles. Reduces checks by ~50%.

---

### F14 — SceneLoader no timeout, hangs forever on stuck scene load

**Severity:** LOW

**Location:** [SceneLoader.cs](../Tile%20Adventure/Assets/Scripts/Services/SceneLoader.cs) `LoadSceneAsync()`

**Issue:** `while (asyncOp.progress < 0.9f)` loops forever if scene load stalls. `SceneManager.LoadSceneAsync` practically never hangs in production — would require disk failure or corrupted asset bundle.

**Fix if desired:** Add a 30-second timeout with `Task.WhenAny` + `Task.Delay`. On timeout, log error and allow navigation retry.

---

### F15 — TileData uses public fields with public event

**Severity:** LOW — code style, not a bug.

**Location:** [TileData.cs](../Tile%20Adventure/Assets/Scripts/Core/TileData.cs)

**Issue:** `isExposed`, `isRemoved`, `isMoving` are public fields. `SetExposed()` fires `OnExposureChanged` only on change, but direct `isExposed = true` bypasses the guard. In practice, only `BoardLogic.RefreshExposure()` sets `isExposed` via `SetExposed()`, and `isRemoved`/`isMoving` have no events so direct field access is fine.

**Fix if desired:** Convert to `{ get; private set; }` with explicit mutation methods.

---

## Category 3: Never Reachable / Dead Code

### N4 — FindInsertIndex fallback to _slotCount - 1

**Severity:** LOW

**Location:** [RackLogic.cs](../Tile%20Adventure/Assets/Scripts/Core/RackLogic.cs) `FindInsertIndex()`

**Issue:** When all icons are lower and no empty slot exists, returns `_slotCount - 1`. But `AddTile` gates on `emptyIndex < 0` first, so this branch is never reached. Misleading defense code.

**Fix if desired:** Remove `return _slotCount - 1;` and let it fall through to return 0, or add `Debug.LogError` for the unexpected case.

---

## Recap

| Priority | Count | What |
|----------|-------|------|
| 🔴 Patched | 6 | F1, N1, F12, N2, N3, F6 |
| 🟡 Async rework | 3 | F2, F3, F10 |
| 🟢 Low / trivial | 9 | F4, F7, F8, F9, F11, F13, F14, F15, N4 |
| ⚪ Already resolved | 1 | F5 |
