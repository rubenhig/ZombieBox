# Game State Management Plan

**Goal:** Implement a centralized Finite State Machine (FSM) to manage the game session lifecycle (Start, Pause, Game Over) in a modular and multiplayer-compatible way.

## 1. Component: `GameStateManager`
A dedicated manager responsible for the session flow, separate from the `GameManager` (which handles session data/scoring).

### Game States (Enum)
- **`WaitingToStart`**: Initial phase (intro/lobby). Entities are spawned but inactive.
- **`Playing`**: Main gameplay loop active.
- **`Paused`**: Temporary halt (Single-player only).
- **`GameOver`**: Match ended. Victory or defeat determined.

### Transitions Table

| From | To | Trigger | Action | Authority |
| :--- | :--- | :--- | :--- | :--- |
| `WaitingToStart` | `Playing` | Timer Timeout (Start Delay) | Enable `World`. | Server |
| `Playing` | `Paused` | Input `ESC` | Disable `World`. Show Pause Menu. | Local Client |
| `Paused` | `Playing` | Button `Resume` | Enable `World`. Hide Pause Menu. | Local Client |
| `Playing` | `GameOver` | Signal `Player.Died` (All Dead) | Disable `World`. Show Game Over. | Server |
| `GameOver` | `Playing` | Button `Restart` | Reload Game Session. | Server (Host) |
| `GameOver` | `Menu` | Button `Quit` | Load Main Menu scene. | Local Client |

## 2. Decoupling Strategy: `ProcessMode`
To avoid coupling Entities with the Manager, we utilize Godot's native **ProcessMode**.

*   **Structure:** `GameSession` has a `World` node (containing Players, Enemies, etc.) and a `Managers` node.
*   **Mechanism:** `GameStateManager` toggles the `ProcessMode` of the `World` container.
    *   **Playing:** `World.ProcessMode = Inherit` (Running).
    *   **GameOver/Paused:** `World.ProcessMode = Disabled` (Frozen).

**Benefit:** Entities (`Player`, `Enemy`) remain completely agnostic. They stop moving because the Engine stops processing them, not because they check a flag.

## 3. Implementation Steps

### Phase 1: Core FSM
- [x] Create `scripts/Core/GameStateManager.cs`.
- [x] Define the `GameState` enum and signals.
- [x] Add `GameStateManager` node to `GameSession.tscn` under `Managers`.
- [x] Inject reference to the `World` node into `GameStateManager`.

### Phase 2: Wiring & UI
- [x] Update `GameManager.cs` to notify `GameStateManager` on death.
- [x] Connect `GameStateManager` signals to `HUD` to show/hide panels.
- [x] Implement `SetState(GameState)` logic to toggle `World.ProcessMode`.
- [x] Fix: Use `CallDeferred` for `ProcessMode` changes to avoid physics engine locks.

### Phase 3: Multiplayer Sync
- [ ] Add `MultiplayerSynchronizer` to `GameStateManager` to ensure the global state (Playing/GameOver) is consistent across all clients.

## 4. Design Principles
- **Zero Coupling:** Entities do not know about the GameState.
- **Engine Native:** Leverage Godot's tree hierarchy and processing rules instead of custom flags.
