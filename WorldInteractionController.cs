using System;
using Godot;

namespace Wancraft;

public class WorldInteractionController
{
    private IntPtr _previouslyHitObjectReference;
    private Vector3 _previousNormal;
    private Vector3I _blockCoordinates;
    
    private Player _player;
    private RayCast3D _rayCast;
    private float _halvedBlockSize;
    
    public WorldInteractionController(Player player, RayCast3D rayCast, float blockSize)
    {
        _rayCast = rayCast;
        _player = player;
        _halvedBlockSize = blockSize / 2;
    }
    
    public void CheckForWorldInteraction()
    {
        if (Input.IsActionJustPressed("Mine") 
            && _rayCast.IsColliding()
            && _rayCast.GetCollider() is StaticBody3D)
        {
            /*var collisionPoint = _rayCast.GetCollisionPoint();
            var collisionNormal = _rayCast.GetCollisionNormal() * -_halvedBlockSize;

            _blockCoordinates.X = (int)float.Floor(collisionPoint.X + collisionNormal.X + _halvedBlockSize);
            _blockCoordinates.Y = (int)float.Floor(collisionPoint.Y + collisionNormal.Y + _halvedBlockSize);
            _blockCoordinates.Z = (int)float.Floor(collisionPoint.Z + collisionNormal.Z + _halvedBlockSize);
            */
            
            _player.EmitSignal(Player.SignalName.MineBlock, GetBlockCoordinates());
        }

        if (Input.IsActionJustPressed("PlaceBlock")
            && _rayCast.IsColliding()
            && _rayCast.GetCollider() is StaticBody3D)
        {
            _player.EmitSignal(Player.SignalName.BlockPlaced, GetBlockCoordinates(true));
        }
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