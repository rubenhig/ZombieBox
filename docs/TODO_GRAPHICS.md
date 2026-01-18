# Graphics & Visual Overhaul Plan

This document outlines the roadmap for transforming ZombieBox from a prototype into a visually polished game.

## Phase 1: Environment (The Arena)
**Goal:** Replace the empty void with a concrete level.

- [ ] **Assets Acquisition:**
    - [ ] Obtain a TileSet (recommendation: Kenney Topdown Shooter).
    - [ ] Place assets in `assets/sprites/environment/`.
- [ ] **TileMap Setup:**
    - [ ] Create a `TileMapLayer` node in `GameSession/World`.
    - [ ] Configure TileSet resource (define physics collision for walls).
    - [ ] Paint the ground (Layer 0) and walls (Layer 1).
- [ ] **Navigation Integration:**
    - [ ] Bake the `NavigationRegion2D` to match the new TileMap layout (walkable areas vs walls).
    - [ ] Remove the hardcoded debug polygon.

## Phase 2: Characters (Visual Identity)
**Goal:** Replace `icon.svg` with animated sprites.

- [ ] **Player:**
    - [ ] Import Player spritesheet (Idle, Run, Shoot).
    - [ ] Replace `Sprite2D` with `AnimatedSprite2D` or `AnimationPlayer` in `player.tscn`.
    - [ ] Create `PlayerVisuals.cs` component (decoupled from logic) to handle animation states based on `PlayerInput`.
- [ ] **Enemies:**
    - [ ] Import Zombie spritesheet.
    - [ ] Update `enemy.tscn` visuals.
    - [ ] Add simple "Hit Flash" shader for damage feedback.

## Phase 3: VFX & Juice (Game Feel)
**Goal:** Make actions feel impactful.

- [ ] **Shooting:**
    - [ ] Muzzle Flash (particle/sprite at gun tip).
    - [ ] Shell ejection particles.
    - [ ] Screen Shake on shoot (Camera2D offset noise).
- [ ] **Impacts:**
    - [ ] Blood splatters on enemy hit/death (GPU Particles).
    - [ ] Floating damage numbers.
- [ ] **Lighting (Optional but recommended):**
    - [ ] Add `PointLight2D` to player and bullets for atmosphere.
    - [ ] Add `CanvasModulate` to darken the rest of the level.

## Phase 4: UI Polish
**Goal:** Clean and readable interface.

- [ ] **Theme:** Create a Godot `Theme` resource for consistent fonts/colors.
- [ ] **HUD:** Replace plain text labels with icons (Heart icon + Bar) and stylized counters.
- [ ] **Menus:** Add background image and button animations.


# Notas en español
Es un momento emocionante. Pasar de "cajas moviéndose" a un juego que "entra por los ojos".

  Para abordar el remozado visual (Juice & Graphics) de forma ordenada y profesional, te propongo este orden de batalla:

  1. El Escenario (TileMap) - Prioridad 1
  El mapa define la atmósfera. Actualmente tenemos un "vacío" con un polígono de navegación invisible.
   * Qué haremos: Crear un TileMapLayer para el suelo y las paredes.
   * Por qué primero: Porque nos permitirá definir visualmente las zonas navegables y los obstáculos, sustituyendo el polígono de depuración feo.

  2. Los Personajes (Sprites y Animaciones) - Prioridad 2
  Sustituir el icon.svg por personajes reales.
   * Qué haremos:
       * Cambiar Sprite2D por `AnimatedSprite2D` (si usas secuencias de frames simples) o `AnimationPlayer` + `SpriteSheet` (más potente).
       * Crear una Máquina de Estados de Animación (Idle, Run, Shoot, Die) en el código.

  3. Feedback Visual (Juice) - Prioridad 3
  Cosas que hacen que el juego se sienta bien.
   * Partículas: Sangre al morir, casquillos al disparar, humo.
   * Shaders: Flash blanco al recibir daño (Hit Flash).
   * Cámara: Screen Shake (temblor) al disparar.

  ---

  ¿Por dónde empezamos? El Mapa (TileMap)

  Para esto necesitamos Assets (Gráficos).
  ¿Tienes ya algún tileset (imagen con los cuadraditos del suelo/paredes) y sprites para el personaje?

   * Si los tienes: Por favor, cópialos a la carpeta assets/sprites/.
   * Si NO los tienes: Puedo sugerirte usar un pack gratuito de Kenney.nl (como el "Topdown Shooter" que es perfecto para esto) o podemos intentar usar placeholders geométricos más bonitos
     (colores planos con bordes).

  Mi recomendación: Si quieres que quede "resultón" rápido, busca "Kenney Topdown Shooter" (es gratis), descarga el pack, y pon las imágenes en assets/.

  Dime tu situación con los assets y te guío en el siguiente paso técnico (Configurar el TileMap en GameSession).