using Godot;
using System;

public partial class Player : CharacterBody3D
{
    [Export] public int Speed { get; set; } = 14;
    [Export] public int FallAcceleration { get; set; } = 75;
    [Export] public float JumpForce { get; set; } = 4.5f;
    
    private Vector3 _targetVelocity = Vector3.Zero;
    const float _sensitivity = 0.005f;
    
    [Export] public Marker3D HeadPivot { get; set; }
    [Export] public Camera3D Camera { get; set; }
    
    private bool _playerControllerDisabled;
    
    public override void _Ready()
    {
        Input.SetMouseMode(Input.MouseModeEnum.Captured);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_playerControllerDisabled) return;
        
        if (@event is InputEventMouseMotion mouseMotion)
        {
            HeadPivot.RotateY(-mouseMotion.Relative.X * _sensitivity);
            Camera.RotateX(-mouseMotion.Relative.Y * _sensitivity);
            
            
            Camera.Rotation = new Vector3(float.Clamp(Camera.Rotation.X, float.DegreesToRadians(-60), float.DegreesToRadians(60)), Camera.Rotation.Y, Camera.Rotation.Z);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("Enter")) _playerControllerDisabled = false;
        
        if (_playerControllerDisabled) return;
        
        if (Input.IsActionPressed("Exit"))
        {
            _playerControllerDisabled = true;
            Input.SetMouseMode(Input.MouseModeEnum.Visible);
        }
        
        if (!IsOnFloor())
            _targetVelocity.Y -= 9.8f * (float)delta;
        
        if (Input.IsActionJustPressed("Jump") && IsOnFloor())
            _targetVelocity.Y = JumpForce;
        
        var inputDirection = Input.GetVector("MoveLeft", "MoveRight", "MoveForward", "MoveBack" );
        var directionVector = (HeadPivot.Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y)).Normalized();

        _targetVelocity.X = directionVector.X * Speed;
        _targetVelocity.Z = directionVector.Z * Speed;
        
        Velocity = _targetVelocity;
        MoveAndSlide();
    }
}


