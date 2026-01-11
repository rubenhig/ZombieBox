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

        // If we are starting single player or are the server, we might need to spawn ourselves
        // For SinglePlayer, ID is 1.
        if (Multiplayer.IsServer())
        {
            SpawnPlayer(1);
        }
    }

    public void StartSinglePlayer()
    {
        GD.Print("NetworkManager: Starting Single Player (Offline Mode)...");
        
        // Use OfflineMultiplayerPeer for local play without networking overhead/ports
        var peer = new OfflineMultiplayerPeer();
        Multiplayer.MultiplayerPeer = peer;
        
        GD.Print("Offline server started.");
        
        // Load the game scene through Master. 
        // We do NOT spawn here. We wait for OnGameLevelLoaded callback.
        GetNode<Master>("/root/Master").LoadGame();
    }

    public void StartServer()
    {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(Port, MaxPlayers);
        if (error != Error.Ok)
        {
            GD.PrintErr("Failed to create server: " + error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Server started on port " + Port);
    }

    public void JoinServer(string ipAddress)
    {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(ipAddress, Port);
        if (error != Error.Ok)
        {
            GD.PrintErr("Failed to create client: " + error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Joining server at " + ipAddress + ":" + Port);
        
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
        player.Name = id.ToString(); // Explicit name matching Authority ID
        _playersContainer.AddChild(player, true);
        
        // Set authority so the client with this ID can control this player
        player.SetMultiplayerAuthority((int)id);
        
        // Also set authority for the PlayerInput child if it exists
        // Note: The Player script or component should handle this, but setting it here is safe.
        var input = player.GetNodeOrNull("PlayerInput");
        if (input != null)
        {
            input.SetMultiplayerAuthority((int)id);
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
