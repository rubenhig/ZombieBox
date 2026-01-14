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

	private Vector2 _direction = Vector2.Zero;

	public void SetDirection(Vector2 direction)
	{
		_direction = direction.Normalized();
		Rotation = _direction.Angle();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_direction != Vector2.Zero)
		{
			Position += _direction * Speed * (float)delta;
		}
	}

	private void _on_screen_exited()
	{
		QueueFree();
	}

	private void _on_body_entered(Node2D body)
	{
		if (body.IsInGroup("enemies"))
		{
			// Notify shooter (Player) that we hit something
			EmitSignal(SignalName.EnemyKilled); 
			
			// Apply damage using Domain Interface
			if (body is Enemy enemy)
			{
				enemy.TakeDamage(Damage);
			}
			
			QueueFree();
		}
	}
}
