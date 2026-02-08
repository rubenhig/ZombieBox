using Godot;
using System;

public enum WeaponType { Pistol, MachineGun }

public partial class Player : CharacterBody2D, IDamageable
{
    [Export]
    public float Speed { get; set; } = 300.0f;

    // Velocity wrapper for MultiplayerSynchronizer (built-in Velocity is not [Export])
    [Export]
    public Vector2 SyncedVelocity
    {
        get => Velocity;
        set => Velocity = value;
    }

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
    private int _health = 3;
    [Export]
    public int Health
    {
        get => _health;
        set
        {
            if (_health != value)
            {
                _health = value;
                EmitSignal(SignalName.HealthChanged, _health);

                if (_health <= 0)
                {
                    Die();
                }
            }
        }
    }

    private WeaponType _currentWeapon = WeaponType.Pistol;
    [Export]
    public WeaponType CurrentWeapon
    {
        get => _currentWeapon;
        set
        {
            if (_currentWeapon != value)
            {
                _currentWeapon = value;
                EmitSignal(SignalName.WeaponSwitched, (int)_currentWeapon);
            }
        }
    }

    // Kill tracking (synchronized for UI)
    private int _kills = 0;
    [Export]
    public int Kills
    {
        get => _kills;
        set
        {
            if (_kills != value)
            {
                _kills = value;
                EmitSignal(SignalName.EnemyKilled, _kills);
            }
        }
    }

    // Last tick processed by server (for client prediction reconciliation)
    [Export]
    public uint LastProcessedTick { get; set; } = 0;

    // ========================================
    // CHILD NODE REFERENCES
    // ========================================
    // Using [Export] for editor-assigned references (robust against scene restructuring)

    /// <summary>
    /// PlayerInput component - handles input capture and synchronization.
    /// Assigned via editor (drag & drop in Inspector).
    /// </summary>
    [Export]
    private PlayerInput _input;

    /// <summary>
    /// ClientPredictor component - handles client-side prediction (optional, only on clients).
    /// Assigned via editor (drag & drop in Inspector).
    /// Can be null if not present in scene.
    /// </summary>
    [Export]
    private Node _clientPredictorNode;

    // Cached interface reference for type-safe initialization
    private ITickSystemUser _clientPredictor;

    private bool _initialized = false;

    public override void _EnterTree()
    {
        // Player Body authority is always Server (1)
        SetMultiplayerAuthority(1);
    }

    public override void _Ready()
    {
        // ========================================
        // VALIDATION: Editor-Assigned References
        // ========================================
        // Fail-fast if critical [Export] references are missing

        bool validationFailed = false;

        if (_input == null)
        {
            GD.PrintErr("╔════════════════════════════════════════════════════════════════╗");
            GD.PrintErr("║ CRITICAL ERROR: Player Setup Incomplete                       ║");
            GD.PrintErr("╠════════════════════════════════════════════════════════════════╣");
            GD.PrintErr($"║ Player Node: {Name}                                            ");
            GD.PrintErr("║ Missing: _input (PlayerInput)                                  ║");
            GD.PrintErr("║                                                                ║");
            GD.PrintErr("║ FIX:                                                           ║");
            GD.PrintErr("║ 1. Open: scenes/entities/player/player.tscn                    ║");
            GD.PrintErr("║ 2. Select root Player node                                     ║");
            GD.PrintErr("║ 3. In Inspector, find 'Input' property                         ║");
            GD.PrintErr("║ 4. Drag 'PlayerInput' child node to that property              ║");
            GD.PrintErr("║ 5. Save scene (Ctrl+S)                                         ║");
            GD.PrintErr("╚════════════════════════════════════════════════════════════════╝");
            validationFailed = true;
        }

        if (validationFailed)
        {
            // Disable this player to prevent cascading errors
            SetPhysicsProcess(false);
            SetProcess(false);
            return;
        }

        // ========================================
        // SUCCESS: All Critical References Valid
        // ========================================
        GD.Print($"✓ Player {Name}: Editor references validated successfully");

        // Cache interface reference for ClientPredictor (optional, only on clients)
        if (_clientPredictorNode is ITickSystemUser predictor)
        {
            _clientPredictor = predictor;
            GD.Print($"✓ Player {Name}: ClientPredictor found and cached");
        }
        else if (_clientPredictorNode != null)
        {
            GD.PrintErr($"⚠ Player {Name}: ClientPredictor node assigned but doesn't implement ITickSystemUser!");
        }

        // Set authority for PlayerInput based on player ID (peer ID encoded in Name)
        // This is safe because children are guaranteed ready in _Ready()
        if (int.TryParse(Name, out int authorityId))
        {
            _input.SetMultiplayerAuthority(authorityId);
            if (authorityId == Multiplayer.GetUniqueId())
            {
                GD.Print($"✓ Player {Name}: Authority assigned to local client {authorityId}");
            }
        }

        // Safety check: Warn if Initialize() was never called
        GetTree().CreateTimer(0.5).Timeout += () =>
        {
            if (!_initialized)
            {
                GD.PrintErr("╔════════════════════════════════════════════════════════════════╗");
                GD.PrintErr("║ WARNING: Dependencies Not Injected                            ║");
                GD.PrintErr("╠════════════════════════════════════════════════════════════════╣");
                GD.PrintErr($"║ Player {Name}: Initialize() was never called!                  ");
                GD.PrintErr("║                                                                ║");
                GD.PrintErr("║ This should be called by SpawnSystem or SessionSystem.        ║");
                GD.PrintErr("║ Check that dependency injection is working correctly.         ║");
                GD.PrintErr("╚════════════════════════════════════════════════════════════════╝");
            }
        };

        // UI state will be refreshed when HUD connects via RefreshUI()
    }

    /// <summary>
    /// Inject dependencies from external orchestrator (SpawnSystem or SessionSystem).
    /// MUST be called BEFORE adding to scene tree (before AddChild).
    ///
    /// Uses [Export] references (editor-assigned) instead of GetNode strings.
    /// Uses ITickSystemUser interface for type-safe initialization.
    ///
    /// Benefits:
    /// - Robust against scene restructuring (no hardcoded paths)
    /// - Compile-time type safety (no reflection)
    /// - Editor validation (missing references visible in Inspector)
    /// </summary>
    public void Initialize(TickManager tickManager)
    {
        // Guard: Prevent double initialization
        if (_initialized)
        {
            GD.PrintErr($"⚠ Player {Name}: Initialize() called twice! Ignoring.");
            return;
        }

        // Guard: Validate parameters
        if (tickManager == null)
        {
            GD.PrintErr($"✗ Player {Name}: Initialize() called with null TickManager!");
            return;
        }

        int successCount = 0;
        int totalComponents = 0;

        // Inject into PlayerInput (required component)
        totalComponents++;
        if (_input != null)
        {
            _input.Initialize(tickManager);
            successCount++;
            GD.Print($"  ✓ PlayerInput injected for {Name}");
        }
        else
        {
            GD.PrintErr($"  ✗ PlayerInput is null for {Name} (not assigned in editor)");
        }

        // Inject into ClientPredictor (optional - only on clients)
        if (_clientPredictorNode != null)
        {
            totalComponents++;
            if (_clientPredictorNode is ITickSystemUser predictor)
            {
                predictor.Initialize(tickManager);
                successCount++;
                GD.Print($"  ✓ ClientPredictor injected for {Name}");
            }
            else
            {
                GD.PrintErr($"  ✗ ClientPredictor node doesn't implement ITickSystemUser for {Name}");
            }
        }

        _initialized = true;
        GD.Print($"✓ Player {Name}: Dependencies injected ({successCount}/{totalComponents} components)");
    }

    /// <summary>
    /// Re-emits all UI-related signals for components that connect after _Ready().
    /// Called by SessionSystem after HUD connects.
    /// </summary>
    public void RefreshUI()
    {
        GD.Print($"Player {Name}: RefreshUI() called - Health={Health}, Kills={Kills}, Weapon={CurrentWeapon}");
        EmitSignal(SignalName.HealthChanged, Health);
        GD.Print($"Player {Name}: Emitted HealthChanged({Health})");
        EmitSignal(SignalName.EnemyKilled, Kills);
        GD.Print($"Player {Name}: Emitted EnemyKilled({Kills})");
        EmitSignal(SignalName.WeaponSwitched, (int)CurrentWeapon);
        GD.Print($"Player {Name}: Emitted WeaponSwitched({(int)CurrentWeapon})");
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

        Health -= damage;  // Setter emits HealthChanged signal automatically
        GD.Print($"{Name} took damage. Health: {Health}");

        // Die() is called by Health setter if health <= 0
    }

    // --- Event Handlers ---

    /// <summary>
    /// Called when a bullet owned by this player kills an enemy.
    /// Server-only.
    /// </summary>
    public void OnEnemyKilledByBullet()
    {
        if (!NetworkUtils.IsServer()) return;

        Kills++;  // Setter emits EnemyKilled signal automatically
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
