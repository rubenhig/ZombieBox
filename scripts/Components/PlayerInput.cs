using Godot;
using System;

public partial class PlayerInput : Node
{
    [Export]
    public Vector2 MoveVector { get; set; } = Vector2.Zero;

    [Export]
    public Vector2 AimDirection { get; set; } = Vector2.Right;

    [Export]
    public bool IsShooting { get; set; } = false; // Kept for automatic weapons (state)

    private Player _player;

    public override void _Ready()
    {
        _player = GetParent<Player>();
    }

    public override void _Process(double delta)
    {
        // Only read input if this node belongs to the local player
        if (IsMultiplayerAuthority())
        {
            ReadInput();
        }
    }

    private void ReadInput()
    {
        MoveVector = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        
        if (MoveVector != Vector2.Zero)
        {
            AimDirection = MoveVector;
        }

        IsShooting = Input.IsActionPressed("shoot");
        
        // Direct Action Call (Command Pattern)
        if (Input.IsActionJustPressed("shoot"))
        {
            _player.TryShoot(); 
        }

        if (Input.IsActionJustPressed("switch_weapon"))
        {
            _player.TrySwitchWeapon();
        }
    }
}