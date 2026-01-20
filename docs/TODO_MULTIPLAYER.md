# Multiplayer & Dedicated Server Plan

**Goal:** Implement a dedicated server architecture where a single binary can run as a Headless Server or a Client, using CLI arguments.

## 1. Requirements
- **Unified Binary:** The same executable serves both roles.
- **Headless Server:** Triggered via `--server` flag. Runs without graphics/audio.
    - Argument `--port <int>`: Configurable listening port (default 7777).
- **Client:** Triggered by default. Connects to a hardcoded address (`localhost`) for this prototype phase.
- **Seamless Logic:** Game logic (`Player`, `Enemy`, `Bullet`) remains identical for Singleplayer (Offline Peer) and Multiplayer (ENet Peer).

## 2. Architecture

### Bootstrapping
A new entry point logic (likely in `Master.cs` or an Autoload) to parse arguments:
- If `--server`:
    - Do NOT load Menu.
    - Start `NetworkManager` as Dedicated Server.
    - Load `GameSession` immediately.
- If (default):
    - Load `Menu`.
    - Wait for user interaction.

### NetworkManager Extensions
- `StartDedicatedServer(int port)`: Initializes `ENetMultiplayerPeer` in Server mode.
- `StartClient(string ip, int port)`: Initializes `ENetMultiplayerPeer` in Client mode.

## 3. Implementation Steps

### Phase 1: Bootstrapper & Args
- [ ] Implement argument parsing in `Master.cs` (`_Ready`).
- [ ] Detect `--server` and `--port` (default 7777).
- [ ] Branch logic: Server start vs Menu load.

### Phase 2: NetworkManager Upgrade
- [ ] Add `StartDedicatedServer` method.
- [ ] Add `StartClient` method (hardcoded to `127.0.0.1` for now).
- [ ] Update `PlayerServerController` to handle input from remote peers correctly (ensure authority works).

### Phase 3: Client UI
- [ ] Add "Play Online" button to `Menu.tscn`.
- [ ] Wire button to `StartClient`.

### Phase 4: Validation
- [ ] Create a launch script to run:
    - 1x Server (`--headless --server`).
    - 2x Clients.
- [ ] Verify players can see each other and kill zombies together.

## 4. Technical Considerations
- **Authority:** `PlayerInput` uses `SetMultiplayerAuthority`. Ensure this propagates correctly to clients connecting late.
- **State Sync:** `GameStateManager` must sync the `Playing` state to new clients so they don't join a frozen world.
