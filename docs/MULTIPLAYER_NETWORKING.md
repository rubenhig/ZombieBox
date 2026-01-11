# Multiplayer and Networking Model

This document describes the networking architecture for the game, which is designed from the ground up to support online multiplayer.

## Core Principles

-   **Authoritative Server**: The game uses a server-authoritative model. One player acts as the host (the server), or a dedicated server is used. The server is the single source of truth for all game state.
-   **Client Responsibilities**: Clients are responsible for sending their input to the server and rendering the game state they receive from the server. They do not simulate game logic locally.
-   **Godot High-Level Multiplayer API**: The implementation will leverage Godot's built-in high-level networking API, which uses RPCs (Remote Procedure Calls) and object synchronization over ENet (UDP).

## Server-Authoritative Logic

In this model, the server has the final say on everything that happens in the game.

-   **Physics and Logic**: The server is solely responsible for:
    -   Moving all characters (players and enemies).
    -   Processing player inputs.
    -   Calculating all physics and collisions (e.g., bullets hitting enemies).
    -   Spawning enemies and managing game waves.
    -   Determining win/loss conditions.

-   **Client Actions**:
    -   Clients capture player input (e.g., movement direction, shooting action).
    -   This input is sent to the server via an RPC.
    -   Clients receive state updates from the server (e.g., new positions of all objects) and apply them, ensuring their local view of the game world matches the server's state.

This approach prevents cheating and ensures consistency across all connected clients, as no client can manipulate the game state directly.

## Implementation Details

### NetworkManager

A `NetworkManager` singleton will handle the networking lifecycle:
-   `start_server()`: Creates a `MultiplayerPeer` and sets it as the tree's peer. It listens for incoming connections.
-   `connect_to_server(ip)`: Creates a client peer and connects to the specified server IP.
-   It will handle `peer_connected` and `peer_disconnected` signals to manage player objects.

### Player Spawning

1.  When a client connects, the `peer_connected` signal fires on the server.
2.  The server instantiates a new `Player` scene for the newly connected client.
3.  The server stores a reference to this new player, associating it with the client's unique network ID.
4.  The server notifies all clients about the new player so they can create a local instance of it.

### Input Handling and State Synchronization (Component Based)

We avoid mixing networking code directly into game entities. Instead, we use a dedicated **PlayerInput** component.

1.  **Input Collection (Client/Owner Side)**:
    -   The `PlayerInput` component runs on the client that owns the player authority.
    -   It reads local hardware input (Keyboard/Mouse) and updates public properties (e.g., `Vector2 InputDirection`).
    -   A `MultiplayerSynchronizer` node is configured to automatically replicate these input properties from the Client to the Server.

2.  **Processing (Server Side)**:
    -   The Server-side `Player` entity reads the `InputDirection` from its `PlayerInput` component.
    -   Crucially, the server code **does not care** if this input came from a local keyboard (Singleplayer Host) or was synchronized over the network (Multiplayer Client). It just applies the physics.
    -   The server simulates the movement authoritatively using `MoveAndSlide`.

3.  **State Broadcasting (Server to Clients)**:
    -   The Server's resulting position/rotation is synchronized back to all clients using a second `MultiplayerSynchronizer` (or the same one configured for bi-directional sync if appropriate, usually separate for security).
    -   Clients receive the new transform and update their visual representation. Interpolation is applied for smoothness.

### Authoritative Spawning (MultiplayerSpawner)

Godot 4's `MultiplayerSpawner` node simplifies object replication.
-   **Players**: A `MultiplayerSpawner` in `Main.tscn` watches the "Players" container. When the server instantiates a `Player.tscn` for a new client, the Spawner automatically replicates it to all connected clients.
-   **Bullets/Enemies**: Similarly, Spawners handle the creation of projectiles and zombies initiated by the Server logic.


### Authoritative Spawning (Bullets, Enemies)

-   **Shooting**: When a client presses the "shoot" button, it sends an RPC to the server. The server receives the request, validates it (e.g., checks for fire rate), and then spawns the bullet. The bullet's existence and trajectory are then synced to all clients.
-   **Enemies**: The `WaveManager` runs **only on the server**. When it spawns an enemy, the server creates the `Enemy` instance, and this creation is replicated to all clients. The enemy's AI and movement are also processed exclusively on the server, with the results synced to clients.

This authoritative approach ensures that critical game events are handled consistently and securely.
