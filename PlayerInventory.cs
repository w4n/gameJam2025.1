using System;
using Godot;

namespace Wancraft;

public sealed class PlayerInventory
{
    private static PlayerInventory _instance;
    private readonly InventoryStack[] _hotbarItems = new InventoryStack[10];
    private int _selectedHotbarSlot = -1;

    private PlayerInventory()
    {
        for (var i = 0; i < _hotbarItems.Length; i++)
            _hotbarItems[i] = new InventoryStack();

        _hotbarItems[1] = new InventoryStack { ToolType = ToolType.PickAxe };
    }

    public static PlayerInventory Instance
    {
        get { return _instance ??= new PlayerInventory(); }
    }

    public Action<int, InventoryStack> HotbarItemCountChanged { get; set; }

    public Action<int> HotbarSlotEmpty { get; set; }

    public bool TryAddBlock(BlockType blockType)
    {
        var blockAdded = false;

        GD.Print($"Try adding {blockType} to hotbar...");

        for (var slotNumber = 0; slotNumber < _hotbarItems.Length; slotNumber++)
        {
            if (_hotbarItems[slotNumber].BlockType == BlockType.Air &&
                _hotbarItems[slotNumber].ToolType == ToolType.None)
                _hotbarItems[slotNumber].BlockType = blockType;

            if (_hotbarItems[slotNumber].BlockType == blockType && _hotbarItems[slotNumber].Count < 64)
            {
                GD.Print($"Added {blockType} to hotbar slot {slotNumber}");
                _hotbarItems[slotNumber].Count++;
                blockAdded = true;

                HotbarItemCountChanged?.Invoke(slotNumber, _hotbarItems[slotNumber]);

                break;
            }
        }

        return blockAdded;
    }

    public bool TryGetBlock(out BlockType blockType)
    {
        var blockInInventory = false;
        blockType = BlockType.Air;

        if (_hotbarItems[_selectedHotbarSlot].BlockType != BlockType.Air && _hotbarItems[_selectedHotbarSlot].Count > 0)
        {
            blockType = _hotbarItems[_selectedHotbarSlot].BlockType;
            _hotbarItems[_selectedHotbarSlot].Count--;
            blockInInventory = true;

            if (_hotbarItems[_selectedHotbarSlot].Count == 0)
            {
                _hotbarItems[_selectedHotbarSlot].BlockType = BlockType.Air;
                HotbarSlotEmpty?.Invoke(_selectedHotbarSlot);
            }
            else
            {
                HotbarItemCountChanged?.Invoke(_selectedHotbarSlot, _hotbarItems[_selectedHotbarSlot]);
            }
        }

        return blockInInventory;
    }

    public InventoryStack PeekHotbarSlot(int slotNumber)
    {
        if (slotNumber < 0 || slotNumber >= _hotbarItems.Length)
            return null;

        return _hotbarItems[slotNumber];
    }

    public InventoryStack SelectHotbarSlot(int slotNumber)
    {
        if (slotNumber < 0 || slotNumber >= _hotbarItems.Length)
            _selectedHotbarSlot = -1;

        _selectedHotbarSlot = slotNumber;

        return _hotbarItems[_selectedHotbarSlot];
    }
}