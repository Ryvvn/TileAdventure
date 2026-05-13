# Unity Developer Test: Tile Trip Match

## Introduction
This is a technical assessment for Junior Unity Developers. You will recreate a simplified version of the *Tile Adventure* mobile game using **Unity 6.0** and **C# 9**.

The game is a tile-matching puzzle where players tap exposed tiles on a layered board to move them into a rack at the bottom. When three tiles of the same icon are collected in the rack, they are cleared (matched). The goal is to clear a target number of triples before the rack overflows.

* **Reference Game:** [Play the published version for gameplay reference](https://www.facebook.com/gaming/play/1387488059031932)

---

## Scope & Scene Flow
You are expected to implement a scene-based flow: 
**Loading Scene** ➔ **Home Scene** ➔ **Gameplay Scene**

* **Loading Scene:** A loading screen that loads all required assets asynchronously, then transitions to Home.
* **Home Scene:** A simple menu screen with the game logo, a "Play" button, and a level selector showing levels 1–10.
* **Gameplay Scene:** The core tile-matching gameplay.

### Level Progression
You must implement level progression for the first 10 levels. Each level should increase in difficulty (your choice on how—e.g., more layers, more tile types, fewer rack slots, tighter time limit).

---

## Gameplay Mechanics

### Board & Tiles
* The board contains tiles arranged in multiple layers (stacked on top of each other).
* A tile is **exposed** (tappable) only when no tile from a higher layer overlaps it.
* There are **14 different tile icons** available.
* The player **wins** when they clear a target number of triples (e.g., 4 triples for a level).
* The player **loses** if the rack overflows (all slots filled with no match possible).

### Rack Behavior
* Tapping an exposed tile moves it to the rack (a row of slots at the bottom of the screen).
* The rack has a **fixed number of slots** (e.g., 7).
* Tiles in the rack are grouped by icon type. A newly placed tile will automatically slide next to matching tiles already in the rack.
* **Matching:** When 3 tiles of the same icon are adjacent in the rack, it triggers a "clear" animation and removes those 3 tiles.

### Visual Requirements
* **Tile Styling:** Use the provided `tile-base.png` as the tile background, with the icon rendered on top.
* **Depth/Layering:** Blocked (non-exposed) tiles should appear visually darker or dimmer to indicate they cannot be tapped.
* **Animations:** * Tile movement from the board to the rack must be animated (tweened).
  * Matched triples should have a brief clear animation/effect.

---

## Technical Specifications

| Requirement | Detail |
| :--- | :--- |
| **Unity Version** | Unity 6.0 (6000.x) |
| **Language** | C# 9 |
| **Render Pipeline**| URP 2D |
| **Asset Loading** | Use Addressables or Unity's async asset loading APIs |
| **UI Framework** | UI Toolkit or Unity UI (uGUI) — your choice |
| **Level Progress** | 10 levels with persistent progress (PlayerPrefs or JSON) |
| **Audio** | Background music + tap/match SFX using the provided audio files |
| **Responsive Design**| Support both portrait (9:16) and landscape (16:9) aspect ratios |

### Code Quality Expectations
* **Architecture:** Clear separation of concerns (MVC, MVP, or similar pattern).
* **Class Design:** Avoid `MonoBehaviour` for pure data/logic classes. Use plain C# classes or `ScriptableObject`s where appropriate.
* **Asynchronous Operations:** Use `async / await` (UniTask or native) for async operations instead of coroutines.
* **Naming Conventions:** Use descriptive naming (e.g., `isExposed`, `hasMatched`, `shouldAnimate`, `canTap`).
* **Constants:** No magic numbers. Use constants or config `ScriptableObject`s.

---

## Provided Assets
All assets are located in the `Assets/` folder of the provided package.

### Images
| Path | Description |
| :--- | :--- |
| `Images/Tiles/1.png` – `14.png` | 14 distinct tile icons |
| `Images/UI/tile-base.png` | Tile background/card shape |
| `Images/UI/background.png` | Game background |
| `Images/UI/game_logo.png` | Game logo for Home screen |
| `Images/UI/hand.png` | Tutorial hand pointer |
| `Images/UI/btn_green.png` | Green button background |
| `Images/UI/btn_orange.png` | Orange button background |
| `Images/UI/failed.png` | Game over icon |

### Audio
| Path | Description |
| :--- | :--- |
| `Audio/bg_music.ogg` | Background music (looping) |
| `Audio/tap.ogg` | Tile tap SFX |
| `Audio/match.ogg` | Triple match SFX |

---

## What You Must Design Yourself

* **Level Layouts:** How tiles are arranged on the board for each of the 10 levels (positions, layers, icon distribution).
* **Board Generation Logic:** Ensure every generated level is solvable.
* **Difficulty Progression:** How levels get harder (more tiles, more layers, fewer rack slots, more icon variety, etc.).
* **Game State Management:** How you track board state, rack state, score, and win/lose conditions.
* **Animations & Juice:** Tile movement, match effects, UI transitions.

---

## Deliverables & Evaluation

### Deliverables
1. A **Unity project (zipped)** that opens and runs in Unity 6.0 without errors.
2. All **10 levels** playable from start to finish.
3. A brief **`DESIGN.md`** file in the project root explaining:
   * Your architecture decisions.
   * How you structured level data.
   * How you ensured level solvability.
   * Any trade-offs or areas you would improve with more time.

### Evaluation Criteria
| Weight | Category | What We Look For |
| :--- | :--- | :--- |
| **High** | **Functionality** | All scenes work, levels are playable, win/lose conditions function properly. |
| **High** | **Code Quality** | Clean architecture, proper C# patterns, descriptive naming, no code smells. |
| **Medium** | **Technical Choices** | Appropriate use of Unity APIs, async patterns, and data persistence. |
| **Medium** | **Visual Polish** | Smooth animations, transitions, and responsive layout across aspect ratios. |
| **Low** | **Level Design** | 10 distinct levels with clear, sensible difficulty progression. |

> **Time Expectation:** You have **3 calendar days** to complete this test. Focus on clean, working code over feature completeness. *A polished 7 levels is better than a buggy 10.*
