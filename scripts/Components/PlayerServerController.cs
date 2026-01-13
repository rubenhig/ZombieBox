using Godot;
using System;

public partial class PlayerServerController : Node
{
    private Player _player;
    private PlayerInput _input;
    private Timer _shootTimer;
    private float _machineGunFireRate = 5.0f;

    // Previous frame state for Edge Detection
    private bool _wasShooting = false;
    private bool _wasSwitching = false;

    public override void _Ready()
    {
        // STRICT SEPARATION: This node should ONLY exist on the server.
        if (!Multiplayer.IsServer())
        {
            QueueFree();
            return;
        }

        _player = GetParent<Player>();
        _input = _player.GetNode<PlayerInput>("PlayerInput");

        // Timer belongs to logic, so we create it here or manage it here
        _shootTimer = new Timer();
        AddChild(_shootTimer);
        _shootTimer.WaitTime = 1.0f / _machineGunFireRate;
        _shootTimer.OneShot = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        // No need for IsServer() check here, because this node doesn't exist on clients.
        if (_player.Health <= 0) return;

        HandleMovement();
        HandleWeaponSwitch();
        HandleShooting();

        // Update previous state for next frame
        _wasShooting = _input.IsShooting;
        _wasSwitching = _input.IsSwitchingWeapon;
    }

    private void HandleMovement()
    {
        // Read intent from Input component
        Vector2 direction = _input.MoveVector;

        if (direction != Vector2.Zero)
        {
            _player.Velocity = direction.Normalized() * _player.Speed;
            _player.Rotation = _input.AimDirection.Angle();
        }
        else
        {
            _player.Velocity = Vector2.Zero;
        }

        // Apply physics to the Body
        _player.MoveAndSlide();
    }

    private void HandleWeaponSwitch()
    {
        // Rising Edge Detection: Only trigger when input goes from False -> True
        if (_input.IsSwitchingWeapon && !_wasSwitching)
        {
            _player.SwitchWeapon();
        }
    }

    private void HandleShooting()
    {
        if (_player.BulletScene == null)
        {
            GD.PrintErr("PlayerServerController: BulletScene is not assigned on Player!");
            return;
        }

        switch (_player.CurrentWeapon)
        {
            case WeaponType.Pistol:
                // Rising Edge Detection for semi-auto fire
                if (_input.IsShooting && !_wasShooting)
                {
                    PerformShoot();
                }
                break;
            case WeaponType.MachineGun:
                // Continuous fire (State checking)
                if (_input.IsShooting && _shootTimer.IsStopped())
                {
                    PerformShoot();
                    _shootTimer.Start();
                }
                break;
        }
    }

    private void PerformShoot()
    {
        // Logic to spawn bullet
        Node bulletsContainer = GetTree().Root.FindChild("Bullets", true, false);
        if (bulletsContainer == null) return;

        Bullet bullet = _player.BulletScene.Instantiate<Bullet>();
        // Unique naming for network
        bullet.Name = "Bullet_" + _player.Name + "_" + Time.GetTicksMsec();

        // Connect signal to the Player's method (which emits the signal upwards)
        bullet.EnemyKilled += _player.OnEnemyKilledByBullet;

        bullet.SetDirection(_input.AimDirection);
        bullet.GlobalPosition = _player.GlobalPosition;

        bulletsContainer.AddChild(bullet, true);
    }
}
