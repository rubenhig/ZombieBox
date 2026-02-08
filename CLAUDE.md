# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ZombieBox** is a cooperative top-down shooter built with Godot 4.x (C#/.NET) using a dedicated server architecture. The game supports 2-4 players surviving endless waves of zombies with server-authoritative networking.

**Tech Stack:**
- Godot Engine 4.x (Mono/.NET version)
- C# with .NET SDK 8.0+
- ENet for UDP-based multiplayer networking
- GdUnit4 for testing

## Essential Commands

### Building and Running

```bash
# Build the C# solution
dotnet build

# Run as client (GUI) - opens at Menu
godot --path .

# Run as dedicated server (headless)
godot --headless --path . -- --server --port 7777

# Run client with auto-connect for testing
godot --path . -- --client-test
```

### Development Workflow

```bash
# Run tests using GdUnit4
godot --headless --path . -s addons/gdUnit4/bin/GdUnitCmdTool.gd -a test/ --ignoreHeadlessMode

# Launch multiplayer simulation (use VSCode compound launch config)
# "Multiplayer Simulation (Server + 2 Clients)" - launches server + 2 clients
```

## Architecture Fundamentals

### Server-Authoritative Model

**CRITICAL RULE:** The server is the ONLY source of truth. Clients propose actions; the server validates and executes them.

```
SERVER:                           CLIENT:
- Executes physics                - Captures input
- Validates actions               - Renders state
- Calculates collisions           - Predicts (Phase 2, optional)
- Controls AI                     - Interpolates remotes
- IS THE TRUTH                    - Proposes, doesn't decide
```

### Layered Architecture

```
BOOTSTRAP (Master.cs)
    ↓
SYSTEMS (scripts/Systems/)
    SessionSystem - Manages game state: Lobby → Playing → GameOver
    SpawnSystem - Creates/destroys entities (players, enemies)
    WaveSystem - Progresses zombie waves
    ↓
ENTITIES (scripts/Entities/)
    Player, Enemy, Bullet - Domain objects with state + physics
    Emit signals, never call systems directly
    ↓
COMPONENTS (scripts/Components/)
    PlayerInput - Captures input (client-side)
    PlayerController - Applies physics (server-side)
    PlayerVisuals, EnemyVisuals - Rendering
    ↓
INFRASTRUCTURE (scripts/Core/)
    NetworkSystem - Connection management (autoload)
    GameStateManager - Game state (WaitingToStart/Playing/GameOver, part of GameSession)
    TickManager - Simulation clock (part of GameSession, controlled by GameStateManager)
    NetworkUtils, MovementUtils - Helper utilities
```

### Communication Rules

| Direction | Mechanism | Example |
|-----------|-----------|---------|
| System → System | `GetNode()` direct reference | SessionSystem calls SpawnSystem.SpawnPlayer() |
| System → Entity | Direct method call | SpawnSystem creates Player instance |
| Entity → System | **Signals** | Player.Died → SessionSystem listens |
| Entity → Entity | **NEVER** | Always through a system intermediary |

**Key Principle:** Entities are SIGNAL EMITTERS, Systems are SIGNAL LISTENERS. Entities never know about systems.

## Network Synchronization

### Tick System
- **60 ticks/second** (16.67ms per tick)
- Physics tick rate configured in project.godot: `physics/common/physics_ticks_per_second=60`
- Server advances simulation in discrete ticks for determinism
- **TickManager lifecycle**: Part of GameSession/Managers, not autoload
  - Listens to GameStateManager.StateChanged
  - WaitingToStart: Ticks paused at 0
  - Playing: Ticks active, reset to 0 when game starts
  - GameOver: Ticks paused
  - This ensures server and clients start synchronized at tick=0

### Data Replication

| Type | Mechanism | Frequency | Examples |
|------|-----------|-----------|----------|
| Continuous state | MultiplayerSynchronizer | Every tick | Position, rotation, health |
| Discrete actions | RPC | On event | Shoot, change weapon |
| Spawn/Despawn | MultiplayerSpawner | On event | Create player, bullet |

### Authority Model

```
Player Entity:
├── Position/Physics → SERVER authority (server calculates, replicates)
├── Input → CLIENT OWNER authority (only owning peer modifies)
├── Visuals → LOCAL (each client renders independently)
└── Controller logic → SERVER only (doesn't exist on client)
```

## Scene Structure

**Entry Point:** `scenes/Master.tscn` (Master.cs script)
- Parses CLI args: `--server`, `--port`, `--client-test`
- Routes to Menu (client) or GameSession (server/game)

**Game Session:** `scenes/systems/GameSession.tscn`
```
GameSession
├── Managers/          # State and sync managers
│   ├── GameStateManager (controls WaitingToStart/Playing/GameOver)
│   ├── TickManager (simulation clock, active only in Playing)
│   ├── WaveSystem, SpawnSystem
│   └── MultiplayerSpawners
├── World/
│   ├── Level/         # TileMap, navigation mesh
│   └── Entities/      # Players/enemies spawned here (MultiplayerSpawner target)
└── UI/                # HUD, screens
```

## Key Files

| File | Purpose |
|------|---------|
| scripts/Core/Master.cs | Bootstrap: CLI parsing, scene switching |
| scripts/Core/NetworkSystem.cs | Autoload singleton: server/client initialization |
| scripts/Core/GameStateManager.cs | Game state controller (part of GameSession) |
| scripts/Core/TickManager.cs | Simulation clock (part of GameSession, lifecycle-controlled) |
| scripts/Systems/SessionSystem.cs | Session orchestrator: player management, state transitions |
| scripts/Systems/SpawnSystem.cs | Entity instantiation (server-authoritative) |
| scripts/Entities/Player.cs | Player entity: health, state, signals, component injection |
| scripts/Components/PlayerInput.cs | Input capture (client owner only) |
| scripts/Components/PlayerController.cs | Physics application (server only) |
| scripts/Netcode/ClientPredictor.cs | Client-side prediction with reconciliation |
| scripts/Core/MovementUtils.cs | Shared movement physics (server + client) |

## Physics Layers

Configured in project.godot:
1. World - Environment/walls
2. Player - Player entities
3. Enemy - Enemy entities
4. Projectiles - Bullets

## Development Patterns

### Adding a New Entity

1. Create scene in `scenes/entities/` (CharacterBody2D or Area2D)
2. Create script in `scripts/Entities/` extending appropriate Godot node
3. Add signal declarations for key events (e.g., `Died`, `HealthChanged`)
4. Configure MultiplayerSynchronizer for replicated properties
5. Register with SpawnSystem's MultiplayerSpawner
6. Entities should be NETWORK-AGNOSTIC - no RPC calls, no multiplayer logic

### Adding a System

1. Create script in `scripts/Systems/` extending Node
2. Add as child to GameSession scene under `Systems/`
3. Connect to entity signals in `_Ready()`
4. Use `GetNode()` to call other systems (keep coupling intentional)
5. Systems orchestrate, never hold game state (entities do)

### Client-Server Split

```csharp
// Server-only logic
if (Multiplayer.IsServer())
{
    // Validate, calculate, apply changes
    player.Health -= damage;
    EmitSignal(SignalName.HealthChanged, player.Health);
}

// Client-owner input
if (IsMultiplayerAuthority())
{
    // Capture input, send to server via synchronized properties or RPC
    _moveDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
}
```

## Design Patterns & Lessons Learned

### Dependency Injection Pattern

**Problem**: Components (PlayerInput, ClientPredictor) need TickManager reference, but shouldn't search for it themselves (violates IoC).

**Solution**: Hybrid Strategy
- **Server**: SpawnSystem injects dependencies BEFORE AddChild
- **Client**: SessionSystem observes ChildEnteredTree and injects via CallDeferred

```csharp
// Entity (passive - receives dependencies)
public void Initialize(TickManager tickManager) {
    _input.Initialize(tickManager);
    if (_clientPredictor is ITickSystemUser predictor) {
        predictor.Initialize(tickManager);  // Type-safe interface
    }
}

// Server (SpawnSystem)
Player player = PlayerScene.Instantiate<Player>();
player.Initialize(tickManager);  // BEFORE AddChild
_playersContainer.AddChild(player);

// Client (SessionSystem)
_playersContainer.ChildEnteredTree += OnPlayerNodeAdded;
void OnPlayerNodeAdded(Node node) {
    if (node is Player player) {
        CallDeferred(nameof(InjectDependencies), player, tickManager);
    }
}
```

### [Export] vs GetNode Strings

**Anti-pattern**: `GetNode<PlayerInput>("PlayerInput")` - Breaks if scene restructured

**Best practice**: `[Export] private PlayerInput _input;` - Editor-assigned, robust

**Benefits**:
- Survives scene reorganization (move/rename nodes)
- Editor validation (missing refs visible in Inspector)
- No magic strings

### Interfaces vs Reflection

**Anti-pattern**: `node.Call("Initialize", param)` - Slow, no compile-time safety

**Best practice**: `if (node is ITickSystemUser user) user.Initialize(param);`

**Benefits**:
- Compile-time type checking
- Faster (no reflection)
- Refactoring-safe

### Godot Lifecycle: _EnterTree vs _Ready

**Rule**: Use _EnterTree only for setting own properties (authority, etc.)

**Rationale**: Children may not be ready during _EnterTree (parent executes first)

```csharp
// ✓ CORRECT
public override void _EnterTree() {
    SetMultiplayerAuthority(1);  // Own property only
}

public override void _Ready() {
    _input = GetNode<PlayerInput>("PlayerInput");  // Children ready now
    _input.SetMultiplayerAuthority(peerId);
}

// ✗ WRONG
public override void _EnterTree() {
    var input = GetNode<PlayerInput>("PlayerInput");  // May be null!
}
```

### RPC CallLocal Pattern

**Pattern**: `[Rpc(CallLocal = true)]` with server guard

```csharp
[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
private void RequestFire() {
    if (!NetworkUtils.IsServer()) return;  // Guard prevents client execution
    DoFire();
}
```

**Purpose**: Allows Listen Server (host that plays) to execute locally while clients only send RPC

**Why guard is needed**: Prevents clients from executing server-only logic when CallLocal executes

## Current Phase

**Phase 1: Base Functional** ✅ Complete
- Dedicated server + multiple clients working
- Basic player movement and shooting
- Enemy spawning and waves
- Tick system operational
- Dependency injection architecture solid

**Phase 2: Client-Side Prediction** 🔄 In Progress
- ClientPredictor component implemented
- MovementUtils shared logic
- Testing reconciliation needed

## Common Pitfalls to Avoid

1. **Don't** make entities call systems directly (use signals)
2. **Don't** put game logic in clients (server-authoritative only)
3. **Don't** use hardcoded node paths (use `[Export] NodePath` or GetNode with validation)
4. **Don't** ignore MultiplayerAuthority checks (causes desync)
5. **Don't** duplicate state (one source of truth + replication)
6. **Don't** make systems hold entity references indefinitely (use signals for observation)

## Testing

GdUnit4 tests located in `test/`
- Example: `test/PlayerTest.cs` verifies Player initial health

Run tests:
```bash
godot --headless --path . -s addons/gdUnit4/bin/GdUnitCmdTool.gd -a test/ --ignoreHeadlessMode
```

## Input Actions

Defined in project.godot:
- `move_up` (W), `move_down` (S), `move_left` (A), `move_right` (D)
- `shoot` (Left Click or Space)
- `switch_weapon` (Q)
- `toggle_debug` (F1)

## Documentation

- `docs/ARCHITECTURE.md` - Complete architectural reference (Spanish)
- `docs/PHASE1_REQUIREMENTS.md` - Current phase requirements
- `docs/multiplayer_info_godot.md` - Godot multiplayer notes
- `README.md` - Quick start guide

## Future Integration

The architecture is designed for backend integration (auth, matchmaking, orchestration). Currently, clients connect directly via IP:Port. Backend integration is planned for Phase 4.
