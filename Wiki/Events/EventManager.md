# EventManager

`EventManager` is a static, centralized event system for ScarletCore that allows mods and systems to subscribe to, listen for, and react to a wide variety of game events. It provides a unified interface for handling player connections, chat, deaths, damage, unit spawns, war events, and more, with safe invocation and subscriber management.

## Overview

```csharp
using ScarletCore.Events;

// Subscribe to a player connection event
EventManager.OnUserConnected += (sender, args) => {
    Log.Info($"Player connected: {args.PlayerData.CharacterName}");
};

// Subscribe to chat messages
EventManager.OnChatMessage += (sender, args) => {
    Log.Info($"Chat: {args.PlayerData.CharacterName}: {args.Message}");
};

// Unsubscribe when no longer needed
EventManager.OnUserConnected -= MyHandler;
```

## Features

- Subscribe to a wide range of game events (chat, connection, death, spawn, war, etc.)
- Safe event invocation with error handling
- Access to event arguments for context and control
- Query subscriber counts for each event
- Clear all subscribers for full reset (use with caution)

## Event Categories & List

### System Events
- `OnInitialize` — ScarletCore initialization

### Chat Events
- `OnChatMessage` — Chat message sent

### User Connection Events
- `OnUserConnected` — User connected
- `OnUserDisconnected` — User disconnected
- `OnUserKicked` — User kicked
- `OnUserBanned` — User banned

### Death Events
- `OnAnyDeath` — Any entity death
- `OnOtherDeath` — Unfiltered deaths
- `OnPlayerDeath` — Player deaths
- `OnVBloodDeath` — V Blood unit deaths
- `OnServantDeath` — Servant deaths

### Unit Spawn Events
- `OnUnitSpawn` — Units spawned

### Damage Events
- `OnDealDamage` — Damage dealt

### War Events
- `OnWarEventsStarted` — War events started
- `OnWarEventsEnded` — War events ended

### Player Downed Events
- `OnPlayerDowned` — Player/vampire downed

## Event Subscription Example

```csharp
// Listen for player deaths
EventManager.OnPlayerDeath += (sender, args) => {
    foreach (var death in args.Deaths) {
        Log.Info($"Player died: {death.PlayerName}");
    }
};

// Listen for unit spawns
EventManager.OnUnitSpawn += (sender, args) => {
    Log.Info($"Units spawned: {args.UnitEntities.Count}");
};
```

## Event Invocation (For Mod Developers)

Events are invoked internally by ScarletCore patches and systems. Each event has a corresponding `Invoke...` method (e.g., `InvokeUserConnected`, `InvokeChatMessage`) that safely triggers the event and passes relevant arguments. These methods are not intended to be called directly by mods.

## Subscriber Management

You can query the number of subscribers for each event:

```csharp
var count = EventManager.UserConnectedSubscriberCount;
```

To clear all subscribers (for debugging or full reset):

```csharp
EventManager.ClearAllSubscribers();
```

> **Warning:** Clearing all subscribers will remove all event listeners for all mods and systems.

## Event Argument Types

Each event provides a strongly-typed argument class with relevant context, such as:
- `PlayerData` for player events
- `ChatMessageEventArgs` for chat
- `DeathEventArgs` for deaths
- `DamageEventArgs` for damage
- `UnitSpawnEventArgs` for spawns
- ...and more

Refer to the `Events/` folder for details on each argument type.

## Usage Examples

### Listen for chat messages and cancel them
```csharp
EventManager.OnChatMessage += (sender, args) => {
    if (args.Message.Contains("forbidden")) {
        args.Cancel = true;
        Log.Info("Blocked forbidden chat message.");
    }
};
```

### Listen for war events
```csharp
EventManager.OnWarEventsStarted += (sender, args) => {
    Log.Info($"War started: {args.WarEvents.Count} events");
};
EventManager.OnWarEventsEnded += (sender, args) => {
    Log.Info("All war events ended.");
};
```

### Using method handlers (with correct parameter types)

You can also use named methods instead of lambdas for event handlers. When doing so, your method signature must match the event's expected parameters.

> **Note:** The `sender` parameter in all event handlers is the instance of the ScarletCore patch or system that detected and raised the event (for example, `ChatMessageSystem`, `ServerBootstrapSystem`, `DeathEventListenerSystem`, etc). This allows you to access additional context or methods from the system that triggered the event if needed.

#### Example: Method handler for OnUserConnected
```csharp
void OnUserConnectedHandler(object sender, UserConnectedEventArgs args) {
    // sender is the ServerBootstrapSystem instance that detected the connection
    Log.Info($"User connected: {args.PlayerData.CharacterName}");
}

// Subscribe
EventManager.OnUserConnected += OnUserConnectedHandler;
// Unsubscribe
EventManager.OnUserConnected -= OnUserConnectedHandler;
```

#### Example: Method handler for OnChatMessage
```csharp
void OnChatMessageHandler(object sender, ChatMessageEventArgs args) {
    // sender is the ChatMessageSystem instance that detected the chat
    if (args.Message.Contains("forbidden")) {
        args.Cancel = true;
        Log.Info("Blocked forbidden chat message.");
    }
}

EventManager.OnChatMessage += OnChatMessageHandler;
```

#### Example: Method handler for OnPlayerDeath
```csharp
void OnPlayerDeathHandler(object sender, DeathEventArgs args) {
    // sender is the DeathEventListenerSystem instance that detected the death
    foreach (var death in args.Deaths) {
        Log.Info($"Player died: {death.PlayerName}");
    }
}

EventManager.OnPlayerDeath += OnPlayerDeathHandler;
```

## Important Notes

- **Events are invoked on the main thread** and should not block for long periods.
- **Always unsubscribe** when your handler is no longer needed to avoid memory leaks.
- **Do not call `Invoke...` methods directly** unless you are extending ScarletCore internals.
- **Use event arguments** to access context and, where supported, to cancel or modify behavior.

## When to Use EventManager

Use `EventManager` when you need to:
- React to game events in a decoupled, modular way
- Build mods that listen for player actions, deaths, chat, or world changes
- Integrate with other ScarletCore systems via events

> **Tip:** For custom or mod-specific events, consider using `CustomEventManager`.
