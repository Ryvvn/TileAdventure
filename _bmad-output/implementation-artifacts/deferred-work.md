## Deferred from: code review of spec-tile-adventure-game (2026-05-14)

- [x] `BuildBoard` cascade iterates all layers 0..maxLayers, causing unnecessary delays on empty layers — minor polish optimization [BoardView.cs — BuildBoard]
- [x] `OnPointerClick` hardcoded `0.3f` debounce (pre-existing, not in this diff) — pre-existing [TileView.cs — OnPointerClick]
