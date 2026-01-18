using Godot;
using System;
using System.Linq;

public partial class GameManager : Node
{
	[Signal]
	public delegate void GameEndedEventHandler();

	[Export]
	public PackedScene LevelScene { get; set; }

	[Export]
	public WaveManager WaveManager { get; set; }

	// public bool IsGameOver { get; private set; } = false; // Replaced by FSM

	private HUD _hud;
	private GameStateManager _gameStateManager;

	public override void _Ready()
	{
		GD.Print("GameManager: _Ready started.");
		
		// Locate HUD (Still magic string for UI, acceptable or can be exported too)
		_hud = GetNode<HUD>("UI/HUD");
		if (_hud != null)
		{
			// Connect signal to HUD, but logic is driven by FSM
			GameEnded += _hud.ShowGameOver; 
		}

		_gameStateManager = GetNode<GameStateManager>("Managers/GameStateManager");
		GD.Print($"GameManager: GameStateManager found: {_gameStateManager != null}");

		// Load Level
		if (LevelScene == null)
		{
			LevelScene = GD.Load<PackedScene>("res://scenes/maps/Arena01.tscn");
		}

		var levelContainer = GetNode("World/Level");
		GD.Print("GameManager: Instantiating level...");
		var levelNode = LevelScene.Instantiate();
		levelContainer.AddChild(levelNode);
		GD.Print("GameManager: Level instantiated.");

		// Initialize FSM with World node
		if (_gameStateManager != null)
		{
			var world = GetNode("World");
			GD.Print("GameManager: Initializing FSM with World...");
			_gameStateManager.Initialize(world);
			// Start game immediately for now (transitions to Playing)
			_gameStateManager.SetState(GameState.Playing);
		}

		// Magic String for container is acceptable as it's internal to GameSession structure
		var playersNode = GetNode("World/Entities/Players");
		var enemiesNode = GetNode("World/Entities/Enemies");

		if (levelNode is Level level)
		{
			// Safe access via property
			if (WaveManager != null)
			{
				GD.Print("GameManager: Configuring WaveManager...");
				WaveManager.Configure(level.SpawnPoints, enemiesNode);
			}
		}
		else
		{
			GD.PrintErr("GameManager: Loaded level does not have Level script attached!");
		}
		
		// Notify NetworkManager that the game level is ready
		var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		
		if (networkManager != null)
		{
			GD.Print("GameManager: Notifying NetworkManager...");
			networkManager.PlayerSpawned += OnPlayerSpawned;
			networkManager.OnGameLevelLoaded(playersNode, enemiesNode);
		}
		
		GD.Print("GameManager: _Ready completed.");
	}

	private void OnPlayerSpawned(Player player)
	{
		// Register player with HUD using stored reference
		if (_hud != null)
		{
			_hud.RegisterPlayer(player);
		}

		// Clean DDD: Listen to the player's death to trigger game over
		player.Died += GameOver;
	}

	public void GameOver()
	{
		GD.Print("GameManager: Triggering Game Over via FSM");
		
		if (_gameStateManager != null)
		{
			_gameStateManager.SetState(GameState.GameOver);
		}

		// Legacy signal for HUD (until HUD subscribes to FSM)
		EmitSignal(SignalName.GameEnded);
	}
}
