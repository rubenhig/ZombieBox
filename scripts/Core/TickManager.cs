using Godot;
using System;

/// <summary>
/// TickManager - Maintains synchronized tick counter across server and clients.
/// Server increments ServerTick at 60Hz, clients receive updates via MultiplayerSynchronizer.
/// Clients maintain ClientTick for local prediction and interpolation.
/// </summary>
public partial class TickManager : Node
{
    // Server tick counter - authoritative, synchronized to all clients
    private uint _serverTick = 0;

    [Export]
    public uint ServerTick
    {
        get => _serverTick;
        set => _serverTick = value;
    }

    // Client tick counter - local only, used for prediction
    // Each client increments this independently based on their physics ticks
    public uint ClientTick { get; private set; } = 0;

    // Tick difference (latency in ticks)
    public int TickDiff => (int)(ClientTick - ServerTick);

    // Track if client has synchronized initial tick
    private bool _clientSynchronized = false;

    public override void _Ready()
    {
        if (NetworkUtils.IsServer())
        {
            GD.Print("TickManager: Server mode - will increment ServerTick at 60Hz");
            _clientSynchronized = true; // Server is always synchronized
        }
        else
        {
            GD.Print("TickManager: Client mode - receiving ServerTick from server");
        }

        // Add MultiplayerSynchronizer for ServerTick replication
        var synchronizer = new MultiplayerSynchronizer();
        AddChild(synchronizer);

        // Load and set replication config
        var config = GD.Load<SceneReplicationConfig>("res://scenes/systems/tickmanager_sync.tres");
        synchronizer.ReplicationConfig = config;

        GD.Print("TickManager: Autoload initialized with synchronizer");
    }

    public override void _PhysicsProcess(double delta)
    {
        // Server increments authoritative tick
        if (NetworkUtils.IsServer())
        {
            ServerTick++;
            ClientTick++; // Server's ClientTick matches ServerTick
        }
        else
        {
            // CLIENT LOGIC

            // 1. Initial synchronization: Set ClientTick ahead of ServerTick
            if (!_clientSynchronized && ServerTick > 0)
            {
                // Sync with latency compensation: start 10 ticks ahead
                ClientTick = ServerTick + 10;
                _clientSynchronized = true;
                GD.Print($"TickManager: Client synchronized - ServerTick={ServerTick}, ClientTick={ClientTick}, InitialDiff={TickDiff}");
            }

            // 2. Drift correction: Keep ClientTick in optimal range
            if (_clientSynchronized)
            {
                int currentDiff = TickDiff;

                // Target range: +5 to +15 ticks ahead (83ms to 250ms)
                if (currentDiff < 5)
                {
                    // Too far behind: Speed up (increment by 2 this frame)
                    ClientTick += 2;
                    if (ClientTick % 60 == 0)
                    {
                        GD.Print($"TickManager: [DRIFT CORRECTION] Speeding up - Diff was {currentDiff}, now {TickDiff}");
                    }
                }
                else if (currentDiff > 15)
                {
                    // Too far ahead: Slow down (don't increment this frame)
                    if (ClientTick % 60 == 0)
                    {
                        GD.Print($"TickManager: [DRIFT CORRECTION] Slowing down - Diff was {currentDiff}");
                    }
                    // Don't increment ClientTick this frame
                }
                else
                {
                    // In optimal range: Normal increment
                    ClientTick++;
                }
            }
            else
            {
                // Not synchronized yet, increment normally
                ClientTick++;
            }
        }

        // Debug output every 60 ticks (1 second)
        if (ClientTick % 60 == 0)
        {
            if (NetworkUtils.IsServer())
            {
                GD.Print($"TickManager: ServerTick={ServerTick}");
            }
            else
            {
                GD.Print($"TickManager: ClientTick={ClientTick}, ServerTick={ServerTick}, Diff={TickDiff} ticks (~{TickDiff * 16.67f:F1}ms)");
            }
        }
    }

    /// <summary>
    /// Get the current server tick. Safe to call from any peer.
    /// </summary>
    public uint GetServerTick() => ServerTick;

    /// <summary>
    /// Get the current client tick (local). Safe to call from any peer.
    /// </summary>
    public uint GetClientTick() => ClientTick;
}
