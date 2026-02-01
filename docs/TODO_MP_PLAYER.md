# Plan: Trinidad del Multijugador para Player

## Objetivo
Implementar los tres pilares del netcode profesional en la entidad Player:
1. **Client-Side Prediction** - Movimiento local inmediato
2. **Entity Interpolation** - Entidades remotas suaves
3. **Lag Compensation** - Disparos justos con rebobinado

## Estado Actual (Fase 1)
- Server-authoritative funcionando
- MultiplayerSynchronizer replica estado bidireccional
- RPCs para acciones discretas (disparo, cambio arma)
- **NO hay**: interpolación, predicción, lag compensation, tick system explícito

---

## Arquitectura Propuesta

### Jerarquía Actualizada de Player
```
Player (CharacterBody2D)
├── ServerSynchronizer       # Existente
├── ServerController         # Existente (solo servidor)
├── PlayerInput              # Existente
│   └── InputSynchronizer    # Existente
├── ClientPredictor          # NUEVO (solo cliente local)
├── RemoteInterpolator       # NUEVO (solo clientes remotos)
├── PlayerVisuals            # Existente
├── Sprite2D
└── CollisionShape2D
```

### Nuevos Archivos a Crear
| Archivo | Propósito |
|---------|-----------|
| `scripts/Core/TickManager.cs` | Reloj global sincronizado |
| `scripts/Core/StateSnapshot.cs` | Estructuras de datos |
| `scripts/Core/CircularBuffer.cs` | Buffer circular genérico |
| `scripts/Components/ClientPredictor.cs` | Predicción + reconciliación |
| `scripts/Components/RemoteInterpolator.cs` | Interpolación visual |
| `scripts/Core/LagCompensator.cs` | Rebobinado server-side |

---

## Secuencia de Implementación

### Paso 1: TickManager (Base del sistema)

**Crear** `scripts/Core/TickManager.cs`:
```csharp
public partial class TickManager : Node
{
    public static TickManager Instance { get; private set; }

    [Export] public uint ServerTick { get; private set; } = 0;
    public uint LocalTick { get; private set; } = 0;

    public const int TickRate = 60;
    public const float TickDuration = 1f / TickRate;

    [Signal] public delegate void TickAdvancedEventHandler(uint tick);
}
```

**Modificar** `GameSession.tscn`:
- Agregar TickManager como hijo de Managers
- Crear `tick_server_to_all.tres` para sincronizar ServerTick

**Modificar** `PlayerInput.cs`:
- Agregar `[Export] public uint InputTick { get; set; }`
- En ReadInput(): `InputTick = TickManager.Instance.LocalTick`

**Modificar** `player_input_client_to_server.tres`:
- Agregar InputTick a propiedades sincronizadas

---

### Paso 2: Client-Side Prediction

**Crear** `scripts/Core/StateSnapshot.cs`:
```csharp
public struct StateSnapshot {
    public uint Tick;
    public Vector2 Position;
    public float Rotation;
    public Vector2 Velocity;
    public Vector2 InputMoveVector;
}
```

**Crear** `scripts/Core/CircularBuffer.cs` (buffer genérico)

**Crear** `scripts/Components/ClientPredictor.cs`:
- Solo activo en cliente local (auto-QueueFree si servidor o remoto)
- Guarda historial de estados por tick
- Aplica movimiento inmediatamente (replica lógica de PlayerController)
- Cuando llega estado servidor: compara y reconcilia si error > umbral
- Reconciliación = reset + resimular inputs desde tick servidor

**Modificar** `Player.cs`:
- Agregar `[Export] public uint LastProcessedTick { get; set; }`
- El servidor lo actualiza en cada frame

**Modificar** `player_state_server_to_all.tres`:
- Agregar LastProcessedTick

**Modificar** `player.tscn`:
- Agregar nodo ClientPredictor

---

### Paso 3: Entity Interpolation

**Crear** `scripts/Components/RemoteInterpolator.cs`:
- Solo activo en clientes remotos (auto-QueueFree si local o servidor)
- Buffer de estados recibidos (últimos ~100ms)
- Renderiza en el "pasado" interpolando entre estados
- Aplica a posición VISUAL (Sprite2D), no a CharacterBody2D

**Modificar** `player.tscn`:
- Agregar nodo RemoteInterpolator

---

### Paso 4: Lag Compensation

**Crear** `scripts/Core/LagCompensator.cs`:
- Singleton server-side
- Rastrea posiciones de entidades "compensables" (enemigos)
- Método `PerformLagCompensatedHitCheck(origin, direction, clientTick)`:
  1. Guardar posiciones actuales
  2. Rebobinar entidades al tick del cliente
  3. Ejecutar raycast
  4. Restaurar posiciones
  5. Retornar hit

**Modificar** `Player.cs`:
```csharp
public void TryShoot()
{
    uint clientTick = TickManager.Instance.LocalTick;
    RpcId(1, nameof(RequestFireWithTick), clientTick);
}

[Rpc(...)]
private void RequestFireWithTick(uint clientTick)
{
    if (!NetworkUtils.IsServer()) return;
    // Usar LagCompensator.PerformLagCompensatedHitCheck(...)
}
```

**Modificar** `SpawnSystem.cs`:
- Registrar enemigos en LagCompensator al spawnear
- Desregistrar al morir

**Agregar** LagCompensator a `GameSession.tscn`

---

## Archivos Críticos a Modificar

| Archivo | Cambios |
|---------|---------|
| `scripts/Entities/Player.cs` | Agregar LastProcessedTick, modificar TryShoot para lag comp |
| `scripts/Components/PlayerInput.cs` | Agregar InputTick |
| `scenes/entities/player/player.tscn` | Agregar ClientPredictor y RemoteInterpolator |
| `scenes/entities/player/player_state_server_to_all.tres` | Agregar LastProcessedTick |
| `scenes/entities/player/player_input_client_to_server.tres` | Agregar InputTick |
| `scenes/systems/GameSession.tscn` | Agregar TickManager y LagCompensator |
| `scripts/Systems/SpawnSystem.cs` | Integrar con LagCompensator |

---

## Verificación

### Test 1: TickManager
- Conectar cliente a servidor
- Verificar que `ServerTick` se sincroniza (drift < 5 ticks)

### Test 2: Predicción
- Simular latencia artificial (100ms)
- Mover jugador - debe sentirse inmediato
- Verificar que reconciliaciones son < 5% de frames

### Test 3: Interpolación
- Conectar 2 clientes
- Cliente A mueve, Cliente B observa
- Movimiento de A debe verse suave en B (sin saltos)

### Test 4: Lag Compensation
- Simular latencia (100ms)
- Enemigo moviéndose
- Disparar a posición VISUAL del enemigo
- Debe acertar

---

## Notas de Implementación

1. **Componentes auto-desactivados**: ClientPredictor y RemoteInterpolator se auto-destruyen en contextos incorrectos
2. **Interpolación visual**: RemoteInterpolator mueve el Sprite2D, no el CharacterBody2D (evita conflictos con sync)
3. **Ventana de lag comp**: Máximo ~200ms para evitar abusos
4. **Resimulación limitada**: Máximo 10 ticks para evitar lag spikes
