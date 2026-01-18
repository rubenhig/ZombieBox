# Developer & Agent Guidelines

This document serves as the **Instruction Manual** for coding in this repository. It defines the mandatory patterns, architectural constraints, and decision-making frameworks for ZombieBox.

**Goal:** Maintain a clean, scalable, Server-Authoritative DDD architecture.

---

## 0. Interaction Protocol: Plan First
Before implementing any complex feature, refactor, or bug fix, the Agent MUST:
1.  **Analyze:** Explain the current state and the problem/goal.
2.  **Design:** Propose a solution strategy (Architecture, Patterns).
3.  **Plan:** List the specific steps/files to be modified.
4.  **Confirm:** Ask for user approval before executing code changes.

---

## 1. Golden Rule: "Everything is Multiplayer"
*   **Concept:** There is no "Singleplayer code". Singleplayer is simply a Local Server with 1 client.
*   **Implication:** Never write code that assumes "local only" logic for gameplay. Always check `Multiplayer.IsServer()` for game state changes and `IsMultiplayerAuthority()` for input.

## 2. Architecture & DDD Pattern

### Entities (The "Body")
*   **Path:** `scripts/Player.cs`, `scripts/Enemy.cs`
*   **Responsibility:** Physics, Health, State management.
*   **Constraint:** **NEVER** access `Input` or `NetworkManager` directly. Entities are agnostic.
*   **Communication:** Use **Signals** to notify the world of changes (`Died`, `HealthChanged`).

### Components (The "Brain")
*   **Path:** `scripts/Components/PlayerInput.cs`
*   **Responsibility:** Read hardware input or network data and feed "Intent" to the Entity.
*   **Pattern:** The Component calls methods on the Entity (e.g., `_player.TryShoot()`).

### Orchestrators (The "Manager")
*   **Path:** `scripts/GameManager.cs`, `scripts/WaveManager.cs`
*   **Responsibility:** Connect systems. Listen to Entity signals.
*   **Pattern:** Dependency Injection over `GetNode`. Connect signals dynamically when spawning objects.

---

## 3. Networking Implementation Guide

When adding a new feature, choose the correct pattern:

| Feature Type | Example | Pattern | Implementation |
| :--- | :--- | :--- | :--- |
| **Continuous State** | Movement, Aiming | **State Sync** | Use `MultiplayerSynchronizer` to replicate variables (`MoveVector`). |
| **Discrete Action** | Shoot, Jump, Switch Weapon | **RPC** | Client calls `TryAction` -> Sends `Rpc(RequestAction)` -> Server executes `DoAction`. |
| **Spawning** | Bullet, Enemy | **Spawner** | Server instantiates -> Adds to `MultiplayerSpawner` tracked node -> Auto-replicates. |

**Important:**
*   **Do not** poll booleans for discrete actions (e.g., `if (isShooting)` for a pistol). It causes input loss. Use RPCs.
*   **Do** poll booleans for continuous actions (e.g., Machine Gun fire).

---

## 4. Scene & Node Structure

*   **Entry Point:** `scenes/Master.tscn` (Persistent). NEVER destroy this.
*   **Gameplay:** `scenes/GameSession.tscn`.
    *   **Logic:** `Managers/` node.
    *   **Physics:** `World/` node (contains `NavigationRegion`).
    *   **Objects:** `World/Entities/` (Y-Sorted).

**Navigation:**
*   Always use `NavigationAgent2D` for AI.
*   Enemies use **Avoidance** (RVO) to prevent stacking.
*   Physical collision between enemies should be enabled via Layers (Mask 15) to prevent overlapping, but Avoidance handles the smooth movement.

---

## 5. Coding Standards

1.  **C# Events:** Prefer C# Events or Godot Signals (`[Signal]`) over direct reference calls for upstream communication.
2.  **Explicit Naming:** When spawning network objects, assign a unique name (e.g., `GUID` or `PeerID`) to avoid Spawner errors.
3.  **Safety:** Always check `IsInstanceValid()` before accessing nodes in deferred calls (UI updates, death logic).
4.  **No Magic Strings:** Use `nameof(Method)` for RPCs and Signals when possible.

---

## 6. How to Add a New Feature (Example)

**Task:** "Add a Grenade throw."

1.  **Input:** Add `throw_grenade` to `PlayerInput.cs`. Detect `JustPressed`.
2.  **Entity (Client):** Add `TryThrowGrenade()` in `Player.cs`. Call `Rpc(nameof(RequestThrowGrenade))`.
3.  **Entity (Server):** Add `RequestThrowGrenade()`. Validate cooldown/ammo. Call `DoThrowGrenade()`.
4.  **Logic (Server):** `DoThrowGrenade()` instantiates `Grenade.tscn` (Rigidbody) and adds it to `Bullets` container.
5.  **Replication:** Ensure `Grenade.tscn` is in `BulletSpawner` list and has `MultiplayerSynchronizer`.