using Godot;
using System;

/// <summary>
/// PlayerController Component - Server-side physics and game logic.
/// Only exists on the server. Reads PlayerInput state and applies physics.
/// </summary>
public partial class PlayerController : Node
{
    private Player _player;
    private PlayerInput _input;
    private Timer _shootTimer;

    [Export]
    public float MachineGunFireRate { get; set; } = 5.0f;

    public override void _Ready()
    {
        // Server-only component: Remove on clients
        if (!NetworkUtils.IsServer())
        {
            QueueFree();
            return;
        }

        _player = GetParent<Player>();
        _input = _player.GetNode<PlayerInput>("PlayerInput");

        // Setup machine gun fire rate timer
        _shootTimer = new Timer();
        AddChild(_shootTimer);
        _shootTimer.WaitTime = 1.0f / MachineGunFireRate;
        _shootTimer.OneShot = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player.Health <= 0) return;

        HandleMovement();
        HandleShooting();
    }

    private void HandleMovement()
    {
        // Read synchronized input from PlayerInput component
        Vector2 direction = _input.MoveVector;

        if (direction != Vector2.Zero)
        {
            _player.Velocity = direction.Normalized() * _player.Speed;
            _player.Rotation = _input.AimDirection.Angle();
        }
        else
        {
            _player.Velocity = Vector2.Zero;
        }

        // Apply physics movement
        _player.MoveAndSlide();
    }

    private void HandleShooting()
    {
        // Machine Gun: Continuous fire based on synchronized state
        // Pistol: Single-shot handled via RPC in Player.cs
        if (_player.CurrentWeapon == WeaponType.MachineGun)
        {
            if (_input.IsShooting && _shootTimer.IsStopped())
            {
                _player.DoFire();
                _shootTimer.Start();
            }
        }
    }
}
