using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export]
	public float Speed = 150.0f;

	[Signal]
	public delegate void DiedEventHandler();

	private CharacterBody2D _target;

	public override void _Ready()
	{
		// Default behavior: find nearest player
		_target = GetTree().GetFirstNodeInGroup("players") as CharacterBody2D;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Only server should process physics in a fully authoritative model
		// For now, we keep it simple for local play
		if (_target != null && IsInstanceValid(_target))
		{
			Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
			Velocity = direction * Speed;
			Rotation = direction.Angle();
		}
		else
		{
			Velocity = Vector2.Zero;
		}

		MoveAndSlide();
	}

	public void SetTarget(CharacterBody2D target)
	{
		_target = target;
	}

	public void Die()
	{
		EmitSignal(SignalName.Died);
		QueueFree();
	}

	private void _on_damage_area_body_entered(Node2D body)
	{
		if (body is Player player)
		{
			player.TakeDamage(1);
			Die(); // The enemy is destroyed after attacking
		}
	}
}
