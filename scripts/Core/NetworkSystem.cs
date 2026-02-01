using Godot;
using System;
using System.Collections.Generic;

public partial class NetworkSystem : Node
{
    [Signal]
    public delegate void PlayerSpawnedEventHandler(Player player);

    private const int Port = 7777;
    private const int MaxPlayers = 4;

    // private Node _playersContainer; // Removed

    public override void _Ready()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        // Network Logic Only
    }

    // Logic moved to SessionSystem
    // public void OnGameLevelLoaded(...)

    public void StartSinglePlayer()
    {
        GD.Print("NetworkSystem: Starting Single Player (Offline Mode)...");

        // Use OfflineMultiplayerPeer for local play without networking overhead/ports
        var peer = new OfflineMultiplayerPeer();
        Multiplayer.MultiplayerPeer = peer;

        GD.Print("Offline session initialized.");

        // Load the game scene through Master. 
        GetNode<Master>("/root/Master").LoadGame();
    }

    public void StartDedicatedServer(int port)
    {
        GD.Print($"NetworkSystem: Attempting to start Dedicated Server on port {port}...");

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
        GD.Print($"NetworkSystem: Attempting to connect to {ipAddress}:{port}...");

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
        GD.Print("NetworkSystem: Peer connected: " + id);
        // SessionSystem will handle spawning via signal subscription
    }

    // Replaced by direct signal connection from SessionSystem to Multiplayer.PeerConnected
    // private void SpawnPlayer(long id) { ... }

    private void OnPeerDisconnected(long id)
    {
        GD.Print("NetworkSystem: Peer disconnected: " + id);
    }
}
