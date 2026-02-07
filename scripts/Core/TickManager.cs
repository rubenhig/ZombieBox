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
    }

    public override void _PhysicsProcess(double delta)
    {
        // Server increments authoritative tick
        if (NetworkUtils.IsServer())
        {
            ServerTick++;
        }

        // Client initial synchronization: set ClientTick to ServerTick on first valid update
        if (!_clientSynchronized && ServerTick > 0)
        {
            ClientTick = ServerTick;
            _clientSynchronized = true;
            GD.Print($"TickManager: Client synchronized at tick {ServerTick}");
        }

        // All peers (server + clients) increment their local client tick
        ClientTick++;

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
