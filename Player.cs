using Godot;
using System;
using Wancraft;

namespace Wancraft;

public partial class Player : CharacterBody3D
{
    [Export] public int ChunkSize { get; set; } = 16;
    
    [Export] public int Speed { get; set; } = 14;
    [Export] public int FallAcceleration { get; set; } = 75;
    [Export] public float JumpForce { get; set; } = 4.5f;
    [Export] public Node3D BlockSelector { get; set; }
    
    private Vector3 _targetVelocity = Vector3.Zero;
    const float _sensitivity = 0.005f;
    
    [Export] public Marker3D HeadPivot { get; set; }
    [Export] public Camera3D Camera { get; set; }
    [Export] public RayCast3D RayCast { get; set; }
    
    /// <summary>
    ///     Is emitted when the player tries to mine a block.
    /// </summary>
    [Signal]
    public delegate void MineBlockEventHandler(Vector3I blockCoordinates);
    
    /// <summary>
    ///     Is emitted when the player tries to mine a block.
    /// </summary>
    [Signal]
    public delegate void BlockPlacedEventHandler(Vector3I blockCoordinates);
    
    [Signal]
    public delegate void PlayerEnteredNewChunkEventHandler(Vector2I chunkCoordinates);

    private WorldInteractionController _interactionController;
    
    private bool _playerControllerDisabled;
    private Vector2I _lastPlayerChunkPosition = Vector2I.Zero;
    
    public override void _Ready()
    {
        _interactionController = new WorldInteractionController(this, RayCast, 1.0f, BlockSelector);
        
        Input.SetMouseMode(Input.MouseModeEnum.Captured);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_playerControllerDisabled) return;
        
        if (@event is InputEventMouseMotion mouseMotion)
        {
            HeadPivot.RotateY(-mouseMotion.Relative.X * _sensitivity);
            Camera.RotateX(-mouseMotion.Relative.Y * _sensitivity);
            
            Camera.Rotation = new Vector3(float.Clamp(Camera.Rotation.X, float.DegreesToRadians(-80), float.DegreesToRadians(60)), Camera.Rotation.Y, Camera.Rotation.Z);
        }
    }

    private bool _gravityDisabled;
    
    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("Enter"))
        {
            _playerControllerDisabled = false;
            Input.SetMouseMode(Input.MouseModeEnum.Captured);
        }
        
        if (_playerControllerDisabled) return;
        
        if (Input.IsActionPressed("Exit"))
        {
            _playerControllerDisabled = true;
            Input.SetMouseMode(Input.MouseModeEnum.Visible);
        }

        if (Input.IsActionJustPressed("DisableGravity"))
        {
            _gravityDisabled = !_gravityDisabled;
        }
        
        _interactionController.CheckForWorldInteraction();
        
        if (!IsOnFloor())
            _targetVelocity.Y -= 9.8f * (float)delta;
        
        if (Input.IsActionJustPressed("Jump") && (IsOnFloor() || _gravityDisabled))
            _targetVelocity.Y = JumpForce;
        
        var inputDirection = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBack" );
        var directionVector = (HeadPivot.Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y)).Normalized();

        _targetVelocity.X = directionVector.X * Speed;
        _targetVelocity.Z = directionVector.Z * Speed;
        
        Velocity = _targetVelocity;
        MoveAndSlide();
        
        CheckChunkPosition();
    }
    
    
    private void CheckChunkPosition()
    {
        if ((int)Position.X / ChunkSize != _lastPlayerChunkPosition.X ||
            (int)Position.Z / ChunkSize != _lastPlayerChunkPosition.Y)
        {
            _lastPlayerChunkPosition.X = (int)Position.X / ChunkSize;
            _lastPlayerChunkPosition.Y = (int)Position.Z / ChunkSize;
            
            EmitSignal(SignalName.PlayerEnteredNewChunk, _lastPlayerChunkPosition);
        }
    }
}


