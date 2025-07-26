using Godot;

namespace wancraft;

public partial class Block : Node3D
{
    public Main BlockController { get; set; }
    
    public void OnClick()
    {
        if (BlockController != null)
            BlockController.ShowBlockInfo(this.Position);
    }

    public void OnStaticBody3dInputEvent(Node camera, InputEvent inputEvent, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        

        if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            GD.Print("OnStaticBody3dInputEvent");

            if (BlockController != null)
            {
                GD.Print("BlockController not null");
                BlockController.ShowBlockInfo(this.Position);    
            }
            else
            {
                GD.Print("BlockController null");
                
            }
        }
    }
}