# Player Entity Documentation

## Overview

**Player** es la entidad principal del jugador en ZombieBox. Maneja el estado del jugador (vida, arma, kills) y emite signals para comunicarse con sistemas externos.

**Archivo:** `scripts/Entities/Player.cs`
**Escena:** `scenes/player.tscn`
**Patrón:** Signal-based communication (Godot idiom)

---

## Arquitectura de la Escena

### Jerarquía de Nodos

```
Player (CharacterBody2D)                    ← Entidad raíz
├── ServerSynchronizer (MultiplayerSynchronizer)  ← Infraestructura
├── ServerController (PlayerController)     ← Componente: Lógica
├── PlayerInput (Node)                      ← Componente: Input
│   └── InputSynchronizer (MultiplayerSynchronizer)  ← Infraestructura
├── PlayerVisuals (Node)                    ← Componente: Rendering
├── Sprite2D                                ← Visual (Godot)
└── CollisionShape2D                        ← Física (Godot)
```

### Separación de Responsabilidades

| Nodo | Capa | Responsabilidad |
|------|------|-----------------|
| **Player** | Entidad | Estado del juego, emitir signals |
| **ServerController** | Componente | Física y lógica (server-only) |
| **PlayerInput** | Componente | Captura de input (client-only) |
| **PlayerVisuals** | Componente | Rendering y animaciones |
| **ServerSynchronizer** | Infraestructura | Replicación server→clients |
| **InputSynchronizer** | Infraestructura | Replicación client→server |

---

## Signals Públicos

Player emite signals para comunicarse con **sistemas externos** (fuera de `player.tscn`).

### 1. `HealthChanged`

```csharp
[Signal]
public delegate void HealthChangedEventHandler(int newHealth);
```

**Cuándo se emite:**
- Cuando el jugador recibe daño
- Cuando el jugador se cura (futuro)
- En `_Ready()` para emitir estado inicial

**Escuchado por:**
- `HUD` - Actualiza la barra de vida

**Ejemplo de conexión:**
```csharp
// En HUD.cs
player.HealthChanged += OnPlayerHealthChanged;

private void OnPlayerHealthChanged(int newHealth)
{
    _healthBar.Value = newHealth;
}
```

---

### 2. `WeaponSwitched`

```csharp
[Signal]
public delegate void WeaponSwitchedEventHandler(WeaponType newWeapon);
```

**Cuándo se emite:**
- Cuando el jugador cambia de arma con Q
- En `_Ready()` para emitir estado inicial

**Escuchado por:**
- `HUD` - Actualiza el icono de arma

**Ejemplo de conexión:**
```csharp
// En HUD.cs
player.WeaponSwitched += OnPlayerWeaponSwitched;

private void OnPlayerWeaponSwitched(WeaponType newWeapon)
{
    _weaponIcon.Texture = GetWeaponIcon(newWeapon);
}
```

---

### 3. `EnemyKilled`

```csharp
[Signal]
public delegate void EnemyKilledEventHandler(int totalKills);
```

**Cuándo se emite:**
- Cuando una bala del jugador mata un enemigo
- En `_Ready()` para emitir estado inicial

**Escuchado por:**
- `HUD` - Actualiza contador de kills
- `AchievementSystem` (futuro) - Desbloquear logros

**Ejemplo de conexión:**
```csharp
// En HUD.cs
player.EnemyKilled += OnPlayerEnemyKilled;

private void OnPlayerEnemyKilled(int totalKills)
{
    _killsLabel.Text = $"Kills: {totalKills}";
}
```

---

### 4. `WeaponFired`

```csharp
[Signal]
public delegate void WeaponFiredEventHandler(Vector2 position, Vector2 direction, string shooterName);
```

**Cuándo se emite:**
- Cuando el jugador dispara (pistola o metralleta)
- Solo en el servidor

**Escuchado por:**
- `SpawnSystem` - Crea el proyectil
- `AudioManager` (futuro) - Reproduce sonido de disparo
- `VFXManager` (futuro) - Efecto de flash del cañón

**Ejemplo de conexión:**
```csharp
// En SessionSystem.cs (conecta Player → SpawnSystem)
player.WeaponFired += (position, direction, shooterName) =>
    SpawnSystem.OnPlayerWeaponFired(player, position, direction, shooterName);

// En SpawnSystem.cs
public void OnPlayerWeaponFired(Player player, Vector2 position, Vector2 direction, string shooterName)
{
    Bullet bullet = SpawnBullet(position, direction, shooterName);
    if (bullet != null)
    {
        bullet.EnemyKilled += player.OnEnemyKilledByBullet;
    }
}
```

**Notas:**
- Este signal cruza el límite de escena (Player.tscn → GameSession.tscn)
- Player NO conoce SpawnSystem (desacoplado)
- SpawnSystem SÍ conoce Player (correcto según capas)

---

### 5. `Died` (Signal + Event)

```csharp
[Signal]
public delegate void DiedSignalEventHandler();

public event Action Died; // C# event version (IDamageable)
```

**Cuándo se emite:**
- Cuando Health <= 0
- Solo en el servidor

**Escuchado por:**
- `SessionSystem` - Detecta game over (todos los jugadores muertos)
- `WaveSystem` - Pausa spawns si no hay jugadores vivos
- `ReplaySystem` (futuro) - Guarda momento de muerte

**Ejemplo de conexión:**
```csharp
// En SessionSystem.cs
player.Died += OnPlayerDied;

private void OnPlayerDied()
{
    _playersAlive--;
    if (_playersAlive <= 0)
    {
        GameOver();
    }
}
```

**Notas:**
- Tiene dos versiones: Godot Signal + C# Event
- La versión C# Event es para IDamageable (interfaz del dominio)

---

## Estado Público

### Propiedades de Solo Lectura

```csharp
public int Health { get; private set; } = 3;
public WeaponType CurrentWeapon { get; private set; } = WeaponType.Pistol;
public float Speed { get; set; } = 300.0f;
```

**Acceso:**
- ✅ Lectura desde componentes (PlayerVisuals, PlayerController)
- ✅ Lectura desde sistemas externos (solo para display/logic)
- ❌ Escritura solo desde Player internamente

**Sincronización:**
- `Health`, `CurrentWeapon`: Replicados por ServerSynchronizer (server→clients)
- `Position`, `Rotation`: Replicados automáticamente por CharacterBody2D

---

## API Pública

### Métodos Llamables

#### `TakeDamage(int damage)`
```csharp
public void TakeDamage(int damage)
```

**Propósito:** Aplica daño al jugador (implementación de IDamageable)
**Autoridad:** Server-only
**Llamado por:** Enemy (cuando colisiona), otros sistemas de daño
**Efectos:**
- Reduce Health
- Emite `HealthChanged`
- Si Health <= 0, llama `Die()` y emite `Died`

---

#### `OnEnemyKilledByBullet()`
```csharp
public void OnEnemyKilledByBullet()
```

**Propósito:** Incrementa contador de kills
**Autoridad:** Server-only
**Llamado por:** Bullet (cuando mata un enemigo)
**Efectos:**
- Incrementa `_kills`
- Emite `EnemyKilled`

---

### Métodos Internos (No llamar desde fuera)

#### `DoFire()`
```csharp
public void DoFire()
```

**Propósito:** Dispara el arma
**Autoridad:** Server-only
**Llamado por:** PlayerController (metralleta) o RPC (pistola)
**Efectos:**
- Emite signal `WeaponFired`

---

#### `TryShoot()` / `TrySwitchWeapon()`
```csharp
public void TryShoot()
public void TrySwitchWeapon()
```

**Propósito:** Envía RPCs al servidor para acciones de input
**Autoridad:** Client (local player)
**Llamado por:** PlayerInput
**Efectos:**
- Envía RPC al servidor
- Servidor ejecuta la acción

---

## Flujo de Datos

### Input → Movimiento (Continuous)

```
1. PlayerInput._Process()              [Client dueño]
   ↓
   Lee teclado/ratón
   ↓
   Actualiza MoveVector, AimDirection
   ↓
2. InputSynchronizer                    [Infraestructura]
   ↓
   Replica MoveVector → Servidor
   ↓
3. PlayerController._PhysicsProcess()   [Server]
   ↓
   Lee MoveVector
   ↓
   Calcula Velocity
   ↓
   Player.MoveAndSlide()
   ↓
4. ServerSynchronizer                   [Infraestructura]
   ↓
   Replica Position → Todos los clientes
```

---

### Input → Disparar (Event)

```
1. PlayerInput._Process()              [Client dueño]
   ↓
   Detecta Input.IsActionJustPressed("shoot")
   ↓
   Player.TryShoot()
   ↓
2. RPC RequestFire()                    [Network]
   ↓
   Cliente → Servidor
   ↓
3. Player.DoFire()                      [Server]
   ↓
   EmitSignal(WeaponFired, ...)
   ↓
4. SpawnSystem.OnPlayerWeaponFired()    [Server]
   ↓
   SpawnBullet(...)
   ↓
   Conecta bullet.EnemyKilled → player.OnEnemyKilledByBullet
```

---

### Recibir Daño

```
1. Enemy colisiona con Player           [Server]
   ↓
   Enemy._on_damage_area_body_entered(player)
   ↓
2. Player.TakeDamage(1)                 [Server]
   ↓
   Health -= 1
   ↓
   EmitSignal(HealthChanged, Health)
   ↓
3. HUD.OnPlayerHealthChanged()          [Todos los clientes]
   ↓
   Actualiza UI
   ↓
4. Si Health <= 0:
   ↓
   Player.Die()
   ↓
   EmitSignal(Died)
   ↓
   SessionSystem.OnPlayerDied()
   ↓
   Verifica game over
```

---

## Reglas de Arquitectura

### ✅ Player PUEDE:

1. **Manejar su propio estado**
   ```csharp
   Health -= damage;
   CurrentWeapon = WeaponType.MachineGun;
   ```

2. **Emitir signals para eventos importantes**
   ```csharp
   EmitSignal(SignalName.WeaponFired, ...);
   EmitSignal(SignalName.Died);
   ```

3. **Llamar métodos de sus componentes**
   ```csharp
   var controller = GetNode<PlayerController>("ServerController");
   // (aunque actualmente no lo hace, emite signals)
   ```

4. **Exponer API pública para ser llamado**
   ```csharp
   public void TakeDamage(int damage) { ... }
   ```

---

### ❌ Player NO DEBE:

1. **Buscar sistemas externos**
   ```csharp
   // ❌ INCORRECTO:
   var spawnSystem = GetTree().Root.FindChild("SpawnSystem", ...);
   ```

2. **Conocer lógica de sistemas**
   ```csharp
   // ❌ INCORRECTO:
   sessionSystem.CheckGameOver();
   waveSystem.SpawnNextWave();
   ```

3. **Actualizar UI directamente**
   ```csharp
   // ❌ INCORRECTO:
   hud.UpdateHealthBar(Health);
   ```

4. **Manejar lógica de otros sistemas**
   ```csharp
   // ❌ INCORRECTO:
   public void DoFire()
   {
       Bullet bullet = new Bullet(); // NO, usa signal
   }
   ```

---

## Componentes de Player

### PlayerInput (Componente)

**Responsabilidad:** Captura de input del jugador local

**Proceso:** `_Process(delta)` - Lee cada frame visual
**Autoridad:** Solo procesa si `IsMultiplayerAuthority()`
**Sincronización:** Propiedades replicadas via InputSynchronizer

**Propiedades sincronizadas:**
- `MoveVector` - Dirección de movimiento
- `AimDirection` - Dirección de apuntado
- `IsShooting` - Estado de botón de disparo

**Acciones:**
- Llama `Player.TryShoot()` cuando se presiona disparo
- Llama `Player.TrySwitchWeapon()` cuando se presiona Q

---

### PlayerController (Componente)

**Responsabilidad:** Lógica de juego y física (server-only)

**Proceso:** `_PhysicsProcess(delta)` - Lógica en fixed timestep
**Autoridad:** Solo existe en el servidor (auto-destruye en clientes)
**Dependencias:** Lee PlayerInput, modifica Player

**Funciones:**
- `HandleMovement()` - Aplica física de movimiento
- `HandleShooting()` - Disparo automático (metralleta)

**Configuración:**
- `MachineGunFireRate` - Balas por segundo (default: 5.0)

---

### PlayerVisuals (Componente)

**Responsabilidad:** Rendering y animaciones visuales

**Proceso:** `_Process(delta)` - Rendering en frame visual
**Autoridad:** Corre en todos los clientes
**Dependencias:** Lee Player.CurrentWeapon, PlayerInput.MoveVector

**Funciones:**
- `UpdateSpriteTexture()` - Cambia sprite según arma
- `AnimateMovement()` - Efecto de "bobbing" al caminar

**Configuración:**
- `TexturePistol` - Sprite de pistola
- `TextureMachineGun` - Sprite de metralleta

---

## Sincronización de Red

### ServerSynchronizer

**Propiedades replicadas (Server → Clients):**
```
Position     (CharacterBody2D automático)
Rotation     (CharacterBody2D automático)
Health       (custom)
CurrentWeapon (custom)
```

**Configuración:** `scenes/player_server_sync.tres`

---

### InputSynchronizer

**Propiedades replicadas (Client → Server):**
```
MoveVector
AimDirection
IsShooting
```

**Configuración:** `scenes/player_input_sync.tres`

**Nota:** Solo replica desde el cliente dueño (autoridad establecida en `_EnterTree()`)

---

## Autoridad de Red

### Configuración en `_EnterTree()`

```csharp
public override void _EnterTree()
{
    // Player body: Siempre servidor
    SetMultiplayerAuthority(1);

    // PlayerInput: Cliente dueño (basado en Name)
    var input = GetNodeOrNull<PlayerInput>("PlayerInput");
    if (input != null && int.TryParse(Name, out int authorityId))
    {
        input.SetMultiplayerAuthority(authorityId);
    }
}
```

**Explicación:**
- El cuerpo del Player (física) es autoritativo en el servidor
- El input del Player es autoritativo en el cliente dueño
- El Name del nodo Player es el Peer ID (ejemplo: "1", "2", "3")

---

## Testing

### Test Unitario (sin dependencies)

```csharp
[Test]
public void Player_DoFire_EmitsWeaponFiredSignal()
{
    // Arrange
    var player = new Player();
    bool signalEmitted = false;
    player.WeaponFired += (pos, dir, name) => signalEmitted = true;

    // Act
    player.DoFire();

    // Assert
    Assert.IsTrue(signalEmitted);
}
```

### Test de Integración (con escena)

```csharp
[Test]
public void Player_TakeDamage_EmitsHealthChanged()
{
    // Arrange
    var playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
    var player = playerScene.Instantiate<Player>();
    int healthReceived = -1;
    player.HealthChanged += (newHealth) => healthReceived = newHealth;

    // Act
    player.TakeDamage(1);

    // Assert
    Assert.AreEqual(2, healthReceived);
}
```

---

## Preguntas Frecuentes

### ¿Por qué Player usa signals en vez de llamar SpawnSystem directamente?

**Desacoplamiento.** Player es una entidad que puede existir en diferentes contextos:
- Tutorial (sin SpawnSystem)
- Testing (sin ningún sistema)
- Diferentes modos de juego

Si Player conociera SpawnSystem directamente:
- ❌ Estaría acoplado a GameSession.tscn
- ❌ No podría testearse aisladamente
- ❌ No sería reutilizable

Con signals:
- ✅ Player funciona solo
- ✅ Testeable sin dependencies
- ✅ Reutilizable en cualquier escena

---

### ¿PlayerInput debería usar signals en vez de llamar Player.TryShoot()?

**No.** PlayerInput y Player están en la **misma escena** (mismo bounded context).

Regla:
- Dentro de una escena: Acoplamiento directo OK
- Entre escenas: Usar signals

PlayerInput es específico de Player (no es genérico), así que llamar métodos directamente es pragmático y claro.

---

### ¿Cuándo se sincronizan los datos?

**Automático:**
- `Position`, `Rotation`: Cada tick de física (60 FPS)
- `Health`, `CurrentWeapon`: Cuando cambian (via ServerSynchronizer)
- `MoveVector`, `AimDirection`: Cada frame visual (via InputSynchronizer)

**Manual (via signals):**
- Eventos discretos como `WeaponFired`, `Died`

---

### ¿Qué pasa si un cliente modifica Health directamente?

**No puede.** `Health` tiene `private set`, solo Player puede modificarlo.

Además, aunque pudiera modificarlo localmente, no se replicaría porque:
1. ServerSynchronizer solo replica desde el servidor
2. Solo el servidor tiene autoridad (SetMultiplayerAuthority(1))

El cliente debe enviar RPC (`TakeDamage` via Enemy collision) y el servidor modifica Health.

---

## Changelog

### Phase 1 - Bottom-Up Architecture (Current)

**Signals implementados:**
- ✅ HealthChanged
- ✅ WeaponSwitched
- ✅ EnemyKilled
- ✅ WeaponFired
- ✅ Died

**Arquitectura:**
- ✅ Signals para comunicación externa
- ✅ Acoplamiento directo dentro de escena
- ✅ PlayerController sin referencia a SpawnSystem
- ✅ Desacoplamiento completo de sistemas

**Pendiente para Phase 2:**
- RemoteInterpolator (suavizar movimiento de jugadores remotos)
- Client-side prediction
- Lag compensation

---

## Referencias

- **Código fuente:** `scripts/Entities/Player.cs`
- **Escena:** `scenes/player.tscn`
- **Componentes:** `scripts/Components/PlayerInput.cs`, `PlayerController.cs`, `PlayerVisuals.cs`
- **Sistemas relacionados:** `SessionSystem.cs`, `SpawnSystem.cs`, `WaveSystem.cs`
- **Interfaz:** `IDamageable.cs`
