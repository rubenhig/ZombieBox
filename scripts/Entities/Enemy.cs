using Godot;
using System;

/// <summary>
/// Enemy entity - Server-authoritative zombie with simple chase AI.
/// Implements IDamageable for damage handling.
/// </summary>
public partial class Enemy : CharacterBody2D, IDamageable
{
    [Export]
    public float Speed { get; set; } = 150.0f;

    [Export]
    public int Health { get; private set; } = 1;

    // IDamageable Implementation
    public event Action Died;

    // Godot signal version of Died (for editor connections)
    [Signal]
    public delegate void DiedSignalEventHandler();

    private CharacterBody2D _target;
    private NavigationAgent2D _navAgent;

    public override void _Ready()
    {
        _navAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");

        // Connect avoidance callback
        _navAgent.VelocityComputed += OnVelocityComputed;

        // Default behavior: find nearest player
        _target = GetTree().GetFirstNodeInGroup("players") as CharacterBody2D;
    }

    /// <summary>
    /// Applies damage to the enemy. Server-only.
    /// Implements IDamageable interface.
    /// </summary>
    public void TakeDamage(int amount)
    {
        // Server authority check
        if (!NetworkUtils.IsServer()) return;

        Health -= amount;
        if (Health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles enemy death. Server-only.
    /// Emits Died signal for systems (like WaveSystem) to listen to.
    /// </summary>
    private void Die()
    {
        if (!NetworkUtils.IsServer()) return;

        // Emit signals for systems to react
        EmitSignal(SignalName.DiedSignal);
        Died?.Invoke();

        // Remove from game
        QueueFree();
    }

    /// <summary>
    /// Server-side physics process - handles AI and movement.
    /// Simple chase AI: finds nearest player and navigates towards them.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        // Server authority - only server calculates AI and physics
        if (!NetworkUtils.IsServer()) return;

        // Find target if we don't have one
        if (_target == null || !IsInstanceValid(_target))
        {
            _target = GetTree().GetFirstNodeInGroup("players") as CharacterBody2D;

            if (_target == null)
            {
                // No target found, stop moving
                Velocity = Vector2.Zero;
                MoveAndSlide();
                return;
            }
        }

        // Update navigation target
        _navAgent.TargetPosition = _target.GlobalPosition;

        // Get next point in path
        Vector2 nextPathPosition = _navAgent.GetNextPathPosition();
        Vector2 currentAgentPosition = GlobalPosition;

        // Calculate desired velocity
        Vector2 newVelocity = (nextPathPosition - currentAgentPosition).Normalized() * Speed;

        // Rotate towards movement direction
        if (newVelocity != Vector2.Zero)
        {
            Rotation = newVelocity.Angle();
        }

        // Send to avoidance system (triggers OnVelocityComputed callback)
        _navAgent.Velocity = newVelocity;
    }

    /// <summary>
    /// Callback from NavigationAgent2D with collision-avoided velocity.
    /// </summary>
    private void OnVelocityComputed(Vector2 safeVelocity)
    {
        if (!NetworkUtils.IsServer()) return;

        Velocity = safeVelocity;
        MoveAndSlide();
    }

    /// <summary>
    /// Manually set the target for this enemy to chase.
    /// Useful for directing enemies to specific players.
    /// </summary>
    public void SetTarget(CharacterBody2D target)
    {
        _target = target;
    }

    /// <summary>
    /// Called when enemy's damage area collides with a player.
    /// Deals damage to the player and destroys the enemy.
    /// </summary>
    private void _on_damage_area_body_entered(Node2D body)
    {
        if (!NetworkUtils.IsServer()) return;

        if (body is IDamageable target)
        {
            target.TakeDamage(1);
            Die(); // Zombie attacks once then dies
        }
    }
}
