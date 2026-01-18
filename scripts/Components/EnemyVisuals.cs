using Godot;
using System;

public partial class EnemyVisuals : Node
{
    [Export]
    public Enemy EnemyRef { get; set; }

    private Sprite2D _sprite;

    // Juice parameters
    private float _walkBobFrequency = 12.0f;
    private float _walkBobAmplitude = 0.15f; 
    private float _timeAccumulator = 0.0f;

    public override void _Ready()
    {
        if (EnemyRef == null)
        {
            GD.PrintErr("EnemyVisuals: EnemyRef is not assigned!");
            return;
        }

        _sprite = EnemyRef.GetNode<Sprite2D>("Sprite2D");
        
        // Randomize phase so zombies don't walk in sync
        _timeAccumulator = GD.Randf() * 10.0f;
    }

    public override void _Process(double delta)
    {
        if (EnemyRef == null) return;
        AnimateMovement(delta);
    }

    private void AnimateMovement(double delta)
    {
        // Zombies are always "moving" towards target if they have one
        if (EnemyRef.Velocity.LengthSquared() > 0.1f)
        {
            _timeAccumulator += (float)delta * _walkBobFrequency;
            float bob = Mathf.Sin(_timeAccumulator) * _walkBobAmplitude;
            _sprite.Rotation = bob;
            
            // Add a slight scale squash/stretch for extra juice
            float stretch = 1.0f + Mathf.Sin(_timeAccumulator * 2.0f) * 0.05f;
            _sprite.Scale = new Vector2(stretch, 2.0f - stretch);
        }
        else
        {
            _sprite.Rotation = Mathf.Lerp(_sprite.Rotation, 0, (float)delta * 5.0f);
            _sprite.Scale = _sprite.Scale.Lerp(Vector2.One, (float)delta * 5.0f);
        }
    }
}
