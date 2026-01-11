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

        // WaveManager is usually present at start
        WaveManager waveManager = GetNodeOrNull<WaveManager>("../Game/WaveManager");
        // Fallback for different path structure
        if (waveManager == null) waveManager = GetTree().Root.FindChild("WaveManager", true, false) as WaveManager;
        
        if (waveManager != null)
        {
            waveManager.WaveChanged += OnWaveChanged;
        }

        GameManager gameManager = GetTree().Root.FindChild("GameManager", true, false) as GameManager;
        if (gameManager != null)
        {
            gameManager.GameEnded += OnGameEnded;
        }
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

    private void OnPlayerHealthChanged(int newHealth)
    {
        _healthLabel.Text = "Health: " + newHealth;
    }

    private void OnWaveChanged(int newWave)
    {
        _waveLabel.Text = "Wave: " + newWave;
    }

    private void OnPlayerKilledEnemy(int newKills)
    {
        _killsLabel.Text = "Kills: " + newKills;
    }

    private void OnGameEnded()
    {
        _gameOverPanel.Visible = true;
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
