using System;
using Godot;

namespace Wancraft;

public class WorldInteractionController
{
    private Vector3I _blockCoordinates;
    
    private Player _player;
    private RayCast3D _rayCast;
    private float _halvedBlockSize;
    private Node3D _blockMarker;
    private Vector3I _currentBlockMarkerCoordinates;
    
    private InteractionMode _placementMode;

    private enum InteractionMode
    {
        None,
        Mine,
        Place
    }
    
    public WorldInteractionController(Player player, RayCast3D rayCast, float blockSize, Node3D blockMarker)
    {
        _rayCast = rayCast;
        _player = player;
        _halvedBlockSize = blockSize / 2;
        _blockMarker = blockMarker;
    }
    
    public void CheckForWorldInteraction()
    {
        CheckToolbarInteraction();
        
        if (_rayCast.IsColliding() && _rayCast.GetCollider() is StaticBody3D)
        {
            Vector3I newCoords;
            
            if (_placementMode == InteractionMode.Place)
                newCoords = GetBlockCoordinates(adjacentBlock: true);
            else
                newCoords = GetBlockCoordinates();

            if (newCoords != _currentBlockMarkerCoordinates)
            {
                _currentBlockMarkerCoordinates = newCoords;
                _blockMarker.GlobalPosition = _currentBlockMarkerCoordinates;
                _blockMarker.Visible = true;
            }
        }
        else
        {
            _blockMarker.Visible = false;
        }
        
        if (Input.IsActionJustPressed("Mine") 
            && _rayCast.IsColliding()
            && _rayCast.GetCollider() is StaticBody3D)
        {
            if (_placementMode == InteractionMode.Place)
                _player.EmitSignal(Player.SignalName.BlockPlaced, GetBlockCoordinates(true));
            
            if (_placementMode == InteractionMode.Mine)
                _player.EmitSignal(Player.SignalName.MineBlock, GetBlockCoordinates());
        }
    }

    private void CheckToolbarInteraction()
    {
        if (Input.IsActionJustPressed("Hotbar1"))
            UpdateInteractionMode(PlayerInventory.Instance.SelectHotbarSlot(1));
        
        if (Input.IsActionJustPressed("Hotbar2"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(2));
        
        if (Input.IsActionJustPressed("Hotbar3"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(3));
        
        if (Input.IsActionJustPressed("Hotbar4"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(4));
        
        if (Input.IsActionJustPressed("Hotbar5"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(5));
        
        if (Input.IsActionJustPressed("Hotbar6"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(6));
        
        if (Input.IsActionJustPressed("Hotbar7"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(7));
        
        if (Input.IsActionJustPressed("Hotbar8"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(8));
        
        if (Input.IsActionJustPressed("Hotbar9"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(9));
        
        if (Input.IsActionJustPressed("Hotbar0"))
            UpdateInteractionMode( PlayerInventory.Instance.SelectHotbarSlot(0));
    }
    
    private void UpdateInteractionMode(InventoryStack stack)
    {
        if (stack == null)
        {
            _placementMode = InteractionMode.None;
            return;
        }

        if (stack.ToolType == ToolType.PickAxe)
        {
            _placementMode = InteractionMode.Mine;
            return;
        }

        if (stack.Count > 0 && stack.BlockType != BlockType.Air)
        {
            _placementMode = InteractionMode.Place;
            return;
        }
        
        _placementMode = InteractionMode.None;
    }

    private Vector3I GetBlockCoordinates(bool adjacentBlock = false)
    {
        var collisionPoint = _rayCast.GetCollisionPoint();
        var collisionNormal = _rayCast.GetCollisionNormal();

        if (adjacentBlock)
            collisionNormal *= _halvedBlockSize;
        else
            collisionNormal *= -_halvedBlockSize;
        
        _blockCoordinates.X = (int)float.Floor(collisionPoint.X + collisionNormal.X + _halvedBlockSize);
        _blockCoordinates.Y = (int)float.Floor(collisionPoint.Y + collisionNormal.Y + _halvedBlockSize);
        _blockCoordinates.Z = (int)float.Floor(collisionPoint.Z + collisionNormal.Z + _halvedBlockSize);

        return _blockCoordinates;
    }
}