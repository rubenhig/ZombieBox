# ZombieBox

[![Godot Engine](https://img.shields.io/badge/Godot-4.x-blue.svg)](https://godotengine.org)
[![C#](https://img.shields.io/badge/C%23-Mono-green.svg)](https://www.mono-project.com/)

ZombieBox is a cooperative top-down shooter with online multiplayer support, built with Godot Engine and C#.

Survive endless waves of enemies with up to 4 players on a dedicated server architecture.

## Getting Started

### Requirements
- Godot Engine 4.x (Mono/.NET version)
- .NET SDK 8.0+

### Running the Game

1. Clone the repository
2. Open the project in Godot Engine
3. Build the C# solution (click Build in the editor or run `dotnet build`)
4. Run the project (F5 or click Play)

### Entry Point
The project launches `scenes/Master.tscn`, which manages the menu and gameplay session.

## Documentation

See **[docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)** for the complete technical reference: networking model, systems, project structure, and roadmap.
