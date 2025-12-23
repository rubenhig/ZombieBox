# Game Architecture

This document details the proposed architecture for the ZombieBox project, focusing on a modular, scalable, and multiplayer-ready design that is idiomatic to Godot Engine.

## Folder Structure

A clean separation of concerns is enforced through the following directory structure:

-   `scenes/`: Contains all main game scenes (`.tscn` files).
    -   Examples: `main.tscn`, `player.tscn`, `enemy.tscn`.
-   `scripts/` (or `src/`): Contains all C# source code files (`.cs`). Scripts are organized by functionality.
    -   Examples: `Player.cs`, `Enemy.cs`, `NetworkManager.cs`.
-   `assets/`: All game resources, such as graphics, sound effects, and music.
    -   Subdirectories: `assets/sprites/`, `assets/sounds/`.
-   `addons/`: Reserved for any future Godot plugins.

File and folder names will use `snake_case` (e.g., `player_scene.tscn`) to ensure cross-platform compatibility, while C# class names will use `PascalCase`.

## Scene and Node Composition

The game is built around modular, reusable scenes. Each major game entity is its own self-contained scene.

### Main Scene Hierarchy

Here is the proposed node structure for the main game scene:

```text
Main.tscn (Node or Node2D)
├── NetworkManager (Node) - Manages network sessions (can be an Autoload singleton).
├── Game (Node2D) - Container for the game world and logic.
│   ├── Player (CharacterBody2D) - Instanced from player.tscn for the local player.
│   │   ├── Sprite2D - Visual representation.
│   │   └── CollisionShape2D - Physics body.
│   ├── (Remote Players) - Additional Player instances for connected clients.
│   ├── Enemies (Node2D) - Container for active enemy instances.
│   │   └── *Enemy (CharacterBody2D)* - Instanced from enemy.tscn.
│   ├── WaveManager (Node) - Controls enemy spawning and waves.
│   │   └── SpawnPoint1, SpawnPoint2, ... (Marker2D)
│   └── (World Elements) - TileMap, obstacles, etc.
└── UI (CanvasLayer) - User interface layer.
    ├── HUD (Control) - Heads-up display (health, score, wave count).
    └── (Other UI Elements) - Game Over screen, menus.
```

### Key Components

-   **Main.tscn**: The game's entry point. It orchestrates the main components but contains little to no game logic itself.
-   **NetworkManager**: A global singleton (Autoload) or a dedicated node responsible for creating, joining, and managing multiplayer game sessions. It handles all low-level networking.
-   **Game**: A Node2D that represents the actual gameplay area. It contains all players, enemies, and other world entities.
-   **Player.tscn**: A `CharacterBody2D`-based scene representing a player. It encapsulates player logic, including movement and shooting. There will be one instance per connected player.
-   **Enemy.tscn**: A `CharacterBody2D`-based scene for enemies. It contains the AI logic for chasing players.
-   **WaveManager**: A node that procedurally spawns enemies in waves of increasing difficulty.
-   **UI**: A `CanvasLayer` that renders the HUD and other UI elements, keeping them separate from the game world.

This architecture promotes encapsulation and modularity. Communication between nodes is handled primarily through signals and well-defined methods, minimizing hard dependencies and making the codebase easier to maintain and expand.
