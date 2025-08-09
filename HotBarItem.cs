using Godot;

namespace Wancraft;

public partial class HotBarItem : Control
{
    private BlockType _blockType;
    [Export] public int SlotNumber { get; set; }
    [Export] public TextureRect ItemTextureRect { get; set; }
    [Export] public Label ItemCountLabel { get; set; }

    [Export] public Label BlockTypeLabel { get; set; }

    public override void _Ready()
    {
        PlayerInventory.Instance.HotbarItemCountChanged += HotbarItemCountChanged;
        PlayerInventory.Instance.HotbarSlotEmpty += HotbarSlotEmpty;
        
        var currentItem = PlayerInventory.Instance.PeekHotbarSlot(SlotNumber);
        HotbarItemCountChanged(SlotNumber, currentItem);
    }

    private void HotbarSlotEmpty(int slotNumber)
    {
        if (SlotNumber != slotNumber)
            return;
        
        ItemCountLabel.Text = string.Empty;
        BlockTypeLabel.Text = string.Empty;
    }

    public override void _ExitTree()
    {
        PlayerInventory.Instance.HotbarItemCountChanged -= HotbarItemCountChanged;
    }

    private void HotbarItemCountChanged(int slotNumber, InventoryStack stack)
    {
        if (SlotNumber != slotNumber)
            return;

        if (stack.ToolType == ToolType.PickAxe)
        {
            BlockTypeLabel.Text = nameof(ToolType.PickAxe);
            ItemCountLabel.Text = string.Empty;
        }
        else if (stack.BlockType == BlockType.Air)
        {
            BlockTypeLabel.Text = string.Empty;
            BlockTypeLabel.Text = string.Empty;
        }
        else
        {
            BlockTypeLabel.Text = stack.BlockType.ToString();
            ItemCountLabel.Text = stack.Count.ToString();
        }
    }
}