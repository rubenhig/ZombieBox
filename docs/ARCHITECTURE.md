# ZombieBox - Documento de Arquitectura

> **Versión**: 1.0 (Fase 1)
> **Última actualización**: Enero 2026

---

## Índice

1. [Visión y Principios](#1-visión-y-principios)
2. [Estructura del Proyecto](#2-estructura-del-proyecto)
3. [Modelo de Red](#3-modelo-de-red)
4. [Flujo de Sesión](#4-flujo-de-sesión)
5. [Entidades y Componentes](#5-entidades-y-componentes)
6. [Roadmap de Fases](#6-roadmap-de-fases)

---

## 1. Visión y Principios

### 1.1 Visión del Proyecto

**ZombieBox** es un shooter top-down multijugador cooperativo donde 2-4 jugadores sobreviven oleadas de enemigos en un Dedicated Server autoritativo.

```
┌─────────────────────────────────────────────────────────────────┐
│                      VISIÓN TÉCNICA                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  "Un juego donde el SERVIDOR es la única fuente de verdad,     │
│   los CLIENTES son terminales de visualización e input,        │
│   y la ARQUITECTURA permite escalar sin reescribir."           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Objetivo de Fase 1**: Dos jugadores conectados a un Dedicated Server con netcode profesional (tick system, interpolación, lag compensation) y gameplay básico funcional.

---

### 1.2 Principios Fundamentales

#### Principio 1: Server-Authoritative Absoluto

```
EL SERVIDOR:                          EL CLIENTE:
─────────────                         ────────────
• Ejecuta física                      • Captura input
• Valida acciones                     • Renderiza estado
• Calcula colisiones                  • Predice (opcional)
• Controla IA                         • Interpola remotos
• Es la VERDAD                        • Propone, no decide
```

**Regla**: Ninguna decisión de gameplay ocurre en el cliente. El cliente propone acciones; el servidor las valida y ejecuta.

---

#### Principio 2: Godot-Native Architecture

La arquitectura abraza los mecanismos nativos de Godot en lugar de forzar patrones externos.

| Mecanismo Godot | Uso en ZombieBox |
|-----------------|------------------|
| **Scene Tree** | Contenedor de dependencias. Los sistemas son nodos. |
| **Signals** | Comunicación Entidad → Sistema (desacoplada). |
| **GetNode()** | Comunicación Sistema → Sistema (acoplada, controlada). |
| **MultiplayerSynchronizer** | Replicación de estado continuo. |
| **RPC** | Acciones discretas (disparar, cambiar arma). |
| **MultiplayerSpawner** | Instanciación replicada de entidades. |

**Regla**: No reinventar lo que Godot ya provee.

---

#### Principio 3: Separación por Responsabilidad

Cada nodo/clase tiene **una única razón para cambiar**.

```
┌─────────────────────────────────────────────────────────────────┐
│                    CAPAS DE RESPONSABILIDAD                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  SISTEMAS (Orquestación)                                        │
│  └── Coordinan flujos, escuchan signals, invocan otros sistemas │
│      Ejemplos: SessionSystem, SpawnSystem, WaveSystem           │
│                                                                 │
│  ENTIDADES (Dominio)                                            │
│  └── Estado + física. Emiten signals. Agnósticas de red.        │
│      Ejemplos: Player, Enemy, Bullet                            │
│                                                                 │
│  COMPONENTES (Infraestructura)                                  │
│  └── Alimentan a las entidades. Input, networking, visuales.    │
│      Ejemplos: PlayerInput, RemoteInterpolator, PlayerVisuals   │
│                                                                 │
│  INFRAESTRUCTURA (Utilidades)                                   │
│  └── Helpers transversales. Sin estado de juego.                │
│      Ejemplos: NetworkUtils, TickManager                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Regla**: Las entidades NUNCA conocen a los sistemas. Los sistemas conocen a las entidades (vía signals o referencias).

---

#### Principio 4: Tick Determinístico

El juego opera en **ticks discretos**, no en tiempo continuo.

```
Tick Rate: 60 ticks/segundo (16.67ms por tick)

Servidor:
────────────────────────────────────────────────────────────────►
Tick 0      Tick 1      Tick 2      Tick 3      Tick 4
   │           │           │           │           │
   └── Procesa inputs, simula física, broadcast estado

Cada tick es atómico: mismo input → mismo resultado (determinismo)
```

**Regla**: Toda acción se etiqueta con un número de tick. Esto permite reconciliación, lag compensation y reproducibilidad.

---

#### Principio 5: Extensibilidad sobre Flexibilidad

El código se diseña para **añadir** sin **modificar**.

**Regla**: Usar interfaces para puntos de extensión conocidos (armas, enemigos, power-ups).

---

#### Principio 6: Fallo Explícito

Los errores se manejan, no se ignoran silenciosamente.

**Regla**: `GetNodeOrNull` + validación explícita. Logs claros cuando algo falla.

---

### 1.3 Decisiones Arquitectónicas Clave

| Decisión | Elección | Justificación |
|----------|----------|---------------|
| Modelo de red | Server-Authoritative | Anti-cheat, consistencia, escalabilidad |
| Hosting | Dedicated Server only | Control total, sin NAT issues, backend-ready |
| Tick system | 60 ticks/s fijo | Balance entre precisión y bandwidth |
| Sincronización | MultiplayerSynchronizer + RPC | Nativo de Godot, bien integrado |
| Lenguaje | C# | Tipado fuerte, mejor tooling |
| Predicción cliente | Sí (Fase 2) | Responsividad, correctamente reconciliada |
| Interpolación | Sí | Suavidad visual para entidades remotas |
| Lag Compensation | Sí (Fase 2) | Disparos justos |

---

### 1.4 Contrato con Backend (Futuro)

El Game Runtime (Godot) se integrará con un Backend externo:

```
CLIENTE → BACKEND:
• Autenticación (Google OAuth)
• Solicitar partida (matchmaking)
• Obtener IP:Puerto del Dedicated Server asignado

DEDICATED SERVER → BACKEND:
• Registrarse como disponible
• Validar tokens de jugadores
• Reportar fin de partida (stats)
```

**Para Fase 1**: El cliente conectará a IP:Puerto hardcoded. La integración con Backend vendrá en fases posteriores.

---

### 1.5 Anti-Patterns a Evitar

| Anti-Pattern | Por qué es malo | Alternativa |
|--------------|-----------------|-------------|
| **God Object** | GameManager que hace todo | Múltiples sistemas especializados |
| **Paths hardcoded** | Cambio de estructura = break | `[Export] NodePath` o referencias inyectadas |
| **Cliente decide** | Inconsistencia, cheats | RPC al servidor, servidor aplica |
| **Polling para eventos** | Ineficiente, acoplado | Signals → reacción |
| **Acoplamiento cíclico** | Difícil de mantener | Signals para romper el ciclo |
| **Estado duplicado** | Desincronización | Una fuente de verdad + replicación |

---

## 2. Estructura del Proyecto

### 2.1 Capas del Sistema

```
┌─────────────────────────────────────────────────────────────────┐
│                         BOOTSTRAP                               │
│         Punto de entrada. Decide modo: Server / Client          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         SISTEMAS                                │
│                                                                 │
│   Orquestan el juego. Escuchan eventos. Coordinan entidades.   │
│                                                                 │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│   │   Session    │  │    Spawn     │  │     Wave     │         │
│   │   System     │  │    System    │  │    System    │         │
│   └──────────────┘  └──────────────┘  └──────────────┘         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        ENTIDADES                                │
│                                                                 │
│   Objetos del mundo. Estado + física. Emiten señales.          │
│                                                                 │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│   │    Player    │  │    Enemy     │  │    Bullet    │         │
│   └──────────────┘  └──────────────┘  └──────────────┘         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       COMPONENTES                               │
│                                                                 │
│   Lógica adjunta a entidades. Input, networking, visuales.     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      INFRAESTRUCTURA                            │
│                                                                 │
│   Utilidades transversales: Ticks, helpers de red.             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

### 2.2 Sistemas y Responsabilidades

| Sistema | Responsabilidad | Autoridad |
|---------|-----------------|-----------|
| **SessionSystem** | Flujo de la partida: Lobby → Playing → GameOver | Servidor |
| **SpawnSystem** | Crear y destruir entidades (players, enemies) | Servidor |
| **WaveSystem** | Progresión de oleadas, spawn de enemigos | Servidor |
| **NetworkSystem** | Gestión de conexiones, peers | Servidor |
| **TickManager** | Sincronización temporal, heartbeat | Servidor (broadcast) |

**Regla**: Los sistemas son nodos en el Scene Tree. Se comunican entre sí por referencia directa. Escuchan a las entidades por signals.

---

### 2.3 Comunicación entre Capas

```
                    SISTEMAS
                       │
         ┌─────────────┼─────────────┐
         │             │             │
         ▼             ▼             ▼
    ┌─────────┐   ┌─────────┐   ┌─────────┐
    │ Player  │   │  Enemy  │   │  Bullet │
    └────┬────┘   └────┬────┘   └────┬────┘
         │             │             │
         └─────────────┼─────────────┘
                       │
                   SIGNALS
                  (upstream)
                       │
         ┌─────────────┼─────────────┐
         ▼             ▼             ▼
    ┌─────────┐   ┌─────────┐   ┌─────────┐
    │ Session │   │  Spawn  │   │   HUD   │
    │ System  │   │  System │   │         │
    └─────────┘   └─────────┘   └─────────┘
```

| Dirección | Mecanismo | Ejemplo |
|-----------|-----------|---------|
| Sistema → Sistema | `GetNode()` directo | SessionSystem llama a SpawnSystem.SpawnPlayer() |
| Sistema → Entidad | Referencia o método | SpawnSystem crea Player, guarda referencia |
| Entidad → Sistema | **Signal** | Player.Died → SessionSystem escucha |
| Entidad → Entidad | **Nunca directo** | Siempre a través de un sistema |

---

### 2.4 Organización de Carpetas

```
ZombieBox/
├── scripts/
│   ├── Core/           # Bootstrap, TickManager, NetworkUtils
│   ├── Systems/        # SessionSystem, SpawnSystem, WaveSystem
│   ├── Entities/       # Player, Enemy, Bullet
│   ├── Components/     # PlayerInput, RemoteInterpolator
│   └── Weapons/        # IWeapon, implementaciones
│
├── scenes/
│   ├── entities/       # Player.tscn, Enemy.tscn, Bullet.tscn
│   ├── maps/           # Arena01.tscn, ...
│   └── ui/             # HUD.tscn, MainMenu.tscn
│
├── assets/             # Sprites, audio, fonts
└── docs/               # Documentación
```

**Principio**: La estructura de carpetas refleja las capas. Un desarrollador nuevo entiende dónde buscar.

---

### 2.5 Escena de Juego

```
GameSession
├── Systems/           # Todos los sistemas como hijos
├── World/
│   ├── Level/         # Mapa actual (TileMap, Navigation)
│   └── Entities/      # Players y Enemies spawneados aquí
└── UI/                # HUD, pantallas
```

**Regla**: `World/Entities/` es el contenedor donde SpawnSystem coloca las entidades. Los MultiplayerSpawners apuntan aquí para replicación automática.

---

## 3. Modelo de Red

### 3.1 Arquitectura Cliente-Servidor

```
┌─────────────────────────────────────────────────────────────────┐
│                      DEDICATED SERVER                           │
│                        (Headless)                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   • Ejecuta toda la lógica de juego                            │
│   • Simula física de todas las entidades                       │
│   • Controla la IA de enemigos                                 │
│   • Valida acciones de jugadores                               │
│   • Broadcast de estado a todos los clientes                   │
│                                                                 │
│   NO tiene: Renderizado, audio, input local                    │
│                                                                 │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │  ENet (UDP)
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│   CLIENTE 1  │   │   CLIENTE 2  │   │   CLIENTE N  │
├──────────────┤   ├──────────────┤   ├──────────────┤
│              │   │              │   │              │
│ • Captura    │   │ • Captura    │   │ • Captura    │
│   input      │   │   input      │   │   input      │
│              │   │              │   │              │
│ • Renderiza  │   │ • Renderiza  │   │ • Renderiza  │
│   estado     │   │   estado     │   │   estado     │
│              │   │              │   │              │
│ • Predice    │   │ • Predice    │   │ • Predice    │
│   (opcional) │   │   (opcional) │   │   (opcional) │
│              │   │              │   │              │
└──────────────┘   └──────────────┘   └──────────────┘
```

**Principio clave**: El servidor nunca espera al cliente. El servidor avanza su simulación; los clientes se adaptan.

---

### 3.2 Tick System

El tiempo de juego se divide en **ticks discretos**, no en tiempo continuo.

```
Tick Rate: 60 ticks/segundo
Duración de tick: ~16.67ms

Servidor:
═══╤═══════╤═══════╤═══════╤═══════╤═══════╤═══════►  tiempo
   │       │       │       │       │       │
 Tick 0  Tick 1  Tick 2  Tick 3  Tick 4  Tick 5

Cada tick, el servidor:
  1. Procesa inputs recibidos de clientes
  2. Simula física (MoveAndSlide, colisiones)
  3. Actualiza estado del juego
  4. Envía snapshot a clientes
```

| Beneficio | Explicación |
|-----------|-------------|
| Determinismo | Mismo input en mismo tick = mismo resultado |
| Reconciliación | Se puede comparar "tick 50 del cliente" vs "tick 50 del servidor" |
| Lag Compensation | Se puede rebobinar al "tick que el cliente vio" |
| Eficiencia | Se agrupan actualizaciones en lugar de enviar cada cambio |

---

### 3.3 Tipos de Datos y Sincronización

| Tipo de dato | Mecanismo | Frecuencia | Ejemplo |
|--------------|-----------|------------|---------|
| **Estado continuo** | MultiplayerSynchronizer | Cada tick | Posición, rotación, vida |
| **Acción discreta** | RPC | Por evento | Disparar, cambiar arma |
| **Spawn/Despawn** | MultiplayerSpawner | Por evento | Crear jugador, crear bala |
| **Estado de sesión** | MultiplayerSynchronizer | Por cambio | GameState (Lobby, Playing) |

---

### 3.4 Flujo de Input (Cliente → Servidor)

```
┌─────────────────────────────────────────────────────────────────┐
│ CLIENTE                                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. PlayerInput detecta tecla/mouse                           │
│   2. Empaqueta: { moveVector, aimDirection, tick }             │
│   3. Envía al servidor (MultiplayerSynchronizer o RPC)         │
│                                                                 │
│   Para acciones discretas (disparar):                          │
│   → RPC inmediato al servidor                                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ SERVIDOR                                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. Recibe input del cliente                                  │
│   2. Valida (¿puede hacer esta acción?)                        │
│   3. Aplica al jugador correspondiente                         │
│   4. El estado resultante se replica automáticamente           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

### 3.5 Flujo de Estado (Servidor → Clientes)

```
┌─────────────────────────────────────────────────────────────────┐
│ SERVIDOR (cada tick)                                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Estado actual de todas las entidades:                        │
│   • Player_2: pos(150,200), health=3, weapon=Pistol            │
│   • Player_3: pos(300,180), health=2, weapon=MachineGun        │
│   • Enemy_1:  pos(400,250), health=1                           │
│                                                                 │
│   MultiplayerSynchronizer replica automáticamente              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                    Broadcast a todos
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
   ┌─────────┐          ┌─────────┐          ┌─────────┐
   │Cliente 2│          │Cliente 3│          │Cliente N│
   │         │          │         │          │         │
   │Renderiza│          │Renderiza│          │Renderiza│
   │ estado  │          │ estado  │          │ estado  │
   └─────────┘          └─────────┘          └─────────┘
```

---

### 3.6 Autoridad por Componente

```
Player (CharacterBody2D)
│
├── Posición/Física ────────► Autoridad: SERVIDOR
│                              El servidor calcula y replica
│
├── Input ──────────────────► Autoridad: CLIENTE DUEÑO
│                              Solo el peer dueño puede modificar
│
├── Visuales ───────────────► Autoridad: LOCAL
│                              Cada cliente renderiza independiente
│
└── Controller (lógica) ────► Autoridad: SERVIDOR
                               Solo existe en el servidor
```

**Regla de oro**: Si algo afecta el estado del juego → Servidor. Si es solo visualización → Cliente.

---

### 3.7 Compensación de Latencia

#### Client-Side Prediction (Movimiento local)

```
Problema: Sin predicción, el jugador siente 100ms+ de delay al moverse.

Solución: El cliente mueve su personaje INMEDIATAMENTE (predicción).
          Cuando llega la confirmación del servidor, corrige si hay diferencia.
```

#### Entity Interpolation (Entidades remotas)

```
Problema: Los otros jugadores "saltan" porque los updates llegan discretos.

Solución: Renderizar entidades remotas ligeramente en el PASADO,
          interpolando entre estados conocidos.

Resultado: Movimiento suave, sin saltos.
```

#### Lag Compensation (Disparos justos)

```
Problema: Cuando disparas, apuntas donde VES al enemigo.
          Pero por latencia, el enemigo ya no está ahí en el servidor.

Solución: El servidor "rebobina" al momento que el cliente vio,
          y evalúa el disparo en ese estado pasado.
```

---

### 3.8 Resumen de Flujos

| Acción | Iniciador | Validador | Resultado |
|--------|-----------|-----------|-----------|
| Mover | Cliente (input) | Servidor (física) | Posición replicada |
| Disparar | Cliente (RPC) | Servidor (cooldown, lag comp) | Bala spawneada |
| Recibir daño | Servidor (colisión) | Servidor | Health replicado |
| Morir | Servidor (health=0) | Servidor | Signal Died, entidad removida |
| Cambiar arma | Cliente (RPC) | Servidor | Weapon replicado |

---

## 4. Flujo de Sesión

### 4.1 Estados de la Partida

```
┌─────────────────────────────────────────────────────────────────┐
│                    MÁQUINA DE ESTADOS                           │
└─────────────────────────────────────────────────────────────────┘

    ┌───────────────┐         ┌───────────────┐
    │               │         │               │
    │  INITIALIZING │────────►│    LOBBY      │
    │               │         │               │
    └───────────────┘         └───────┬───────┘
                                      │
                         Mínimo de jugadores
                              conectados
                                      │
                                      ▼
                              ┌───────────────┐
                              │               │
                              │    PLAYING    │◄─────┐
                              │               │      │
                              └───────┬───────┘      │
                                      │              │
                    ┌─────────────────┼──────────────┤
                    │                 │              │
              Todos mueren      Oleada completa   Reiniciar
                    │                 │           (host)
                    ▼                 │              │
            ┌───────────────┐         │              │
            │               │         │              │
            │   GAME OVER   │─────────┴──────────────┘
            │               │
            └───────────────┘
```

---

### 4.2 Descripción de Estados

| Estado | Descripción | El mundo está... |
|--------|-------------|------------------|
| **Initializing** | Servidor arrancando, cargando nivel | Pausado |
| **Lobby** | Esperando jugadores. UI muestra "Esperando X/N" | Pausado |
| **Playing** | Partida activa. Oleadas, combate, movimiento | Activo |
| **GameOver** | Todos murieron. UI muestra resultado | Pausado |

**Mecanismo de pausa**: Se usa `ProcessMode` de Godot. El nodo `World` se desactiva, congelando física y lógica de entidades sin código adicional.

---

### 4.3 Transiciones

| Transición | Trigger | Acciones |
|------------|---------|----------|
| Init → Lobby | Nivel cargado | Activar UI de lobby, Broadcast estado |
| Lobby → Playing | Jugadores ≥ mínimo | Spawn de jugadores, Iniciar WaveSystem, Activar World |
| Playing → GameOver | Todos los jugadores muertos | Pausar World, Mostrar UI resultado |
| GameOver → Playing | Acción de reinicio | Resetear estado, Respawnear jugadores |

---

### 4.4 Flujo de Conexión de Jugador

```
        [Servidor escuchando]
                │
                │◄──────────────────── Conexión del cliente
                │
        PeerConnected(peer_id)
                │
                ├── Si estado = Lobby:
                │       ├── Registrar peer
                │       ├── ¿Alcanzado mínimo? → Transición a Playing
                │       └── Broadcast: "Jugador X conectado"
                │
                └── Si estado = Playing:
                        ├── Registrar peer
                        ├── Spawn jugador (late join)
                        └── Sincronizar estado actual
```

---

### 4.5 Flujo de Desconexión de Jugador

```
        PeerDisconnected(peer_id)
                │
                ├── Remover jugador del mundo
                ├── Notificar a otros sistemas (WaveSystem, SessionSystem)
                │
                └── ¿Quedan jugadores?
                        ├── Sí → Continuar partida
                        └── No → Volver a Lobby (o cerrar servidor)
```

---

### 4.6 Sincronización del Estado de Sesión

El estado de la sesión se replica a todos los clientes mediante MultiplayerSynchronizer.

**Regla**: Los clientes NUNCA cambian el estado de sesión. Solo lo leen y reaccionan visualmente.

---

### 4.7 Integración Futura con Backend

```
                              BACKEND
                                 │
        ┌────────────────────────┼────────────────────────┐
        │                        │                        │
        ▼                        ▼                        ▼
   Crear partida           Asignar server           Terminar partida
        │                        │                        │
        └────► Dedicated Server arranca ◄─────────────────┘
                      │
                      ▼
               Gameplay normal
                      │
                      ▼
               Reportar stats al Backend
```

Para Fase 1, estas integraciones son stubs. El servidor arranca por CLI y no reporta al backend.

---

## 5. Entidades y Componentes

### 5.1 Filosofía de Diseño

Las entidades son **objetos del mundo del juego** con identidad y estado. No conocen la red, no conocen los sistemas, solo saben existir y emitir señales cuando algo relevante ocurre.

```
┌─────────────────────────────────────────────────────────────────┐
│                      ENTIDAD IDEAL                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   TIENE:                           NO TIENE:                    │
│   ─────────                        ──────────                   │
│   • Estado (health, position)      • Referencias a sistemas     │
│   • Física (collision, movement)   • Conocimiento de la red     │
│   • Señales (Died, Damaged)        • Lógica de UI               │
│   • Métodos públicos simples       • Acceso a otros jugadores   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

Los **componentes** son piezas de lógica que se adjuntan a las entidades para darles capacidades específicas (input, networking, visuales).

---

### 5.2 Catálogo de Entidades

| Entidad | Responsabilidad | Autoridad |
|---------|-----------------|-----------|
| **Player** | Representar jugador. Moverse, recibir daño, portar armas. | Servidor |
| **Enemy** | Enemigo con IA. Perseguir jugadores, infligir daño por contacto. | Servidor |
| **Bullet** | Proyectil. Viaja en línea recta, detecta colisiones. | Servidor |

**Señales comunes**:
- `Died` → Cuando health llega a 0
- `HealthChanged(int)` → Cuando health cambia

---

### 5.3 Catálogo de Componentes

| Componente | Se adjunta a | Autoridad | Responsabilidad |
|------------|--------------|-----------|-----------------|
| **PlayerInput** | Player | Cliente dueño | Capturar input, exponerlo como estado sincronizado |
| **PlayerController** | Player | Servidor | Leer input, aplicar física, validar acciones |
| **RemoteInterpolator** | Player, Enemy | Cliente (no dueño) | Suavizar movimiento de entidades remotas |
| **EntityVisuals** | Todas | Cliente local | Sprites, animaciones, efectos visuales |

---

### 5.4 Diagrama de Activación por Contexto

```
                        SERVIDOR    CLIENTE     CLIENTE
                                    (dueño)     (otros)
                        ────────    ────────    ────────
Player (entidad)           ✓           ✓           ✓
├── PlayerInput            ✗           ✓           ✗
├── PlayerController       ✓           ✗           ✗
├── RemoteInterpolator     ✗           ✗           ✓
└── PlayerVisuals          ✗           ✓           ✓
```

---

### 5.5 Sistema de Armas (Extensible)

Las armas se implementan como estrategias intercambiables mediante interfaz `IWeapon`:

```
IWeapon
  ├── Pistol      (semi-automático, cooldown medio)
  ├── MachineGun  (automático, cooldown bajo)
  └── Shotgun     (semi-automático, múltiples proyectiles)
```

**Principio**: Añadir arma nueva = crear clase. No modificar Player.

---

### 5.6 Flujo de Daño

```
Bullet colisiona (servidor)
        │
        ▼
enemy.TakeDamage(damage)
        │
        ▼
health -= damage
EmitSignal(HealthChanged)
        │
        ▼
if (health <= 0) → EmitSignal(Died)
        │
        ▼
Sistemas escuchan Died → Reaccionan
```

**Principio**: La entidad cambia estado y emite señales. Los sistemas reaccionan.

---

## 6. Roadmap de Fases

### 6.1 Visión General

```
┌─────────┐     ┌─────────┐     ┌─────────┐     ┌─────────┐
│ FASE 1  │────►│ FASE 2  │────►│ FASE 3  │────►│ FASE 4  │
│  Base   │     │ Netcode │     │Gameplay │     │ Backend │
│         │     │Avanzado │     │Completo │     │  Integr │
└─────────┘     └─────────┘     └─────────┘     └─────────┘
```

---

### 6.2 Fase 1: Base Funcional

**Objetivo**: Dos jugadores conectados a Dedicated Server con gameplay básico.

| Componente | Alcance |
|------------|---------|
| Dedicated Server | Arranca por CLI, acepta conexiones |
| Cliente | Conecta por IP:Puerto hardcoded |
| Tick System | Implementado y funcionando |
| Player | Movimiento, disparo básico |
| Enemy | Spawn, persecución simple |
| Sincronización | MultiplayerSynchronizer |
| Sesión | Lobby → Playing → GameOver |

**NO incluye**: Predicción, interpolación, lag compensation.

---

### 6.3 Fase 2: Netcode Avanzado

**Objetivo**: Experiencia de red profesional.

| Componente | Alcance |
|------------|---------|
| Client Prediction | Movimiento local inmediato |
| Reconciliación | Corrección cuando servidor difiere |
| Entity Interpolation | Entidades remotas suaves |
| Lag Compensation | Disparos justos con rebobinado |

---

### 6.4 Fase 3: Gameplay Completo

**Objetivo**: Modo Survival jugable.

| Componente | Alcance |
|------------|---------|
| Armas | Sistema extensible, múltiples armas |
| Enemigos | Variedad, comportamientos distintos |
| Oleadas | Progresión, dificultad escalada |
| Audio/VFX | Feedback completo |

---

### 6.5 Fase 4: Integración Backend

**Objetivo**: Producción-ready.

| Componente | Alcance |
|------------|---------|
| Autenticación | Login con Google |
| Matchmaking | Buscar/crear partidas |
| Server Orchestration | Spin up/down automático |
| Persistencia | Stats, progreso |
