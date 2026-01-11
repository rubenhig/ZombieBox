using Godot;
using System;
using System.Linq;

public partial class GameManager : Node
{
	[Signal]
	public delegate void GameEndedEventHandler();

	public bool IsGameOver { get; private set; } = false;

	public override void _Ready()
	{
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
		// Find HUD and connect it to the new player
		// Assuming HUD is at ../UI/HUD relative to GameManager (Main)
		var hud = GetNodeOrNull<HUD>("../UI/HUD");
		if (hud == null) hud = GetTree().Root.FindChild("HUD", true, false) as HUD;
		
		if (hud != null)
		{
			hud.RegisterPlayer(player);
		}
	}

	public void GameOver()
	{
		if (IsGameOver) return;
		IsGameOver = true;
		
		GD.Print("GameManager: Game Over!");
		EmitSignal(SignalName.GameEnded);

		// Stop spawning enemies
		var waveManager = GetNodeOrNull<WaveManager>("../Game/WaveManager"); // Adjust path if needed, assuming GameManager is sibling to Game or root
		if (waveManager != null)
		{
			waveManager.StopWaves();
		}
		else 
		{
             // Fallback search if path structure varies
             waveManager = GetTree().Root.FindChild("WaveManager", true, false) as WaveManager;
             waveManager?.StopWaves();
		}
	}
}
