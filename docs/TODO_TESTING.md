# Testing Strategy & Roadmap

**Goal:** Ensure game stability and architectural integrity using automated tests (GdUnit4).

## 1. Tools & Frameworks
- **Primary Framework:** [GdUnit4](https://github.com/MikeSchulze/gdUnit4) (C# API).
- **Test Runner:** CLI (via `dotnet test` or GdUnit4 runner) and Godot Editor integration.

## 2. Test Categories

### Unit Tests (Logic Only)
Testing pure C# methods without requiring a full scene tree where possible.
- **Player Stats:** Health reduction, death trigger.
- **Weapon Logic:** Damage values, weapon switching.
- **State Machine:** State transitions validation in `GameStateManager`.

### Integration Tests (Scene/Engine)
Testing nodes interacting within the Godot Scene Tree.
- **Spawning:** Verify `MultiplayerSpawner` instantiates the correct scenes.
- **Collisions:** Verify bullets damage enemies and enemies damage players.
- **Wave Flow:** Verify `WaveManager` triggers the next wave when enemies reach 0.

## 3. Implementation Steps

### Phase 1: Environment Setup
- [ ] Add `GdUnit4` package to `ZombieBox.csproj`.
- [ ] Create `test/` directory structure.
- [ ] Configure a basic test to verify the runner works.

### Phase 2: Domain Logic Tests
- [ ] **Player Tests:**
    - [ ] `TestTakeDamage`: Health decreases correctly.
    - [ ] `TestDeathSignal`: `Died` signal is emitted at 0 HP.
- [ ] **GameState Tests:**
    - [ ] `TestStateTransitions`: Verify `World.ProcessMode` changes on state toggle.

### Phase 3: Gameplay Integration Tests
- [ ] **Spawning Tests:** Verify player appears on server start.
- [ ] **Combat Tests:** Instantiate bullet and enemy, simulate hit, verify enemy death.

## 4. Design Principles for Testing
- **Isolate Logic:** Keep domain logic in entities testable without deep scene dependencies.
- **Fast Execution:** Prioritize unit tests over scene-heavy tests for quick feedback.
- **Multiplayer Aware:** Mock network peers to test server-only logic.
