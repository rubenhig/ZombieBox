using Godot;
using System;
using System.Collections.Generic;

public partial class NetworkManager : Node
{
    [Signal]
    public delegate void PlayerSpawnedEventHandler(Player player);

    [Export]
    public PackedScene PlayerScene { get; set; }

    private const int Port = 7777;
    private const int MaxPlayers = 4;

    private Node _playersContainer;

    public override void _Ready()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        // Auto-load player scene if not set via inspector
        if (PlayerScene == null)
        {
            PlayerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
        }
    }

    public void OnGameLevelLoaded(Node playersNode, Node enemiesNode)
    {
        _playersContainer = playersNode;
        GD.Print("NetworkManager: Game level loaded. Containers registered.");

        // If we are starting single player, spawn ourselves
        if (Multiplayer.IsServer() && Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer)
        {
            SpawnPlayer(1);
        }
        // In Dedicated Server or Client mode, spawning is handled by PeerConnected signal 
        // or by the server for us.
    }

    public void StartSinglePlayer()
    {
        GD.Print("NetworkManager: Starting Single Player (Offline Mode)...");
        
        // Use OfflineMultiplayerPeer for local play without networking overhead/ports
        var peer = new OfflineMultiplayerPeer();
        Multiplayer.MultiplayerPeer = peer;
        
        GD.Print("Offline session initialized.");
        
        // Load the game scene through Master. 
        GetNode<Master>("/root/Master").LoadGame();
    }

    public void StartDedicatedServer(int port)
    {
        GD.Print($"NetworkManager: Attempting to start Dedicated Server on port {port}...");
        
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, MaxPlayers);
        if (error != Error.Ok)
        {
            GD.PrintErr("FATAL: Failed to create dedicated server: " + error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"SUCCESS: Dedicated Server active and listening on port {port}");
        GD.Print($"Server Peer Status: {peer.GetConnectionStatus()}");
    }

    public void StartClient(string ipAddress, int port)
    {
        GD.Print($"NetworkManager: Attempting to connect to {ipAddress}:{port}...");
        
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(ipAddress, port);
        if (error != Error.Ok)
        {
            GD.PrintErr("FATAL: Failed to initialize client peer: " + error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Client peer initialized. Waiting for connection...");
        
        // Connect signals to track connection state
        Multiplayer.ConnectedToServer += () => GD.Print("SUCCESS: Connected to Server!");
        Multiplayer.ConnectionFailed += () => GD.PrintErr("FATAL: Connection to Server Failed!");
        Multiplayer.ServerDisconnected += () => GD.PrintErr("FATAL: Disconnected from Server!");
        
        // Load the game scene through Master
        GetNode<Master>("/root/Master").LoadGame();
    }

    private void OnPeerConnected(long id)
    {
        GD.Print("Player connected: " + id);
        // Only spawn if the level is ready. If not, OnGameLevelLoaded will handle it later?
        // Actually, if a peer connects mid-game, we spawn.
        if (Multiplayer.IsServer() && _playersContainer != null)
        {
            SpawnPlayer(id);
        }
    }

    private void SpawnPlayer(long id)
    {
        if (_playersContainer == null)
        {
            GD.PrintErr("Cannot spawn player. Players container is null.");
            return;
        }

        GD.Print("Spawning player for ID: " + id);
        
        Player player = PlayerScene.Instantiate<Player>();
        player.Name = id.ToString(); // Unique Name for the Node
        _playersContainer.AddChild(player, true);
        
        // === AUTHORITY SETUP ===
        // 1. The Player Body (Root) is owned by the Server (ID 1).
        //    It's the "Physical Reality".
        player.SetMultiplayerAuthority(1);
        
        // 2. The Player Input (Brain) is owned by the Client (ID 'id').
        //    It's the "Intent".
        var input = player.GetNodeOrNull("PlayerInput");
        if (input != null)
        {
            input.SetMultiplayerAuthority((int)id);
        }
        else
        {
            GD.PrintErr("PlayerInput node missing in Player scene!");
        }

        EmitSignal(SignalName.PlayerSpawned, player);
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print("Player disconnected: " + id);
        if (_playersContainer != null)
        {
            Node player = _playersContainer.GetNodeOrNull(id.ToString());
            player?.QueueFree();
        }
    }
}
