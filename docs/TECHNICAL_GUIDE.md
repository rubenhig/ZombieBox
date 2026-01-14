# Technical Guide

## Architecture

### Scene Structure
*   **`scenes/Master.tscn`**: Persistent root. Orchestrates scene loading (`Menu` <-> `GameSession`).
*   **`scenes/GameSession.tscn`**: The gameplay container.
    *   `Managers/`: `WaveManager`, `MultiplayerSpawner`s.
    *   `World/`: `NavigationRegion2D`, `TileMap`, `SpawnPoints`.
    *   `World/Entities/`: Dynamic container for Players, Enemies, and Bullets (Y-Sorted).
    *   `UI/`: `HUD`.

### Code Patterns (DDD)
*   **Entities (`Player.cs`):** Handle physics and state logic. Agnostic of input source.
*   **Components (`PlayerInput.cs`):** Handle input reading. Only active on the Authority client.
*   **Events:** Decoupled communication via Signals (e.g., `Player.Died` -> `GameManager`).

## Networking Model

### Core Philosophy
**"Singleplayer is a Local Server."**
The game always runs with a server-authoritative logic. Singleplayer uses `OfflineMultiplayerPeer`.

### Authority & Synchronization
1.  **Server Authority:** The Server owns game state (Enemy positions, Health, Wave logic).
2.  **Client Authority:** Clients own their Input intent (`MoveVector`).
3.  **Synchronization:**
    *   `MultiplayerSynchronizer`: Replicates continuous data (Position, Input Vector).
    *   `RPC`: Handles discrete actions (Pistol Fire, Weapon Switch).

### Input Flow
1.  **Input:** `PlayerInput` (Client) reads hardware -> Updates `MoveVector` (Synced).
2.  **Action:** `PlayerInput` detects Trigger -> Calls `Player.TryAction()` -> Sends RPC to Server.
3.  **Execution:** Server receives Input/RPC -> Executes Logic -> Replicates State/Spawn via Spawners.

---

## DDD Implementation Strategy

We apply Domain-Driven Design principles to decouple core gameplay from infrastructure (networking/input).

1.  **Entities (Domain):** Objects with identity and state (e.g., `Player`, `Enemy`). They own the "Rules of the Game" (Physics, Health). They are agnostic of networking.
2.  **Components (Infrastructure):** Decoupled logic units that feed data to Entities. `PlayerInput` is an infrastructure component that translates hardware/network into "Intention".
3.  **Events (Decoupling):** We use Signals as **Domain Events**. Entities emit signals (e.g., `Died`) when their state changes significantly. Other systems (UI, Game Managers) listen to these events to react, keeping the entities clean and independent.
