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
    private Timer _waveTimer;
    private Node _spawnPointsContainer;
    private List<Marker2D> _spawnPoints = new List<Marker2D>();
    private Node _enemiesContainer;

    public override void _Ready()
    {
        _spawnPointsContainer = GetNode("../SpawnPoints");
        _enemiesContainer = GetNode("../Enemies");

        foreach (Node child in _spawnPointsContainer.GetChildren())
        {
            if (child is Marker2D marker)
            {
                _spawnPoints.Add(marker);
            }
        }

        _waveTimer = new Timer();
        AddChild(_waveTimer);
        _waveTimer.WaitTime = TimeBetweenWaves;
        _waveTimer.OneShot = false; // The timer will repeat
        _waveTimer.Timeout += OnWaveTimerTimeout;
        
        StartFirstWave();
    }

    private void StartFirstWave()
    {
        GD.Print("Starting Wave " + _currentWave);
        EmitSignal(SignalName.WaveChanged, _currentWave);
        SpawnWave();
        _waveTimer.Start();
    }

    private void OnWaveTimerTimeout()
    {
        _currentWave++;
        GD.Print("Starting Wave " + _currentWave);
        EmitSignal(SignalName.WaveChanged, _currentWave);
        SpawnWave();
    }

    private void SpawnWave()
    {
        if (EnemyScene == null || _spawnPoints.Count == 0)
        {
            GD.PrintErr("Enemy scene or spawn points not set up in WaveManager.");
            return;
        }

        int enemiesToSpawn = InitialZombiesPerWave * _currentWave;
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
        _enemiesContainer.AddChild(enemy, true);
    }

    public void StopWaves()
    {
        _waveTimer.Stop();
        GD.Print("WaveManager: Waves stopped.");
    }
}
