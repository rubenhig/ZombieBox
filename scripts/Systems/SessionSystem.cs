using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public partial class SessionSystem : Node
{
    [Signal]
    public delegate void GameEndedEventHandler();

    [Export]
    public PackedScene LevelScene { get; set; }

    [Export]
    public WaveSystem WaveSystem { get; set; }

    [Export]
    public SpawnSystem SpawnSystem { get; set; }

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
        GD.Print("SessionSystem: _Ready started.");

        // 1. Locate and Cache Dependencies
        _hud = GetNode<HUD>("UI/HUD");
        _lobbyScreen = GetNode<LobbyScreen>("UI/LobbyScreen");
        _gameStateManager = GetNode<GameStateManager>("Managers/GameStateManager");

        var playersNode = GetNode("World/Entities/Players");
        var enemiesNode = GetNode("World/Entities/Enemies");
        var bulletsNode = GetNode("World/Entities/Bullets");
        _playersContainer = playersNode;

        // 2. Configure SpawnSystem with entity containers
        if (SpawnSystem != null)
        {
            SpawnSystem.Configure(playersNode, enemiesNode, bulletsNode);
        }

        // 3. Client-Side Reactive Logic: Listen for ANY player appearing in the scene
        _playersContainer.ChildEnteredTree += OnPlayerNodeAdded;

        // 4. Load Level Resource
        if (LevelScene == null)
            LevelScene = GD.Load<PackedScene>("res://scenes/maps/Arena01.tscn");

        var levelContainer = GetNode("World/Level");
        var levelNode = LevelScene.Instantiate();
        levelContainer.AddChild(levelNode);

        // 4. Initialize Game State & Wave Systems
        if (_gameStateManager != null)
        {
            _gameStateManager.Initialize(GetNode("World"));
            _gameStateManager.StateChanged += OnGameStateChanged;

            // Server Logic: Set initial State
            if (NetworkUtils.IsServer())
            {
                _gameStateManager.SetState(GameState.WaitingToStart);
            }
            else
            {
                // Client Logic: Sync initial UI
                OnGameStateChanged((long)_gameStateManager.CurrentState);
            }
        }

        if (levelNode is Level level && WaveSystem != null)
        {
            WaveSystem.Configure(level.SpawnPoints, SpawnSystem);
        }

        // 5. Network Session Managment
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        // 6. Handle Local Host / Singleplayer immediately
        if (NetworkUtils.IsServer())
        {
            // Check if we are running as a Dedicated Server (Headless)
            bool isHeadless = DisplayServer.GetName() == "headless";

            if (!isHeadless)
            {
                // Listen Server (Host plays)
                OnPeerConnected(1);
            }
            else
            {
                GD.Print("SessionSystem: Running in Headless Mode (Dedicated Server). ID 1 will NOT be a player.");
            }

            // Add already connected clients (if any)
            foreach (int id in Multiplayer.GetPeers())
            {
                OnPeerConnected(id);
            }
        }

        GD.Print("SessionSystem: _Ready completed.");
    }

    // --- 1. Session Management (Server Only Logic) ---

    private void OnPeerConnected(long id)
    {
        if (!NetworkUtils.IsServer()) return;

        GD.Print($"SessionSystem: Peer {id} joined session.");
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
        GD.Print($"SessionSystem: Peer {id} left session.");
        _connectedPeerIds.Remove(id);

        if (NetworkUtils.IsServer() && _playersContainer != null)
        {
            var player = _playersContainer.GetNodeOrNull(id.ToString());
            player?.QueueFree();
        }

        CheckStartConditions();
    }

    private void CheckStartConditions()
    {
        if (!NetworkUtils.IsServer()) return;

        bool isOffline = Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer;
        int currentCount = _connectedPeerIds.Count;

        if (isOffline)
        {
            if (_gameStateManager.CurrentState == GameState.WaitingToStart)
            {
                GD.Print("SessionSystem: Offline Mode - Starting immediately.");
                _gameStateManager.SetState(GameState.Playing);
            }
        }
        else
        {
            _connectedPlayers = currentCount;

            if (currentCount >= MinPlayersToStart && _gameStateManager.CurrentState == GameState.WaitingToStart)
            {
                GD.Print($"SessionSystem: Threshold reached ({currentCount}/{MinPlayersToStart}). Starting Game!");
                _gameStateManager.SetState(GameState.Playing);
            }
            else if (_gameStateManager.CurrentState == GameState.WaitingToStart)
            {
                GD.Print($"SessionSystem: Waiting for players... ({currentCount}/{MinPlayersToStart})");
                if (_lobbyScreen != null) _lobbyScreen.UpdateStatus(currentCount, MinPlayersToStart);
            }
        }
    }

    // --- 2. State & Gameplay Logic ---

    private void OnGameStateChanged(long stateIdx)
    {
        GameState state = (GameState)stateIdx;
        GD.Print($"SessionSystem: State Changed -> {state}");

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
            if (NetworkUtils.IsServer())
            {
                GD.Print("SessionSystem: Game Starting! Spawning all players...");
                foreach (long id in _connectedPeerIds)
                {
                    SpawnPlayer(id);
                }

                if (WaveSystem != null) WaveSystem.StartWaves();
            }
        }

        if (state == GameState.GameOver)
        {
            if (_hud != null) _hud.ShowGameOver();
        }
    }

    // --- 3. Spawning (delegates to SpawnSystem) ---

    private void SpawnPlayer(long id)
    {
        if (!NetworkUtils.IsServer()) return;

        if (SpawnSystem == null)
        {
            GD.PrintErr("SessionSystem: SpawnSystem not configured!");
            return;
        }

        SpawnSystem.SpawnPlayer(id);
    }

    // --- 4. Client Reactive System ---

    private void OnPlayerNodeAdded(Node node)
    {
        if (node is Player player)
        {
            GD.Print($"SessionSystem: Player detected in scene: {player.Name}");

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
        if (!NetworkUtils.IsServer()) return;

        _playersAlive--;
        GD.Print($"SessionSystem: Player Died. Alive: {_playersAlive}");

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
