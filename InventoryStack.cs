namespace Wancraft;

public record InventoryStack
{
    public ToolType ToolType { get; set; } = ToolType.None;
    public BlockType BlockType { get; set; } = BlockType.Air;

    public int Count { get; set; }
}