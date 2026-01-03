# EventManager

The `EventManager` provides a comprehensive event management system for handling game events with support for priorities, custom events, and dynamic subscription.

## Overview

EventManager supports multiple event categories:
- **PrefixEvents** - Triggered before core game logic
- **PostfixEvents** - Triggered after core game logic
- **PlayerEvents** - Player-related events (join, leave, character creation, etc.)
- **ServerEvents** - Server-wide events (initialization, save)
- **CommandEvents** - Command execution lifecycle events (internal)
- **Custom Events** - User-defined events with dynamic typing

All events support **priority-based execution** using the `[EventPriority]` attribute, ensuring handlers run in the correct order.

## Quick Start

### Subscribe to a built-in event

```csharp
EventManager.On(PlayerEvents.PlayerJoined, (playerData) => {
  Log.Message($"Player {playerData.Name} joined!");
});
```

### Subscribe with priority

```csharp
[EventPriority(EventPriority.High)]
private void OnPlayerJoin(PlayerData playerData) {
  // This runs before handlers with normal or low priority
  Log.Message($"High priority: {playerData.Name} joined");
}

// Subscribe the method
EventManager.On(PlayerEvents.PlayerJoined, OnPlayerJoin);
```

### Create and use custom events

```csharp
// Emit a custom event
EventManager.Emit("MyMod:OnReward", new RewardData { Gold = 100 });

// Subscribe to the custom event
EventManager.On("MyMod:OnReward", (RewardData data) => {
  Log.Message($"Received {data.Gold} gold!");
});
```

### One-time subscription

```csharp
// Handler is automatically removed after first invocation
EventManager.Once(ServerEvents.OnInitialize, () => {
  Log.Message("Server initialized - this runs once!");
});
```

## Event Types

### PrefixEvents

Triggered **before** core game logic executes. Useful for intercepting or modifying behavior.

```csharp
EventManager.On(PrefixEvents.OnChatMessage, (entities) => {
  // Runs before chat message is processed
  // entities is a NativeArray<Entity>
});
```

**Available PrefixEvents:**
- `OnChatMessage` - Before chat message processing
- `OnDealDamage` - Before damage is dealt
- `OnDeath` - Before entity death
- `OnCastStarted`, `OnPreCastFinished`, `OnPostCastEnded`, `OnCastInterrupted` - Ability casting
- `OnUnitSpawned` - Before unit spawns
- `OnPlayerDowned` - Before player is downed
- `OnWarEvent` - Before war event processing
- `OnReplaceAbilityOnSlot` - Before ability replacement
- `OnInteract`, `OnInteractStop` - Entity interactions
- `OnShapeshift` - Before shapeshift transformation
- `OnWaypointTeleport` - Before waypoint teleport
- `OnDestroyTravelBuff` - Before travel buff destruction
- `OnInventoryChanged`, `OnMoveItem` - Inventory operations

### PostfixEvents

Triggered **after** core game logic executes. Useful for post-processing or monitoring.

```csharp
EventManager.On(PostfixEvents.OnDeath, (entities) => {
  // Runs after entity death is processed
});
```

PostfixEvents mirror all PrefixEvents with the same names.

### PlayerEvents

Player lifecycle and state change events.

```csharp
EventManager.On(PlayerEvents.PlayerJoined, (playerData) => {
  playerData.SendMessage($"Welcome {playerData.Name}!");
});
```

**Available PlayerEvents:**
- `PlayerJoined` - Player joins the server
- `PlayerLeft` - Player leaves the server
- `PlayerKicked` - Player is kicked
- `PlayerBanned` - Player is banned
- `CharacterCreated` - New character created
- `CharacterRenamed` - Character renamed

### ServerEvents

Server-wide lifecycle events.

```csharp
// No parameters
EventManager.On(ServerEvents.OnInitialize, () => {
  Log.Message("Server is ready!");
});

// With save name parameter
EventManager.On(ServerEvents.OnSave, (saveName) => {
  Log.Message($"Saving: {saveName}");
});
```

**Available ServerEvents:**
- `OnInitialize` - Server initialized and ready
- `OnSave` - Server state is being saved (provides save name)

### Custom Events

Create your own events with any name and data type.

```csharp
// Define a custom data type
public class QuestCompletedData {
  public string QuestName;
  public int Reward;
  public PlayerData Player;
}

// Emit the event
var data = new QuestCompletedData {
  QuestName = "Dragon Slayer",
  Reward = 1000,
  Player = playerData
};
EventManager.Emit("MyMod:QuestCompleted", data);

// Subscribe to the event
EventManager.On("MyMod:QuestCompleted", (QuestCompletedData data) => {
  Log.Message($"{data.Player.Name} completed {data.QuestName}!");
  // Award the reward...
});
```

**Custom event naming conventions:**
- Use your mod name as prefix: `"MyMod:EventName"`
- Use descriptive names: `"CombatMod:OnCriticalHit"`
- Avoid conflicts with other mods

## Event Priorities

Control the execution order of event handlers using the `[EventPriority]` attribute.

### Priority Constants

```csharp
EventPriority.First = 999          // Runs first
EventPriority.VeryHigh = 300
EventPriority.High = 200
EventPriority.HigherThanNormal = 100
EventPriority.Normal = 0           // Default
EventPriority.LowerThanNormal = -100
EventPriority.Low = -200
EventPriority.VeryLow = -300
EventPriority.Last = -999          // Runs last
```

### Using Priorities

```csharp
[EventPriority(EventPriority.First)]
private void OnDeathFirst(NativeArray<Entity> entities) {
  Log.Message("1. Runs first");
}

[EventPriority(EventPriority.Normal)]
private void OnDeathNormal(NativeArray<Entity> entities) {
  Log.Message("2. Runs second (default priority)");
}

[EventPriority(EventPriority.Last)]
private void OnDeathLast(NativeArray<Entity> entities) {
  Log.Message("3. Runs last");
}

// Subscribe all handlers
EventManager.On(PrefixEvents.OnDeath, OnDeathFirst);
EventManager.On(PrefixEvents.OnDeath, OnDeathNormal);
EventManager.On(PrefixEvents.OnDeath, OnDeathLast);
```

### Custom Priority Values

You can use any integer value:

```csharp
[EventPriority(500)]  // Very high priority
private void CriticalHandler(PlayerData player) {
  // This runs before EventPriority.VeryHigh (300)
}

[EventPriority(-50)]  // Between Normal and LowerThanNormal
private void CustomPriorityHandler(PlayerData player) {
  // Fine-grained control
}
```

## Subscribing and Unsubscribing

### Subscribe (On)

```csharp
// Lambda
EventManager.On(PlayerEvents.PlayerJoined, (player) => {
  Log.Message($"Welcome {player.Name}");
});

// Method reference
EventManager.On(PlayerEvents.PlayerLeft, OnPlayerLeft);

// Custom event
EventManager.On("MyMod:CustomEvent", (MyData data) => {
  // Handle custom event
});
```

### Unsubscribe (Off)

```csharp
// Save reference to unsubscribe later
Action<PlayerData> handler = (player) => {
  Log.Message($"Player: {player.Name}");
};

EventManager.On(PlayerEvents.PlayerJoined, handler);
// Later...
bool removed = EventManager.Off(PlayerEvents.PlayerJoined, handler);
```

### One-time subscription (Once)

Handler is automatically removed after first invocation:

```csharp
EventManager.Once(ServerEvents.OnInitialize, () => {
  Log.Message("Runs only once on server start");
});

EventManager.Once("MyMod:FirstTimeEvent", (data) => {
  Log.Message("This handler self-destructs after running");
});
```

## Utility Methods

### Get subscriber count

```csharp
int count = EventManager.GetSubscriberCount(PlayerEvents.PlayerJoined);
Log.Message($"PlayerJoined has {count} subscribers");

int customCount = EventManager.GetSubscriberCount("MyMod:CustomEvent");
```

### Get all registered custom events

```csharp
var events = EventManager.GetRegisteredEvents();
foreach (var eventName in events) {
  Log.Message($"Registered: {eventName}");
}
```

### Get event statistics

```csharp
var stats = EventManager.GetEventStatistics();
foreach (var kvp in stats) {
  Log.Message($"{kvp.Key}: {kvp.Value} handlers");
}
```

### Clear specific custom event

```csharp
bool cleared = EventManager.ClearEvent("MyMod:CustomEvent");
```

### Clear all custom events

```csharp
EventManager.ClearAllEvents();
// All custom events and their handlers are removed
```

### Unregister assembly

Remove all event handlers from a specific assembly (useful for mod cleanup):

```csharp
// Unregister all handlers from calling assembly
int removed = EventManager.UnregisterAssembly();
Log.Message($"Removed {removed} handlers");

// Unregister specific assembly
int removed = EventManager.UnregisterAssembly(typeof(MyMod).Assembly);
```

## Best Practices

### 1. Use descriptive event names

```csharp
// Good
EventManager.Emit("CombatMod:OnCriticalHit", critData);
EventManager.Emit("EconomyMod:OnTradeCompleted", tradeData);

// Bad
EventManager.Emit("event1", data);
EventManager.Emit("x", data);
```

### 2. Namespace your custom events

```csharp
// Prefix with your mod name to avoid conflicts
EventManager.On("MyMod:OnSpecialAbility", handler);
EventManager.On("MyMod:OnQuestUpdate", handler);
```

### 3. Clean up on mod unload

```csharp
public void OnDisable() {
  // Unsubscribe specific handlers
  EventManager.Off(PlayerEvents.PlayerJoined, OnPlayerJoin);
  
  // Or unregister all handlers from your assembly
  EventManager.UnregisterAssembly();
}
```

### 4. Use priorities wisely

```csharp
// Validation should run first
[EventPriority(EventPriority.First)]
private void ValidateData(PlayerData player) {
  if (!IsValid(player)) return;
}

// Normal processing
[EventPriority(EventPriority.Normal)]
private void ProcessData(PlayerData player) {
  // Main logic
}

// Cleanup should run last
[EventPriority(EventPriority.Last)]
private void CleanupData(PlayerData player) {
  // Cleanup
}
```

### 5. Handle errors gracefully

```csharp
EventManager.On(PlayerEvents.PlayerJoined, (player) => {
  try {
    // Your logic here
  } catch (Exception ex) {
    Log.Error($"Error in PlayerJoined handler: {ex}");
  }
});
```

### 6. Use Once for initialization

```csharp
EventManager.Once(ServerEvents.OnInitialize, () => {
  // One-time setup
  InitializeDatabase();
  LoadConfiguration();
});
```

## Thread Safety

EventManager is thread-safe for custom events (uses locks). Built-in game events (Prefix, Postfix, Player, Server) are called from the game's main thread.

## Performance Considerations

- EventManager uses compiled expression trees for fast custom event invocation
- Handlers are stored with priorities for efficient sorted execution
- Use `Off` to unsubscribe when handlers are no longer needed
- Avoid heavy processing in high-frequency events (e.g., `OnDealDamage`)

## Examples

See [Examples.md](Examples.md) for comprehensive usage examples and patterns.
