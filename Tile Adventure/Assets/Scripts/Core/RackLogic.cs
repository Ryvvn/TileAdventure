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

        public int AddTile(TileData tile)
        {
            var insertIndex = FindInsertIndex(tile.iconId);

            if (IsFull())
            {
                OnRackOverflow?.Invoke();
                return -1;
            }

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

            return insertIndex;
        }

        private int FindInsertIndex(int iconId)
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].IsEmpty) continue;
                if (_slots[i].tile.iconId == iconId) continue;
                if (_slots[i].tile.iconId > iconId)
                {
                    int insertBefore = i;
                    while (insertBefore > 0 && _slots[insertBefore - 1].IsEmpty)
                        insertBefore--;
                    return i;
                }
            }

            for (int i = _slotCount - 1; i >= 0; i--)
            {
                if (_slots[i].IsEmpty) return i;
            }

            return _slotCount - 1;
        }

        private void ShiftSlots(int fromEmpty, int toInsert)
        {
            int direction = fromEmpty < toInsert ? 1 : -1;
            int i = fromEmpty;
            while (i != toInsert)
            {
                _slots[i].tile = _slots[i + direction].tile;
                i += direction;
            }
            _slots[toInsert].tile = null;
        }

        public void CheckMatch(int changedIndex)
        {
            var matches = FindMatches();
            if (matches.Count > 0)
            {
                var iconId = matches[0];
                int first = -1, last = -1;
                for (int i = 0; i < _slotCount; i++)
                {
                    if (!_slots[i].IsEmpty && _slots[i].tile.iconId == iconId)
                    {
                        if (first < 0) first = i;
                        last = i;
                    }
                }
                RemoveMatched(first, last, iconId);
                CompactRack();
                OnMatchCleared?.Invoke(first, last, iconId);

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

        private List<int> FindMatches()
        {
            var result = new List<int>();
            int runStart = -1;
            int runIcon = -1;
            int runLength = 0;

            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    runStart = -1;
                    runLength = 0;
                    continue;
                }

                if (_slots[i].tile.iconId == runIcon && runStart >= 0)
                {
                    runLength++;
                }
                else
                {
                    runStart = i;
                    runIcon = _slots[i].tile.iconId;
                    runLength = 1;
                }

                if (runLength >= _constants.matchCount)
                {
                    result.Add(runIcon);
                }
            }

            return result;
        }

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

        private void CompactRack()
        {
            var remaining = new List<TileData>();
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty) remaining.Add(slot.tile);
            }

            for (int i = 0; i < _slotCount; i++)
            {
                _slots[i].tile = i < remaining.Count ? remaining[i] : null;
            }
        }

        public bool IsFull()
        {
            int occupied = 0;
            foreach (var slot in _slots)
                if (!slot.IsEmpty) occupied++;
            return occupied >= _slotCount;
        }

        public int GetOccupiedCount()
        {
            int count = 0;
            foreach (var slot in _slots)
                if (!slot.IsEmpty) count++;
            return count;
        }

        public bool WouldOverflowWithNext()
        {
            if (!IsFull()) return false;

            foreach (var slot in _slots)
            {
                if (slot.IsEmpty) return false;
            }

            var iconCounts = new Dictionary<int, int>();
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty) continue;
                if (!iconCounts.ContainsKey(slot.tile.iconId))
                    iconCounts[slot.tile.iconId] = 0;
                iconCounts[slot.tile.iconId]++;
            }

            foreach (var kvp in iconCounts)
            {
                if (kvp.Value >= _constants.matchCount - 1) return false;
            }

            return true;
        }

        public void Clear()
        {
            foreach (var slot in _slots)
            {
                slot.tile = null;
            }
        }
    }
}
