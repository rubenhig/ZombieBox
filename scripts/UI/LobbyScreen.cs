using Godot;
using System;

public partial class LobbyScreen : CanvasLayer
{
    private Label _statusLabel;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("Control/CenterContainer/VBoxContainer/StatusLabel");
    }

    public void UpdateStatus(int currentPlayers, int requiredPlayers)
    {
        if (IsInstanceValid(_statusLabel))
        {
            _statusLabel.Text = $"Waiting for players... ({currentPlayers}/{requiredPlayers})";
        }
    }
}
