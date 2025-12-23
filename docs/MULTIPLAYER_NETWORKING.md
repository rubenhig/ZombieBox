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

### Input Handling and State Synchronization

1.  **Input from Client to Server**:
    -   On the client side, the `_PhysicsProcess` function reads local input.
    -   Instead of applying the movement locally, it sends the input data (e.g., a direction vector and a "shooting" boolean) to the server using an RPC.
    -   Example RPC: `RpcId(1, "ReceivePlayerInput", input_direction, is_shooting)`. (The ID `1` is always the server).

2.  **Processing on Server**:
    -   The server receives the `ReceivePlayerInput` RPC.
    -   It finds the `Player` node corresponding to the client that sent the RPC.
    -   It applies the input to that player's physics, moving the character and handling any other actions like shooting.

3.  **Broadcasting State from Server to Clients**:
    -   After the server updates an object's state (e.g., a player's position), it needs to inform all clients.
    -   This can be done via RPCs or Godot's `MultiplayerSynchronizer` node.
    -   For example, the server could broadcast the new positions of all dynamic objects at a fixed interval (e.g., 10-20 times per second).
    -   Example RPC: `Rpc("UpdateObjectTransform", object_id, new_transform)`.

### Authoritative Spawning (Bullets, Enemies)

-   **Shooting**: When a client presses the "shoot" button, it sends an RPC to the server. The server receives the request, validates it (e.g., checks for fire rate), and then spawns the bullet. The bullet's existence and trajectory are then synced to all clients.
-   **Enemies**: The `WaveManager` runs **only on the server**. When it spawns an enemy, the server creates the `Enemy` instance, and this creation is replicated to all clients. The enemy's AI and movement are also processed exclusively on the server, with the results synced to clients.

This authoritative approach ensures that critical game events are handled consistently and securely.
