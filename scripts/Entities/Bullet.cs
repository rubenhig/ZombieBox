using Godot;
using System;

/// <summary>
/// Bullet entity - Server-authoritative projectile with simple straight-line movement.
/// Synchronized to clients via MultiplayerSpawner.
/// </summary>
public partial class Bullet : Area2D
{
    [Signal]
    public delegate void EnemyKilledEventHandler();

    [Export]
    public float Speed { get; set; } = 600.0f;

    [Export]
    public int Damage { get; set; } = 1;

    [Export]
    public double Lifetime { get; set; } = 3.0; // Seconds before auto-destroy

    private Vector2 _direction = Vector2.Zero;
    private bool _initialized = false;

    public override void _Ready()
    {
        // Server: Always initialized immediately
        // Client: Wait for sync data to prevent (0,0) spawn glitch
        if (NetworkUtils.IsServer())
        {
            _initialized = true;
        }
        else
        {
            // Start invisible and wait for position data from server
            Visible = false;
            SetPhysicsProcess(true);
        }
    }

    /// <summary>
    /// Sets the bullet's direction and rotation. Server-only.
    /// Called by SpawnSystem when spawning the bullet.
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        _direction = direction.Normalized();
        Rotation = _direction.Angle();
        _initialized = true;
    }

    /// <summary>
    /// Physics process - handles movement and lifetime.
    /// Movement runs on both server and clients (client-side prediction).
    /// Lifetime and collision logic only on server.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        // --- Client Initialization ---
        if (!_initialized)
        {
            // Wait for network sync - check if we received valid position/rotation
            if (GlobalPosition.IsZeroApprox() && Mathf.IsZeroApprox(Rotation))
            {
                return; // Still waiting for server data
            }

            // Data received! Initialize from synced state
            _initialized = true;
            Visible = true;

            // Client-side prediction: derive direction from synced rotation
            _direction = Vector2.FromAngle(Rotation);
        }

        // --- Movement (Both server and clients) ---
        if (_direction != Vector2.Zero)
        {
            Position += _direction * Speed * (float)delta;
        }

        // --- Server Lifecycle ---
        if (NetworkUtils.IsServer())
        {
            Lifetime -= delta;
            if (Lifetime <= 0)
            {
                QueueFree();
            }
        }
    }

    /// <summary>
    /// Called when bullet collides with something. Server-only.
    /// Applies damage to enemies and destroys the bullet.
    /// </summary>
    private void _on_body_entered(Node2D body)
    {
        // Only server processes collision logic
        if (!NetworkUtils.IsServer()) return;

        if (body.IsInGroup("enemies"))
        {
            // Notify shooter that we hit an enemy
            EmitSignal(SignalName.EnemyKilled);

            // Apply damage
            if (body is IDamageable target)
            {
                target.TakeDamage(Damage);
            }

            // Destroy bullet
            QueueFree();
        }
    }
}
