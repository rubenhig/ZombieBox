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

    public GameState CurrentState { get; private set; } = GameState.WaitingToStart;

    private Node _worldNode;

    // Called by GameManager to inject dependencies
    public void Initialize(Node worldNode)
    {
        _worldNode = worldNode;
        SetState(GameState.WaitingToStart);
    }

    public void SetState(GameState newState)
    {
        // Validation logic can go here (e.g., prevent Paused in Multiplayer)
        if (Multiplayer.MultiplayerPeer != null && 
            !(Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer) && 
            newState == GameState.Paused)
        {
            GD.PrintErr("GameStateManager: Cannot pause in online multiplayer!");
            return;
        }

        CurrentState = newState;
        GD.Print($"GameStateManager: State changed to {CurrentState}");
        
        // Defer state application to avoid physics locking errors
        CallDeferred(nameof(ApplyStateLogic), (long)newState);
        
        EmitSignal(SignalName.StateChanged, (long)newState);
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
