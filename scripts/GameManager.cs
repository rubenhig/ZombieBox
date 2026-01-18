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

	public bool IsGameOver { get; private set; } = false;

	private HUD _hud;

	public override void _Ready()
	{
		// Locate HUD (Still magic string for UI, acceptable or can be exported too)
		_hud = GetNode<HUD>("UI/HUD");
		if (_hud != null)
		{
			GameEnded += _hud.ShowGameOver;
		}

		// Load Level
		if (LevelScene == null)
		{
			LevelScene = GD.Load<PackedScene>("res://scenes/maps/Arena01.tscn");
		}

		var levelContainer = GetNode("World/Level");
		var levelNode = LevelScene.Instantiate();
		levelContainer.AddChild(levelNode);

		// Magic String for container is acceptable as it's internal to GameSession structure
		var playersNode = GetNode("World/Entities/Players");
		var enemiesNode = GetNode("World/Entities/Enemies");

		if (levelNode is Level level)
		{
			// Safe access via property
			if (WaveManager != null)
			{
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
			networkManager.PlayerSpawned += OnPlayerSpawned;
			networkManager.OnGameLevelLoaded(playersNode, enemiesNode);
		}
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
		if (IsGameOver) return;
		IsGameOver = true;
		
		GD.Print("GameManager: Game Over!");
		EmitSignal(SignalName.GameEnded);

		// Stop spawning enemies
		if (WaveManager != null)
		{
			WaveManager.StopWaves();
		}
	}
}
