using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export]
	public float Speed = 150.0f;

	[Export]
	public int Health { get; private set; } = 1;

	[Signal]
	public delegate void DiedEventHandler();

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

	public void TakeDamage(int amount)
	{
		// Authoritative check
		if (!Multiplayer.IsServer()) return;

		Health -= amount;
		if (Health <= 0)
		{
			Die();
		}
	}

	// Logic for dying is internal to the Entity, triggered by state change (Health <= 0)
	private void Die()
	{
		if (!Multiplayer.IsServer()) return;
		
		EmitSignal(SignalName.Died);
		QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		// Authoritative physics: only calculated on server
		if (!Multiplayer.IsServer()) return;
		
		if (_target == null || !IsInstanceValid(_target))
		{
			// Try to find a target if we don't have one
			_target = GetTree().GetFirstNodeInGroup("players") as CharacterBody2D;
			
			if (_target == null)
			{
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
		
		// Rotate towards movement
		if (newVelocity != Vector2.Zero)
		{
			Rotation = newVelocity.Angle();
		}

		// Send to avoidance system (this will trigger OnVelocityComputed)
		_navAgent.Velocity = newVelocity;
	}

	private void OnVelocityComputed(Vector2 safeVelocity)
	{
		// Safe velocity also only matters on server
		if (!Multiplayer.IsServer()) return;

		Velocity = safeVelocity;
		MoveAndSlide();
	}

	public void SetTarget(CharacterBody2D target)
	{
		_target = target;
	}

	private void _on_damage_area_body_entered(Node2D body)
	{
		if (!Multiplayer.IsServer()) return;

		if (body is Player player)
		{
			player.TakeDamage(1);
			Die(); // The enemy is destroyed after attacking
		}
	}
}
