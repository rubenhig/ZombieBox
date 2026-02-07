using Godot;
using System;

/// <summary>
/// PlayerInput Component - Captures input from the local player.
/// Only runs on the client that owns this player.
/// Synchronized via MultiplayerSynchronizer to the server.
/// </summary>
public partial class PlayerInput : Node
{
    // Synchronized properties - exported for MultiplayerSynchronizer
    [Export]
    public Vector2 MoveVector { get; set; } = Vector2.Zero;

    [Export]
    public Vector2 AimDirection { get; set; } = Vector2.Right;

    [Export]
    public bool IsShooting { get; set; } = false;

    // Edge-detected actions (not synchronized, handled via RPC)
    [Export]
    public bool ShootJustPressed { get; set; } = false;

    [Export]
    public bool SwitchWeaponJustPressed { get; set; } = false;

    private Player _player;

    public override void _Ready()
    {
        _player = GetParent<Player>();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Only read input if this node belongs to the local player
        if (IsMultiplayerAuthority())
        {
            ReadInput();
        }
    }

    private void ReadInput()
    {
        // Read continuous input
        MoveVector = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        // Update aim direction when moving
        if (MoveVector != Vector2.Zero)
        {
            AimDirection = MoveVector;
        }

        IsShooting = Input.IsActionPressed("shoot");

        // Edge-detected actions - these trigger RPCs to server
        if (Input.IsActionJustPressed("shoot"))
        {
            ShootJustPressed = true;
            _player.TryShoot();
        }
        else
        {
            ShootJustPressed = false;
        }

        if (Input.IsActionJustPressed("switch_weapon"))
        {
            SwitchWeaponJustPressed = true;
            _player.TrySwitchWeapon();
        }
        else
        {
            SwitchWeaponJustPressed = false;
        }
    }
}