# Blind Hunter Review (Adversarial General)

## Instructions
You are reviewing code changes for a Unity 6.0 C# puzzle game called "Tile Trip Match."
You have NO access to the specification, project context, or design documents.
Review ONLY the diff below. Find bugs, logic errors, security issues, and code smells.
Be adversarial — assume every change is wrong until proven correct.

## Changed Files

### New Files Created (all in d:\UnityForWork\Project\TileAdventure\)

**Config Layer:**
- `Assets/Scripts/Config/GameConstants.cs` — ScriptableObject holding all magic numbers (rack size 7, match count 3, tile size, colors, animation durations, scene names)
- `Assets/Scripts/Config/LevelConfig.cs` — ScriptableObject with tile placements (iconId, layerIndex, gridPosition), target triples, layer count, active icon count, rack slot count

**Core Logic (plain C#, no MonoBehaviour):**
- `Assets/Scripts/Core/TileData.cs` — Data model: tileId, iconId, layerIndex, gridPosition, worldPosition, isExposed, isRemoved, isMoving. Has overlap detection and exposure change events.
- `Assets/Scripts/Core/BoardLogic.cs` — Board state: initialize from config or generate procedurally, track tiles by layer, determine exposure (tile is exposed only when no higher-layer tile overlaps it), remove tiles, hit-test click position
- `Assets/Scripts/Core/RackLogic.cs` — Rack state: fixed slot array, add tile (group by icon type), detect match-3 runs, remove matched tiles, compact rack, detect overflow
- `Assets/Scripts/Core/GameState.cs` — Game phase (Playing/Won/Lost), track triples cleared vs target, events for Win/Lose/TripleCleared, time tracking
- `Assets/Scripts/Core/LevelManager.cs` — Orchestrator: owns BoardLogic + RackLogic + GameState, load levels, wire win/lose events
- `Assets/Scripts/Core/LevelGenerator.cs` — Procedural level generation: creates solvable layouts by generating matching triples first, distributing across layers, difficulty scales per level (1 easy → 10 hard)
- `Assets/Scripts/Core/LevelDatabase.cs` — ScriptableObject wrapper for LevelConfig array

**Gameplay Views/Controllers (MonoBehaviour):**
- `Assets/Scripts/Gameplay/TileView.cs` — Renders tile with icon + background, handles tap input (IPointerClickHandler), dims blocked tiles, shake animation on blocked tap, double-tap protection
- `Assets/Scripts/Gameplay/BoardView.cs` — Instantiates TileViews from BoardLogic, positions them, animates tile movement to rack, animates tile removal
- `Assets/Scripts/Gameplay/RackView.cs` — Displays rack slots, handles tile insertion visuals, match-clear scale+fade animation, slot refresh
- `Assets/Scripts/Gameplay/GameplayController.cs` — Main orchestrator: wires BoardView+RackView to BoardLogic+RackLogic, handles tap→rack flow, win/lose popup, restart/home navigation

**Services:**
- `Assets/Scripts/Services/SaveService.cs` — JSON persistence for level progress (highest unlocked level, per-level scores), handles corrupt save files
- `Assets/Scripts/Services/SceneLoader.cs` — Async scene loading with progress polling
- `Assets/Scripts/Services/AudioManager.cs` — Persistent singleton (DontDestroyOnLoad), bg music loop + SFX pool (4 sources), volume control

**UI Screens:**
- `Assets/Scripts/UI/HomeScreen.cs` — Logo, Play button, 10-level grid (green=unlocked, orange=locked), loads SaveService, navigates to Gameplay
- `Assets/Scripts/UI/LoadingScreen.cs` — Async asset preloader (sprites + audio), progress bar, transitions to Home

**Other:**
- `Assets/Scripts/SceneBootstrapper.cs` — Auto-configures Canvas/Camera per scene type
- `Assets/Scripts/Editor/SceneGenerator.cs` — Unity Editor menu items to auto-generate all 3 scenes with proper hierarchy and serialized field wiring

**Documentation & Config:**
- `DESIGN.md` — Architecture decisions, solvability strategy, trade-offs
- `Packages/manifest.json` — Unity 6.0 URP 2D package dependencies
- `.gitignore` — Standard Unity gitignore
- `ProjectSettings/ProjectVersion.txt` — Unity 6000.0.0f1

## Key Code Patterns to Review

1. **TileData.Overlaps()** — Uses Rect.Overlaps with half-size of 0.5, but tiles are placed at grid positions mapped through GridToWorld which uses tileSize + spacing. The overlap check may not correctly detect overlapping tiles at different grid positions.

2. **RackLogic.CheckMatch()** — Recursive after clearing. May recurse infinitely if CompactRack leaves tiles in an unexpected state.

3. **RackLogic.WouldOverflowWithNext()** — Counts icon frequencies but doesn't check if any icon has exactly 2 tiles (which would trigger a match on the 3rd). Returns true (would overflow) even when a match is possible.

4. **TileView.ShakeBlockedTile()** — async void with Task.Yield() loop. May cause issues with Unity's main thread synchronization.

5. **BoardView.AnimateTileRemoval()** — async void. The TileView is destroyed at end, but the coroutine may access it after destruction.

6. **GameplayController.OnTileTapped()** — Calls WouldOverflowWithNext() before AddTile(), but AddTile still has its own IsFull() + OnRackOverflow check. Double overflow detection could cause issues.

7. **SceneGenerator.cs** — SerializedProperty paths reference private fields by name. Any field rename breaks the generator silently.
