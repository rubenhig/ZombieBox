# Fase 1 - Requisitos Funcionales

> **Objetivo**: Dos jugadores conectados a un Dedicated Server con gameplay básico funcional.

---

## 1. Servidor Dedicado

| ID | Requisito | Criterio de Aceptación |
|----|-----------|------------------------|
| S1 | Arranque por CLI | El servidor arranca sin interfaz gráfica usando `--headless --server` |
| S2 | Escucha en puerto configurable | Acepta conexiones ENet en el puerto especificado (default: 7777) |
| S3 | Soporte de 2-4 jugadores | Acepta conexiones de múltiples clientes simultáneos |
| S4 | Tick System | Ejecuta simulación a 60 ticks/segundo de forma determinista |

---

## 2. Cliente

| ID | Requisito | Criterio de Aceptación |
|----|-----------|------------------------|
| C1 | Menú principal | UI para introducir IP:Puerto y conectar |
| C2 | Conexión al servidor | Conecta vía ENet al Dedicated Server |
| C3 | Renderizado de estado | Muestra el estado del juego recibido del servidor |
| C4 | Captura de input | Lee teclado/mouse y envía al servidor |

---

## 3. Player

| ID | Requisito | Criterio de Aceptación |
|----|-----------|------------------------|
| P1 | Movimiento WASD | El jugador se mueve en 8 direcciones con WASD |
| P2 | Apuntado con mouse | El jugador rota hacia la posición del mouse |
| P3 | Disparo básico | Click izquierdo dispara un proyectil en dirección del mouse |
| P4 | Sistema de vida | El jugador tiene health, puede recibir daño y morir |
| P5 | Spawn en posición designada | Al conectar, el jugador aparece en un spawn point |

---

## 4. Enemy

| ID | Requisito | Criterio de Aceptación |
|----|-----------|------------------------|
| E1 | Spawn controlado | Los enemigos se crean en posiciones designadas del mapa |
| E2 | Persecución básica | Los enemigos se mueven hacia el jugador más cercano |
| E3 | Daño por contacto | Colisionar con un jugador le quita vida |
| E4 | Sistema de vida | Los enemigos pueden recibir daño y morir |

---

## 5. Bullet

| ID | Requisito | Criterio de Aceptación |
|----|-----------|------------------------|
| B1 | Movimiento lineal | El proyectil viaja en línea recta desde el origen |
| B2 | Detección de colisión | Al colisionar con Enemy, aplica daño |
| B3 | Tiempo de vida | El proyectil se destruye tras X segundos o al colisionar |

---

## 6. Sesión de Juego

| ID | Requisito | Criterio de Aceptación |
|----|-----------|------------------------|
| G1 | Estado Lobby | Al arrancar, el servidor espera jugadores |
| G2 | Transición a Playing | Con ≥2 jugadores, inicia la partida automáticamente |
| G3 | Oleada básica | Se spawean N enemigos al inicio |
| G4 | Estado GameOver | Si todos los jugadores mueren, termina la partida |
| G5 | Reinicio | Posibilidad de reiniciar la partida tras GameOver |

---

## 7. Sincronización de Red

| ID | Requisito | Criterio de Aceptación |
|----|-----------|------------------------|
| N1 | Sincronización de posición | Las posiciones de Player y Enemy se replican a todos los clientes |
| N2 | Sincronización de estado | Health, GameState se replican correctamente |
| N3 | Spawn replicado | Cuando el servidor crea una entidad, aparece en todos los clientes |
| N4 | Despawn replicado | Cuando una entidad muere, desaparece en todos los clientes |
| N5 | Input del cliente | El input se envía al servidor y se procesa correctamente |

---

## Fuera de Alcance (Fase 1)

Los siguientes features NO están incluidos en Fase 1:

- ❌ Client-side prediction (movimiento predictivo local)
- ❌ Entity interpolation (suavizado de entidades remotas)
- ❌ Lag compensation (rebobinado para disparos)
- ❌ Múltiples armas
- ❌ Variedad de enemigos
- ❌ Progresión de oleadas
- ❌ Audio/VFX
- ❌ Autenticación con backend
- ❌ Matchmaking

---

## Métricas de Éxito

| Métrica | Objetivo |
|---------|----------|
| Conexión exitosa | 2 clientes pueden conectarse al servidor |
| Gameplay funcional | Los jugadores pueden moverse, disparar y eliminar enemigos |
| Sincronización | El estado es consistente entre servidor y clientes |
| Estabilidad | No hay crashes durante una sesión de 5 minutos |
