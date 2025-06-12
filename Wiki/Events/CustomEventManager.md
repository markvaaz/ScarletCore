# CustomEventManager

`CustomEventManager` is a static, centralized system for registering, emitting, and managing custom events in ScarletCore. It allows mods and systems to define their own event names, subscribe with callbacks (typed or untyped), and trigger events with arbitrary data. This enables decoupled communication between mods and systems beyond the built-in game events handled by `EventManager`.

## Overview

```csharp
using ScarletCore.Events;

// Subscribe to a custom event (untyped)
CustomEventManager.On("MyCustomEvent", data => {
    Log.Info($"MyCustomEvent triggered with data: {data}");
});

// Subscribe to a custom event (typed)
CustomEventManager.On("ScoreChanged", (int score) => {
    Log.Info($"Score changed: {score}");
});

// Emit a custom event
CustomEventManager.Emit("MyCustomEvent", "Hello World");
CustomEventManager.Emit("ScoreChanged", 42);

// Unsubscribe
CustomEventManager.Off("MyCustomEvent", myHandler);
```

## Features

- Register and emit any custom event by name
- Subscribe with untyped (`Action<object>`) or typed (`Action<T>`) callbacks
- Safe callback invocation with error handling
- Query subscriber counts and registered events
- Remove individual handlers or clear all subscribers
- Thread-safe management

## API Reference

### Registering Handlers
- `On(string eventName, Action<object> callback)` — Register an untyped callback
- `On<T>(string eventName, Action<T> callback)` — Register a typed callback

### Emitting Events
- `Emit(string eventName, object data = null)` — Trigger an event and notify all subscribers

### Unsubscribing
- `Off(string eventName, Action<object> callback)` — Remove a specific callback from an event

### Subscriber Management
- `GetSubscriberCount(string eventName)` — Get the number of subscribers for an event
- `GetRegisteredEvents()` — List all registered event names
- `ClearEvent(string eventName)` — Remove all subscribers for a specific event
- `ClearAllEvents()` — Remove all custom events and subscribers
- `GetEventStatistics()` — Get a dictionary of event names and subscriber counts

## Usage Examples

### Listen for a custom event (untyped)
```csharp
CustomEventManager.On("PlayerLeveledUp", data => {
    Log.Info($"Player leveled up! Data: {data}");
});
```

### Listen for a custom event (typed)
```csharp
CustomEventManager.On("PlayerJoined", (PlayerData player) => {
    Log.Info($"Player joined: {player.CharacterName}");
});
```

### Emit a custom event
```csharp
CustomEventManager.Emit("PlayerLeveledUp", new { PlayerId = 123, Level = 10 });
```

### Unsubscribe a handler
```csharp
void MyHandler(object data) {
    // ...
}
CustomEventManager.On("TestEvent", MyHandler);
CustomEventManager.Off("TestEvent", MyHandler);
```

### Get subscriber count and registered events
```csharp
int count = CustomEventManager.GetSubscriberCount("PlayerLeveledUp");
var events = CustomEventManager.GetRegisteredEvents();
```

### Clear all subscribers for an event or all events
```csharp
CustomEventManager.ClearEvent("PlayerLeveledUp");
CustomEventManager.ClearAllEvents();
```

## Important Notes

- **CustomEventManager and EventManager are completely separate systems.** Custom events registered with `CustomEventManager` do not receive or listen to any of the built-in events (such as `OnPlayerDeath`, `OnChatMessage`, etc.) triggered by `EventManager`. Likewise, built-in events do not trigger custom event handlers. If you want to listen to core game events, use `EventManager`. Use `CustomEventManager` only for your own custom, mod-defined events.
- **Custom events are invoked on the main thread**; avoid blocking operations in handlers.
- **Always unsubscribe** when your handler is no longer needed to prevent memory leaks.
- **Typed handlers** will log a warning if the emitted data does not match the expected type.
- **Event names are case-sensitive** and must not be null or empty.
- **Use for mod-to-mod or system-to-system communication** where built-in events are not sufficient.

## When to Use CustomEventManager

Use `CustomEventManager` when you need to:
- Create and listen for mod-specific or system-specific events
- Decouple logic between mods or systems
- Broadcast information or actions that are not covered by built-in game events

> **Tip:** For core game events (chat, connection, death, etc.), use `EventManager`. For your own custom triggers, use `CustomEventManager`.