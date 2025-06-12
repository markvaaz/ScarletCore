# OnAnyDeath Event

The `OnAnyDeath` event in ScarletCore allows you to listen for, inspect, and react to all death events that occur in the game. This is useful for custom death logic, analytics, death-based triggers, or implementing special effects and mechanics in your mod.

---

## Event Signature

```csharp
public static event EventHandler<DeathEventArgs> OnAnyDeath;
```

- **sender**: The `DeathEventListenerSystem` instance that detected and raised the event.
- **args**: A `DeathEventArgs` object containing all relevant information about the batch of deaths.

---

## DeathEventArgs Properties

- `List<DeathInfo> Deaths` — List of all deaths that occurred in the frame
- `int DeathCount` — Number of deaths in this batch

### DeathInfo Properties
- `Entity Died` — The entity that died
- `Entity Killer` — The entity that caused the death
- `Entity Source` — The source of the damage/death

---

## Basic Usage

### Listen for all death events
```csharp
EventManager.OnAnyDeath += (sender, args) => {
    foreach (var death in args.Deaths) {
        Log.Info($"{death.Died} was killed by {death.Killer} (source: {death.Source})");
    }
};
```

### Count total deaths in a frame
```csharp
EventManager.OnAnyDeath += (sender, args) => {
    Log.Info($"Deaths this frame: {args.DeathCount}");
};
```

---

## Using a Method Handler

You can use a named method for your handler. The method must match the event signature:

```csharp
void OnAnyDeathHandler(object sender, DeathEventArgs args) {
    // sender is the DeathEventListenerSystem instance
    foreach (var death in args.Deaths) {
        // Custom logic here
    }
}

EventManager.OnAnyDeath += OnAnyDeathHandler;
EventManager.OnAnyDeath -= OnAnyDeathHandler; // Unsubscribe when done
```

> **Note:** When using a method handler, the first parameter must be of type `object` (the sender, which is the DeathEventListenerSystem instance), and the second parameter must be of type `DeathEventArgs`.

---

## Advanced: Accessing the Sender (DeathEventListenerSystem)

The `sender` parameter is the `DeathEventListenerSystem` instance that detected the death. You can cast it if you need to access system-specific methods or properties:

```csharp
EventManager.OnAnyDeath += (sender, args) => {
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
EventManager.OnAnyDeath -= OnAnyDeathHandler;
```

---

## When to Use

- Custom death logic or effects
- Analytics and logging
- Death-based triggers or achievements
- Modifying or blocking death (if supported by future versions)

> **Tip:** For more advanced death features, combine this event with other ScarletCore systems and services.
