# Edge Case Hunter Review

## Instructions
You are reviewing code for edge cases, boundary conditions, and unexpected state transitions.
You have read access to the project files. Focus on: null checks, array bounds, race conditions, state machine holes, input edge cases, memory/performance.

## Files to Review (Absolute Paths)

- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Core\TileData.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Core\BoardLogic.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Core\RackLogic.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Core\GameState.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Core\LevelManager.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Core\LevelGenerator.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Core\LevelDatabase.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Config\GameConstants.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Config\LevelConfig.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Gameplay\TileView.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Gameplay\BoardView.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Gameplay\RackView.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Gameplay\GameplayController.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Services\SaveService.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Services\SceneLoader.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Services\AudioManager.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\UI\HomeScreen.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\UI\LoadingScreen.cs`
- `d:\UnityForWork\Project\TileAdventure\Assets\Scripts\Editor\SceneGenerator.cs`

## Edge Case Categories to Hunt

### Board Logic
- What happens when all tiles are on the same layer?
- What happens when a tile's grid position is at the maximum int value?
- Board with 0 tiles — does initialization handle it?
- Layer with all tiles blocked by higher layer — is exposure correctly determined?
- Tile removal when the tile was already removed?
- GenerateBoard called twice without clearing?

### Rack Logic
- Rack with 0 slots?
- Adding a tile when rack is full but a match is possible — does WouldOverflowWithNext() correctly predict?
- Removing matched tiles when they straddle the boundary of the slot array?
- Match-3 detection when all 7 slots contain the same icon (chain of 7)?
- CompactRack after clearing all tiles?
- FindInsertIndex when rack is completely empty?
- FindInsertIndex when all icons are higher than the new icon?

### Input
- Tap on a removed tile's view?
- Tap during animation (isMoving flag)?
- Very rapid taps?
- Tap on rack area?
- Tap on empty board area (no tile)?
- Tap during win/lose popup shown?

### Save Data
- Save file with truncated JSON?
- Save file with negative highestUnlockedLevel?
- Save file from a future version with unknown fields?
- Application.persistentDataPath unavailable (permission denied)?
- Concurrent save while loading?

### Scene/State
- Restart called during tile animation?
- GoHome called during match-clear animation?
- Scene loaded but LevelManager was never initialized?
- GameplayController.Update() called after Dispose()?
- OnDestroy called during an async animation loop?

### Resource Loading
- Sprite at path "Images/Tiles/1" not found?
- Resources.Load returns null for a tile icon?
- Loading screen completes but Home scene doesn't exist?
