using Godot;
using System;

public enum WeaponType { Pistol, MachineGun }

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 300.0f;
    [Export] public PackedScene BulletScene { get; set; }

    [Signal]
    public delegate void HealthChangedEventHandler(int newHealth);
    [Signal]
    public delegate void EnemyKilledEventHandler(int newKills);
    [Signal]
    public delegate void DiedEventHandler();

    // State properties
    public int Health { get; private set; } = 3;
    public WeaponType CurrentWeapon { get; private set; } = WeaponType.Pistol;

    private int _kills = 0;

    public override void _Ready()
    {
        // Initial state sync
        EmitSignal(SignalName.HealthChanged, Health);
        EmitSignal(SignalName.EnemyKilled, _kills);
    }

    // --- State Modification Methods (Called by ServerController) ---

    public void SwitchWeapon()
    {
        CurrentWeapon = CurrentWeapon == WeaponType.Pistol ? WeaponType.MachineGun : WeaponType.Pistol;
        GD.Print($"{Name} switched to {CurrentWeapon}");
        // Here we could play a sound or animation
    }

    public void TakeDamage(int damage)
    {
        // Guard: Only allow server to call this (redundant if called by ServerController, but safe)
        if (!Multiplayer.IsServer()) return;

        if (Health <= 0) return;

        Health -= damage;
        GD.Print($"{Name} took damage. Health: {Health}");

        EmitSignal(SignalName.HealthChanged, Health);
        
        if (Health <= 0)
        {
            Die();
        }
    }

    // --- Event Handlers ---

    public void OnEnemyKilledByBullet()
    {
        // Logic might stay here or move to controller.
        // Keeping it here makes it easy to bind to the Bullet signal.
        if (!Multiplayer.IsServer()) return;

        _kills++;
        EmitSignal(SignalName.EnemyKilled, _kills);
    }

    private void Die()
    {
        GD.Print($"{Name} died!");
        
        // Disable visuals locally
        Hide();

        EmitSignal(SignalName.Died);

        // Notify GameManager (This part assumes GameManager exists)
        var gameManager = GetTree().Root.FindChild("GameManager", true, false) as GameManager;
        gameManager?.GameOver();
    }
}
