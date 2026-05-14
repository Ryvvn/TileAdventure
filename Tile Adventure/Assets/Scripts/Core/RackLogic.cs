using System;
using System.Collections.Generic;
using TileAdventure.Config;

namespace TileAdventure.Core
{
    public class RackSlot
    {
        public int slotIndex;
        public TileData tile;

        public bool IsEmpty => tile == null;
    }

    public class RackLogic
    {
        private readonly GameConstants _constants;
        private List<RackSlot> _slots;
        private int _slotCount;

        public IReadOnlyList<RackSlot> Slots => _slots;
        public event Action<int, int, int> OnMatchCleared;
        public event Action OnRackOverflow;
        public event Action<int, TileData> OnTileAdded;
        public event Action<int, TileData> OnTileShift;

        public RackLogic(GameConstants constants, int slotCount = -1)
        {
            _constants = constants;
            _slotCount = slotCount > 0 ? slotCount : constants.rackSlotCount;
            _slots = new List<RackSlot>(_slotCount);
            for (int i = 0; i < _slotCount; i++)
            {
                _slots.Add(new RackSlot { slotIndex = i });
            }
        }

        /// <summary>
        /// Add a tile to the rack. Finds the correct sorted position (grouped by icon),
        /// shifts existing tiles to make room, inserts the tile, then checks for a match-3.
        /// Returns the slot index where the tile was placed, or -1 if the rack overflows.
        /// 
        /// Example: rack [1,2,3] + add icon 2 → shift 3 right → [1,2,2,3]
        /// Example: rack [1,2,3] + add icon 1 → shift 2,3 right → [1,1,2,3]
        /// </summary>
        public int AddTile(TileData tile)
        {
            int insertIndex = FindInsertIndex(tile.iconId);

            int emptyIndex = -1;
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex < 0) return -1;

            if (insertIndex != emptyIndex)
            {
                ShiftSlots(emptyIndex, insertIndex);
            }

            _slots[insertIndex].tile = tile;
            tile.isMoving = true;
            OnTileAdded?.Invoke(insertIndex, tile);

            CheckMatch(insertIndex);

            if (IsFull())
            {
                OnRackOverflow?.Invoke();
            }

            return insertIndex;
        }

        /// <summary>
        /// Find the target slot index for a tile with the given iconId.
        /// The rack is kept sorted by icon ID, with same-icon tiles grouped adjacently.
        /// A new tile goes:
        ///   - After the last existing tile with the same icon (grouping)
        ///   - Before the first tile with a strictly higher icon (sort order)
        ///   - At the first empty slot if all icons are lower
        /// 
        /// Example: rack [1, 1, 3, 4, _, _, _]
        ///          iconId=1 → returns 2 (after last 1, before 3)
        ///          iconId=2 → returns 2 (before 3, which is first higher)
        ///          iconId=5 → returns 4 (first empty slot)
        /// </summary>
        private int FindInsertIndex(int iconId)
        {
            int lastMatchingSlot = -1;   // rightmost slot containing the SAME icon
            int firstHigherSlot = -1;    // leftmost slot containing a HIGHER icon

            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].IsEmpty) continue;

                if (_slots[i].tile.iconId == iconId)
                {
                    lastMatchingSlot = i;  // keep updating — we want the LAST one
                }
                else if (_slots[i].tile.iconId > iconId && firstHigherSlot < 0)
                {
                    firstHigherSlot = i;   // only the FIRST higher icon matters
                }
            }

            // If we have matching tiles, insert right after the last one to keep them grouped
            if (lastMatchingSlot >= 0)
                return lastMatchingSlot + 1;

            // No matching icon — insert before the first higher icon to maintain sort order
            if (firstHigherSlot >= 0)
                return firstHigherSlot;

            // All existing icons are lower — insert at the first empty slot (leftmost)
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].IsEmpty) return i;
            }

            return _slotCount - 1;
        }

        /// <summary>
        /// Shift tiles between the empty slot and the target insertion slot
        /// by one position toward the empty slot, opening up space at the target.
        /// 
        /// The empty slot always sits at the right edge of the occupied block (leftmost gap).
        /// Direction depends on relative positions:
        ///   empty &lt; target: tiles slide LEFT into the empty, target opens up
        ///   empty &gt; target: tiles slide RIGHT away from target, target opens up
        /// 
        /// Example: slots [1,2,_,3,4], empty=2, target=4
        ///          → direction=1, copy 4→3→2 → [1,2,3,4,_] → target 4 is now empty ✓
        /// Example: slots [1,2,3,_,_], empty=3, target=1
        ///          → direction=-1, copy 1→2→3 → [_,1,2,3,_] → target 1 is now empty ✓
        /// </summary>
        private void ShiftSlots(int fromEmpty, int toInsert)
        {
            // Determine which way tiles need to slide.
            // direction = 1 means we copy data from right-to-left (shifting tiles leftward).
            // direction = -1 means we copy data from left-to-right (shifting tiles rightward).
            int direction = fromEmpty < toInsert ? 1 : -1;

            // Walk from the empty slot toward the target, copying each adjacent tile
            // one step toward the empty slot. This creates a cascade effect.
            int i = fromEmpty;
            while (i != toInsert)
            {
                int sourceIdx = i + direction;
                _slots[i].tile = _slots[sourceIdx].tile;
                OnTileShift?.Invoke(i, _slots[i].tile);
                i += direction;
            }

            // The target slot is now empty and ready for the new tile.
            _slots[toInsert].tile = null;
        }

        /// <summary>
        /// Scan all rack slots for runs of matchCount identical consecutive icons.
        /// Clears any matched groups and compacts the rack afterward.
        /// Recursively re-checks after compacting (chain reactions).
        /// </summary>
        public void CheckMatch(int changedIndex)
        {
            var matches = FindMatches();
            if (matches.Count > 0)
            {
                // Take the first matched icon ID found
                var iconId = matches[0];

                // Find the first and last slot index for this icon in the rack
                int first = -1, last = -1;
                for (int i = 0; i < _slotCount; i++)
                {
                    if (!_slots[i].IsEmpty && _slots[i].tile.iconId == iconId)
                    {
                        if (first < 0) first = i;
                        last = i;
                    }
                }

                // Clear the matched tiles and compact
                RemoveMatched(first, last, iconId);
                CompactRack();
                OnMatchCleared?.Invoke(first, last, iconId);

                // After compacting, new runs may have formed — check again
                if (GetOccupiedCount() > 0)
                {
                    for (int i = 0; i < _slotCount; i++)
                    {
                        if (!_slots[i].IsEmpty)
                        {
                            CheckMatch(i);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Scan the rack left-to-right for consecutive runs of identical icons.
        /// A run of length >= matchCount (default 3) is a match.
        /// Returns a list of icon IDs that have qualifying runs.
        /// 
        /// Example: rack [1,1,2,2,2,3,3] → returns [2] (run of three 2s)
        ///          rack [1,1,1,2,2,2]   → returns [1,2] (both have runs of 3)
        /// </summary>
        private List<int> FindMatches()
        {
            var result = new List<int>();
            int runStart = -1;    // first slot index of the current run
            int runIcon = -1;     // icon ID of the current run
            int runLength = 0;    // how many consecutive tiles in the current run

            for (int i = 0; i < _slotCount; i++)
            {
                // Empty slot breaks any ongoing run
                if (_slots[i].IsEmpty)
                {
                    runStart = -1;
                    runLength = 0;
                    continue;
                }

                // Same icon as the current run: extend the run
                if (_slots[i].tile.iconId == runIcon && runStart >= 0)
                {
                    runLength++;
                }
                // Different icon: start a new run
                else
                {
                    runStart = i;
                    runIcon = _slots[i].tile.iconId;
                    runLength = 1;
                }

                // Check if current run has reached the match threshold
                if (runLength >= _constants.matchCount)
                {
                    // Avoid duplicate entries for the same icon
                    if (!result.Contains(runIcon))
                        result.Add(runIcon);
                }
            }

            return result;
        }

        /// <summary>
        /// Clear tiles between first and last (inclusive) that match the given iconId.
        /// Only removes tiles with the exact iconId within the range.
        /// </summary>
        private void RemoveMatched(int first, int last, int iconId)
        {
            for (int i = first; i <= last; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].tile.iconId == iconId)
                {
                    _slots[i].tile = null;
                }
            }
        }

        /// <summary>
        /// Remove all empty gaps by shifting remaining tiles to the left.
        /// After compacting, all occupied slots are at indices [0..GetOccupiedCount()-1].
        /// 
        /// Example: [1,_,2,_,_,3] → [1,2,3,_,_,_]
        /// </summary>
        private void CompactRack()
        {
            // Collect all non-empty tiles in order
            var remaining = new List<TileData>();
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty) remaining.Add(slot.tile);
            }

            // Rewrite the slot array: remaining tiles go at the front, rest become empty
            for (int i = 0; i < _slotCount; i++)
            {
                _slots[i].tile = i < remaining.Count ? remaining[i] : null;
            }
        }

        /// <summary>
        /// Returns true when every rack slot is occupied.
        /// </summary>
        public bool IsFull()
        {
            int occupied = 0;
            foreach (var slot in _slots)
                if (!slot.IsEmpty) occupied++;
            return occupied >= _slotCount;
        }

        /// <summary>
        /// How many tiles are currently in the rack.
        /// </summary>
        public int GetOccupiedCount()
        {
            int count = 0;
            foreach (var slot in _slots)
                if (!slot.IsEmpty) count++;
            return count;
        }

        /// <summary>
        /// Checks if adding one more tile would cause an immediate loss.
        /// The rack is full AND no existing icon has 2+ tiles (which would trigger a match on the 3rd).
        /// If any icon already has matchCount-1 tiles, adding that same icon creates a match, avoiding overflow.
        /// </summary>
        public bool WouldOverflowWithNext()
        {
            // If not full yet, there's room — no overflow
            if (!IsFull()) return false;

            // Double-check: is there any empty slot? (shouldn't happen if IsFull is correct)
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty) return false;
            }

            // Count occurrences of each icon in the rack
            var iconCounts = new Dictionary<int, int>();
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty) continue;
                if (!iconCounts.ContainsKey(slot.tile.iconId))
                    iconCounts[slot.tile.iconId] = 0;
                iconCounts[slot.tile.iconId]++;
            }

            // If any icon has matchCount-1 tiles, adding one more creates a match → no overflow
            foreach (var kvp in iconCounts)
            {
                if (kvp.Value >= _constants.matchCount - 1) return false;
            }

            // Rack is full, no pending match possible → overflow
            return true;
        }

        /// <summary>
        /// Remove all tiles from the rack.
        /// </summary>
        public void Clear()
        {
            foreach (var slot in _slots)
            {
                slot.tile = null;
            }
        }
    }
}
