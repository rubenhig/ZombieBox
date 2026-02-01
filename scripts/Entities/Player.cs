using Godot;
using System;

public enum WeaponType { Pistol, MachineGun }

public partial class Player : CharacterBody2D, IDamageable
{
    [Export]
    public float Speed { get; set; } = 300.0f;

    // Signals for external systems (SessionSystem, HUD, SpawnSystem, etc.)
    [Signal]
    public delegate void HealthChangedEventHandler(int newHealth);

    [Signal]
    public delegate void EnemyKilledEventHandler(int newKills);

    [Signal]
    public delegate void WeaponSwitchedEventHandler(WeaponType newWeapon);

    [Signal]
    public delegate void WeaponFiredEventHandler(Vector2 position, Vector2 direction, string shooterName);

    // IDamageable Implementation
    public event Action Died;

    // Godot signal version of Died (for editor connections)
    [Signal]
    public delegate void DiedSignalEventHandler();

    // State properties - synchronized via MultiplayerSynchronizer
    public int Health { get; private set; } = 3;
    public WeaponType CurrentWeapon { get; private set; } = WeaponType.Pistol;

    private int _kills = 0;
    private PlayerInput _input;

    public override void _EnterTree()
    {
        // 1. Player Body authority is always Server (1)
        SetMultiplayerAuthority(1);

        // 2. Player Input authority depends on the node name (which is the Peer ID)
        // Note: GetNode might fail if children are not ready yet in EnterTree? 
        // In Godot 4, children EnterTree happens after parent EnterTree but before Ready.
        // Let's use GetNodeOrNull to be safe, though packed scenes usually have children.
        var input = GetNodeOrNull<PlayerInput>("PlayerInput");
        if (input != null && int.TryParse(Name, out int authorityId))
        {
            input.SetMultiplayerAuthority(authorityId);
            if (authorityId == Multiplayer.GetUniqueId())
            {
                GD.Print($"Player {_EnterTree}: Authority assigned to local client {authorityId}");
            }
        }
    }

    public override void _Ready()
    {
        _input = GetNode<PlayerInput>("PlayerInput");

        // Emit initial state
        EmitSignal(SignalName.HealthChanged, Health);
        EmitSignal(SignalName.EnemyKilled, _kills);
        EmitSignal(SignalName.WeaponSwitched, (int)CurrentWeapon);
    }

    // --- Client Input Commands (RPC to Server) ---

    /// <summary>
    /// Called by PlayerInput when player presses shoot.
    /// Sends RPC to server for pistol shots.
    /// </summary>
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
        if (!NetworkUtils.IsServer()) return;
        DoFire();
    }

    /// <summary>
    /// Called by PlayerInput when player presses switch weapon.
    /// Sends RPC to server.
    /// </summary>
    public void TrySwitchWeapon()
    {
        RpcId(1, nameof(RequestSwitchWeapon));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RequestSwitchWeapon()
    {
        if (!NetworkUtils.IsServer()) return;
        SwitchWeapon();
    }

    // --- Server-side Actions ---

    /// <summary>
    /// Fires a weapon. Called by PlayerController (MachineGun) or RPC (Pistol).
    /// Server-only. Emits signal for external systems (SpawnSystem) to handle.
    /// </summary>
    public void DoFire()
    {
        // Emit signal for external systems to handle
        // SpawnSystem will listen and create the appropriate projectile
        EmitSignal(SignalName.WeaponFired, GlobalPosition, _input.AimDirection, Name);
    }

    /// <summary>
    /// Switches the current weapon. Server-only.
    /// </summary>
    public void SwitchWeapon()
    {
        CurrentWeapon = CurrentWeapon == WeaponType.Pistol ? WeaponType.MachineGun : WeaponType.Pistol;
        GD.Print($"{Name} switched to {CurrentWeapon}");
        EmitSignal(SignalName.WeaponSwitched, (int)CurrentWeapon);
    }

    /// <summary>
    /// Applies damage to the player. Server-only.
    /// Implements IDamageable interface.
    /// </summary>
    public void TakeDamage(int damage)
    {
        // Server authority check
        if (!NetworkUtils.IsServer()) return;

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

    /// <summary>
    /// Called when a bullet owned by this player kills an enemy.
    /// Server-only.
    /// </summary>
    public void OnEnemyKilledByBullet()
    {
        if (!NetworkUtils.IsServer()) return;

        _kills++;
        EmitSignal(SignalName.EnemyKilled, _kills);
    }

    /// <summary>
    /// Handles player death. Server-only.
    /// Emits Died signal for systems to listen to.
    /// </summary>
    private void Die()
    {
        GD.Print($"{Name} died!");

        // Hide the player
        Hide();

        // Emit signals for systems to react
        EmitSignal(SignalName.DiedSignal);
        Died?.Invoke();
    }
}
