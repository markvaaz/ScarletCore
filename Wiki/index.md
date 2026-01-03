# ScarletCore Documentation

## Overview

**ScarletCore** is a comprehensive modding framework for V Rising that simplifies server-side mod development by providing high-level services, utilities, and systems built on top of Unity's Entity Component System (ECS). It abstracts complex game mechanics into clean, easy-to-use APIs, allowing developers to focus on creating engaging gameplay features rather than wrestling with low-level implementation details.

**Version:** 1.5.x  
**Game:** V Rising  
**Framework:** BepInEx + Bloodstone  
**Language:** C# (.NET 6.0)

---

## Key Features

- **Service Layer** — 14+ specialized services for player management, communication, combat, inventory, and more
- **ECS Extensions** — Fluent API for entity operations with 40+ extension methods
- **Action Scheduling** — Frame and time-based action scheduling with full lifecycle control
- **Event System** — Comprehensive event management with priority ordering
- **Localization** — Multi-language support with player and server language contexts
- **Command System** — Attribute-based command routing with auto-help generation
- **Utilities** — Logging, mathematics, text formatting, and visual enhancements
- **Type Safety** — Strongly-typed APIs with helpful error messages
- **Performance** — Optimized with caching, batching, and efficient ECS patterns

---

## Documentation Structure

### [Services](Services/)
High-level service APIs for common modding tasks. Services are static classes that abstract complex game systems into simple method calls.

**Categories:**
- **Player Management** — PlayerService, PlayerData
- **Communication** — MessageService
- **Authorization** — AdminService, RoleService, KickBanService
- **Entity & World** — EntityLookupService, SpawnerService, TeleportService
- **Combat & Stats** — BuffService, StatModifierService, AbilityService
- **Inventory** — InventoryService
- **Social** — ClanService
- **Exploration** — RevealMapService

[Browse Services Documentation →](Services/)

---

### [Systems](Systems/)
Core infrastructure systems for timing, scheduling, and game system access.

**Available Systems:**
- **GameSystems** — Central access to Unity ECS and V Rising game systems
- **ActionScheduler** — Frame and time-based action scheduling with sequences

[Browse Systems Documentation →](Systems/)

---

### [Utils](Utils/)
Utility classes for logging, mathematics, text formatting, and visual enhancement.

**Available Utilities:**
- **Logger** — Flexible logging with debug mode and specialized formatters
- **MathUtility** — Comprehensive math operations for game development
- **RichTextFormatter** — Rich text markup with markdown-like syntax
- **Symbols** — Unicode symbol constants for visual enhancement

[Browse Utils Documentation →](Utils/)

---

### [Extensions](Extensions.md)
Extension methods for cleaner, more readable code across ECS, localization, and player data operations.

**Extension Categories:**
- **ECSExtensions** — 40+ methods for entity and component operations
- **LocalizationExtensions** — String localization with language contexts
- **PlayerDataExtensions** — Retrieve PlayerData from various types

[View Extensions Documentation →](Extensions.md)

---

### [Commanding](Commanding/)
Attribute-based command system with automatic routing, help generation, and type conversion.

**Features:**
- **Command Attributes** — Declarative command definition
- **Auto-Help** — Generated help text from attributes
- **Type Converters** — Automatic parameter parsing
- **Context Access** — Rich command context with player, arguments, and reply helpers

[Browse Commanding Documentation →](Commanding/)

---

### [Events](Events/)
Comprehensive event management system with priority ordering and flexible handler registration.

**Available Events:**
- **Server Events** — OnInitialize, OnSave
- **Player Events** — Connect, disconnect, death, level up
- **Combat Events** — Damage, heal, buff application
- **Event Priorities** — Control execution order with priority attributes

[Browse Events Documentation →](Events/)

---

### [Localization](Localization/)
Multi-language translation system with automatic fallback and player-specific language support.

**Features:**
- **Language Support** — English, Portuguese, Spanish, French, German, Italian, and more
- **Player Languages** — Per-player language preferences
- **Translation Files** — JSON-based translation management
- **Composite Keys** — Mod-specific translation namespacing
- **Prefab Names** — Localized game item/entity names

[Browse Localization Documentation →](Localization/)

---

### [Data](Data/)
Data management systems including database abstraction and configuration management.

**Available Classes:**
- **Database** — Generic JSON database with type safety
- **JsonDatabase** — JSON-based persistent storage
- **SharedDatabase** — Cross-mod data sharing
- **Settings** — Configuration management

[Browse Data Documentation →](Data/)

---

## Quick Start

### Installation

1. Install **BepInEx IL2CPP** for V Rising
2. Install **Bloodstone** mod dependency
3. Download **ScarletCore** and place in `BepInEx/plugins/`
4. Start server to generate config files

### Basic Usage

```csharp
using ScarletCore;
using ScarletCore.Services;
using ScarletCore.Utils;

// Get online players
var players = PlayerService.GetOnlinePlayers();

// Send messages
MessageService.SendSystemMessage("Server event starting!");

// Query entities
var enemies = EntityLookupService.QueryAll<Enemy>();

// Log information
Log.Info("Mod loaded successfully!");
```

### Creating Your First Mod

1. Reference `ScarletCore.dll` in your project
2. Create a BepInEx plugin class
3. Use services and utilities as needed
4. Register events and commands
5. Build and deploy to server

---

## Architecture Overview

### Layer Structure

```
┌─────────────────────────────────────┐
│   Your Mod / Gameplay Features     │
├─────────────────────────────────────┤
│   Services Layer (High-Level API)  │
├─────────────────────────────────────┤
│   Systems & Utils (Infrastructure) │
├─────────────────────────────────────┤
│   Extensions (ECS Helpers)         │
├─────────────────────────────────────┤
│   Unity ECS / V Rising Game Logic  │
└─────────────────────────────────────┘
```

### Design Principles

- **Abstraction** — Hide complex ECS operations behind simple APIs
- **Type Safety** — Strongly-typed interfaces with compile-time checking
- **Performance** — Optimized for server-side execution with caching and batching
- **Consistency** — Uniform naming conventions and patterns across all APIs
- **Extensibility** — Easy to extend with custom services and utilities
- **Documentation** — Comprehensive docs with examples and best practices

---

## Common Patterns

### Service-First Development
Use services for high-level operations before dropping to raw ECS. Services handle edge cases and validation.

### Extension Methods for Clean Code
Use ECS extensions (`entity.Read<T>()`, `entity.With()`) for readable entity manipulation.

### Event-Driven Architecture
Register event handlers for player actions, combat events, and system events instead of polling.

### Scheduled Actions
Use ActionScheduler for delayed and repeating operations instead of manual update loops.

### Localized Messages
Use localization extensions for player-facing text to support multiple languages.

---

## Best Practices

**Check Entity Existence** — Always validate entities exist before operations, especially in delayed callbacks.

**Use Appropriate Services** — Don't use EntityLookupService for player queries when PlayerService exists.

**Cache Frequently Used Data** — PlayerService and EntityLookupService cache data; reuse references.

**Dispose NativeCollections** — Always dispose NativeArray and NativeList to prevent memory leaks.

**Log Appropriately** — Use correct log levels (Debug, Info, Warning, Error) for different message types.

**Handle Null Returns** — Many service methods return nullable types; always check before use.

**Batch Operations** — Use bulk methods instead of loops when available (e.g., broadcast messages).

**Test Edge Cases** — Test with offline players, destroyed entities, and invalid inputs.

---

## Performance Tips

- Use 2D math variants for ground-based gameplay
- Cache entity queries and PlayerData references
- Minimize `OncePerFrame` actions
- Prefer time-based over frame-based scheduling
- Batch entity operations when possible
- Use query caching in EntityLookupService
- Cache formatted and localized strings

---

## Troubleshooting

### Common Issues

**"Entity doesn't exist"** — Check `entity.Exists()` before operations, especially in delayed actions.

**"Component not found"** — Use `TryGetComponent()` for optional components or check with `Has<T>()`.

**"Null PlayerData"** — Player might be offline or not found; always check null before use.

**"Translation key not found"** — Ensure translation files are loaded and key exists in language file.

**"Action not executing"** — Verify ActionScheduler is running and action wasn't cancelled.

---

## Community & Support

- **GitHub Issues** — Report bugs and request features
- **Documentation** — Comprehensive API reference and examples
- **Examples** — Sample mods demonstrating common patterns
- **Discord** — Community support and discussions (if available)

---

## Contributing

Contributions are welcome! Areas of interest:

- Additional services for uncovered game systems
- Performance optimizations
- Additional language translations
- Documentation improvements
- Bug fixes and edge case handling

---

## Version History

### 1.5.x (Current)
- Comprehensive service layer with 14+ services
- Action scheduling with sequences
- Multi-language localization
- Command system with auto-help
- Event management with priorities
- Extensive documentation

### 1.4.x
- Initial service implementations
- Basic ECS extensions
- Core systems foundation

---

## License

Check the project repository for licensing information.

---

## Credits

**ScarletCore** is built on top of:
- **BepInEx** — Plugin framework
- **Bloodstone** — V Rising modding utilities
- **Unity ECS** — Entity Component System
- **V Rising** — Game by Stunlock Studios

Special thanks to the V Rising modding community for feedback and contributions.

---

## Navigation

- [Services Documentation](Services/)
- [Systems Documentation](Systems/)
- [Utils Documentation](Utils/)
- [Extensions Documentation](Extensions.md)
- [Commanding Documentation](Commanding/)
- [Events Documentation](Events/)
- [Localization Documentation](Localization/)
- [Data Documentation](Data/)
