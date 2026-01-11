# Game Domain Specification

This document defines the core Domain Entities, Value Objects, Events, and Component Interfaces of ZombieBox. It serves as the contract for the game's logic and architecture.

## 1. Core Entities

### Player
The main character controlled by a human (or AI).
*   **Properties:** `Health` (int), `Speed` (float), `CurrentWeapon` (WeaponType), `Kills` (int).
*   **Behaviors:** `Move(Vector2)`, `Rotate(float)`, `TakeDamage(int)`, `Shoot()`, `SwitchWeapon()`.

### Enemy (Zombie)
A hostile NPC that chases players.
*   **Properties:** `Speed` (float), `Target` (Player).
*   **Behaviors:** `FindNearestTarget()`, `Chase()`, `Attack()`, `Die()`.

### Weapon (Concept)
*   **Properties:** `FireRate`, `Damage`, `Type` (Pistol/MachineGun).

### WaveManager (Service)
Director of the game loop.
*   **Properties:** `CurrentWave`, `ZombiesPerWave`, `TimeBetweenWaves`.
*   **Behaviors:** `StartWave()`, `SpawnEnemy()`, `Stop()`.

---

## 2. Component Interfaces

We use composition to separate logic. The core contract is between the Entity (Body) and its Controller (Brain).

### IPlayerInput (Component)
Responsibility: Provide intent to the Player entity. It does *not* execute logic, only holds the desired state.

*   **Properties:**
    *   `Vector2 MoveVector`: The desired movement direction (normalized, e.g., from WASD).
    *   `Vector2 AimDirection`: The desired facing direction (e.g., to mouse cursor).
    *   `bool IsShooting`: True if the attack button is held/pressed.
    *   `bool IsSwitchingWeapon`: True if the switch weapon button was pressed this frame.

*   **Implementation Variants:**
    *   `LocalPlayerInput`: Reads `Godot.Input` (Keyboard/Mouse).
    *   `NetworkPlayerInput`: Reads data synchronized via `MultiplayerSynchronizer` from a remote client.
    *   `BotPlayerInput`: Calculated by an AI algorithm.

---

## 3. Domain Events (Signals)

These events decouple systems. A system triggers an event, and interested parties react without direct dependency.

### Global Game Events (GameManager / EventBus)
| Event Name | Payload | Emitted By | Listened By | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `GameStarted` | - | `GameManager` | `WaveManager`, `HUD` | Triggers initial spawn and UI reset. |
| `GameEnded` | - | `GameManager` | `WaveManager`, `HUD`, `Master` | Stops spawning, shows Game Over screen. |
| `WaveStarted` | `int WaveNumber` | `WaveManager` | `HUD` | Updates "Wave: X" text. |
| `WaveCompleted`| `int WaveNumber` | `WaveManager` | `HUD` (optional) | Visual feedback. |

### Entity Events
| Event Name | Payload | Emitted By | Listened By | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `HealthChanged` | `int CurrentHealth` | `Player` | `HUD` | Updates health bar/counter. |
| `Died` | - | `Player` | `GameManager` | Checks for Game Over condition. |
| `EnemyKilled` | `int TotalKills` | `Player` | `HUD` | Updates score/kill counter. |
| `EnemyDied` | - | `Enemy` | `WaveManager` (internal) | Tracks progress to complete wave. |

### Infrastructure Events (NetworkManager)
| Event Name | Payload | Emitted By | Listened By | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `PlayerConnected` | `int PeerId` | `NetworkManager` | `GameManager` | Spawns a new Player entity. |
| `PlayerDisconnected` | `int PeerId` | `NetworkManager` | `GameManager` | Removes the Player entity. |

---

## 4. Design Rules
1.  **Dependency Rule:** Entities should not depend on the UI or NetworkManager directly. Use Signals (Events) to communicate changes outwards.
2.  **Input Flow:** `Hardware -> IPlayerInput -> Player -> Server Physics`.
3.  **State Flow:** `Server Physics -> MultiplayerSynchronizer -> Client Puppet`.