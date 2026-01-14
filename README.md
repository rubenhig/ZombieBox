# ZombieBox

[![Godot Engine](httpshttps://img.shields.io/badge/Godot-4.x-blue.svg)](https://godotengine.org)
[![C#](https://img.shields.io/badge/C%23-Mono-green.svg)](https://www.mono-project.com/)

ZombieBox is an endless 2D top-down shooter with online multiplayer support, built from scratch with Godot Engine and C#.

The goal is to survive endless waves of enemies in a cooperative (or competitive) environment. This project emphasizes a clean, scalable architecture to facilitate future development and expansions.

## Project Organization

This repository is organized to help development (human or AI-assisted). The key documents are:

- **[ROADMAP.md](./ROADMAP.md):** The step-by-step implementation plan and development tasks.
- **[docs/](./docs):** Contains detailed documentation.
  - **[GAME_DESIGN.md](./docs/GAME_DESIGN.md):** Core mechanics, entities, and rules.
  - **[TECHNICAL_GUIDE.md](./docs/TECHNICAL_GUIDE.md):** Architecture, networking model, and code patterns.

## Getting Started

1.  Ensure you have the .NET SDK and Godot Engine 4.x (Mono version) installed.
2.  Clone this repository.
3.  Open the project in Godot Engine.
4.  **Entry Point:** The project launches `scenes/Master.tscn`, which orchestrates the menu and the gameplay scene (`scenes/GameSession.tscn`).
5.  Build the C# solution by clicking the build button in the Godot editor's C# panel or running `dotnet build`.
6.  Follow the [ROADMAP.md](./ROADMAP.md) to contribute to the development.
