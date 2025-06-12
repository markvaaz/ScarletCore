# OnServantDeath Event

The `OnServantDeath` event in ScarletCore allows you to listen for, inspect, and react specifically to servant death events in the game. This is useful for custom servant logic, analytics, servant-based triggers, or implementing special effects and mechanics for servant deaths in your mod.

---

## Event Signature

```csharp
public static event EventHandler<DeathEventArgs> OnServantDeath;
```

- **sender**: The `DeathEventListenerSystem` instance that detected and raised the event.
- **args**: A `DeathEventArgs` object containing all relevant information about the batch of servant deaths.

---

## DeathEventArgs Properties

- `List<DeathInfo> Deaths` — List of all servant deaths that occurred in the frame
- `int DeathCount` — Number of servant deaths in this batch

### DeathInfo Properties
- `Entity Died` — The servant entity that died
- `Entity Killer` — The entity that caused the death
- `Entity Source` — The source of the damage/death

---

## Basic Usage

### Listen for all servant death events
```csharp
EventManager.OnServantDeath += (sender, args) => {
    foreach (var death in args.Deaths) {
        Log.Info($"[ServantDeath] {death.Died} was killed by {death.Killer} (source: {death.Source})");
    }
};
```

### Count total servant deaths in a frame
```csharp
EventManager.OnServantDeath += (sender, args) => {
    Log.Info($"Servant deaths this frame: {args.DeathCount}");
};
```

---

## Using a Method Handler

You can use a named method for your handler. The method must match the event signature:

```csharp
void OnServantDeathHandler(object sender, DeathEventArgs args) {
    // sender is the DeathEventListenerSystem instance
    foreach (var death in args.Deaths) {
        // Custom logic here
    }
}

EventManager.OnServantDeath += OnServantDeathHandler;
EventManager.OnServantDeath -= OnServantDeathHandler; // Unsubscribe when done
```

> **Note:** When using a method handler, the first parameter must be of type `object` (the sender, which is the DeathEventListenerSystem instance), and the second parameter must be of type `DeathEventArgs`.

---

## Advanced: Accessing the Sender (DeathEventListenerSystem)

The `sender` parameter is the `DeathEventListenerSystem` instance that detected the servant death. You can cast it if you need to access system-specific methods or properties:

```csharp
EventManager.OnServantDeath += (sender, args) => {
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
EventManager.OnServantDeath -= OnServantDeathHandler;
```

---

## When to Use

- Custom servant death logic or effects
- Analytics and logging for servant deaths
- Servant death-based triggers or achievements
- Special effects for servant encounters

> **Tip:** For more advanced death features, combine this event with other ScarletCore systems and services.
