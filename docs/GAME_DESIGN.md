# Game Design Document

## Core Loop
ZombieBox is an endless top-down shooter.
1.  **Survive:** Players fight against increasing waves of enemies.
2.  **Wave Progression:** A wave ends when all enemies are defeated. A cooldown timer starts before the next wave.
3.  **Lose Condition:** All players reach 0 Health.

## Entities

### Player
*   **Stats:** Health (3), Speed (300).
*   **Controls:**
    *   `WASD`: Move.
    *   `Mouse`: Aim.
    *   `Left Click`: Shoot.
    *   `Q`: Switch Weapon.
*   **Weapons:**
    *   **Pistol:** Semi-automatic (click per shot).
    *   **Machine Gun:** Automatic (hold to fire).

### Enemy (Zombie)
*   **Behavior:** Finds the nearest player and chases them using pathfinding (NavigationAgent2D).
*   **Combat:** Deals 1 damage on contact and dies (Kamikaze).
*   **Stats:** Health (1), Speed (150).

### WaveManager
*   **Spawning:** Spawns `3 * WaveNumber` enemies at random spawn points.
*   **Progression:** Tracks alive enemies. Triggers next wave only when count reaches 0.

## Events (Domain Signals)
| Event | Emitted By | Trigger | Listener | Action |
| :--- | :--- | :--- | :--- | :--- |
| `Died` | `Player` | Health <= 0 | `GameManager` | Triggers Game Over. |
| `Died` | `Enemy` | Health <= 0 | `WaveManager` | Decrements enemy count. |
| `EnemyKilled` | `Bullet` | Impact | `Player` | Updates kill score. |
