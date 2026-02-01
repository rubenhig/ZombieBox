using Godot;
using System;

public partial class HUD : CanvasLayer
{
    private Label _healthLabel;
    private Label _waveLabel;
    private Label _killsLabel;
    
    private Control _gameOverPanel;
    private Button _btnRestart;
    private Button _btnMenu;

    public override void _Ready()
    {
        _healthLabel = GetNode<Label>("HealthLabel");
        _waveLabel = GetNode<Label>("WaveLabel");
        _killsLabel = GetNode<Label>("KillsLabel");
        
        _gameOverPanel = GetNode<Control>("GameOverPanel");
        _btnRestart = GetNode<Button>("GameOverPanel/CenterContainer/VBoxContainer/BtnRestart");
        _btnMenu = GetNode<Button>("GameOverPanel/CenterContainer/VBoxContainer/BtnMenu");

        _btnRestart.Pressed += OnRestartPressed;
        _btnMenu.Pressed += OnMenuPressed;

        // WaveSystem connection is handled by SessionSystem via dependency injection
    }

    public void RegisterPlayer(Player player)
    {
        GD.Print($"HUD: RegisterPlayer called for player {player.Name}");

        // Disconnect first to avoid duplicates if re-registering
        player.HealthChanged -= OnPlayerHealthChanged;
        player.EnemyKilled -= OnPlayerKilledEnemy;

        GD.Print("HUD: Connecting to player signals...");
        player.HealthChanged += OnPlayerHealthChanged;
        player.EnemyKilled += OnPlayerKilledEnemy;

        GD.Print("HUD: Calling player.RefreshUI()...");
        // Refresh UI by asking player to re-emit all signals
        player.RefreshUI();
        GD.Print("HUD: RegisterPlayer completed");
    }

    public void ShowGameOver()
    {
        _gameOverPanel.Visible = true;
    }

    private void OnPlayerHealthChanged(int newHealth)
    {
        GD.Print($"HUD: OnPlayerHealthChanged({newHealth})");
        if (IsInstanceValid(_healthLabel))
        {
            _healthLabel.Text = "Health: " + newHealth;
            GD.Print($"HUD: Health label updated to '{_healthLabel.Text}'");
        }
        else
        {
            GD.PrintErr("HUD: _healthLabel is not valid!");
        }
    }

    public void OnWaveChanged(int newWave)
    {
        GD.Print($"HUD: OnWaveChanged({newWave})");
        if (IsInstanceValid(_waveLabel))
        {
            _waveLabel.Text = "Wave: " + newWave;
            GD.Print($"HUD: Wave label updated to '{_waveLabel.Text}'");
        }
        else
        {
            GD.PrintErr("HUD: _waveLabel is not valid!");
        }
    }

    private void OnPlayerKilledEnemy(int newKills)
    {
        GD.Print($"HUD: OnPlayerKilledEnemy({newKills})");
        if (IsInstanceValid(_killsLabel))
        {
            _killsLabel.Text = "Kills: " + newKills;
            GD.Print($"HUD: Kills label updated to '{_killsLabel.Text}'");
        }
        else
        {
            GD.PrintErr("HUD: _killsLabel is not valid!");
        }
    }

    private void OnRestartPressed()
    {
        var master = GetNode<Master>("/root/Master");
        master.LoadGame();
    }

    private void OnMenuPressed()
    {
        var master = GetNode<Master>("/root/Master");
        master.LoadMenu();
    }
}
