using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public partial class GameManager : Node
{
    [Signal]
    public delegate void GameEndedEventHandler();

    [Export]
    public PackedScene LevelScene { get; set; }

    [Export]
    public WaveManager WaveManager { get; set; }

    [Export]
    public PackedScene PlayerScene { get; set; }

    // Peer Tracking
    private HashSet<long> _connectedPeerIds = new HashSet<long>();

    // Players Container Reference
    private Node _playersContainer;

    private HUD _hud;
    private LobbyScreen _lobbyScreen;
    private GameStateManager _gameStateManager;
    private int _connectedPlayers = 0; // Used for UI display, not actual peer count
    private int _playersAlive = 0;
    private const int MinPlayersToStart = 2; // TODO: Make configurable via args

    public override void _Ready()
    {
        GD.Print("GameManager: _Ready started.");

        // 1. Locate and Cache Dependencies
        _hud = GetNode<HUD>("UI/HUD");
        _lobbyScreen = GetNode<LobbyScreen>("UI/LobbyScreen");
        _gameStateManager = GetNode<GameStateManager>("Managers/GameStateManager");

        var playersNode = GetNode("World/Entities/Players");
        var enemiesNode = GetNode("World/Entities/Enemies");
        _playersContainer = playersNode;

        // 2. Client-Side Reactive Logic: Listen for ANY player appearing in the scene
        _playersContainer.ChildEnteredTree += OnPlayerNodeAdded;

        // 3. Load Level & Player Resources
        if (LevelScene == null)
            LevelScene = GD.Load<PackedScene>("res://scenes/maps/Arena01.tscn");

        if (PlayerScene == null)
            PlayerScene = GD.Load<PackedScene>("res://scenes/player.tscn");

        var levelContainer = GetNode("World/Level");
        var levelNode = LevelScene.Instantiate();
        levelContainer.AddChild(levelNode);

        // 4. Initialize Game State & Wave Systems
        if (_gameStateManager != null)
        {
            _gameStateManager.Initialize(GetNode("World"));
            _gameStateManager.StateChanged += OnGameStateChanged;

            // Server Logic: Set initial State
            if (Multiplayer.IsServer())
            {
                _gameStateManager.SetState(GameState.WaitingToStart);
            }
            else
            {
                // Client Logic: Sync initial UI
                OnGameStateChanged((long)_gameStateManager.CurrentState);
            }
        }

        if (levelNode is Level level && WaveManager != null)
        {
            WaveManager.Configure(level.SpawnPoints, enemiesNode);
        }

        // 5. Network Session Managment
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        // 6. Handle Local Host / Singleplayer immediately
        if (Multiplayer.IsServer())
        {
            // Check if we are running as a Dedicated Server (Headless)
            // In Godot 4, headless mode is detected via DisplayServer name
            bool isHeadless = DisplayServer.GetName() == "headless";

            // Also checking command line args as a fallback or explicit override if "headless" isn't set on some OS
            // But for now, let's rely on the concept: "If I am ID 1, do I play?"
            // A dedicated server (ID 1) does NOT play.

            if (!isHeadless)
            {
                // Listen Server (Host plays)
                OnPeerConnected(1);
            }
            else
            {
                GD.Print("GameManager: Running in Headless Mode (Dedicated Server). ID 1 will NOT be a player.");
            }

            // Add already connected clients (if any)
            foreach (int id in Multiplayer.GetPeers())
            {
                OnPeerConnected(id);
            }
        }

        GD.Print("GameManager: _Ready completed.");
    }

    // --- 1. Session Management (Server Only Logic) ---

    private void OnPeerConnected(long id)
    {
        if (!Multiplayer.IsServer()) return;

        GD.Print($"GameManager: Peer {id} joined session.");
        _connectedPeerIds.Add(id);

        // If we are ALREADY playing, late-join spawn immediately
        if (_gameStateManager.CurrentState == GameState.Playing)
        {
            SpawnPlayer(id);
        }

        CheckStartConditions();
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"GameManager: Peer {id} left session.");
        _connectedPeerIds.Remove(id);

        if (Multiplayer.IsServer() && _playersContainer != null)
        {
            var player = _playersContainer.GetNodeOrNull(id.ToString());
            player?.QueueFree();
        }

        CheckStartConditions();
    }

    private void CheckStartConditions()
    {
        if (!Multiplayer.IsServer()) return;

        bool isOffline = Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer;
        int currentCount = _connectedPeerIds.Count;

        if (isOffline)
        {
            if (_gameStateManager.CurrentState == GameState.WaitingToStart)
            {
                GD.Print("GameManager: Offline Mode - Starting immediately.");
                _gameStateManager.SetState(GameState.Playing);
            }
        }
        else
        {
            _connectedPlayers = currentCount;

            if (currentCount >= MinPlayersToStart && _gameStateManager.CurrentState == GameState.WaitingToStart)
            {
                GD.Print($"GameManager: Threshold reached ({currentCount}/{MinPlayersToStart}). Starting Game!");
                _gameStateManager.SetState(GameState.Playing);
            }
            else if (_gameStateManager.CurrentState == GameState.WaitingToStart)
            {
                GD.Print($"GameManager: Waiting for players... ({currentCount}/{MinPlayersToStart})");
                if (_lobbyScreen != null) _lobbyScreen.UpdateStatus(currentCount, MinPlayersToStart);
            }
        }
    }

    // --- 2. State & Gameplay Logic ---

    private void OnGameStateChanged(long stateIdx)
    {
        GameState state = (GameState)stateIdx;
        GD.Print($"GameManager: State Changed -> {state}");

        if (_lobbyScreen != null)
        {
            _lobbyScreen.Visible = (state == GameState.WaitingToStart);
            if (state == GameState.WaitingToStart)
            {
                _lobbyScreen.UpdateStatus(_connectedPeerIds.Count, MinPlayersToStart);
            }
        }

        if (state == GameState.Playing)
        {
            if (Multiplayer.IsServer())
            {
                GD.Print("GameManager: Game Starting! Spawning all players...");
                foreach (long id in _connectedPeerIds)
                {
                    SpawnPlayer(id);
                }

                if (WaveManager != null) WaveManager.StartWaves();
            }
        }

        if (state == GameState.GameOver)
        {
            if (_hud != null) _hud.ShowGameOver();
        }
    }

    // --- 3. Spawning System (Server Authoritative) ---

    private void SpawnPlayer(long id)
    {
        if (!Multiplayer.IsServer()) return;

        if (_playersContainer.HasNode(id.ToString()))
        {
            GD.PrintErr($"GameManager Warning: Player {id} already exists. Skipping spawn.");
            return;
        }

        GD.Print($"GameManager: Spawning Avatar for Peer {id}");

        Player player = PlayerScene.Instantiate<Player>();
        player.Name = id.ToString();
        _playersContainer.AddChild(player, true);

        player.SetMultiplayerAuthority(1);

        var input = player.GetNodeOrNull("PlayerInput");
        if (input != null)
        {
            input.SetMultiplayerAuthority((int)id);
        }
    }

    // --- 4. Client Reactive System ---

    private void OnPlayerNodeAdded(Node node)
    {
        if (node is Player player)
        {
            GD.Print($"GameManager: Player detected in scene: {player.Name}");

            _connectedPlayers++;
            _playersAlive++;

            if (_hud != null)
            {
                _hud.RegisterPlayer(player);
            }

            player.Died += OnPlayerDied;
        }
    }

    private void OnPlayerDied()
    {
        if (!Multiplayer.IsServer()) return;

        _playersAlive--;
        GD.Print($"GameManager: Player Died. Alive: {_playersAlive}");

        if (_playersAlive <= 0)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        if (_gameStateManager != null)
            _gameStateManager.SetState(GameState.GameOver);

        EmitSignal(SignalName.GameEnded);
    }
}
