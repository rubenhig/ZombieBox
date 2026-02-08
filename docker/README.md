# Docker Setup for ZombieBox Server

Este directorio contiene la configuración Docker para ejecutar el servidor dedicado de ZombieBox.

## Archivos

- `Dockerfile` - Imagen con Godot 4.3 Mono + .NET 8.0
- `docker-compose.yml` - Configuración del servicio servidor
- `.dockerignore` - Archivos excluidos de la imagen

## Uso

### Construcción de la imagen

```bash
cd docker
docker build --platform linux/amd64 -t zombiebox-server .
```

### Comandos docker-compose

Desde la raíz del proyecto:

```bash
# Iniciar servidor
docker-compose -f docker/docker-compose.yml up -d

# Ver logs
docker-compose -f docker/docker-compose.yml logs -f server

# Detener servidor
docker-compose -f docker/docker-compose.yml down
```

O desde el directorio docker:

```bash
cd docker

# Iniciar
docker-compose up -d

# Logs
docker-compose logs -f server

# Detener
docker-compose down
```

### Desde VSCode

Usa las tareas configuradas en `.vscode/tasks.json`:

- **Terminal > Run Task > `docker-server-up`** - Inicia el servidor
- **Terminal > Run Task > `docker-server-logs`** - Ver logs en tiempo real
- **Terminal > Run Task > `docker-server-down`** - Detener servidor

O ejecuta el compound **"Multiplayer Simulation (Docker Server + 2 Clients)"** desde Run and Debug.

## Notas

- El servidor escucha en el puerto **7777/UDP**
- Los clientes deben conectarse a `127.0.0.1:7777`
- El directorio del proyecto se monta en `/app` dentro del contenedor
- La imagen usa plataforma `linux/amd64` para compatibilidad con Apple Silicon
