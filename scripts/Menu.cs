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
        // Placeholder for now, or could show Host/Join buttons
        GD.Print("Multiplayer clicked - Feature coming soon!");
        // For now, let's just log it. Later this will toggle a visibility of a Host/Join sub-menu.
    }

    private void OnQuitPressed()
    {
        GD.Print("Quitting game...");
        GetTree().Quit();
    }
}
