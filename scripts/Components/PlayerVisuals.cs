using Godot;
using System;

public partial class PlayerVisuals : Node
{
    [Export]
    public Player PlayerRef { get; set; }

    [Export]
    public Texture2D TexturePistol { get; set; }
    
    [Export]
    public Texture2D TextureMachineGun { get; set; }

    private Sprite2D _sprite;
    private PlayerInput _input;

    // Juice parameters
    private float _walkBobFrequency = 15.0f;
    private float _walkBobAmplitude = 0.1f; // Rotation angle in radians
    private float _timeAccumulator = 0.0f;

    public override void _Ready()
    {
        if (PlayerRef == null)
        {
            GD.PrintErr("PlayerVisuals: PlayerRef is not assigned!");
            return;
        }

        _sprite = PlayerRef.GetNode<Sprite2D>("Sprite2D");
        _input = PlayerRef.GetNode<PlayerInput>("PlayerInput");
    }

    public override void _Process(double delta)
    {
        if (PlayerRef == null) return;
        UpdateSpriteTexture();
        AnimateMovement(delta);
    }

    private void UpdateSpriteTexture()
    {
        // Simple state machine for textures based on current weapon
        switch (PlayerRef.CurrentWeapon)
        {
            case WeaponType.Pistol:
                if (_sprite.Texture != TexturePistol) 
                    _sprite.Texture = TexturePistol;
                break;
            case WeaponType.MachineGun:
                if (_sprite.Texture != TextureMachineGun) 
                    _sprite.Texture = TextureMachineGun;
                break;
        }
    }

    private void AnimateMovement(double delta)
    {
        // Procedural animation: simple rotation bobbing when moving
        if (_input.MoveVector.LengthSquared() > 0.1f)
        {
            _timeAccumulator += (float)delta * _walkBobFrequency;
            float bob = Mathf.Sin(_timeAccumulator) * _walkBobAmplitude;
            _sprite.Rotation = bob;
        }
        else
        {
            // Reset rotation smoothly when stopped
            _sprite.Rotation = Mathf.Lerp(_sprite.Rotation, 0, (float)delta * 10.0f);
            _timeAccumulator = 0; // Optional: reset phase
        }
    }
}
