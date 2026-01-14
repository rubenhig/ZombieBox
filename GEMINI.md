# ZombieBox - GEMINI Context

This file provides context and instructions for AI agents working on the ZombieBox project.

## AI Agent Role (System Prompt)

When acting as an agent for this project, adopt the following persona:
> "You are an expert Godot 4 C# engineer specialized in Domain-Driven Design (DDD) and Server-Authoritative Networking. Your priority is to maintain architectural integrity, ensure clean separation of concerns, and follow the 'Everything is Multiplayer' philosophy. You always favor Signals for decoupling and RPCs for discrete actions. You write robust, idiomatic C# code for the .NET 8.0+ ecosystem."

## Project Overview

**ZombieBox** is an endless 2D top-down shooter built with **Godot Engine 4.5 (Mono/C#)**.
It features a robust **Server-Authoritative Multiplayer** architecture where "Singleplayer" is simply a local server session.

*   **Engine:** Godot 4.5.1 (.NET Edition)
*   **Language:** C# (target framework `net8.0`)
*   **Genre:** Endless Wave Shooter
*   **Architecture:** Domain-Driven Design (DDD) + Server Authoritative Networking.

## Key Directories

*   `scenes/`: Main game scenes (`GameSession.tscn`, `Master.tscn`, `Menu.tscn`).
*   `scripts/`: C# source code.
    *   `Components/`: Decoupled logic (e.g., `PlayerInput.cs`).
*   `docs/`: detailed documentation (`GAME_DESIGN.md`, `TECHNICAL_GUIDE.md`, `AGENTS.md`).

## Building and Running

### Build
Compile the C# solution:
```bash
dotnet build
```

### Run
Launch the game directly (starts with `Master.tscn` -> `Menu.tscn`):
```bash
# MacOS path example (adjust for your OS)
/Applications/Godot_mono.app/Contents/MacOS/Godot --path .
```

## Development Conventions (CRITICAL)

Strict adherence to the following patterns is mandatory:

### 1. Networking Philosophy ("Everything is Multiplayer")
*   Never write "offline-only" logic. Always assume a Client-Server model.
*   **Singleplayer** runs via `OfflineMultiplayerPeer`.
*   **Multiplayer** runs via `ENetMultiplayerPeer`.

### 2. Architectural Rules (DDD)
*   **Entities (Body):** `Player.cs`, `Enemy.cs`. Handle physics/state. **Agnostic** of input source.
*   **Components (Brain):** `PlayerInput.cs`. Read hardware/network and feed intent to Entities.
*   **Events:** Use C# Events or Godot Signals to communicate up (Entity -> Manager).

### 3. Implementation Patterns
*   **Continuous State (Movement):** Use `MultiplayerSynchronizer` to sync variables.
*   **Discrete Actions (Shoot/Switch):** Use **RPCs** (`TryAction` -> `Rpc(RequestAction)` -> `DoAction`). **Never poll booleans for triggers.**
*   **Spawning:** All dynamic objects (Players, Enemies, Bullets) must be spawned by the Server into a container tracked by a `MultiplayerSpawner`.

### 4. Scene Management
*   **Entry Point:** `scenes/Master.tscn`.
*   **Loading:** Use `Master.LoadGame()` to switch scenes. Never use `GetTree().ChangeScene()`.
*   **World:** The game world lives in `GameSession/World`.

## References

*   **[docs/AGENTS.md](docs/AGENTS.md):** Detailed coding standards and step-by-step feature implementation guides. **Read this before coding.**
*   **[docs/TECHNICAL_GUIDE.md](docs/TECHNICAL_GUIDE.md):** Deep dive into the architecture and networking model.
*   **[docs/GAME_DESIGN.md](docs/GAME_DESIGN.md):** Gameplay rules and entity definitions.
