# GameSystems

`GameSystems` is a static utility class that provides convenient, centralized access to commonly used managed systems in the server world. It caches references to these systems internally, allowing mods and internal code to retrieve them easily without repeated lookups.

## Overview

- All properties are static and provide direct access to managed systems.
- Throws an exception if accessed before ScarletCore is fully initialized.
- Designed for use in mods and ScarletCore internals that need direct access to Unity ECS managed systems.

## Example Usage

```csharp
using ScarletCore.Systems;

// Access the EntityManager
var entityManager = GameSystems.EntityManager;

// Access the ServerBootstrapSystem
var bootstrap = GameSystems.ServerBootstrapSystem;
```

## Available Systems

- `Server` — The server world
- `EntityManager` — The entity manager for the server world
- `ServerGameManager`
- `ServerBootstrapSystem`
- `AdminAuthSystem`
- `PrefabCollectionSystem`
- `KickBanSystem`
- `UnitSpawnerUpdateSystem`
- `EntityCommandBufferSystem`
- `DebugEventsSystem`
- `TriggerPersistenceSaveSystem`
- `EndSimulationEntityCommandBufferSystem`
- `InstantiateMapIconsSystem_Spawn`

> **Note:** This class does not create or manage the lifecycle of these systems. It only provides cached references for easy access.
