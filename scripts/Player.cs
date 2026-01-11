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

    public int Health { get; private set; } = 3;
    private int _kills = 0;

    private PlayerInput _input;
    private WeaponType _currentWeapon = WeaponType.Pistol;
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

    public override void _PhysicsProcess(double delta)
    {
        // Only the server should process physics in a server-authoritative model
        // However, for now we let the authority move and we will add sync later
        if (Health <= 0) return;

        HandleMovement();
        HandleWeaponSwitch();
        HandleShooting();
    }

    public void TakeDamage(int damage)
    {
        if (Health <= 0) return;

        Health -= damage;
        GD.Print($"{Name} took damage. Health: {Health}");
        EmitSignal(SignalName.HealthChanged, Health);
        
        if (Health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        GD.Print($"{Name} died!");
        SetPhysicsProcess(false);
        Hide();
        
        EmitSignal(SignalName.Died);

        // Notify GameManager
        var gameManager = GetTree().Root.FindChild("GameManager", true, false) as GameManager;
        gameManager?.GameOver();
    }

    private void HandleMovement()
    {
        Vector2 direction = _input.MoveVector;

        if (direction != Vector2.Zero)
        {
            Velocity = direction.Normalized() * Speed;
            Rotation = _input.AimDirection.Angle();
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
    }

    private void HandleWeaponSwitch()
    {
        if (_input.IsSwitchingWeapon)
        {
            _currentWeapon = _currentWeapon == WeaponType.Pistol ? WeaponType.MachineGun : WeaponType.Pistol;
            GD.Print($"{Name} switched to {_currentWeapon}");
        }
    }

    private void HandleShooting()
    {
        if (BulletScene == null) 
        {
            GD.PrintErr("Player: BulletScene is not assigned!");
            return;
        }

        switch (_currentWeapon)
        {
            case WeaponType.Pistol:
                if (_input.IsShootingJustPressed) 
                {
                    Shoot();
                }
                break;
            case WeaponType.MachineGun:
                if (_input.IsShooting && _shootTimer.IsStopped())
                {
                    Shoot();
                    _shootTimer.Start();
                }
                break;
        }
    }

    private void Shoot()
    {
        // Find Bullets container
        Node bulletsContainer = GetTree().Root.FindChild("Bullets", true, false);
        if (bulletsContainer == null) return;

        Bullet bullet = BulletScene.Instantiate<Bullet>();
        bullet.Name = "Bullet_" + Name + "_" + Time.GetTicksMsec(); // Unique name for network
        bullet.EnemyKilled += OnEnemyKilledByBullet;
        bullet.SetDirection(_input.AimDirection);
        bullet.GlobalPosition = GlobalPosition;
        
        // Add to bullets container with readable name for spawner
        bulletsContainer.AddChild(bullet, true);
    }

    private void OnEnemyKilledByBullet()
    {
        _kills++;
        EmitSignal(SignalName.EnemyKilled, _kills);
    }
}
