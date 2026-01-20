using Godot;
using System;

public partial class Menu : Control
{
    private Button _btnSinglePlayer;
    private Button _btnMultiplayer;
    private Button _btnQuit;

    public override void _Ready()
    {
        // Get references to buttons. 
        // Note: The paths must match the scene structure we will create.
        _btnSinglePlayer = GetNode<Button>("CenterContainer/VBoxContainer/BtnSinglePlayer");
        _btnMultiplayer = GetNode<Button>("CenterContainer/VBoxContainer/BtnMultiplayer");
        _btnQuit = GetNode<Button>("CenterContainer/VBoxContainer/BtnQuit");

        // Connect signals
        _btnSinglePlayer.Pressed += OnSinglePlayerPressed;
        _btnMultiplayer.Pressed += OnMultiplayerPressed;
        _btnQuit.Pressed += OnQuitPressed;
    }

    private void OnSinglePlayerPressed()
    {
        GD.Print("Starting Single Player...");
        // Use NetworkManager to start a local session
        var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
        networkManager.StartSinglePlayer();
    }

    private void OnMultiplayerPressed()
    {
        GD.Print("Joining Online Session (localhost)...");
        var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
        networkManager.StartClient("127.0.0.1", 7777);
    }

    private void OnQuitPressed()
    {
        GD.Print("Quitting game...");
        GetTree().Quit();
    }
}
