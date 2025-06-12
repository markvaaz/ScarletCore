# ScarletCore Wiki

Welcome to the official ScarletCore Wiki!

ScarletCore is a modular and extensible framework for creating mods in V Rising. It provides a foundation for developers, making it easy to integrate systems, services, events, and utilities in an organized way. While ScarletCore was originally designed to support the ScarletMods ecosystem, it can be used by any developer creating V Rising mods.

## Main Features

- **Modular Architecture:** Organize your mod into independent systems, services, and events.
- **Event Management:** Create, listen to, and handle custom or game events (chat, death, damage, connections, and more).
- **Ready-to-use Services:** Includes PlayerService, BuffService, TeleportService, AdminService, RevealMapService, InventoryService, and others.
- **Data Persistence:** Structures for saving and loading data safely and efficiently (custom player data, settings management).
- **Scheduling Systems:** Run actions asynchronously or on a schedule.
- **Various Utilities:** Logging, text formatting, math utilities, and more.

## Requirements

- **[BepInEx](https://wiki.vrisingmods.com/user/bepinex_install.html)** (modding framework for V Rising)

## Installation (Manual)

1. Download the latest release of ScarletCore from Thunderstore.
2. Extract the contents into your `BepInEx/plugins` folder:
   ```
   <V Rising Server Directory>/BepInEx/plugins/
   BepInEx/plugins/ScarletCore.dll
   ```
3. Start or restart your server.

## Documentation Index

| Section   | Description | Examples |
|-----------|-------------|----------|
| [Data](Wiki/Data/Database.md)      | Data structures and persistence | [PlayerData](Wiki/Data/PlayerData.md), [Settings](Wiki/Data/Settings.md) |
| [Events](Wiki/Events/EventManager.md)    | Custom event system and game event integration | [OnChatMessage](Wiki/Events/OnChatMessage.md), [OnPlayerDeath](Wiki/Events/OnPlayerDeath.md) |
| [Services](Wiki/Services/PlayerService.md)  | Utility services for player, inventory, admin, etc. | [AdminService](Wiki/Services/AdminService.md), [InventoryService](Wiki/Services/InventoryService.md) |
| [Systems](Wiki/Systems/GameSystems.md)   | Game logic systems and action scheduling | [ActionScheduler](Wiki/Systems/ActionScheduler.md), [CoroutineHandler](Wiki/Systems/CoroutineHandler.md) |
| [Utils](Wiki/Utils/Logger.md)      | Various utilities like logging and text formatting | [Logger](Wiki/Utils/Logger.md), [RichTextFormatter](Wiki/Utils/RichTextFormatter.md) |

## Getting Started

1. Install ScarletCore in your V Rising mod project.
2. Explore the sections above to understand each part of the framework.
3. Check practical examples on each page to speed up your development.
4. Use the utilities and services to avoid rework and ensure best practices.

## Quick Examples

```csharp
// Listening to a chat message event
EventManager.OnChatMessage += (sender, args) => {
    Logger.Log($"{args.UserName}: {args.Message}");
};

// Saving custom player data
PlayerData data = PlayerService.GetPlayerData(player);
data.CustomValue = 42;
data.Save();
```

## Support & Contribution

- Questions? Check each section's page or open an issue in the repository.
- Want to contribute? Follow the Wiki's standards and submit your PR.
- Suggestions and feedback are welcome.

---

> ScarletCore is not currently community-maintained, but contributions are welcome if you want to help maintain the project.
