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
    public WeaponType CurrentWeapon { get; private set; } = WeaponType.Pistol; // Public for Controller
    
    private int _kills = 0; // Restored field

    private PlayerInput _input;
    private Timer _shootTimer;
    private float _machineGunFireRate = 5.0f; // Shots per second

    public override void _Ready()
    {
        _input = GetNode<PlayerInput>("PlayerInput");
        
        _shootTimer = new Timer();
        AddChild(_shootTimer);
        _shootTimer.WaitTime = 1.0f / _machineGunFireRate;
        _shootTimer.OneShot = true;
        
        EmitSignal(SignalName.HealthChanged, Health);
        EmitSignal(SignalName.EnemyKilled, _kills);
    }

    // --- Input & Networking ---

    // Called by PlayerInput (Local Client)
    public void TryShoot()
    {
        if (CurrentWeapon == WeaponType.Pistol)
        {
            RpcId(1, nameof(RequestFire));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RequestFire()
    {
        if (!Multiplayer.IsServer()) return;
        DoFire();
    }

    // Called by ServerController (MachineGun) or RPC (Pistol)
    public void DoFire()
    {
        if (BulletScene == null)
        {
            GD.PrintErr("Player: BulletScene is not assigned!");
            return;
        }

        Node bulletsContainer = GetTree().Root.FindChild("Bullets", true, false);
        if (bulletsContainer == null) return;

        Bullet bullet = BulletScene.Instantiate<Bullet>();
        bullet.Name = "Bullet_" + Name + "_" + Time.GetTicksMsec(); 
        
        bullet.EnemyKilled += OnEnemyKilledByBullet;
        
        // We need AimDirection. Since PlayerInput is a child, we can access it.
        // Or better: ServerController passes it? 
        // For simplicity: We use current Rotation or ask Input component.
        var input = GetNode<PlayerInput>("PlayerInput");
        bullet.SetDirection(input.AimDirection);
        
        bullet.GlobalPosition = GlobalPosition;
        bulletsContainer.AddChild(bullet, true);
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
    }
}
