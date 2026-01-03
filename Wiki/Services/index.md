# ScarletCore Services

## Overview

ScarletCore provides a comprehensive collection of static service classes that simplify common V Rising modding tasks. These services abstract complex game systems into clean, easy-to-use APIs, handling the underlying ECS complexity and providing consistent error handling.

All services are static classes in the `ScarletCore.Services` namespace and require no initialization — they're ready to use immediately.

---

## Service Categories

### Player Management

#### [PlayerService](PlayerService.md)
Comprehensive player management including retrieval by name/ID/entity, online player queries, gear score calculation, admin checks, and cached player data access. The foundation for most player-related operations.

#### [PlayerData](PlayerService.md#playerdata)
Data class representing a player with cached information including name, platform ID, entities, admin status, role, clan membership, and connection state. Automatically maintained by PlayerService.

---

### Communication

#### [MessageService](MessageService.md)
Send messages to players, groups, or broadcast to all. Supports system messages, error/success notifications, and custom formatted text. Essential for player communication and feedback.

---

### Authorization & Security

#### [AdminService](AdminService.md)
Manage admin privileges, elevated access, and god mode. Controls who can use admin commands and provides temporary elevation for specific operations.

#### [RoleService](RoleService.md)
Role-based permission system with customizable roles, hierarchical permissions, and player role assignment. Supports Owner, Admin, Moderator, VIP, and Default roles with extensibility for custom roles.

#### [KickBanService](KickBanService.md)
Player moderation including kick and ban operations. Handles both temporary and permanent bans with automatic enforcement.

---

### Entity & World

#### [EntityLookupService](EntityLookupService.md)
Query entities using fluent API with complex filters, spatial lookups, radius-based operations, and optimized query caching. The primary tool for finding and filtering entities in the world.

#### [SpawnerService](SpawnerService.md)
Spawn entities with various configurations including position, lifetime, post-spawn actions, and immediate spawning. Supports copying existing entities and advanced spawn patterns.

#### [TeleportService](TeleportService.md)
Teleport entities and players with safety checks, distance calculations, and helper methods. Handles position updates across all necessary components.

---

### Combat & Stats

#### [BuffService](BuffService.md)
Apply, remove, and manage buffs/debuffs on entities. Check buff presence, modify durations, and create custom buff effects.

#### [StatModifierService](StatModifierService.md)
Apply stat modifiers to entities using buff-based systems. Modify health, damage, speed, resistances, and other stats with permanent or temporary effects.

#### [AbilityService](AbilityService.md)
Manage entity abilities including adding, removing, replacing, and querying ability slots. Full control over player and NPC ability loadouts.

---

### Inventory & Items

#### [InventoryService](InventoryService.md)
Inventory management including item addition/removal, slot queries, stack operations, and equipment handling. Comprehensive inventory manipulation for players and containers.

---

### Social & Clans

#### [ClanService](ClanService.md)
Clan operations including member queries, invitations, role management, and clan information retrieval. Interact with V Rising's built-in clan system.

---

### Exploration

#### [RevealMapService](RevealMapService.md)
Control map visibility for players. Reveal entire map, specific chunks, or hide revealed areas. Manage exploration state and fog of war.

---

## Common Patterns

### Error Handling
Services return boolean success values or nullable types. Always check return values before assuming operations succeeded.

### Entity Safety
Services automatically validate entity existence before operations. Destroyed or invalid entities are handled gracefully without exceptions.

### Performance
Services use caching where appropriate (EntityLookupService query cache, PlayerService player cache). Repeated operations are optimized internally.

### Static Design
All services are static — no instantiation required. Access methods directly from the class name.

---

## Getting Started

Import the services namespace:

```csharp
using ScarletCore.Services;
```

Services are immediately available:

```csharp
var players = PlayerService.GetOnlinePlayers();
MessageService.SendSystemMessage("Server event starting!");
```

---

## Service Dependencies

Most services depend on:
- **GameSystems** — Core ECS system access
- **EntityManager** — Entity operations
- **ServerGameManager** — Game state management

Services handle initialization checks internally and will log warnings if called before systems are ready.

---

## Best Practices

**Use appropriate service for the task** — Don't use EntityLookupService for player queries when PlayerService exists.

**Check return values** — Services return success indicators; validate before proceeding.

**Batch operations when possible** — Use bulk methods (e.g., `MessageService.SendSystemMessage`) instead of loops.

**Cache PlayerData references** — PlayerService maintains cached player data; reuse references instead of repeated lookups.

**Prefer service methods over direct ECS** — Services handle edge cases and validation that raw ECS operations don't.

---

## Documentation Standards

Each service documentation includes:
- Complete method signatures with parameters and returns
- Practical usage examples
- Best practices and performance notes
- Common pitfalls and solutions
- Related service links

---

## Additional Resources

- [Systems Documentation](../Systems/) — Core game systems access
- [Extensions](../Extensions.md) — ECS, Localization, and PlayerData extensions
- [Utils](../Utils/) — Logger, MathUtility, RichTextFormatter, Symbols
- [Events](../Events/) — Event system documentation
