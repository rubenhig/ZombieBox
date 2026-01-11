# Gameplay Features

This document outlines the core gameplay mechanics for the initial version of the game.

## Player Mechanics

*Note: All mechanics below are implemented using a Server-Authoritative logic. In Single-player mode, the local machine acts as the server.*

-   **Movement**:
    -   Top-down movement in 8 directions (WASD or arrow keys).
    -   The player character's sprite will rotate to face the direction of movement or aiming.

-   **Weapons and Shooting**:
    -   Players can shoot projectiles.
    -   The shooting direction is based on the mouse cursor's position or the last movement direction.
    -   Two initial weapon types will be implemented:
        1.  **Pistol**: A semi-automatic weapon. One shot per click.
        2.  **Machine Gun**: A fully-automatic weapon. Continuous fire while the shoot button is held down, limited by a fire rate.
    -   Players can switch between weapons.

## Enemy Mechanics

-   **Basic Enemy (Zombie)**:
    -   A single type of enemy will be implemented initially.
    -   **AI Behavior**: Enemies will use a simple "follow" AI. They will identify the nearest player and move directly toward them.
    -   **Damage**: Enemies damage players on contact. We can decide if the enemy is destroyed on contact or continues to attack.

## Gameplay Loop: Endless Mode

-   **Waves**: The game is structured in waves. Enemies spawn in groups, and each wave is more difficult than the last.
-   **Wave Manager**: A dedicated `WaveManager` node handles the spawning logic:
    -   It spawns a set number of enemies at the start of each wave. The number of enemies increases with each wave.
    -   Enemies appear at predefined `Marker2D` spawn points around the map.
    -   There will be a short delay between waves to give players a moment to prepare.
-   **Objective**: The primary goal is to survive for as long as possible. The game ends when all players have been defeated.
-   **Scoring**: A scoring system will track progress, likely based on the number of waves survived and enemies defeated.

## User Interface (HUD)

A simple HUD will be displayed on-screen to provide essential information to the player:

-   **Player Health**: A visual indicator of the current player's health (e.g., a health bar or hearts).
-   **Wave Counter**: Displays the current wave number.
-   **Score / Enemies Defeated**: Shows the player's current score or kill count.
-   **Game Notifications**: On-screen text for events like "New Wave!", "Game Over", etc.
