using Godot;
using System;
using System.Collections.Generic;

public partial class WaveManager : Node
{
    [Export]
    public PackedScene EnemyScene { get; set; }

    [Export]
    public float TimeBetweenWaves = 5.0f;

    [Export]
    public int InitialZombiesPerWave = 3;

    [Signal]
    public delegate void WaveChangedEventHandler(int newWave);

    private int _currentWave = 1;
    private int _zombiesAlive = 0;
    private Timer _waveTimer;
    private Node _spawnPointsContainer;
    private List<Marker2D> _spawnPoints = new List<Marker2D>();
    private Node _enemiesContainer;

    public override void _Ready()
    {
        _waveTimer = new Timer();
        AddChild(_waveTimer);
        _waveTimer.WaitTime = TimeBetweenWaves;
        _waveTimer.OneShot = true; // Timer is now for cooldown between waves
        _waveTimer.Timeout += StartNextWave; // When cooldown ends, start wave
    }

    public void Configure(Node spawnPointsContainer, Node enemiesContainer)
    {
        _enemiesContainer = enemiesContainer;
        _spawnPointsContainer = spawnPointsContainer;
        _spawnPoints.Clear();

        foreach (Node child in _spawnPointsContainer.GetChildren())
        {
            if (child is Marker2D marker)
            {
                _spawnPoints.Add(marker);
            }
        }
    }

    public void StartWaves()
    {
        GD.Print("WaveManager: Waiting 2 seconds before first wave...");
        // Use a timer node instead of CreateTimer to ensure it respects pause state? 
        // CreateTimer respects SceneTree.Paused if configured, but here we want to control it manually.
        // Let's just trigger the timer we already have or create a one-shot.
        GetTree().CreateTimer(2.0f).Timeout += StartWaveLogic;
    }

    private void StartWaveLogic()
    {
        GD.Print($"WaveManager: Starting Wave {_currentWave}");
        EmitSignal(SignalName.WaveChanged, _currentWave);
        SpawnWave();
    }

    private void StartNextWave()
    {
        _currentWave++;
        StartWaveLogic();
    }

    private void SpawnWave()
    {
        if (EnemyScene == null || _spawnPoints.Count == 0)
        {
            GD.PrintErr("Enemy scene or spawn points not set up in WaveManager.");
            return;
        }

        int enemiesToSpawn = InitialZombiesPerWave * _currentWave;
        // We don't set _zombiesAlive here because SpawnEnemy increments it? 
        // No, better to count successful spawns or just rely on the events.
        // Let's rely on SpawnEnemy connecting the signal.
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        // Only the server should spawn enemies
        if (!Multiplayer.IsServer()) return;

        int spawnIndex = new Random().Next(0, _spawnPoints.Count);
        Marker2D spawnPoint = _spawnPoints[spawnIndex];

        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Name = "Enemy_" + Guid.NewGuid().ToString(); // Unique name for network sync
        enemy.GlobalPosition = spawnPoint.GlobalPosition;
        
        // Connect to Died signal to track progress
        enemy.Died += OnEnemyDied;
        _zombiesAlive++;

        _enemiesContainer.AddChild(enemy, true);
    }

    private void OnEnemyDied()
    {
        _zombiesAlive--;
        GD.Print($"WaveManager: Enemy Died. Zombies alive: {_zombiesAlive}");
        if (_zombiesAlive <= 0)
        {
            WaveCompleted();
        }
    }

    private void WaveCompleted()
    {
        GD.Print($"Wave {_currentWave} Completed! Resting for {TimeBetweenWaves} seconds...");
        _waveTimer.Start();
    }

    public void StopWaves()
    {
        _waveTimer.Stop();
        // Disconnect logic could go here if we wanted to be super clean, 
        // but since enemies are destroyed on Game Over usually, it's fine.
        GD.Print("WaveManager: Waves stopped.");
    }
}
