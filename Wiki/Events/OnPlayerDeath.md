# OnPlayerDeath Event

The `OnPlayerDeath` event in ScarletCore allows you to listen for, inspect, and react specifically to player death events in the game. This is useful for custom player death logic, analytics, player-based triggers, or implementing special effects and mechanics for player deaths in your mod.

---

## Event Signature

```csharp
public static event EventHandler<DeathEventArgs> OnPlayerDeath;
```

- **sender**: The `DeathEventListenerSystem` instance that detected and raised the event.
- **args**: A `DeathEventArgs` object containing all relevant information about the batch of player deaths.

---

## DeathEventArgs Properties

- `List<DeathInfo> Deaths` — List of all player deaths that occurred in the frame
- `int DeathCount` — Number of player deaths in this batch

### DeathInfo Properties
- `Entity Died` — The player entity that died
- `Entity Killer` — The entity that caused the death
- `Entity Source` — The source of the damage/death

---

## Basic Usage

### Listen for all player death events
```csharp
EventManager.OnPlayerDeath += (sender, args) => {
    foreach (var death in args.Deaths) {
        Log.Info($"[PlayerDeath] {death.Died} was killed by {death.Killer} (source: {death.Source})");
    }
};
```

### Count total player deaths in a frame
```csharp
EventManager.OnPlayerDeath += (sender, args) => {
    Log.Info($"Player deaths this frame: {args.DeathCount}");
};
```

---

## Using a Method Handler

You can use a named method for your handler. The method must match the event signature:

```csharp
void OnPlayerDeathHandler(object sender, DeathEventArgs args) {
    // sender is the DeathEventListenerSystem instance
    foreach (var death in args.Deaths) {
        // Custom logic here
    }
}

EventManager.OnPlayerDeath += OnPlayerDeathHandler;
EventManager.OnPlayerDeath -= OnPlayerDeathHandler; // Unsubscribe when done
```

> **Note:** When using a method handler, the first parameter must be of type `object` (the sender, which is the DeathEventListenerSystem instance), and the second parameter must be of type `DeathEventArgs`.

---

## Advanced: Accessing the Sender (DeathEventListenerSystem)

The `sender` parameter is the `DeathEventListenerSystem` instance that detected the player death. You can cast it if you need to access system-specific methods or properties:

```csharp
EventManager.OnPlayerDeath += (sender, args) => {
    var deathSystem = sender as DeathEventListenerSystem;
    // Use deathSystem if needed
};
```

---

## Multiple Handlers

You can register multiple handlers. All will be called in order.

---

## Unsubscribing

Always unsubscribe your handler when it is no longer needed to avoid memory leaks:

```csharp
EventManager.OnPlayerDeath -= OnPlayerDeathHandler;
```

---

## When to Use

- Custom player death logic or effects
- Analytics and logging for player deaths
- Player death-based triggers or achievements
- Special effects for PvP or PvE deaths

> **Tip:** For more advanced death features, combine this event with other ScarletCore systems and services.
