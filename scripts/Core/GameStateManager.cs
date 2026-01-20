using Godot;
using System;

public enum GameState
{
    WaitingToStart,
    Playing,
    Paused, // Singleplayer only
    GameOver
}

public partial class GameStateManager : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(long newState);

    private GameState _currentState = GameState.WaitingToStart;

    [Export]
    public GameState CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState != value)
            {
                _currentState = value;
                GD.Print($"GameStateManager: State synced/changed to {_currentState}");
                CallDeferred(nameof(ApplyStateLogic), (long)_currentState);
                EmitSignal(SignalName.StateChanged, (long)_currentState);
            }
        }
    }

    private Node _worldNode;

    // Called by GameManager to inject dependencies
    public void Initialize(Node worldNode)
    {
        _worldNode = worldNode;
        // Don't reset state here if it was already synced by network!
        // Only set if we are server/offline
        if (Multiplayer.IsServer())
        {
            SetState(GameState.WaitingToStart);
        }
        else
        {
            // Apply initial state received (or default)
            ApplyStateLogic((long)CurrentState);
        }
    }

    public void SetState(GameState newState)
    {
        // Only server can set state
        if (!Multiplayer.IsServer()) return;

        // Validation logic can go here (e.g., prevent Paused in Multiplayer)
        if (Multiplayer.MultiplayerPeer != null && 
            !(Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer) && 
            newState == GameState.Paused)
        {
            GD.PrintErr("GameStateManager: Cannot pause in online multiplayer!");
            return;
        }

        // Setting the property triggers the logic locally and updates the value for sync
        CurrentState = newState;
    }

    private void ApplyStateLogic(long stateIdx)
    {
        GameState state = (GameState)stateIdx;
        if (_worldNode == null) return;

        switch (state)
        {
            case GameState.WaitingToStart:
                // World exists but logic is frozen until start
                _worldNode.ProcessMode = ProcessModeEnum.Disabled;
                break;
            case GameState.Playing:
                _worldNode.ProcessMode = ProcessModeEnum.Inherit;
                break;
            case GameState.Paused:
                _worldNode.ProcessMode = ProcessModeEnum.Disabled;
                break;
            case GameState.GameOver:
                // Freeze game but keep UI running
                _worldNode.ProcessMode = ProcessModeEnum.Disabled;
                break;
        }
    }
}
