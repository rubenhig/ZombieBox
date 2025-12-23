# Development Roadmap

This document outlines the development tasks for the project, broken down into milestones.

## Milestone 1: Project Setup and Core Player Mechanics

- [ ] **Configuración Inicial del Proyecto**
  - [ ] Create Godot 4 (Mono) project.
  - [ ] Set up the folder structure (`scenes`, `scripts`, `assets`).
  - [ ] Configure Input Actions (`move_*`, `shoot`).
  - [ ] Initialize Git repository.

- [ ] **Escena Principal (Main) y Nodo Game**
  - [ ] Create `Main.tscn` as the main scene.
  - [ ] Add a `Game` Node2D to contain the game world.
  - [ ] Add placeholder nodes for enemies and spawn points.

- [ ] **Jugador – Movimiento básico (Single-player)**
  - [ ] Create `player.tscn` with a `CharacterBody2D`.
  - [ ] Implement `Player.cs` script for input-based movement (`MoveAndSlide`).
  - [ ] Ensure the player character rotates towards the movement direction.

- [ ] **Disparo Local – Implementación de Bala**
  - [ ] Create `bullet.tscn` as an `Area2D`.
  - [ ] Implement `Bullet.cs` script for movement and screen exit detection.
  - [ ] Implement shoot functionality in `Player.cs` to instantiate bullets.

- [ ] **Múltiples Armas (Pistola vs. Ametralladora)**
  - [ ] Differentiate between single-shot (pistol) and continuous-fire (machine gun).
  - [ ] Add logic to switch between weapons.

## Milestone 2: Enemies and Gameplay Loop

- [ ] **Enemigo – Escena e IA básica**
  - [ ] Create `enemy.tscn` with a `CharacterBody2D`.
  - [ ] Add the enemy to an "enemigo" group.
  - [ ] Implement `Enemy.cs` with basic AI to follow the nearest player.

- [ ] **Gestión de Oleadas (WaveManager)**
  - [ ] Create a `WaveManager` node.
  - [ ] Implement logic to spawn progressively larger waves of enemies at random spawn points.
  - [ ] Use a timer to control the time between waves.

- [ ] **Combate – Integración Balas y Enemigos**
  - [ ] Implement collision detection between bullets and enemies.
  - [ ] Enemies should be destroyed upon being hit.
  - [ ] Implement basic player health and damage on enemy contact.

- [ ] **Interfaz HUD básica**
  - [ ] Add a `CanvasLayer` for the HUD.
  - [ ] Display wave count, enemies killed, and player health.
  - [ ] Connect HUD to game events to update its state.

## Milestone 3: Multiplayer Implementation

- [ ] **Implementación de Red – Servidor/Cliente**
  - [ ] **Networking Base:**
    - [ ] Create a `NetworkManager` (Singleton/Autoload).
    - [ ] Implement `StartServer()` and `JoinServer()` methods using `MultiplayerAPI`.
    - [ ] Handle `peer_connected` and `peer_disconnected` signals.
    - [ ] Spawn a player character for each connected client.
  - [ ] **Sync Jugadores:**
    - [ ] Send client inputs to the server via RPCs.
    - [ ] The server processes inputs and updates player positions.
    - [ ] Replicate player movement across all clients.
  - [ ] **Sync Disparos:**
    - [ ] Client sends a "shoot" request via RPC.
    - [ ] The server validates the request and spawns the bullet authoritatively.
    - [ ] Replicate the bullet's creation and movement to all clients.
  - [ ] **Sync Enemigos:**
    - [ ] The server authoritatively spawns and controls enemy AI.
    - [ ] Replicate enemy creation, movement, and destruction to all clients.

## Milestone 4: Polishing and Finalizing

- [ ] **Pulido y Pruebas Finales**
  - [ ] Test multiplayer with multiple clients.
  - [ ] Adjust game balance (player/enemy speed, weapon stats, wave difficulty).
  - [ ] Refine visual feedback and game feel.
  - [ ] Clean up code, remove debug prints, and add comments where necessary.
  - [ ] Document the steps to run the game in server/client mode.
