using Godot;
using System;
using System.Linq;

public partial class GameManager : Node
{
	[Signal]
	public delegate void GameEndedEventHandler();

	public bool IsGameOver { get; private set; } = false;

	private HUD _hud;

	public override void _Ready()
	{
		// Locate HUD
		_hud = GetNode<HUD>("UI/HUD");
		if (_hud != null)
		{
			GameEnded += _hud.ShowGameOver;
		}

		// Notify NetworkManager that the game level is ready
		// and provide the containers for spawning
		var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		
		var playersNode = GetNode("Game/Players");
		var enemiesNode = GetNode("Game/Enemies");
		
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

		// Stop spawning enemies - Fix path: GameManager is on "Main", WaveManager is child of "Game"
		var waveManager = GetNodeOrNull<WaveManager>("Game/WaveManager"); 
		if (waveManager != null)
		{
			waveManager.StopWaves();
		}
	}
}
