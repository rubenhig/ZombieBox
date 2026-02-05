# ZombieBox - Análisis y Diseño de Integración con Nakama

> **Versión**: 1.0
> **Fecha**: Febrero 2026
> **Estado**: Análisis y Diseño

---

## Índice

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Análisis de Necesidades](#2-análisis-de-necesidades)
3. [Capacidades de Nakama](#3-capacidades-de-nakama)
4. [Matriz de Cobertura](#4-matriz-de-cobertura)
5. [Arquitectura de Integración](#5-arquitectura-de-integración)
6. [Flujos de Interacción](#6-flujos-de-interacción)
7. [Implementación Custom Requerida](#7-implementación-custom-requerida)
8. [Roadmap de Integración](#8-roadmap-de-integración)
9. [Decisiones Técnicas](#9-decisiones-técnicas)
10. [Alternativas Consideradas](#10-alternativas-consideradas)

---

## 1. Resumen Ejecutivo

### 1.1 Decisión

**Nakama es la solución recomendada** para el backend de ZombieBox por las siguientes razones:

- ✅ Diseñado específicamente para juegos multiplayer con servidores dedicados
- ✅ Cubre 80% de las necesidades identificadas out-of-the-box
- ✅ Open-source con opciones de self-hosting
- ✅ SDK oficial para Godot con documentación activa
- ✅ Arquitectura compatible con el modelo server-authoritative de ZombieBox
- ✅ Extensible mediante Go/TypeScript/Lua para lógica custom

### 1.2 Alcance de la Integración

| Componente | Proveedor | Notas |
|------------|-----------|-------|
| Autenticación | **Nakama** | Google OAuth + device auth |
| Matchmaking | **Nakama** | Sistema de matchmaking flexible |
| Lobbies/Parties | **Nakama** | Grupos y salas de espera |
| Persistencia | **Nakama** | Stats, progreso, perfiles |
| Leaderboards | **Nakama** | Sistema nativo de clasificación |
| Chat/Social | **Nakama** | Chat en tiempo real, amigos |
| Orquestación Servidores | **Custom + Nakama** | Nakama coordina, orquestación externa (Docker/K8s) |
| Gameplay Runtime | **Godot Dedicado** | Sin cambios, ENet directo |

### 1.3 Cobertura General

```
┌─────────────────────────────────────────────────────────────────┐
│              COBERTURA NAKAMA VS NECESIDADES                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ████████████████░░░░  80% Nativo en Nakama                     │
│  ░░░░░░░░░░░░░░░░████  15% Lógica custom en Nakama runtime     │
│  ░░░░░░░░░░░░░░░░░░░█   5% Infraestructura externa             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Análisis de Necesidades

### 2.1 Necesidades Identificadas (del ARCHITECTURE.md)

Basado en la arquitectura actual de ZombieBox (Sección 0.3), se identifican las siguientes necesidades para la Fase 4:

#### Necesidades Críticas (Must-Have)

| ID | Necesidad | Descripción | Prioridad |
|----|-----------|-------------|-----------|
| **N1** | Autenticación | Login con Google OAuth, validación de tokens | 🔴 Crítica |
| **N2** | Matchmaking | Agrupar jugadores (2-4) para partidas cooperativas | 🔴 Crítica |
| **N3** | Orquestación Servidores | Spin up/down de servidores Godot headless | 🔴 Crítica |
| **N4** | Asignación Servidor | Clientes reciben IP:Puerto del servidor asignado | 🔴 Crítica |
| **N5** | Persistencia Stats | Almacenar estadísticas post-partida | 🟡 Alta |
| **N6** | Validación Tokens | Servidor Dedicado valida que jugador está autorizado | 🔴 Crítica |

#### Necesidades Secundarias (Should-Have)

| ID | Necesidad | Descripción | Prioridad |
|----|-----------|-------------|-----------|
| **N7** | Leaderboards | Clasificación por kills, oleadas, tiempo | 🟡 Alta |
| **N8** | Progreso Usuario | Desbloqueos, armas, cosméticos | 🟢 Media |
| **N9** | Party System | Jugadores se agrupan ANTES de matchmaking | 🟡 Alta |
| **N10** | Chat/Social | Chat en lobby, lista de amigos | 🟢 Media |
| **N11** | Gestión Sesiones | Tracking de partidas activas, reconexión | 🟡 Alta |

#### Necesidades Futuras (Nice-to-Have)

| ID | Necesidad | Descripción | Prioridad |
|----|-----------|-------------|-----------|
| **N12** | Modos PvP | Deathmatch, Team Deathmatch | ⚪ Baja |
| **N13** | Torneos | Competiciones estructuradas | ⚪ Baja |
| **N14** | In-Game Economy | Monedas, compras | ⚪ Baja |

### 2.2 Requisitos Técnicos

#### RT1: Arquitectura Server-Authoritative
- El servidor Godot Dedicado DEBE mantener autoridad total sobre el gameplay
- Nakama NO debe intervenir en la lógica de juego (física, colisiones, IA)
- Comunicación gameplay: Cliente ↔ Servidor Dedicado (ENet/UDP directo)

#### RT2: Bajo Acoplamiento
- El runtime de Godot debe funcionar independientemente de Nakama
- Si Nakama cae, partidas en curso NO deben verse afectadas
- Nakama se usa para orquestación, no para relay de gameplay

#### RT3: Escalabilidad
- Debe soportar crecimiento futuro (más jugadores, más servidores)
- Auto-scaling de servidores dedicados según demanda
- Infraestructura cloud-ready (Docker/Kubernetes)

#### RT4: Seguridad
- Tokens JWT para autenticación
- Validación server-side de todas las acciones críticas
- Protección contra suplantación de identidad

---

## 3. Capacidades de Nakama

### 3.1 Features Core

#### Autenticación (N1 ✅)

Nakama soporta múltiples métodos de autenticación:

| Método | Soporte | Uso en ZombieBox |
|--------|---------|------------------|
| Google OAuth | ✅ Nativo | **Primario** para jugadores |
| Facebook/Apple | ✅ Nativo | Opcional futuro |
| Device ID | ✅ Nativo | **Secundario** para testing/guests |
| Email/Password | ✅ Nativo | Opcional futuro |
| Custom Auth | ✅ Via runtime | Opcional futuro |

**Flujo:**
```
Cliente → Nakama.AuthenticateGoogle(token) → JWT Token
```

#### Matchmaking (N2 ✅)

Nakama provee un matchmaker altamente flexible:

| Característica | Soporte | Notas |
|----------------|---------|-------|
| Properties Custom | ✅ | Nivel de habilidad, modo de juego, región |
| Tamaño de Partido | ✅ | Min/Max jugadores (2-4 para ZombieBox) |
| Algoritmo Custom | ✅ | Lógica custom en TypeScript/Go |
| Skill-Based | ✅ | ELO, MMR, o custom |
| Region-Based | ✅ | Latencia óptima |
| Party Matching | ✅ | Grupos pre-formados |

**Flujo:**
```
Cliente → AddMatchmakerParty(criteria) → Match Found → Server IP:Port
```

#### Storage & Persistencia (N5, N8 ✅)

Nakama incluye un motor de almacenamiento de alto rendimiento:

| Feature | Soporte | Uso en ZombieBox |
|---------|---------|------------------|
| User Profiles | ✅ | Datos de jugador, stats |
| Storage Objects | ✅ | Progreso, desbloqueos |
| Leaderboards | ✅ | Rankings globales/por modo |
| Wallets | ✅ | Economía in-game (futuro) |
| Access Control | ✅ | Permisos read/write |

**Almacenamiento:**
```typescript
// Stats post-partida
{
  "user_id": "uuid",
  "match_stats": {
    "kills": 45,
    "deaths": 2,
    "waves_survived": 15,
    "damage_dealt": 5420
  },
  "timestamp": "2026-02-03T18:00:00Z"
}
```

#### Social Features (N10 ✅)

| Feature | Soporte | Notas |
|---------|---------|-------|
| Friends System | ✅ | Lista de amigos, invitaciones |
| Groups/Clans | ✅ | Comunidades |
| Chat | ✅ | Real-time, persistente |
| Presence | ✅ | Online/Offline/In-Match |
| Notifications | ✅ | Push, in-app |

#### Real-Time Features

| Feature | Soporte | Uso en ZombieBox |
|---------|---------|------------------|
| WebSocket API | ✅ | Chat, presence |
| Event Broadcasting | ✅ | Eventos de lobby |
| RPC Calls | ✅ | Cliente → Server custom logic |

### 3.2 Extensibilidad (Runtime Custom)

Nakama permite extender su funcionalidad mediante:

#### Lenguajes Soportados
- **Go** (máximo rendimiento)
- **TypeScript/JavaScript** (balance rendimiento/productividad)
- **Lua** (scripting ligero)

#### Hooks Disponibles

```typescript
// Ejemplo: Validar matchmaking antes de crear partida
function beforeMatchmakerAdd(ctx, payload) {
  // Validar que el usuario no esté baneado
  // Validar nivel mínimo
  // Validar región
  return payload; // o throw error
}
```

#### RPC Functions

```typescript
// RPC custom: Cliente solicita IP:Port de servidor asignado
function rpcGetServerEndpoint(ctx, matchId) {
  // 1. Consultar pool de servidores disponibles
  // 2. Asignar servidor o spin up nuevo (Docker API)
  // 3. Registrar asignación en Nakama storage
  // 4. Retornar { ip, port, token }
}
```

### 3.3 Lo que Nakama NO Provee

| Funcionalidad | Razón | Solución |
|---------------|-------|----------|
| **Orquestación Física Servidores** | No es un orchestrator de containers | Docker API + custom logic |
| **Auto-scaling Infraestructura** | No gestiona máquinas/VMs | Kubernetes / Docker Swarm |
| **Relay de Gameplay** | No está optimizado para eso | ENet directo (como ya tienes) |
| **Lógica de Juego** | No es un game engine | Godot Dedicado (sin cambios) |

---

## 4. Matriz de Cobertura

### 4.1 Tabla de Cobertura

| Necesidad | ID | Nakama Nativo | Runtime Custom | Infraestructura Externa | Notas |
|-----------|----|--------------:|---------------:|------------------------:|-------|
| Autenticación | N1 | ✅ 100% | - | - | Google OAuth nativo |
| Matchmaking | N2 | ✅ 90% | 🟡 10% | - | Algoritmo custom para skill-based |
| Orquestación Servers | N3 | - | 🟡 60% | 🔴 40% | Nakama coordina, Docker/K8s ejecuta |
| Asignación Servidor | N4 | - | ✅ 100% | - | RPC custom retorna IP:Port |
| Persistencia Stats | N5 | ✅ 100% | - | - | Storage API nativo |
| Validación Tokens | N6 | ✅ 100% | - | - | JWT validation |
| Leaderboards | N7 | ✅ 100% | - | - | Leaderboard API nativo |
| Progreso Usuario | N8 | ✅ 100% | - | - | Storage Objects |
| Party System | N9 | ✅ 95% | 🟡 5% | - | Groups API + custom UI |
| Chat/Social | N10 | ✅ 100% | - | - | Chat API nativo |
| Gestión Sesiones | N11 | ✅ 80% | 🟡 20% | - | Match tracking + custom logic |
| Modos PvP | N12 | ✅ 90% | 🟡 10% | - | Matchmaking adaptado |
| Torneos | N13 | ✅ 100% | - | - | Tournament API nativo |
| In-Game Economy | N14 | ✅ 100% | - | - | Wallet + IAP validation |

### 4.2 Desglose por Proveedor

```
┌─────────────────────────────────────────────────────────────────┐
│                    RESPONSABILIDADES                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  NAKAMA NATIVO (80%)                                            │
│  ├── Autenticación (Google OAuth, Device)                      │
│  ├── Matchmaking (Pool, criteria, algoritmo base)              │
│  ├── Storage (Stats, progreso, leaderboards)                   │
│  ├── Social (Friends, chat, presence)                          │
│  └── Validación de tokens JWT                                  │
│                                                                 │
│  NAKAMA RUNTIME CUSTOM (15%)                                    │
│  ├── RPC: Asignar servidor dedicado                            │
│  ├── RPC: Reportar fin de partida                              │
│  ├── Hook: Validar matchmaking criteria                        │
│  ├── Hook: Coordinar orquestación con Docker API               │
│  └── Lógica de auto-scaling (decisiones)                       │
│                                                                 │
│  INFRAESTRUCTURA EXTERNA (5%)                                   │
│  ├── Docker API / Kubernetes                                   │
│  ├── Health checks de servidores dedicados                     │
│  └── Networking (Load balancers, DNS)                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Arquitectura de Integración

### 5.1 Arquitectura Completa

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       ARQUITECTURA ZOMBIEBOX + NAKAMA                        │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌──────────────┐         ┌──────────────┐         ┌──────────────┐
    │   Cliente    │         │   Cliente    │         │   Cliente    │
    │   Godot      │         │   Godot      │         │   Godot      │
    └──────┬───────┘         └──────┬───────┘         └──────┬───────┘
           │                        │                        │
           │ ❶ Auth/Matchmaking     │                        │
           │   (HTTPS/WSS)          │                        │
           └────────────────────────┼────────────────────────┘
                                    │
                                    ▼
                   ┌────────────────────────────────┐
                   │          NAKAMA SERVER          │
                   │                                │
                   │  ┌──────────────────────────┐  │
                   │  │   Core Services          │  │
                   │  │  • Auth (Google OAuth)   │  │
                   │  │  • Matchmaker            │  │
                   │  │  • Storage/Leaderboards  │  │
                   │  │  • Social (Chat/Friends) │  │
                   │  └──────────────────────────┘  │
                   │                                │
                   │  ┌──────────────────────────┐  │
                   │  │   Custom Runtime (TS/Go) │  │
                   │  │  • RPC: Get Server       │  │
                   │  │  • RPC: Report Match     │  │
                   │  │  • Hook: Validate        │  │
                   │  └──────────────────────────┘  │
                   │                                │
                   └───────────┬────────────────────┘
                               │
                               │ ❷ Orquestar
                               ▼
                   ┌────────────────────────────────┐
                   │   CONTAINER ORCHESTRATOR        │
                   │   (Docker / Kubernetes)         │
                   │                                │
                   │  Gestiona pool de:             │
                   │  • Godot Headless Servers      │
                   │  • Health checks               │
                   │  • Auto-scaling                │
                   └───────────┬────────────────────┘
                               │
                               │ ❸ Spawn/Manage
                               ▼
         ┌─────────────────────────────────────────────────────┐
         │                                                      │
         │    ┌──────────────┐     ┌──────────────┐            │
         │    │   Godot      │     │   Godot      │    ...     │
         │    │  Dedicated   │     │  Dedicated   │            │
         │    │  Server #1   │     │  Server #2   │            │
         │    └──────┬───────┘     └──────┬───────┘            │
         │           │                    │                     │
         └───────────┼────────────────────┼─────────────────────┘
                     │                    │
                     │ ❹ ENet (UDP) - Gameplay
                     │ (directo, sin pasar por Nakama)
                     │
         ┌───────────┼────────────────────┼─────────────────┐
         │           │                    │                 │
         ▼           ▼                    ▼                 ▼
    ┌────────┐  ┌────────┐          ┌────────┐        ┌────────┐
    │Cliente │  │Cliente │          │Cliente │        │Cliente │
    │   1    │  │   2    │          │   3    │        │   N    │
    └────────┘  └────────┘          └────────┘        └────────┘
```

### 5.2 Componentes y Responsabilidades

#### Cliente Godot
```
┌─────────────────────────────────────────┐
│           CLIENTE GODOT                 │
├─────────────────────────────────────────┤
│                                         │
│  • Nakama SDK integrado                 │
│  • Auth con Google → JWT token          │
│  • Matchmaking request                  │
│  • Recibe IP:Port de servidor           │
│  • Conecta a Servidor Dedicado (ENet)   │
│  • Reporta stats a Nakama (post-match)  │
│                                         │
└─────────────────────────────────────────┘
```

#### Nakama Server
```
┌─────────────────────────────────────────┐
│          NAKAMA SERVER                  │
├─────────────────────────────────────────┤
│                                         │
│  SERVICIOS NATIVOS:                     │
│  • Autenticación (Google OAuth)         │
│  • Matchmaker (agrupa jugadores)        │
│  • Storage (stats, progreso)            │
│  • Leaderboards                         │
│  • Social (friends, chat)               │
│                                         │
│  RUNTIME CUSTOM (TypeScript/Go):        │
│  • RPC: getServerForMatch()             │
│    └─► Consulta pool servidores         │
│    └─► Asigna o spin up nuevo           │
│    └─► Retorna { ip, port, token }      │
│                                         │
│  • RPC: reportMatchEnd()                │
│    └─► Recibe stats de servidor         │
│    └─► Persiste en storage              │
│    └─► Actualiza leaderboards           │
│                                         │
│  • Hook: beforeMatchmakerAdd()          │
│    └─► Valida usuario (no baneado)      │
│    └─► Enriquece criterios              │
│                                         │
└─────────────────────────────────────────┘
```

#### Orchestrator (Docker/Kubernetes)
```
┌─────────────────────────────────────────┐
│      CONTAINER ORCHESTRATOR             │
├─────────────────────────────────────────┤
│                                         │
│  • Pool de servidores Godot headless    │
│  • API REST para Nakama                 │
│    GET  /servers/available              │
│    POST /servers/spawn                  │
│    DELETE /servers/{id}                 │
│                                         │
│  • Health checks periódicos             │
│  • Auto-scaling según demanda           │
│  • Logs centralizados                   │
│                                         │
└─────────────────────────────────────────┘
```

#### Servidor Dedicado Godot
```
┌─────────────────────────────────────────┐
│      GODOT DEDICATED SERVER             │
├─────────────────────────────────────────┤
│                                         │
│  SIN CAMBIOS desde la arquitectura      │
│  actual:                                │
│                                         │
│  • Arranca en modo --server --headless  │
│  • Acepta conexiones ENet en puerto X   │
│  • Valida tokens JWT (via Nakama)       │
│  • Ejecuta gameplay (autoritativo)      │
│                                         │
│  NUEVA INTEGRACIÓN:                     │
│  • Al finalizar partida:                │
│    └─► RPC a Nakama con stats           │
│  • Health check endpoint HTTP           │
│    └─► GET /health → 200 OK             │
│                                         │
└─────────────────────────────────────────┘
```

---

## 6. Flujos de Interacción

### 6.1 Flujo Completo: De Login a Gameplay

```
┌──────────┐      ┌──────────┐      ┌──────────────┐      ┌──────────┐
│ Cliente  │      │  Nakama  │      │ Orchestrator │      │  Server  │
│  Godot   │      │          │      │              │      │ Dedicado │
└────┬─────┘      └────┬─────┘      └──────┬───────┘      └────┬─────┘
     │                 │                   │                   │
     │ ❶ AuthGoogle   │                   │                   │
     ├────────────────>│                   │                   │
     │                 │                   │                   │
     │ ◄─ JWT Token    │                   │                   │
     │<────────────────┤                   │                   │
     │                 │                   │                   │
     │ ❷ AddMatchmaker │                   │                   │
     │ (2-4 players,   │                   │                   │
     │  mode=PvE)      │                   │                   │
     ├────────────────>│                   │                   │
     │                 │                   │                   │
     │                 │ [Espera otros jugadores...]          │
     │                 │                   │                   │
     │                 │ ❸ Match Found!    │                   │
     │                 │ Need server...    │                   │
     │                 │                   │                   │
     │                 │ ❹ RPC: Get Server │                   │
     │                 ├──────────────────>│                   │
     │                 │                   │                   │
     │                 │                   │ ❺ Spawn/Assign   │
     │                 │                   │   Godot Server    │
     │                 │                   ├──────────────────>│
     │                 │                   │                   │
     │                 │                   │ ◄─ IP:Port        │
     │                 │                   │<──────────────────┤
     │                 │                   │                   │
     │                 │ ◄─ {ip, port, token}                 │
     │                 │<──────────────────┤                   │
     │                 │                   │                   │
     │ ❻ Server Ready! │                   │                   │
     │   IP: 1.2.3.4   │                   │                   │
     │   Port: 7777    │                   │                   │
     │<────────────────┤                   │                   │
     │                 │                   │                   │
     │ ❼ Connect ENet (UDP)                │                   │
     │ + Validate JWT                      │                   │
     ├──────────────────────────────────────────────────────────>│
     │                 │                   │                   │
     │ ◄──── Connection OK ────────────────────────────────────┤
     │                 │                   │                   │
     │ ❽ GAMEPLAY      │                   │                   │
     │ (ENet directo,  │                   │                   │
     │  sin Nakama)    │                   │                   │
     │<────────────────────────────────────────────────────────>│
     │                 │                   │                   │
     │                 │                   │ ❾ Match End       │
     │                 │ ◄─ RPC: Report Stats ─────────────────┤
     │                 │<──────────────────┤                   │
     │                 │                   │                   │
     │                 │ Save to Storage   │                   │
     │                 │ Update Leaderboard│                   │
     │                 │                   │                   │
     │ ❿ Results UI    │                   │                   │
     │<────────────────┤                   │                   │
     │                 │                   │                   │
```

### 6.2 Flujo de Autenticación (Detallado)

```
Cliente                          Nakama                    Google OAuth
  │                                │                            │
  │ ❶ User clicks "Login Google"  │                            │
  │                                │                            │
  │ ❷ Request Google Auth          │                            │
  │───────────────────────────────────────────────────────────>│
  │                                │                            │
  │ ◄─────────── OAuth Token ───────────────────────────────────┤
  │                                │                            │
  │ ❸ AuthenticateGoogle(token)   │                            │
  ├───────────────────────────────>│                            │
  │                                │                            │
  │                                │ ❹ Verify with Google       │
  │                                ├───────────────────────────>│
  │                                │                            │
  │                                │ ◄── User Info ─────────────┤
  │                                │                            │
  │                                │ ❺ Create/Update User       │
  │                                │   in Nakama DB             │
  │                                │                            │
  │ ◄────── JWT Token ─────────────┤                            │
  │       (session_token)          │                            │
  │                                │                            │
  │ ❻ Store token locally          │                            │
  │   (usado en todas las requests)│                            │
  │                                │                            │
```

### 6.3 Flujo de Matchmaking (Detallado)

```
Cliente 1    Cliente 2    Nakama Matchmaker    Orchestrator
    │            │                │                   │
    │ Join Queue │                │                   │
    ├───────────────────────────>│                   │
    │            │                │                   │
    │            │ Join Queue     │                   │
    │            ├───────────────>│                   │
    │            │                │                   │
    │            │                │ ❶ Match Found!    │
    │            │                │   (2 players)     │
    │            │                │                   │
    │            │                │ ❷ Check Available │
    │            │                │   Servers         │
    │            │                ├──────────────────>│
    │            │                │                   │
    │            │                │ ❸ Options:        │
    │            │                │ a) Server idle    │
    │            │                │ b) Spawn new      │
    │            │                │                   │
    │            │                │ [Decision: Spawn] │
    │            │                │                   │
    │            │                │ ❹ POST /spawn     │
    │            │                ├──────────────────>│
    │            │                │                   │
    │            │                │ Docker run godot  │
    │            │                │   --server        │
    │            │                │   --port 7777     │
    │            │                │                   │
    │            │                │ ◄─ IP:Port ───────┤
    │            │                │                   │
    │            │                │ ❺ Store assignment│
    │            │                │   matchId -> server│
    │            │                │                   │
    │ ◄─ Match Ready ─────────────┤                   │
    │   Server: 1.2.3.4:7777      │                   │
    │   Token: eyJhb...           │                   │
    │            │                │                   │
    │            │ ◄─ Match Ready ┤                   │
    │            │                │                   │
```

### 6.4 Flujo de Fin de Partida

```
Servidor Dedicado         Nakama              Storage/Leaderboards
       │                    │                          │
       │ ❶ Match Ends       │                          │
       │   (all players     │                          │
       │    dead/DC)        │                          │
       │                    │                          │
       │ ❷ Collect Stats    │                          │
       │   {                │                          │
       │     players: [     │                          │
       │       {id, kills,  │                          │
       │        deaths}     │                          │
       │     ],             │                          │
       │     duration,      │                          │
       │     waves_survived │                          │
       │   }                │                          │
       │                    │                          │
       │ ❸ RPC: ReportEnd  │                          │
       ├───────────────────>│                          │
       │                    │                          │
       │                    │ ❹ For each player:      │
       │                    │   Update stats           │
       │                    ├─────────────────────────>│
       │                    │                          │
       │                    │ ❺ Update Leaderboards   │
       │                    ├─────────────────────────>│
       │                    │                          │
       │                    │ ❻ Trigger achievements? │
       │                    │   (if any)               │
       │                    │                          │
       │ ◄─── ACK ──────────┤                          │
       │                    │                          │
       │ ❼ Shutdown/Idle    │                          │
       │   (orchestrator    │                          │
       │    decides)        │                          │
       │                    │                          │
```

---

## 7. Implementación Custom Requerida

### 7.1 Nakama Runtime Functions (TypeScript/Go)

#### RPC: Get Server For Match

```typescript
// File: nakama/runtime/server_orchestration.ts

interface GetServerRequest {
  matchId: string;
  playerCount: number;
  region?: string;
}

interface GetServerResponse {
  ip: string;
  port: number;
  token: string;
  serverId: string;
}

function rpcGetServerForMatch(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  const request: GetServerRequest = JSON.parse(payload);

  // 1. Check for idle servers in pool
  const idleServers = checkIdleServers(nk, request.region);

  if (idleServers.length > 0) {
    // Reuse existing server
    const server = idleServers[0];
    assignServerToMatch(nk, server.id, request.matchId);

    return JSON.stringify({
      ip: server.ip,
      port: server.port,
      token: generateMatchToken(request.matchId),
      serverId: server.id
    });
  }

  // 2. No idle servers, spawn new one
  const newServer = spawnNewServer(
    logger,
    request.playerCount,
    request.region
  );

  // 3. Register in Nakama storage
  registerServer(nk, newServer, request.matchId);

  return JSON.stringify({
    ip: newServer.ip,
    port: newServer.port,
    token: generateMatchToken(request.matchId),
    serverId: newServer.id
  });
}

function spawnNewServer(
  logger: nkruntime.Logger,
  playerCount: number,
  region: string
): Server {
  // Call Docker/K8s API
  const dockerEndpoint = "http://orchestrator:2375";
  const response = httpRequest(
    `${dockerEndpoint}/containers/create`,
    "POST",
    {
      Image: "zombiebox-server:latest",
      Cmd: ["--server", "--port", "7777"],
      PortBindings: {
        "7777/udp": [{ HostPort: "0" }] // Random port
      },
      Env: [
        `MAX_PLAYERS=${playerCount}`,
        `REGION=${region}`
      ]
    }
  );

  const containerId = response.Id;

  // Start container
  httpRequest(
    `${dockerEndpoint}/containers/${containerId}/start`,
    "POST"
  );

  // Get assigned port
  const inspectResponse = httpRequest(
    `${dockerEndpoint}/containers/${containerId}/json`,
    "GET"
  );

  const hostPort = inspectResponse.NetworkSettings
    .Ports["7777/udp"][0].HostPort;

  logger.info(`Spawned server ${containerId} on port ${hostPort}`);

  return {
    id: containerId,
    ip: getPublicIP(),
    port: parseInt(hostPort)
  };
}
```

#### RPC: Report Match End

```typescript
// File: nakama/runtime/match_reporting.ts

interface MatchStats {
  matchId: string;
  duration: number; // seconds
  wavesSurvived: number;
  players: PlayerStats[];
}

interface PlayerStats {
  userId: string;
  kills: number;
  deaths: number;
  damageDealt: number;
  damageTaken: number;
}

function rpcReportMatchEnd(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  const stats: MatchStats = JSON.parse(payload);

  // 1. Validate: Only server can call this (check auth)
  if (!isServerAuth(ctx)) {
    throw Error("Unauthorized: Only servers can report matches");
  }

  // 2. Update player stats
  for (const playerStat of stats.players) {
    updatePlayerStats(nk, playerStat);
  }

  // 3. Update leaderboards
  updateLeaderboard(nk, "waves_survived", stats.players, stats.wavesSurvived);
  updateLeaderboard(nk, "total_kills", stats.players);

  // 4. Store match history
  storeMatchHistory(nk, stats);

  // 5. Free server (mark as idle)
  freeServer(nk, stats.matchId);

  logger.info(`Match ${stats.matchId} ended. ${stats.wavesSurvived} waves.`);

  return JSON.stringify({ success: true });
}

function updatePlayerStats(
  nk: nkruntime.Nakama,
  playerStat: PlayerStats
) {
  // Read current stats
  const objects = nk.storageRead([{
    collection: "player_stats",
    key: "career",
    userId: playerStat.userId
  }]);

  let currentStats = objects.length > 0
    ? JSON.parse(objects[0].value)
    : { kills: 0, deaths: 0, matches: 0 };

  // Update
  currentStats.kills += playerStat.kills;
  currentStats.deaths += playerStat.deaths;
  currentStats.matches += 1;

  // Write back
  nk.storageWrite([{
    collection: "player_stats",
    key: "career",
    userId: playerStat.userId,
    value: JSON.stringify(currentStats)
  }]);
}
```

#### Hook: Before Matchmaker Add

```typescript
// File: nakama/runtime/matchmaker_hooks.ts

function beforeMatchmakerAdd(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  data: nkruntime.MatchmakerAdd
): nkruntime.MatchmakerAdd {
  const userId = ctx.userId;

  // 1. Check if user is banned
  const banCheck = nk.storageRead([{
    collection: "user_moderation",
    key: "ban_status",
    userId: userId
  }]);

  if (banCheck.length > 0) {
    const banData = JSON.parse(banCheck[0].value);
    if (banData.banned) {
      throw Error("User is banned from matchmaking");
    }
  }

  // 2. Check minimum level (if required)
  const userProfile = nk.storageRead([{
    collection: "player_stats",
    key: "career",
    userId: userId
  }]);

  if (userProfile.length > 0) {
    const stats = JSON.parse(userProfile[0].value);
    // Enrich matchmaker properties with skill
    data.stringProperties = {
      ...data.stringProperties,
      skill_tier: calculateSkillTier(stats)
    };
  }

  logger.info(`User ${userId} added to matchmaker with properties`, data);

  return data;
}

function calculateSkillTier(stats: any): string {
  // Simple skill calculation
  const kd = stats.kills / Math.max(stats.deaths, 1);
  if (kd > 2.0) return "high";
  if (kd > 1.0) return "medium";
  return "low";
}
```

### 7.2 Cliente Godot - Integración Nakama SDK

```gdscript
# File: scripts/Core/NakamaClient.gd
extends Node

var client: NakamaClient
var session: NakamaSession
var socket: NakamaSocket

func _ready():
    # Initialize Nakama client
    client = Nakama.create_client(
        "defaultkey",  # Server key
        "127.0.0.1",   # Nakama host
        7350,          # HTTP port
        "http"
    )

func authenticate_google(oauth_token: String):
    var result = await client.authenticate_google_async(oauth_token)

    if result.is_exception():
        push_error("Auth failed: " + result.exception.message)
        return

    session = result
    print("Authenticated as: " + session.username)

    # Connect socket for real-time features
    socket = Nakama.create_socket_from(client)
    await socket.connect_async(session)

func start_matchmaking():
    var query = "+mode:pve +players:>=2"
    var min_count = 2
    var max_count = 4

    var ticket = await socket.add_matchmaker_async(
        query, min_count, max_count
    )

    print("Matchmaking ticket: " + ticket.ticket)

    # Listen for match found
    socket.received_matchmaker_matched.connect(_on_match_found)

func _on_match_found(matched: NakamaRTAPI.MatchmakerMatched):
    print("Match found! Match ID: " + matched.match_id)

    # RPC to get server endpoint
    var payload = JSON.stringify({
        "matchId": matched.match_id,
        "playerCount": matched.users.size()
    })

    var result = await client.rpc_async(
        session, "getServerForMatch", payload
    )

    var server_info = JSON.parse(result.payload)
    print("Server: " + server_info.ip + ":" + str(server_info.port))

    # Now connect to dedicated server via ENet
    connect_to_dedicated_server(
        server_info.ip,
        server_info.port,
        server_info.token
    )

func connect_to_dedicated_server(ip: String, port: int, token: String):
    var network_system = get_node("/root/NetworkSystem")
    network_system.StartClient(ip, port)
    # TODO: Send token for validation
```

### 7.3 Servidor Dedicado - Validación de Tokens

```csharp
// File: scripts/Core/ServerAuth.cs
using Godot;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public partial class ServerAuth : Node
{
    private const string NakamaHost = "http://nakama:7350";
    private const string ServerKey = "defaultkey";
    private static HttpClient _httpClient = new HttpClient();

    public async Task<bool> ValidatePlayerToken(long peerId, string jwtToken)
    {
        try
        {
            // Call Nakama to verify JWT
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{NakamaHost}/v2/account");
            request.Headers.Add("Authorization", $"Bearer {jwtToken}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                GD.PrintErr($"Token validation failed for peer {peerId}");
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            var accountData = Json.ParseString(content).AsGodotDictionary();

            var userId = accountData["user"]["id"].AsString();
            GD.Print($"Peer {peerId} authenticated as user {userId}");

            // Store userId -> peerId mapping
            RegisterAuthenticatedPlayer(peerId, userId);

            return true;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error validating token: {e.Message}");
            return false;
        }
    }

    private void RegisterAuthenticatedPlayer(long peerId, string userId)
    {
        // TODO: Store in SessionSystem or similar
    }
}
```

### 7.4 Orquestador de Contenedores (Ejemplo Simplificado)

```javascript
// File: orchestrator/server.js
const express = require('express');
const Docker = require('dockerode');
const docker = new Docker();

const app = express();
app.use(express.json());

// Pool de servidores
let serverPool = [];

// Endpoint: Obtener servidores disponibles
app.get('/servers/available', (req, res) => {
  const idle = serverPool.filter(s => s.status === 'idle');
  res.json({ servers: idle });
});

// Endpoint: Spawn nuevo servidor
app.post('/servers/spawn', async (req, res) => {
  const { playerCount, region } = req.body;

  try {
    const container = await docker.createContainer({
      Image: 'zombiebox-server:latest',
      Cmd: ['--server', '--port', '7777'],
      ExposedPorts: { '7777/udp': {} },
      HostConfig: {
        PortBindings: {
          '7777/udp': [{ HostPort: '0' }] // Random port
        }
      },
      Env: [
        `MAX_PLAYERS=${playerCount}`,
        `REGION=${region || 'us-east'}`
      ]
    });

    await container.start();

    const inspect = await container.inspect();
    const hostPort = inspect.NetworkSettings.Ports['7777/udp'][0].HostPort;

    const server = {
      id: container.id,
      ip: getPublicIP(),
      port: parseInt(hostPort),
      status: 'starting',
      playerCount: 0,
      maxPlayers: playerCount
    };

    serverPool.push(server);

    res.json({ server });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Endpoint: Health check
app.get('/servers/:id/health', async (req, res) => {
  const { id } = req.params;
  const server = serverPool.find(s => s.id === id);

  if (!server) {
    return res.status(404).json({ error: 'Server not found' });
  }

  // TODO: Real health check (HTTP to server endpoint)
  res.json({ status: server.status });
});

// Endpoint: Shutdown servidor
app.delete('/servers/:id', async (req, res) => {
  const { id } = req.params;
  const container = docker.getContainer(id);

  try {
    await container.stop();
    await container.remove();
    serverPool = serverPool.filter(s => s.id !== id);
    res.json({ success: true });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

function getPublicIP() {
  // TODO: Get actual public IP
  return process.env.PUBLIC_IP || '127.0.0.1';
}

app.listen(3000, () => {
  console.log('Orchestrator listening on port 3000');
});
```

---

## 8. Roadmap de Integración

### 8.1 Fase 4.1: Autenticación (Semanas 1-2)

**Objetivo**: Los jugadores pueden autenticarse con Google OAuth.

| Tarea | Responsable | Estimación |
|-------|-------------|------------|
| Setup Nakama server (Docker) | DevOps | 2 días |
| Configurar Google OAuth en Nakama | Backend | 1 día |
| Integrar Nakama SDK en cliente Godot | Client | 3 días |
| Implementar flujo de login UI | Client | 2 días |
| Testing: Auth exitosa → JWT almacenado | QA | 1 día |

**Entregable**: Cliente puede hacer login y recibir JWT token.

---

### 8.2 Fase 4.2: Matchmaking Básico (Semanas 3-4)

**Objetivo**: Jugadores pueden buscar partida y ser agrupados.

| Tarea | Responsable | Estimación |
|-------|-------------|------------|
| Configurar matchmaker en Nakama | Backend | 2 días |
| Implementar UI de búsqueda de partida | Client | 2 días |
| Hook: beforeMatchmakerAdd (validaciones) | Backend | 1 día |
| Testing: 2 jugadores encuentran match | QA | 2 días |

**Entregable**: Matchmaking funcional (sin servidores aún, solo agrupación).

---

### 8.3 Fase 4.3: Orquestación Servidores (Semanas 5-7)

**Objetivo**: Nakama puede spin up servidores Godot headless.

| Tarea | Responsable | Estimación |
|-------|-------------|------------|
| Dockerizar servidor Godot headless | DevOps | 2 días |
| Implementar orchestrator API (Node.js) | Backend | 3 días |
| RPC: getServerForMatch (Nakama → Orchestrator) | Backend | 3 días |
| Testing: Nakama spawnea servidor correctamente | QA | 2 días |

**Entregable**: Al encontrar match, se crea servidor dedicado automáticamente.

---

### 8.4 Fase 4.4: Conexión Cliente-Servidor (Semanas 8-9)

**Objetivo**: Clientes reciben IP:Port y conectan al servidor.

| Tarea | Responsable | Estimación |
|-------|-------------|------------|
| Cliente recibe server endpoint del RPC | Client | 2 días |
| Validación de JWT en servidor dedicado | Server | 3 días |
| Testing E2E: Login → Match → Connect → Gameplay | QA | 3 días |

**Entregable**: Flujo completo funcional (sin persistencia aún).

---

### 8.5 Fase 4.5: Persistencia y Stats (Semanas 10-11)

**Objetivo**: Stats se guardan al finalizar partidas.

| Tarea | Responsable | Estimación |
|-------|-------------|------------|
| RPC: reportMatchEnd (servidor → Nakama) | Backend | 2 días |
| Storage: Player stats, match history | Backend | 2 días |
| Leaderboards: Waves survived, kills | Backend | 2 días |
| UI: Ver stats y leaderboards | Client | 3 días |

**Entregable**: Sistema completo con persistencia y rankings.

---

### 8.6 Fase 4.6: Features Sociales (Semanas 12+)

**Objetivo**: Friends, chat, parties.

| Tarea | Responsable | Estimación |
|-------|-------------|------------|
| Implementar friends system (Nakama nativo) | Client | 3 días |
| Chat en lobby (Nakama nativo) | Client | 2 días |
| Party system (grupos pre-matchmaking) | Backend + Client | 4 días |

**Entregable**: Features sociales completas.

---

## 9. Decisiones Técnicas

### 9.1 Por qué Nakama Runtime en TypeScript (no Go/Lua)

| Criterio | TypeScript | Go | Lua |
|----------|-----------|-----|-----|
| Rendimiento | ⚡⚡ Bueno | ⚡⚡⚡ Excelente | ⚡ Medio |
| Productividad | ✅ Alta (familiaridad) | 🟡 Media (si no hay exp) | 🟡 Media |
| Ecosistema | ✅ npm, types | 🟡 Limitado para game backend | ❌ Limitado |
| Debugging | ✅ Excelente | ✅ Bueno | 🟡 Limitado |
| Mantenimiento | ✅ Fácil | ✅ Fácil | 🟡 Puede ser críptico |

**Decisión**: **TypeScript** para Fase 4, migrar a Go solo si el rendimiento es crítico.

### 9.2 Docker vs Kubernetes para Orchestration

| Aspecto | Docker Swarm/Compose | Kubernetes |
|---------|---------------------|------------|
| Complejidad | ⚡ Baja | 🔴 Alta |
| Escalabilidad | 🟡 Hasta ~50 servers | ✅ Ilimitada |
| Features | 🟡 Básicas | ✅ Avanzadas (auto-scaling, health checks) |
| Curva de aprendizaje | ✅ Fácil | 🔴 Empinada |

**Decisión**:
- **Fase 4 inicial**: Docker Compose (simple, rápido)
- **Producción**: Migrar a Kubernetes cuando escale

### 9.3 Self-Hosted Nakama vs Nakama Cloud

| Aspecto | Self-Hosted | Nakama Cloud |
|---------|-------------|--------------|
| Costo | ✅ Solo infraestructura | 🔴 $$ por MAU |
| Control | ✅ Total | 🟡 Limitado |
| Mantenimiento | 🔴 Manual (updates, backups) | ✅ Automático |
| Escalabilidad | 🟡 Manual | ✅ Automática |

**Decisión**: **Self-Hosted** (control total, menor costo inicial).

---

## 10. Alternativas Consideradas

### 10.1 Comparación con Otras Soluciones

| Solución | Pros | Contras | Veredicto |
|----------|------|---------|-----------|
| **Nakama** | Gaming-first, open-source, Godot SDK | Curva aprendizaje inicial | ✅ **ELEGIDO** |
| **PlayFab** | Muy maduro, muchas features | Vendor lock-in, costos altos | 🟡 Backup |
| **Custom (Go/Node.js)** | Control total | Mucho desarrollo, reinventar rueda | ❌ Demasiado trabajo |
| **Supabase** | Auth/DB gratis, buena DX | No gaming-specific | 🟡 Posible híbrido |
| **Node-RED** | Rápido para prototipos | NO para gaming | ❌ Descartado |

### 10.2 Por qué NO las Alternativas

#### PlayFab
- ❌ **Vendor lock-in**: Migrar fuera es muy difícil
- ❌ **Costos**: Escalan muy rápido con MAU
- ❌ **Menos control**: Limitaciones en lógica custom

#### Custom Backend
- ❌ **Tiempo de desarrollo**: 3-6 meses para alcanzar paridad con Nakama
- ❌ **Mantenimiento**: Equipo necesita mantener todo
- ❌ **Reinventar**: Matchmaking, leaderboards, auth ya existen

#### Supabase
- ❌ **No gaming-specific**: Falta matchmaking, sessions, etc.
- 🟡 **Posible híbrido**: Nakama + Supabase (auth only) pero añade complejidad

---

## Conclusión

**Nakama es la solución óptima para ZombieBox** porque:

1. ✅ Cubre el 80% de necesidades out-of-the-box
2. ✅ Compatible con arquitectura server-authoritative existente
3. ✅ Open-source y self-hostable (control total)
4. ✅ SDK oficial para Godot con buena documentación
5. ✅ Extensible para los casos custom (orquestación, validaciones)
6. ✅ Escalable para crecimiento futuro

**Próximos pasos**:
1. Setup Nakama en Docker (Fase 4.1)
2. Implementar autenticación Google OAuth
3. Integrar SDK en cliente Godot
4. Proceder según roadmap definido

---

**Referencias y Fuentes**:
- [Nakama: Real-time server-authoritative multiplayer networking - Heroic Labs](https://heroiclabs.com/multiplayer/)
- [Authoritative Multiplayer - Nakama](https://heroiclabs.com/docs/nakama/concepts/multiplayer/authoritative/)
- [GitHub - heroiclabs/nakama](https://github.com/heroiclabs/nakama)
- [Nakama: The leading open source game server](https://heroiclabs.com/nakama/)
- [GitHub - heroiclabs/nakama-godot](https://github.com/heroiclabs/nakama-godot)
- [Nakama Godot 4 Client Guide](https://heroiclabs.com/docs/nakama/client-libraries/godot/)
- [Making an online multiplayer game with Godot and Nakama](https://heroiclabs.com/blog/godot-fishgame/)
- [Integrate Godot Headless Server with Nakama](https://juryquinn.com/post/technology/2023-01-04)
