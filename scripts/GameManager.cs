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
	private LobbyScreen _lobbyScreen;
	private GameStateManager _gameStateManager;
	private int _connectedPlayers = 0;
	private int _playersAlive = 0;
	private const int MinPlayersToStart = 2; // TODO: Make configurable via args

	public override void _Ready()
	{
		GD.Print("GameManager: _Ready started.");
		
		// Locate HUD (Still magic string for UI, acceptable or can be exported too)
		_hud = GetNode<HUD>("UI/HUD");
		// Signal connection moved to OnGameStateChanged for clearer flow
		
		// Locate LobbyScreen
		_lobbyScreen = GetNode<LobbyScreen>("UI/LobbyScreen");

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
			
			_gameStateManager.StateChanged += OnGameStateChanged;
			
			// Don't auto-start here anymore. Wait for players.
			if (Multiplayer.IsServer())
			{
				_gameStateManager.SetState(GameState.WaitingToStart);
			}
			else
			{
				// Client: Sync initial state visual
				OnGameStateChanged((long)_gameStateManager.CurrentState);
			}
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
		_connectedPlayers++;
		_playersAlive++;
		GD.Print($"GameManager: Player spawned. Total: {_connectedPlayers}. Alive: {_playersAlive}");

		// Register player with HUD using stored reference
		if (_hud != null)
		{
			_hud.RegisterPlayer(player);
		}

		// Listen to death. We handle the "Check if all dead" logic locally on Server.
		player.Died += OnPlayerDied;

		CheckStartConditions();
	}

	private void OnPlayerDied()
	{
		// Only Server decides Game Over
		if (!Multiplayer.IsServer()) return;

		_playersAlive--;
		GD.Print($"GameManager: Player died. Alive: {_playersAlive}");

		if (_playersAlive <= 0)
		{
			GameOver();
		}
	}

	private void OnGameStateChanged(long stateIdx)
	{
		GameState state = (GameState)stateIdx;
		GD.Print($"GameManager: Handling state change to {state}");

		if (_lobbyScreen != null)
		{
			_lobbyScreen.Visible = (state == GameState.WaitingToStart);
			
			// If waiting, update status text
			if (state == GameState.WaitingToStart)
			{
				if (Multiplayer.IsServer())
					_lobbyScreen.UpdateStatus(_connectedPlayers, MinPlayersToStart);
				else
					_lobbyScreen.UpdateStatus(0, 0); // Client placeholder
			}
		}

		if (state == GameState.Playing)
		{
			// Start Waves only if we are the server (WaveManager logic runs on server)
			if (Multiplayer.IsServer() && WaveManager != null)
			{
				WaveManager.StartWaves();
			}
		}
		
		if (state == GameState.GameOver)
		{
			if (_hud != null) _hud.ShowGameOver();
		}
	}

	private void CheckStartConditions()
	{
		// Logic:
		// 1. If SinglePlayer (OfflinePeer) -> Start immediately (1 player is enough).
		// 2. If Dedicated Server -> Wait for MinPlayersToStart.
		// 3. If Client -> We just wait for server to change state (replicated via FSM).

		if (!Multiplayer.IsServer()) return; // Clients don't decide.

		bool isOffline = Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer;
		
		if (isOffline)
		{
			GD.Print("GameManager: SinglePlayer detected. Starting game immediately.");
			_gameStateManager.SetState(GameState.Playing);
		}
		else
		{
			// Online / Dedicated
			if (_connectedPlayers >= MinPlayersToStart)
			{
				GD.Print($"GameManager: Required players reached ({_connectedPlayers}/{MinPlayersToStart}). Starting game!");
				_gameStateManager.SetState(GameState.Playing);
			}
			else
			{
				GD.Print($"GameManager: Waiting for players... ({_connectedPlayers}/{MinPlayersToStart})");
				// Force UI update on server side immediately
				if (_lobbyScreen != null) _lobbyScreen.UpdateStatus(_connectedPlayers, MinPlayersToStart);
			}
		}
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
