using Godot;
using System;

public partial class PlayerInput : Node
{
    [Export]
    public Vector2 MoveVector { get; set; } = Vector2.Zero;

    [Export]
    public Vector2 AimDirection { get; set; } = Vector2.Right;

    [Export]
    public bool IsShooting { get; set; } = false;

    [Export]
    public bool IsShootingJustPressed { get; set; } = false;

    [Export]
    public bool IsSwitchingWeapon { get; set; } = false;

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
        
        // In 2D, we can use the mouse position to determine aim direction
        Vector2 mousePos = GetViewport().GetMousePosition();
        // This will be refined once we know the player's position relative to the camera
        // For now, we use the last movement direction if mouse isn't used for aiming
        if (MoveVector != Vector2.Zero)
        {
            AimDirection = MoveVector;
        }

        IsShooting = Input.IsActionPressed("shoot");
        IsShootingJustPressed = Input.IsActionJustPressed("shoot");
        
        if (IsShootingJustPressed) GD.Print("PlayerInput: Shoot Just Pressed");

        IsSwitchingWeapon = Input.IsActionJustPressed("switch_weapon");
    }
}
