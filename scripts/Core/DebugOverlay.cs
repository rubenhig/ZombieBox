using Godot;
using System;

public partial class DebugOverlay : CanvasLayer
{
    private Label _statsLabel;
    private double _timeAccumulator = 0.0;
    private double _refreshRate = 0.2; // Update 5 times per second

    public override void _Ready()
    {
        _statsLabel = GetNode<Label>("StatsContainer/Label");
        Visible = false; // Hidden by default
    }

    public override void _Process(double delta)
    {
        // Toggle with F3
        if (Input.IsActionJustPressed("toggle_debug") || Input.IsPhysicalKeyPressed(Key.F3))
        {
            Visible = !Visible;
        }

        if (!Visible) return;

        _timeAccumulator += delta;
        if (_timeAccumulator > _refreshRate)
        {
            _timeAccumulator = 0;
            UpdateStats();
        }
    }

    private void UpdateStats()
    {
        var fps = Engine.GetFramesPerSecond();
        var mem = OS.GetStaticMemoryUsage() / 1024 / 1024; // MB
        
        var players = GetTree().GetNodesInGroup("players").Count;
        var enemies = GetTree().GetNodesInGroup("enemies").Count;
        var bullets = GetTree().GetNodesInGroup("bullets").Count;
        
        // Network status
        var netMode = Multiplayer.MultiplayerPeer?.GetType().Name ?? "None";
        var isServer = Multiplayer.IsServer() ? "Server" : "Client";
        
        string pingText = "Local";
        if (!Multiplayer.IsServer() && Multiplayer.MultiplayerPeer is ENetMultiplayerPeer enetPeer)
        {
            // Peer 1 is always the server
            var peer = enetPeer.GetPeer(1);
            if (peer != null)
            {
                pingText = $"{(int)peer.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime)}ms";
            }
        }

        _statsLabel.Text = $@"FPS: {fps}
Mem: {mem} MB
Net: {netMode} ({isServer}) | Ping: {pingText}
Entities: P:{players} | E:{enemies} | B:{bullets}
Scene: {GetTree().CurrentScene?.Name}";
    }
}
