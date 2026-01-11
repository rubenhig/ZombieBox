# Game Architecture

This document details the architecture for the ZombieBox project. We follow a **Clean Architecture** approach adapted for Godot, emphasizing separation of concerns, scalability, and a "Server Authoritative" model that treats Singleplayer as a local multiplayer session.

## Core Design Philosophy: "Everything is Multiplayer"

To avoid code duplication and architectural splits between "Online" and "Offline" modes, we adopt the following mantra:
**"Singleplayer is just a Local Multiplayer session with one player."**

-   **Unified Codebase:** We do not write separate logic for singleplayer. We write multiplayer logic, and singleplayer is simply a hosted game with no external clients.
-   **Server Authority:** The server (Host) always owns the game state (physics, AI, damage). Clients (including the local host player) only send **Inputs** and receive **Visual Updates**.

## Architectural Patterns (DDD & Composition)

We separate **Game Logic (Domain)** from **Infrastructure (Input/Network)**. Entities should be "agnostic" of where commands come from.

### 1. Entity Agnosticism
Game entities like `Player` or `Enemy` should focus on *what* they do, not *how* they are told to do it.
-   **Bad:** `Player.cs` checking `Input.IsActionPressed` inside `_PhysicsProcess`.
-   **Good:** `Player.cs` reading a `CurrentMovementVector` property, which is populated by an external controller.

### 2. Input Decoupling (The Controller Pattern)
We separate the "Brain" from the "Body".
-   **The Body (`Player.cs`):** Handles physics, collisions, animations, and state (Health, Ammo). It exposes methods like `Move(velocity)` or `SetInputDirection(vector)`.
-   **The Brain (`PlayerInput` Component):** A specialized Node/Component responsible for feeding data to the Body.
    -   **LocalInput:** Reads Keyboard/Mouse/Gamepad.
    -   **NetworkSync:** Reads synchronized data from the server (for remote puppets).
    -   **AIController:** Feeds calculated data (for bots).

### 3. Server Authoritative Flow
1.  **Client (Input):** The `PlayerInput` component reads local hardware input and sends it to the Server (via RPC or `MultiplayerSynchronizer`).
2.  **Server (Processing):** The Server receives the input. The server-side `Player` entity applies this input to its physics simulation.
3.  **Server (State Update):** The Server calculates the result (new position, health change) and replicates the State back to all Clients.
4.  **Client (Rendering):** Clients receive the State (e.g., Position) and update their visual representation (interpolating for smoothness).

## Folder Structure

A clean separation of concerns is enforced through the following directory structure:

-   `scenes/`: Contains all main game scenes (`.tscn` files).
    -   `scenes/Master.tscn`: Persistent root scene (Autoloads & Scene Manager).
    -   `scenes/Menu.tscn`: UI entry point.
    -   `scenes/Main.tscn`: The gameplay container.
-   `scripts/`: Contains all C# source code files (`.cs`), organized by domain.
    -   `scripts/Core/`: Base classes and managers (`Master.cs`, `GameManager.cs`).
    -   `scripts/Network/`: Networking infrastructure (`NetworkManager.cs`).
    -   `scripts/Entities/`: Game objects (`Player.cs`, `Enemy.cs`).
    -   `scripts/Components/`: Reusable behaviors (`PlayerInput.cs`, `HealthComponent.cs`).
-   `assets/`: All game resources.

## Scene and Node Composition

### Master Hierarchy (Persistent)
The application starts with `Master.tscn`, which never unloads.
```text
Master (Node) - [Master.cs] - Scene Switcher & Orchestrator
├── NetworkManager (Autoload) - Handles connections/session.
└── CurrentScene (Node) - Dynamically holds Menu or Main.
```

### Main Scene Hierarchy (Gameplay)
```text
Main.tscn (Node)
├── Game (Node2D)
│   ├── MultiplayerSpawner (Node) - Auto-spawns Players/Enemies for clients.
│   ├── Players (Node2D)
│   │   └── Player (CharacterBody2D) - [Player.cs]
│   │       ├── PlayerInput (Node) - [PlayerInput.cs] (Handles Input/Sync)
│   │       ├── Sprite2D
│   │       └── CollisionShape2D
│   ├── Enemies (Node2D)
│   ├── WaveManager (Node) - [WaveManager.cs] (Server Only logic)
│   └── SpawnPoints (Node)
└── UI (CanvasLayer)
    ├── HUD (Control)
    └── Game Over Panel
```

## Implementation Guidelines

1.  **Naming:** PascalCase for C# classes/filenames. snake_case for Scenes/Nodes (Godot standard).
2.  **Dependencies:** Prefer **Dependency Injection** (assigning references in `_Ready` or via `[Export]`) over deep `GetNode` chains.
3.  **Network Logic:**
    -   Use `Multiplayer.IsServer()` to guard logic that changes Game State (Health, Spawning, Physics).
    -   Use `IsMultiplayerAuthority()` to guard Input logic (only the owner controls their input).