# DESIGN.md — Tile Trip Match

## Architecture Decisions

### MVC with Plain C# Core
The project enforces strict separation between game logic and Unity-specific rendering:

- **Model / Logic Layer** (`Assets/Scripts/Core/`): All game rules, state management, and algorithms live in plain C# classes (`TileData`, `BoardLogic`, `RackLogic`, `GameState`, `LevelManager`). None inherit from `MonoBehaviour`. This satisfies the test requirement and makes the logic independently testable without running Unity.
- **View / Controller Layer** (`Assets/Scripts/Gameplay/`): `MonoBehaviour` components (`BoardView`, `TileView`, `RackView`, `GameplayController`) observe core state via events and drive visual representation. They own no game logic.
- **Config Layer** (`Assets/Scripts/Config/`): `ScriptableObject` assets (`GameConstants`, `LevelConfig`) hold all tunable parameters — no magic numbers in code.

### Level Data Structure
Levels are defined via `LevelConfig` ScriptableObjects with:
- `targetTriples`: number of matched triples required to win
- `layerCount`: how many stacked layers on the board
- `activeIconCount`: how many distinct icon types appear in this level
- `rackSlotCount`: rack size (decreases in harder levels)
- `tiles`: a list of `TilePlacement` entries (iconId, layerIndex, gridPosition)

Each `TilePlacement` maps one tile to a specific icon, layer, and grid cell. The `BoardLogic` class reads these placements and builds the tile grid, resolving exposure via overlap checks.

### Scene Flow
```
Loading → Home → Gameplay
```
- **Loading**: Preloads all sprites and audio clips asynchronously using `Resources.LoadAsync`, shows a progress bar, then transitions to Home.
- **Home**: Displays game logo, Play button, and a 10-level grid. Locked levels show orange buttons (non-interactable); unlocked levels show green. Uses `SaveService` to read `highestUnlockedLevel` from JSON.
- **Gameplay**: The core puzzle. On level start, the `GameplayController` reads `PlayerPrefs` for the selected level number, creates a `LevelManager` (which owns `BoardLogic` + `RackLogic` + `GameState`), and hands control to the views.

### How Level Solvability is Ensured
The `LevelGenerator` class produces solvable layouts via a **triple-first construction** approach:

1. For each required triple, pick an icon ID and generate 3 matching tiles.
2. Distribute those 3 tiles across different layers at random grid positions, ensuring no two tiles occupy the same cell on the same layer.
3. Because every tile placed has at least 2 matching siblings elsewhere on the board (possibly on different layers), and the exposure rule only blocks tiles from higher layers' overlap, there is always a path to uncover and tap all 3 matching tiles — the player just needs to clear blocking tiles first.

This guarantees that every generated level has a valid solution from the start. No level can be generated with a triple where all 3 tiles are permanently blocked.

### Responsive Layout Strategy
- All three scenes use `Canvas Scaler` with `Scale With Screen Size` mode.
- Reference resolution: 1080 × 1920 (portrait 9:16).
- `matchWidthOrHeight = 0.5` balances scaling between width and height, so the same UI works in landscape (16:9) without elements being cut off.
- Board and rack containers use relative anchors (percentage of screen) rather than fixed pixel positions.

### Async Pattern
The project uses `async/await` with `System.Threading.Tasks` for all async operations:
- `Resources.LoadAsync` wrapped in `while (!req.isDone) await Task.Yield()` loops
- `SceneManager.LoadSceneAsync` with progress polling
- Animation routines (tile movement, match clear) use frame-by-frame `await Task.Yield()` loops

We opted for native `Task` over UniTask because: (a) avoiding third-party dependency for demo purposes, (b) Unity 6.0 has stable `Task` support, (c) the simplicity of the project doesn't require UniTask's allocation-free benefits.

### Animation Approach
All animations use manual interpolation within async methods rather than DOTween or AnimationCurves:
- Tile movement: Smooth step (smoothstep) interpolation from board position to rack slot
- Match clear: Scale up + fade out over `tileMatchClearDuration`
- Blocked tile shake: Sinusoidal oscillation with decaying amplitude

### Project Setup Automation
The `SceneGenerator.cs` Editor script provides menu items (`TileAdventure > Generate All Scenes`) that auto-create all three scenes with proper GameObjects, Canvas, UI hierarchy, and serialized field wiring. This eliminates manual scene configuration and ensures consistency.

## Trade-offs & Areas for Improvement

### Current Limitations
1. **Asset Loading**: Uses `Resources` folder for simplicity. A production build should use Addressables for better memory management and update support. The manifest includes Addressables package; migration is a planned next step.
2. **Tile Layout**: Boards use randomized grid placement. A hand-authored layout per level would provide more curated puzzle experiences.
3. **No UI Toolkit**: Uses uGUI (standard `UnityEngine.UI`). The test allows either; uGUI was chosen for familiarity and faster iteration.
4. **Animation System**: Manual async interpolation works but is verbose. DOTween or a dedicated tweening system would be more maintainable for complex sequences.
5. **Save Data**: JSON file with `JsonUtility`. Sufficient for this scope, but lacks encryption and cloud-sync capability.

### What I'd Improve with More Time
- **Addressables Migration**: Non-blocking asset loading with proper reference counting
- **DOTween Integration**: Cleaner animation code with sequencing and callbacks
- **Level Editor Tool**: A custom Unity Editor window for designing level layouts visually
- **Sound Mixer**: Unity Audio Mixer for proper volume control and effects
- **Tutorial Overlay**: Use the provided `hand.png` for a first-launch tutorial
- **Unit Tests**: `BoardLogic` and `RackLogic` are plain C# — they're ready for NUnit tests
- **Pooled Tile Views**: Object pooling for `TileView` instantiation to reduce GC pressure on mobile
