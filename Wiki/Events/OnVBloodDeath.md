# OnVBloodDeath Event

The `OnVBloodDeath` event in ScarletCore allows you to listen for, inspect, and react specifically to V Blood unit death events in the game. This is useful for custom boss logic, analytics, boss-based triggers, or implementing special effects and mechanics for V Blood deaths in your mod.

---

## Event Signature

```csharp
public static event EventHandler<DeathEventArgs> OnVBloodDeath;
```

- **sender**: The `DeathEventListenerSystem` instance that detected and raised the event.
- **args**: A `DeathEventArgs` object containing all relevant information about the batch of V Blood deaths.

---

## DeathEventArgs Properties

- `List<DeathInfo> Deaths` — List of all V Blood deaths that occurred in the frame
- `int DeathCount` — Number of V Blood deaths in this batch

### DeathInfo Properties
- `Entity Died` — The V Blood entity that died
- `Entity Killer` — The entity that caused the death
- `Entity Source` — The source of the damage/death

---

## Basic Usage

### Listen for all V Blood death events
```csharp
EventManager.OnVBloodDeath += (sender, args) => {
    foreach (var death in args.Deaths) {
        Log.Info($"[VBloodDeath] {death.Died} was killed by {death.Killer} (source: {death.Source})");
    }
};
```

### Count total V Blood deaths in a frame
```csharp
EventManager.OnVBloodDeath += (sender, args) => {
    Log.Info($"V Blood deaths this frame: {args.DeathCount}");
};
```

---

## Using a Method Handler

You can use a named method for your handler. The method must match the event signature:

```csharp
void OnVBloodDeathHandler(object sender, DeathEventArgs args) {
    // sender is the DeathEventListenerSystem instance
    foreach (var death in args.Deaths) {
        // Custom logic here
    }
}

EventManager.OnVBloodDeath += OnVBloodDeathHandler;
EventManager.OnVBloodDeath -= OnVBloodDeathHandler; // Unsubscribe when done
```

> **Note:** When using a method handler, the first parameter must be of type `object` (the sender, which is the DeathEventListenerSystem instance), and the second parameter must be of type `DeathEventArgs`.

---

## Advanced: Accessing the Sender (DeathEventListenerSystem)

The `sender` parameter is the `DeathEventListenerSystem` instance that detected the V Blood death. You can cast it if you need to access system-specific methods or properties:

```csharp
EventManager.OnVBloodDeath += (sender, args) => {
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
EventManager.OnVBloodDeath -= OnVBloodDeathHandler;
```

---

## When to Use

- Custom V Blood/boss death logic or effects
- Analytics and logging for boss deaths
- Boss death-based triggers or achievements
- Special effects for boss encounters

> **Tip:** For more advanced death features, combine this event with other ScarletCore systems and services.
