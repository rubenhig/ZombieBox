using Godot;
using System;

public partial class Bullet : Area2D
{
    [Signal]
    public delegate void EnemyKilledEventHandler();

    [Export]
    public float Speed = 600.0f;

    [Export]
    public int Damage = 1;

    [Export]
    public double Lifetime = 3.0; // Seconds before auto-destroy

    private Vector2 _direction = Vector2.Zero;
    private bool _initialized = false;

    public override void _Ready()
    {
        // Server: Always initialized immediately
        // Client: Wait for sync data to prevent (0,0) glitch
        if (Multiplayer.IsServer())
        {
            _initialized = true;
        }
        else
        {
            // Start invisible and wait for position data
            Visible = false;
            SetPhysicsProcess(true); // Ensure process runs to check for data
        }
    }

    public void SetDirection(Vector2 direction)
    {
        _direction = direction.Normalized();
        Rotation = _direction.Angle();
        // If set manually (Server), we are initialized
        _initialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        // --- Client Initialization Logic ---
        if (!_initialized)
        {
            // Check if we have received valid data from server
            // Assumption: (0,0) with Rotation 0 is the uninitialized state.
            // It's rare for a valid bullet to be exactly at 0,0 with 0 rotation.
            if (GlobalPosition.IsZeroApprox() && Mathf.IsZeroApprox(Rotation))
            {
                return; // Still waiting for sync...
            }

            // Data arrived! Initialize.
            _initialized = true;
            Visible = true;

            // Client-side Prediction: Start moving based on the synced rotation
            _direction = Vector2.FromAngle(Rotation);
        }

        // --- Movement Logic ---
        if (_direction != Vector2.Zero)
        {
            Position += _direction * Speed * (float)delta;
        }

        // --- Server Lifecycle Logic ---
        if (Multiplayer.IsServer())
        {
            Lifetime -= delta;
            if (Lifetime <= 0)
            {
                QueueFree();
            }
        }
    }

    // We remove the dependency on _on_screen_exited for game logic reliability
    private void _on_screen_exited()
    {
        // Optional optimization
    }

    private void _on_body_entered(Node2D body)
    {
        // Only server processes hit logic and destruction
        if (!Multiplayer.IsServer()) return;

        if (body.IsInGroup("enemies"))
        {
            // Notify shooter (Player) that we hit something
            EmitSignal(SignalName.EnemyKilled);

            // Apply damage using Domain Interface
            if (body is IDamageable target)
            {
                target.TakeDamage(Damage);
            }

            QueueFree();
        }
    }
}
