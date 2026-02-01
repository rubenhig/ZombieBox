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

        // WaveSystem is usually present at start
        WaveSystem waveSystem = GetNodeOrNull<WaveSystem>("../Game/WaveSystem");
        // Fallback for different path structure
        if (waveSystem == null) waveSystem = GetTree().Root.FindChild("WaveSystem", true, false) as WaveSystem;

        if (waveSystem != null)
        {
            waveSystem.WaveChanged += OnWaveChanged;
        }
        // SessionSystem connection moved to SessionSystem.cs via dependency injection
    }

    public void RegisterPlayer(Player player)
    {
        GD.Print("HUD: Connecting to player signals.");
        // Disconnect first to avoid duplicates if re-registering
        player.HealthChanged -= OnPlayerHealthChanged;
        player.EnemyKilled -= OnPlayerKilledEnemy;

        player.HealthChanged += OnPlayerHealthChanged;
        player.EnemyKilled += OnPlayerKilledEnemy;
        
        // Update initial state
        OnPlayerHealthChanged(player.Health);
    }

    public void ShowGameOver()
    {
        _gameOverPanel.Visible = true;
    }

    private void OnPlayerHealthChanged(int newHealth)
    {
        if (IsInstanceValid(_healthLabel))
        {
            _healthLabel.Text = "Health: " + newHealth;
        }
    }

    private void OnWaveChanged(int newWave)
    {
        if (IsInstanceValid(_waveLabel))
        {
            _waveLabel.Text = "Wave: " + newWave;
        }
    }

    private void OnPlayerKilledEnemy(int newKills)
    {
        if (IsInstanceValid(_killsLabel))
        {
            _killsLabel.Text = "Kills: " + newKills;
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
