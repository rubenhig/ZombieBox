# Development Roadmap

This document outlines the development tasks for the project, broken down into milestones.

## Milestone 1: Project Setup and Core Player Mechanics

- [x] **Configuración Inicial del Proyecto**
  - [x] Create Godot 4 (Mono) project.
  - [x] Set up the folder structure (`scenes`, `scripts`, `assets`).
  - [x] Configure Input Actions (`move_*`, `shoot`, `switch_weapon`).
  - [x] Initialize Git repository.

- [x] **Escena Principal (Main) y Nodo Game**
  - [x] Create `Main.tscn` as the main scene.
  - [x] Add a `Game` Node2D to contain the game world.
  - [x] Add placeholder nodes for enemies and spawn points.

- [x] **Jugador – Movimiento básico (Single-player)**
  - [x] Create `player.tscn` with a `CharacterBody2D`.
  - [x] Implement `Player.cs` script for input-based movement (`MoveAndSlide`).
  - [x] Ensure the player character rotates towards the movement direction.

- [x] **Disparo Local – Implementación de Bala**
  - [x] Create `bullet.tscn` as an `Area2D`.
  - [x] Implement `Bullet.cs` script for movement and screen exit detection.
  - [x] Implement shoot functionality in `Player.cs` to instantiate bullets.

- [x] **Múltiples Armas (Pistola vs. Ametralladora)**
  - [x] Differentiate between single-shot (pistol) and continuous-fire (machine gun).
  - [x] Add logic to switch between weapons.

## Milestone 2: Enemies and Gameplay Loop

- [x] **Enemigo – Escena e IA básica**
  - [x] Create `enemy.tscn` with a `CharacterBody2D`.
  - [x] Add the enemy to an "enemies" group.
  - [x] Implement `Enemy.cs` with basic AI to follow the nearest player.

- [x] **Gestión de Oleadas (WaveManager)**
  - [x] Create a `WaveManager` node.
  - [x] Implement logic to spawn progressively larger waves of enemies at random spawn points.
  - [x] Use a timer to control the time between waves.

- [x] **Combate – Integración Balas y Enemigos**
  - [x] Implement collision detection between bullets and enemies.
  - [x] Enemies should be destroyed upon being hit.
  - [x] Implement basic player health and damage on enemy contact.

- [x] **Interfaz HUD básica**
  - [x] Add a `CanvasLayer` for the HUD.
  - [x] Display wave count, enemies killed, and player health.
  - [x] Connect HUD to game events to update its state.

## Milestone 3: Multiplayer Implementation (Refactored Architecture)

- [ ] **Infraestructura de Red (Everything is Multiplayer)**
  - [ ] **Core Refactoring:**
	- [x] Convert `NetworkManager` to Autoload (`/root/NetworkManager`).
    - [x] Implement `Master` scene for persistent orchestration.
    - [ ] Update `Main.tscn` with `MultiplayerSpawner` nodes for Players and Enemies.
  - [ ] **PlayerInput Component:**
	- [ ] Create `PlayerInput.cs` to handle input reading decoupled from `Player.cs`.
	- [ ] Implement `MultiplayerSynchronizer` logic to sync Input (Client->Server).
  - [ ] **Server Authoritative Physics:**
	- [ ] Refactor `Player.cs` to move only based on `PlayerInput` state, executed by Server authority.
    - [ ] Add `MultiplayerSynchronizer` to `player.tscn` for syncing Position/Rotation (Server->Clients).
  - [ ] **Game Logic Adaptation:**
	- [ ] Ensure `WaveManager` and `Enemy` logic only runs on Server (`IsServer()`).
    - [ ] Refactor Shooting to use Server-validated spawning via `MultiplayerSpawner`.

## Milestone 4: Polishing and Finalizing

- [ ] **Pulido y Pruebas Finales**
  - [ ] Test multiplayer with multiple clients.
  - [ ] Adjust game balance (player/enemy speed, weapon stats, wave difficulty).
  - [ ] Refine visual feedback and game feel.
  - [ ] Clean up code, remove debug prints, and add comments where necessary.
  - [ ] Document the steps to run the game in server/client mode.
